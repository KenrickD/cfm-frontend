using cfm_frontend.Constants;
using cfm_frontend.DTOs;
using cfm_frontend.DTOs.Auth;
using cfm_frontend.Services;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace cfm_frontend.Controllers
{
    /// <summary>
    /// Controller for license-based user registration
    /// </summary>
    public class RegistrationController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegistrationController> _logger;

        private const string LicenseInfoSessionKey = "LicenseInfo";

        public RegistrationController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<RegistrationController> logger,
            IPrivilegeService privilegeService)
            : base(privilegeService, logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        #region License Validation

        /// <summary>
        /// GET: Display license key validation form
        /// </summary>
        public IActionResult ValidateLicense()
        {
            return View(new LicenseValidationViewModel());
        }

        /// <summary>
        /// POST: Validate license key and password
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateLicense(LicenseValidationViewModel model)
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

                var payload = new ValidateLicenseRequest
                {
                    LicenseKey = model.FullLicenseKey,
                    LicensePassword = model.LicensePassword
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Auth.ValidateLicense}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ValidateLicenseResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        // Store license info in session
                        var licenseInfo = new LicenseInfoViewModel
                        {
                            IdUserGroup = apiResponse.Data.IdUserGroup,
                            IdClient = apiResponse.Data.IdClient,
                            ClientName = apiResponse.Data.ClientName,
                            ClientLogoUrl = apiResponse.Data.ClientLogoUrl,
                            LicenseKey = model.FullLicenseKey,
                            RequiredEmailDomain = apiResponse.Data.RequiredEmailDomain,
                            QuotaRemaining = apiResponse.Data.QuotaRemaining,
                            ExpiryDate = apiResponse.Data.ExpiryDate
                        };

                        HttpContext.Session.SetString(LicenseInfoSessionKey, JsonSerializer.Serialize(licenseInfo));

                        _logger.LogInformation("License validated for client {ClientName} (ID: {IdClient})",
                            apiResponse.Data.ClientName, apiResponse.Data.IdClient);

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, apiResponse?.Message ?? "Invalid license key or password");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("License validation failed. Status: {Status}, Response: {Response}",
                        response.StatusCode, errorContent);
                    ModelState.AddModelError(string.Empty, "Invalid license key or password");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating license key");
                ModelState.AddModelError(string.Empty, "Could not connect to server. Please try again later.");
            }

            return View(model);
        }

        #endregion

        #region Registration Form

        /// <summary>
        /// GET: Display registration form (requires valid license in session)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Check if license info exists in session
            var licenseInfoJson = HttpContext.Session.GetString(LicenseInfoSessionKey);
            if (string.IsNullOrEmpty(licenseInfoJson))
            {
                return RedirectToAction("ValidateLicense");
            }

            var licenseInfo = JsonSerializer.Deserialize<LicenseInfoViewModel>(licenseInfoJson);
            if (licenseInfo == null)
            {
                return RedirectToAction("ValidateLicense");
            }

            var model = new RegisterViewModel
            {
                LicenseInfo = licenseInfo
            };

            // Load dropdown options
            await LoadDropdownOptionsAsync(model, licenseInfo.IdClient);

            return View(model);
        }

        /// <summary>
        /// POST: Process registration
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            // Get license info from session
            var licenseInfoJson = HttpContext.Session.GetString(LicenseInfoSessionKey);
            if (string.IsNullOrEmpty(licenseInfoJson))
            {
                return RedirectToAction("ValidateLicense");
            }

            var licenseInfo = JsonSerializer.Deserialize<LicenseInfoViewModel>(licenseInfoJson);
            if (licenseInfo == null)
            {
                return RedirectToAction("ValidateLicense");
            }

            model.LicenseInfo = licenseInfo;

            if (!ModelState.IsValid)
            {
                await LoadDropdownOptionsAsync(model, licenseInfo.IdClient);
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var backendUrl = _configuration["BackendBaseUrl"];

                var payload = new RegisterRequest
                {
                    IdUserGroup = licenseInfo.IdUserGroup,
                    SalutationId = model.SalutationId,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DepartmentId = model.DepartmentId,
                    Title = model.Title,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    TimeZoneId = model.TimeZoneId,
                    CurrencyId = model.CurrencyId,
                    Username = model.Username,
                    Password = model.Password
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Auth.Register}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<object>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true)
                    {
                        _logger.LogInformation("User registered: {Username} ({Email}) for client {ClientId}",
                            model.Username, model.Email, licenseInfo.IdClient);

                        // Clear license info from session
                        HttpContext.Session.Remove(LicenseInfoSessionKey);

                        return RedirectToAction("Confirmation");
                    }
                    else
                    {
                        // Handle specific errors
                        if (apiResponse?.Errors != null && apiResponse.Errors.Any())
                        {
                            foreach (var error in apiResponse.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error);
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, apiResponse?.Message ?? "Registration failed");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Registration failed. Status: {Status}", response.StatusCode);
                    ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "Could not connect to server. Please try again later.");
            }

            await LoadDropdownOptionsAsync(model, licenseInfo.IdClient);
            return View(model);
        }

        #endregion

        #region Confirmation and Verification

        /// <summary>
        /// GET: Display registration confirmation (check your email)
        /// </summary>
        public IActionResult Confirmation()
        {
            return View();
        }

        /// <summary>
        /// GET: Process email verification
        /// </summary>
        public async Task<IActionResult> VerifyEmail(string? token, string? email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index", "Login");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Auth.VerifyEmail}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<object>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true)
                    {
                        _logger.LogInformation("Email verified for {Email}", email);
                        return View("VerificationSuccess");
                    }
                }

                _logger.LogWarning("Email verification failed for {Email}", email);
                ViewBag.ErrorMessage = "Invalid or expired verification link";
                return View("VerificationFailed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for {Email}", email);
                ViewBag.ErrorMessage = "An error occurred during verification";
                return View("VerificationFailed");
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Load dropdown options from backend APIs
        /// </summary>
        private async Task LoadDropdownOptionsAsync(RegisterViewModel model, int idClient)
        {
            var client = _httpClientFactory.CreateClient();
            var backendUrl = _configuration["BackendBaseUrl"];

            // Load all dropdown options in parallel
            var salutationsTask = LoadLookupAsync<List<SelectListItemModel>>(client, $"{backendUrl}/api/v1/lookup/salutations");
            var departmentsTask = LoadLookupAsync<List<SelectListItemModel>>(client, $"{backendUrl}/api/v1/lookup/departments?idClient={idClient}");
            var timeZonesTask = LoadLookupAsync<List<SelectListItemModel>>(client, $"{backendUrl}/api/v1/lookup/timezones");
            var currenciesTask = LoadLookupAsync<List<SelectListItemModel>>(client, $"{backendUrl}/api/v1/lookup/currencies");

            try
            {
                await Task.WhenAll(salutationsTask, departmentsTask, timeZonesTask, currenciesTask);

                model.Salutations = salutationsTask.Result ?? new List<SelectListItemModel>();
                model.Departments = departmentsTask.Result ?? new List<SelectListItemModel>();
                model.TimeZones = timeZonesTask.Result ?? new List<SelectListItemModel>();
                model.Currencies = currenciesTask.Result ?? new List<SelectListItemModel>();

                // Set default selections
                var defaultTimeZone = model.TimeZones.FirstOrDefault(t => t.IsDefault);
                if (defaultTimeZone != null && model.TimeZoneId == 0)
                {
                    model.TimeZoneId = defaultTimeZone.Id;
                }

                var defaultCurrency = model.Currencies.FirstOrDefault(c => c.IsDefault);
                if (defaultCurrency != null && model.CurrencyId == 0)
                {
                    model.CurrencyId = defaultCurrency.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dropdown options");
            }
        }

        /// <summary>
        /// Generic helper to load lookup data from API
        /// </summary>
        private async Task<T?> LoadLookupAsync<T>(HttpClient client, string url) where T : class
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<T>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return apiResponse?.Data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load lookup from {Url}", url);
            }

            return null;
        }

        #endregion
    }
}
