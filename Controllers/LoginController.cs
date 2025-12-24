using cfm_frontend.DTOs.Login;
using cfm_frontend.DTOs.UserInfo;
using cfm_frontend.Models;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace cfm_frontend.Controllers
{
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginController> _logger;

        public LoginController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<LoginController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(SignInViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var backendUrl = _configuration["BackendBaseUrl"];

                var payload = new { username = model.Username, password = model.Password };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}/api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var authResponse = await JsonSerializer.DeserializeAsync<LoginResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (authResponse != null)
                    {
                        // Step 2: Fetch user info using the tokens
                        var userInfoResponse = await FetchUserInfoAsync(authResponse.AccessToken, authResponse.RefreshToken);

                        if (userInfoResponse != null)
                        {
                            // Step 3: Create UserSession object
                            var userInfo = new UserInfo
                            {
                                UserId = userInfoResponse.UserId,
                                Username = userInfoResponse.Username,
                                Email = userInfoResponse.Email,
                                FullName = userInfoResponse.FullName,
                                Role = userInfoResponse.Role,
                                Department = userInfoResponse.Department,
                                PhoneNumber = userInfoResponse.PhoneNumber,
                                ProfilePicture = userInfoResponse.ProfilePicture,
                                PreferredClientId = userInfoResponse.PreferredClientId,
                                IdCompany = userInfoResponse.IdCompany,
                                LoginTime = DateTime.UtcNow
                            };

                            HttpContext.Session.SetString("UserSession", JsonSerializer.Serialize(userInfo));

                            // Step 5: Create claims with user info
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, userInfo.FullName ?? userInfo.Username),
                                new Claim("Username", userInfo.Username),
                                new Claim("UserId", userInfo.UserId.ToString()),
                                new Claim(ClaimTypes.Email, userInfo.Email ?? string.Empty),
                                new Claim(ClaimTypes.Role, userInfo.Role ?? string.Empty)
                            };

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = true,
                                ExpiresUtc = DateTime.UtcNow.AddDays(7)
                            };

                            authProperties.StoreTokens(new List<AuthenticationToken>
                            {
                                new AuthenticationToken { Name = "access_token", Value = authResponse.AccessToken },
                                new AuthenticationToken { Name = "refresh_token", Value = authResponse.RefreshToken }
                            });

                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties
                            );

                            _logger.LogInformation("User {Username} (ID: {UserId}) logged in successfully at {Time}",
                                userInfo.Username, userInfo.UserId, DateTime.UtcNow);

                            return RedirectToAction("Index", "Dashboard");
                        }
                        else
                        {
                            _logger.LogWarning("Failed to fetch user info for {Username}", model.Username);
                            ModelState.AddModelError(string.Empty, "Failed to retrieve user information.");
                        }
                    }
                }

                _logger.LogWarning("Login failed for {Username}. Status: {Status}", model.Username, response.StatusCode);
                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "Could not connect to server.");
            }

            return View("Index", model);
        }

        /// <summary>
        /// Fetch user information from backend API using access and refresh tokens
        /// </summary>
        private async Task<UserInfoResponse?> FetchUserInfoAsync(string accessToken, string refreshToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var backendUrl = _configuration["BackendBaseUrl"];

                var payload = new
                {
                    accessToken = accessToken,
                    refreshToken = refreshToken
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}/api/auth/userinfo", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var userInfo = await JsonSerializer.DeserializeAsync<UserInfoResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return userInfo;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch user info. Status: {Status}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user info");
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear session
            HttpContext.Session.Remove("UserSession");

            // Sign out from authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Login");
        }
    }
}