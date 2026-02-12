using cfm_frontend.Constants;
using cfm_frontend.DTOs;
using cfm_frontend.DTOs.Auth;
using cfm_frontend.DTOs.Login;
using cfm_frontend.DTOs.UserInfo;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using cfm_frontend.Services;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace cfm_frontend.Controllers
{
    public class LoginController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginController> _logger;
        private readonly IPrivilegeService _privilegeService;

        public LoginController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<LoginController> logger,
            IPrivilegeService privilegeService)
            : base(privilegeService, logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _privilegeService = privilegeService;
        }

        public IActionResult Index(string? returnUrl = null, bool sessionExpired = false)
        {
            // Store return URL in ViewBag so login form can include it
            ViewBag.ReturnUrl = returnUrl;

            // Store session expired flag in ViewBag for toast notification
            ViewBag.SessionExpired = sessionExpired;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(SignInViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                // Preserve return URL in ViewBag when returning validation errors
                ViewBag.ReturnUrl = returnUrl;
                return View("Index", model);
            }

            try
            {
                // IMPORTANT: Use plain HttpClient (NOT "BackendAPI") during login flow
                // The "BackendAPI" client has AuthTokenHandler which requires an authenticated session cookie
                // Before HttpContext.SignInAsync() is called , the cookie doesn't exist yet
                var client = _httpClientFactory.CreateClient();
                var backendUrl = _configuration["BackendBaseUrl"];

                // Create Basic Authentication header: "Basic base64(username:password)"
                var credentials = $"{model.Username}:{model.Password}";
                var credentialsBytes = Encoding.UTF8.GetBytes(credentials);
                var base64Credentials = Convert.ToBase64String(credentialsBytes);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Credentials);

                // POST request with empty body (credentials are in Authorization header)
                var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Auth.Login}",content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var authResponse = await JsonSerializer.DeserializeAsync<LoginResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (authResponse?.Success == true && authResponse.Data != null)
                    {
                        // Step 2: Fetch user info using the tokens
                        var userInfoResponse = await FetchUserInfoAsync(authResponse.Data.Token, authResponse.Data.RefreshToken);

                        if (userInfoResponse != null)
                        {
                            // Step 3: Create UserSession object
                            var userInfo = new UserInfo
                            {
                                IdWebUser = userInfoResponse.IdWebUser,
                                //Username = userInfoResponse.Username,
                                //Email = userInfoResponse.Email,
                                FullName = userInfoResponse.FullName,
                                //Role = userInfoResponse.Role,
                                //Department = userInfoResponse.Department,
                                //PhoneNumber = userInfoResponse.PhoneNumber,
                                //ProfilePicture = userInfoResponse.ProfilePicture,

                                PreferredClientId = userInfoResponse.Preferred_Client_idClient,
                                TimeZoneName = userInfoResponse.TimeZoneName,
                                PreferredTimezoneIdTimezone = userInfoResponse.Preferred_TimeZone_idTimeZone,
                                IdCompany = userInfoResponse.Preferred_Company_idCompany,
                                LoginTime = DateTime.UtcNow
                            };

                            HttpContext.Session.SetString("UserSession", JsonSerializer.Serialize(userInfo));

                            // Step 5: Load user privileges (pass token explicitly since auth cookie not created yet)
                            var privileges = await _privilegeService.LoadUserPrivilegesAsync(authResponse.Data.Token);
                            if (privileges != null)
                            {
                                HttpContext.Session.SetPrivileges(privileges);
                                _logger.LogInformation("User privileges loaded successfully: {ModuleCount} modules, {PageCount} pages",
                                    privileges.Modules.Count,
                                    privileges.Modules.Sum(m => m.Pages.Count));
                            }
                            else
                            {
                                _logger.LogWarning("Failed to load user privileges for {Username}. User will have no access.", model.Username);
                                // Continue login - user will have no privileges (all authorization checks will fail)
                            }

                            // Step 6: Create claims with user info
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, userInfo.FullName ?? userInfo.Username),
                                new Claim("Username", userInfo.Username),
                                new Claim("UserId", userInfo.IdWebUser.ToString()),
                                new Claim(ClaimTypes.Email, userInfo.Email ?? string.Empty),
                                new Claim(ClaimTypes.Role, userInfo.Role ?? string.Empty)
                            };

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = model.RememberMe,
                                ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(12)
                            };

                            authProperties.StoreTokens(new List<AuthenticationToken>
                            {
                                new AuthenticationToken { Name = "access_token", Value = authResponse.Data.Token },
                                new AuthenticationToken { Name = "refresh_token", Value = authResponse.Data.RefreshToken }
                            });

                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties
                            );

                            _logger.LogInformation("User {Username} (ID: {UserId}) logged in successfully at {Time}. RememberMe: {RememberMe}",
                                userInfo.Username, userInfo.IdWebUser, DateTime.UtcNow, model.RememberMe);

                            // Redirect to return URL if provided, otherwise go to dashboard
                            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                            {
                                _logger.LogInformation("Redirecting user to return URL: {ReturnUrl}", returnUrl);
                                return Redirect(returnUrl);
                            }

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
        /// IMPORTANT: Uses plain HttpClient (NOT "BackendAPI") because this is called during login
        /// before the authentication cookie is created
        /// </summary>
        private async Task<UserInfoResponse?> FetchUserInfoAsync(string accessToken, string refreshToken)
        {
            try
            {
                // Do NOT use "BackendAPI" client - auth cookie doesn't exist yet
                var client = _httpClientFactory.CreateClient();
                var backendUrl = _configuration["BackendBaseUrl"];

                // Manually add Bearer token to the request header
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.UserInfo.GetUserDetail}");

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
            HttpContext.Session.Remove("UserPrivileges");

            // Sign out from authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Login");
        }

        #region Forgot Password

        /// <summary>
        /// GET: Display forgot password form
        /// </summary>
        public IActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// POST: Process forgot password request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Use plain HttpClient (not "BackendAPI") - no auth required
                var client = _httpClientFactory.CreateClient();
                var backendUrl = _configuration["BackendBaseUrl"];

                var payload = new ForgotPasswordRequest { Email = model.Email };
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Auth.ForgotPassword}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<object>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true)
                    {
                        _logger.LogInformation("Password reset email requested for {Email}", model.Email);
                    }
                }

                // Always redirect to confirmation (prevent email enumeration)
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password for {Email}", model.Email);
                // Still redirect to confirmation (don't reveal errors)
                return RedirectToAction("ForgotPasswordConfirmation");
            }
        }

        /// <summary>
        /// GET: Display forgot password confirmation (check your email)
        /// </summary>
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        /// <summary>
        /// GET: Display reset password form
        /// </summary>
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        /// <summary>
        /// POST: Process password reset
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var backendUrl = _configuration["BackendBaseUrl"];

                var payload = new ResetPasswordRequest
                {
                    Email = model.Email,
                    Token = model.Token,
                    NewPassword = model.Password
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Auth.ResetPassword}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<object>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true)
                    {
                        _logger.LogInformation("Password reset successful for {Email}", model.Email);
                        return RedirectToAction("ResetPasswordConfirmation");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, apiResponse?.Message ?? "Failed to reset password");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid or expired reset token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred while resetting your password");
            }

            return View(model);
        }

        /// <summary>
        /// GET: Display password reset success message
        /// </summary>
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        #endregion
    }
}