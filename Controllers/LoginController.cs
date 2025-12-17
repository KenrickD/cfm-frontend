using cfm_frontend.DTOs.Login;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
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
        private readonly ILogger _logger;
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
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.Username ?? "User"),
                            new Claim("Username", model.Username ?? "User")
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTime.UtcNow.AddDays(7) // Need to be the same as refresh token
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

                        return RedirectToAction("Index", "Dashboard");
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

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }
    }
}

