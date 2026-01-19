using cfm_frontend.DTOs;
using cfm_frontend.Extensions;
using cfm_frontend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace cfm_frontend.Controllers
{
    /// <summary>
    /// Base controller with automatic privilege refresh logic.
    /// All controllers should inherit from this.
    /// </summary>
    public abstract class BaseController : Controller
    {
        private readonly IPrivilegeService _privilegeService;
        private readonly ILogger<BaseController> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        protected BaseController(IPrivilegeService privilegeService, ILogger<BaseController> logger)
        {
            _privilegeService = privilegeService;
            _logger = logger;
        }


        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Smart lazy refresh: Check if privileges are stale
            var privileges = HttpContext.Session.GetPrivileges();

            if (privileges != null)
            {
                var age = DateTime.UtcNow - privileges.LoadedAt;

                // If privileges are older than 30 minutes, trigger background refresh
                if (age.TotalMinutes > 30)
                {
                    _logger.LogInformation("Privileges are stale ({Minutes:F1} min old), triggering background refresh", age.TotalMinutes);

                    // Fire-and-forget async refresh (won't block current request)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var newPrivileges = await _privilegeService.LoadUserPrivilegesAsync();
                            if (newPrivileges != null)
                            {
                                HttpContext.Session.SetPrivileges(newPrivileges);
                                _logger.LogInformation("Privileges auto-refreshed successfully in background");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Background privilege refresh failed");
                            // Don't throw - current request continues with existing privileges
                        }
                    });
                }
            }

            // FUTURE: Load user theme preference from database
            // When implementing, add a ThemePreference field to UserInfo or create a UserPreferences table
            // Example implementation:
            // var userInfo = JsonSerializer.Deserialize<UserInfo>(HttpContext.Session.GetString("UserSession"));
            // if (userInfo?.ThemePreference != null)
            // {
            //     ViewBag.UserThemePreference = userInfo.ThemePreference; // "light" or "dark"
            // }

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Safely executes an API call and handles response deserialization with consistent error handling.
        /// </summary>
        /// <typeparam name="T">The type of data expected in the API response</typeparam>
        /// <param name="apiCall">The async function that performs the HTTP request</param>
        /// <param name="errorMessage">Default error message to use if API doesn't provide one</param>
        /// <returns>A tuple containing success status, data (if successful), and message</returns>
        protected async Task<(bool Success, T? Data, string Message)> SafeExecuteApiAsync<T>(
            Func<Task<HttpResponseMessage>> apiCall,
            string errorMessage = "API call failed")
        {
            try
            {
                var response = await apiCall();

                if (response.IsSuccessStatusCode)
                {
                    using var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<T>>(
                        responseStream,
                        JsonOptions);

                    if (apiResponse != null && apiResponse.Success)
                    {
                        return (true, apiResponse.Data, apiResponse.Message);
                    }

                    // Handle logical error from API (Success = false)
                    var msg = !string.IsNullOrEmpty(apiResponse?.Message) ? apiResponse.Message : errorMessage;
                    _logger.LogWarning("API Logic Error: {Message}", msg);
                    return (false, default, msg);
                }
                else
                {
                    // Handle HTTP error
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API HTTP Error {StatusCode}: {Content}", response.StatusCode, errorContent);
                    return (false, default, errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Exception during: {ErrorMessage}", errorMessage);
                return (false, default, errorMessage);
            }
        }

        /// <summary>
        /// Safely executes an API call with cancellation and timeout support.
        /// Use this overload for parallel API calls with Task.WhenAll.
        /// </summary>
        /// <typeparam name="T">The type of data expected in the API response</typeparam>
        /// <param name="apiCall">The async function that performs the HTTP request with cancellation support</param>
        /// <param name="errorMessage">Default error message to use if API doesn't provide one</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 30000ms)</param>
        /// <returns>A tuple containing success status, data (if successful), and message</returns>
        protected async Task<(bool Success, T? Data, string Message)> SafeExecuteApiAsync<T>(
            Func<CancellationToken, Task<HttpResponseMessage>> apiCall,
            string errorMessage,
            CancellationToken cancellationToken,
            int timeoutMs = 30000)
        {
            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            try
            {
                var response = await apiCall(linkedCts.Token);

                if (response.IsSuccessStatusCode)
                {
                    using var responseStream = await response.Content.ReadAsStreamAsync(linkedCts.Token);
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<T>>(
                        responseStream,
                        JsonOptions,
                        linkedCts.Token);

                    if (apiResponse != null && apiResponse.Success)
                    {
                        return (true, apiResponse.Data, apiResponse.Message);
                    }

                    var msg = !string.IsNullOrEmpty(apiResponse?.Message) ? apiResponse.Message : errorMessage;
                    _logger.LogWarning("API Logic Error: {Message}", msg);
                    return (false, default, msg);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(linkedCts.Token);
                    _logger.LogError("API HTTP Error {StatusCode}: {Content}", response.StatusCode, errorContent);
                    return (false, default, errorMessage);
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning("API call timed out after {TimeoutMs}ms: {ErrorMessage}", timeoutMs, errorMessage);
                return (false, default, $"{errorMessage} (timeout)");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("API call was cancelled: {ErrorMessage}", errorMessage);
                return (false, default, $"{errorMessage} (cancelled)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Exception during: {ErrorMessage}", errorMessage);
                return (false, default, errorMessage);
            }
        }
    }
}
