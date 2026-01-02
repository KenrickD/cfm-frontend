using cfm_frontend.Constants;
using cfm_frontend.DTOs.UserInfo;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using cfm_frontend.Services;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;

namespace cfm_frontend.Middleware
{
    /// <summary>
    /// Middleware that automatically restores session data when session expires
    /// but authentication cookie (with tokens) is still valid.
    /// This allows seamless user experience - tokens auto-refresh via AuthTokenHandler,
    /// and session data auto-reloads from API.
    /// </summary>
    public class SessionRestoreMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionRestoreMiddleware> _logger;

        public SessionRestoreMiddleware(RequestDelegate next, ILogger<SessionRestoreMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPrivilegeService privilegeService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            // Only attempt restore if user is authenticated but session is missing
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userSession = context.Session.GetUserInfo();
                var privileges = context.Session.GetPrivileges();

                // If either session data is missing, attempt to restore
                if (userSession == null || privileges == null)
                {
                    _logger.LogInformation("Detected expired session for authenticated user. Attempting auto-restore...");

                    var restored = await RestoreSessionAsync(context, privilegeService, httpClientFactory, configuration);

                    if (restored)
                    {
                        _logger.LogInformation("Session auto-restored successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to restore session. User will be redirected to login.");
                        // Don't force logout here - let the controller handle it
                        // This allows proper error messages and cleanup
                    }
                }
            }

            await _next(context);
        }

        private async Task<bool> RestoreSessionAsync(
            HttpContext context,
            IPrivilegeService privilegeService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            try
            {
                // Get access token from authentication cookie
                var accessToken = await context.GetTokenAsync("access_token");

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("No access token found in authentication cookie");
                    return false;
                }

                // Use "BackendAPI" client - AuthTokenHandler will auto-refresh if token expired
                var client = httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = configuration["BackendBaseUrl"];

                // Reload UserInfo from API
                var userInfoResponse = await client.GetAsync($"{backendUrl}{ApiEndpoints.UserInfo.GetUserDetail}");

                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to reload UserInfo. Status: {StatusCode}", userInfoResponse.StatusCode);
                    return false;
                }

                var responseStream = await userInfoResponse.Content.ReadAsStreamAsync();
                var userInfoDto = await JsonSerializer.DeserializeAsync<UserInfoResponse>(
                    responseStream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (userInfoDto == null)
                {
                    _logger.LogWarning("Failed to deserialize UserInfo response");
                    return false;
                }

                // Reconstruct UserInfo object
                var userInfo = new UserInfo
                {
                    IdWebUser = userInfoDto.IdWebUser,
                    FullName = userInfoDto.FullName,
                    PreferredClientId = userInfoDto.Preferred_Client_idClient,
                    TimeZoneName = userInfoDto.TimeZoneName,
                    PreferredTimezoneIdTimezone = userInfoDto.Preferred_TimeZone_idTimeZone,
                    IdCompany = userInfoDto.IdCompany,
                    LoginTime = DateTime.UtcNow // Update login time to reflect restore
                };

                // Restore UserSession in session
                context.Session.SetUserInfo(userInfo);
                _logger.LogInformation("UserSession restored for user {UserId}", userInfo.IdWebUser);

                // Reload privileges
                var newPrivileges = await privilegeService.LoadUserPrivilegesAsync();

                if (newPrivileges != null)
                {
                    context.Session.SetPrivileges(newPrivileges);
                    _logger.LogInformation("User privileges restored: {ModuleCount} modules, {PageCount} pages",
                        newPrivileges.Modules.Count,
                        newPrivileges.Modules.Sum(m => m.Pages.Count));
                }
                else
                {
                    _logger.LogWarning("Failed to restore user privileges");
                    // Continue anyway - user will have no privileges but won't be kicked out
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring session");
                return false;
            }
        }
    }

    /// <summary>
    /// Extension method for easy middleware registration
    /// </summary>
    public static class SessionRestoreMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionRestore(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionRestoreMiddleware>();
        }
    }
}
