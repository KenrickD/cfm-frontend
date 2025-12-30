using cfm_frontend.Constants;
using cfm_frontend.Controllers;
using cfm_frontend.DTOs.Login;
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

            var accessToken = await context.GetTokenAsync("access_token");

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
                    //  Perform Refresh via Backend
                    var refreshSuccess = await RefreshTokensAsync(context, accessToken, refreshToken);

                    if (refreshSuccess)
                    {
                        //  Get the NEW access token (updated in RefreshTokensAsync)
                        var newAccessToken = await context.GetTokenAsync("access_token");

                        // Retry the original request
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

                        // We must dispose the previous failed response before retrying
                        response.Dispose();
                        return await base.SendAsync(request, cancellationToken);
                    }
                    else
                    {
                        // Refresh failed (refresh token expired?), force logout
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
            }

            return response;
        }

        private async Task<bool> RefreshTokensAsync(HttpContext context, string expiredAccess, string refresh)
        {
            try
            {
                var client = new HttpClient();
                var backendUrl = _configuration["BackendBaseUrl"];

                var payload = new { accessToken = expiredAccess, refreshToken = refresh };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Auth.RefreshToken}", content);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var newTokens = await JsonSerializer.DeserializeAsync<LoginResponse>(
                        stream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (newTokens != null)
                    {
                        var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        var properties = result.Properties;
                        if (properties != null)
                        {
                            properties.UpdateTokenValue("access_token", newTokens.Token);
                            properties.UpdateTokenValue("refresh_token", newTokens.RefreshToken);

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
                    _logger.LogWarning("Token refresh failed with status code {StatusCode} at {Time}",
                        response.StatusCode, DateTime.UtcNow);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token at {Time}", DateTime.UtcNow);
            }
            return false;
        }
    }
}