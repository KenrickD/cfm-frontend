using cfm_frontend.Controllers;
using cfm_frontend.DTOs.Login;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace cfm_frontend.Handlers
{
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public AuthTokenHandler(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            var accessToken = context?.Session.GetString("AccessToken");

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            var response = await base.SendAsync(request, cancellationToken);

            //Handle 401 Unauthorized (Expired Token)
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Retrieve Refresh Token from Cookie
                var refreshToken = context?.Request.Cookies["RefreshToken"];

                if (!string.IsNullOrEmpty(refreshToken) && !string.IsNullOrEmpty(accessToken))
                {
                    // Call Backend to Refresh Token
                    var newClient = new HttpClient();
                    var backendUrl = _configuration["BackendBaseUrl"];

                    var refreshPayload = new
                    {
                        accessToken = accessToken,
                        refreshToken = refreshToken
                    };

                    var content = new StringContent(JsonSerializer.Serialize(refreshPayload), Encoding.UTF8, "application/json");
                    var refreshResponse = await newClient.PostAsync($"{backendUrl}/api/auth/refresh-token", content);

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var stream = await refreshResponse.Content.ReadAsStreamAsync();
                        var newTokens = await JsonSerializer.DeserializeAsync<LoginResponse>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (newTokens != null)
                        {
                            //Update Session and Cookie with new values
                            context.Session.SetString("AccessToken", newTokens.AccessToken);

                            // Update Refresh Token Cookie
                            context.Response.Cookies.Append("RefreshToken", newTokens.RefreshToken, new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true, 
                                SameSite = SameSiteMode.Strict,
                                Expires = DateTime.UtcNow.AddDays(7) // Adjust based on your refresh token life and/or set it to a global variable
                            });

                            //Retry the original request with the new token
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);

                            // Re-issue the request. 
                            // Note: HttpClient request messages can typically only be sent once. 
                            // We may need to clone it or ensure we aren't reading the stream in a way that prevents rewind.
                            // For simple GETs/POSTs with headers, this works, but for streamed content, standard cloning logic applies.
                            response.Dispose(); // Dispose failed response
                            return await base.SendAsync(request, cancellationToken);
                        }
                    }
                }
            }

            return response;
        }
    }
}