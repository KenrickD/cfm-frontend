using cfm_frontend.Constants;
using cfm_frontend.Controllers;
using cfm_frontend.DTOs.Login;
using cfm_frontend.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace cfm_frontend.Handlers
{
    public class AuthTokenHandler : DelegatingHandler
    {
        private static readonly SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);
        private static readonly HttpClient _refreshClient = new HttpClient();

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthTokenHandler> _logger;

        public AuthTokenHandler(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<AuthTokenHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            var context = _httpContextAccessor.HttpContext;
            if (context == null) return await base.SendAsync(request, cancellationToken);

            // Proactive token refresh if near expiry (< 5 minutes remaining)
            var accessToken = await context.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken) && JwtTokenHelper.IsTokenNearExpiry(accessToken, 5))
            {
                _logger.LogInformation("Token near expiry detected, initiating proactive refresh at {Time}", DateTime.UtcNow);
                var refreshToken = await context.GetTokenAsync("refresh_token");

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await _refreshSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        // Re-check after acquiring lock (another thread may have refreshed it)
                        var currentToken = await context.GetTokenAsync("access_token");
                        if (JwtTokenHelper.IsTokenNearExpiry(currentToken, 5))
                        {
                            _logger.LogInformation("Executing proactive token refresh at {Time}", DateTime.UtcNow);
                            var refreshSuccess = await RefreshTokensAsync(context, currentToken, refreshToken);
                            if (refreshSuccess)
                            {
                                accessToken = await context.GetTokenAsync("access_token");
                                _logger.LogInformation("Proactive token refresh successful at {Time}", DateTime.UtcNow);
                            }
                            else
                            {
                                // CRITICAL: Proactive refresh failed (both tokens expired), force logout immediately
                                _logger.LogWarning("Proactive token refresh failed at {Time}. Forcing user logout.", DateTime.UtcNow);

                                // Clear session data
                                context.Session.Remove("UserSession");
                                context.Session.Remove("UserPrivileges");

                                // Clear authentication cookie
                                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                                // Return 401 to trigger client-side redirect to login
                                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                                {
                                    Content = new StringContent(
                                        "{\"error\":\"Session expired\",\"message\":\"Your session has expired. Please log in again.\"}",
                                        System.Text.Encoding.UTF8,
                                        "application/json")
                                };
                            }
                        }
                        else
                        {
                            // Another thread already refreshed it
                            accessToken = currentToken;
                            _logger.LogInformation("Token was already refreshed by another thread at {Time}", DateTime.UtcNow);
                        }
                    }
                    finally
                    {
                        _refreshSemaphore.Release();
                    }
                }
            }

            // Add bearer token to request header
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            var response = await base.SendAsync(request, cancellationToken);

            //  Handle 401 Unauthorized (Expired Token)
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var refreshToken = await context.GetTokenAsync("refresh_token");

                if (!string.IsNullOrEmpty(refreshToken) && !string.IsNullOrEmpty(accessToken))
                {
                    // Wait for exclusive access to token refresh
                    await _refreshSemaphore.WaitAsync(cancellationToken);

                    try
                    {
                        // RE-CHECK if token still needs refresh (another thread may have refreshed it)
                        var currentToken = await context.GetTokenAsync("access_token");

                        // If token changed while we were waiting, use the new one
                        if (currentToken != accessToken)
                        {
                            _logger.LogInformation("Token was already refreshed by another request at {Time}", DateTime.UtcNow);

                            // Clone the request before retrying (HttpRequestMessage can only be sent once)
                            using var retryRequest = await CloneHttpRequestMessageAsync(request);
                            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
                            response.Dispose();
                            return await base.SendAsync(retryRequest, cancellationToken);
                        }

                        //  Perform Refresh via Backend
                        var refreshSuccess = await RefreshTokensAsync(context, accessToken, refreshToken);

                        if (refreshSuccess)
                        {
                            //  Get the NEW access token (updated in RefreshTokensAsync)
                            var newAccessToken = await context.GetTokenAsync("access_token");

                            // Clone the request before retrying (HttpRequestMessage can only be sent once)
                            using var retryRequest = await CloneHttpRequestMessageAsync(request);
                            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

                            // We must dispose the previous failed response before retrying
                            response.Dispose();
                            return await base.SendAsync(retryRequest, cancellationToken);
                        }
                        else
                        {
                            // Refresh failed (refresh token expired?), force logout
                            _logger.LogWarning("Token refresh failed. Forcing user logout and clearing session.");

                            // Clear session data explicitly
                            context.Session.Remove("UserSession");
                            context.Session.Remove("UserPrivileges");

                            // Clear authentication cookie
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                            // Return custom 401 response with clear message for client-side handling
                            response.Dispose();
                            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                            {
                                Content = new StringContent(
                                    "{\"error\":\"Session expired\",\"message\":\"Your session has expired. Please log in again.\"}",
                                    System.Text.Encoding.UTF8,
                                    "application/json")
                            };
                        }
                    }
                    finally
                    {
                        _refreshSemaphore.Release();
                    }
                }
            }

            return response;
        }

        private async Task<bool> RefreshTokensAsync(HttpContext context, string expiredAccess, string refresh)
        {
            try
            {
                _logger.LogInformation("Starting token refresh at {Time}", DateTime.UtcNow);

                var client = _refreshClient;
                var backendUrl = _configuration["BackendBaseUrl"];

                var url = $"{backendUrl}{ApiEndpoints.Auth.RefreshToken}?refreshToken={Uri.EscapeDataString(refresh)}";
                _logger.LogInformation("Refresh token request: POST {Url}", url);

                var response = await client.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var newTokens = await JsonSerializer.DeserializeAsync<LoginResponse>(
                        stream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (newTokens?.data != null)
                    {
                        var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        var properties = result.Properties;
                        if (properties != null)
                        {
                            properties.UpdateTokenValue("access_token", newTokens.data.Token);
                            properties.UpdateTokenValue("refresh_token", newTokens.data.RefreshToken);

                            await context.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                result.Principal,
                                properties
                            );

                            _logger.LogInformation("Access token refreshed successfully at {Time}", DateTime.UtcNow);
                            return true;
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Token refresh failed with status code {StatusCode} at {Time}. Response: {ErrorContent}",
                        response.StatusCode, DateTime.UtcNow, errorContent);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token at {Time}", DateTime.UtcNow);
            }
            return false;
        }

        private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage original)
        {
            var clone = new HttpRequestMessage(original.Method, original.RequestUri)
            {
                Version = original.Version
            };

            // Copy headers
            foreach (var header in original.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Copy content if present
            if (original.Content != null)
            {
                var ms = new MemoryStream();
                await original.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                // Copy content headers
                if (original.Content.Headers != null)
                {
                    foreach (var header in original.Content.Headers)
                    {
                        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            return clone;
        }
    }
}