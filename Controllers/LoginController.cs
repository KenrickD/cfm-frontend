using cfm_frontend.DTOs.Login;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Mvc;
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
                    var authResponse = await JsonSerializer.DeserializeAsync<LoginResponse>(responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (authResponse != null)
                    {
                        
                        HttpContext.Session.SetString("AccessToken", authResponse.AccessToken);
                        HttpContext.Response.Cookies.Append("RefreshToken", authResponse.RefreshToken, new CookieOptions
                        {
                            HttpOnly = true, 
                            Secure = true,   
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddDays(7) //match with the backend token expiry time, and/or  set it to a global variable
                        });
                        return RedirectToAction("Index", "Dashboard");
                    }
                }
                ViewBag.ErrorMessage = "Invalid login credentials";
                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during sign-in for user {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "An error occurred while connecting to the server.");
                ViewBag.ErrorMessage = "An error occurred while connecting to the server.";
            }

            return View("Index", model);
        }
    }
}

