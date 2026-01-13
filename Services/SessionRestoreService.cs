using cfm_frontend.Constants;
using cfm_frontend.DTOs.UserInfo;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;

namespace cfm_frontend.Services
{
    public interface ISessionRestoreService
    {
        Task<bool> TryRestoreSessionAsync();
    }

    public class SessionRestoreService : ISessionRestoreService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IPrivilegeService _privilegeService;
        private readonly ILogger<SessionRestoreService> _logger;

        public SessionRestoreService(
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IPrivilegeService privilegeService,
            ILogger<SessionRestoreService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _privilegeService = privilegeService;
            _logger = logger;
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null || context.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Cannot restore session - context null or user not authenticated");
                return false;
            }

            try
            {
                // Check if already restored
                var existingSession = context.Session.GetString("UserSession");
                var existingPrivileges = context.Session.GetString("UserPrivileges");

                if (!string.IsNullOrEmpty(existingSession) && !string.IsNullOrEmpty(existingPrivileges))
                {
                    _logger.LogInformation("Session already restored, skipping");
                    return true;
                }

                var accessToken = await context.GetTokenAsync("access_token");
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("No access token found in auth cookie");
                    return false;
                }

                // Fetch UserInfo from backend
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];
                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.UserInfo.GetUserDetail}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch user info. Status: {Status}", response.StatusCode);
                    return false;
                }

                var userInfoResponse = await JsonSerializer.DeserializeAsync<UserInfoResponse>(
                    await response.Content.ReadAsStreamAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (userInfoResponse == null)
                {
                    _logger.LogWarning("UserInfo response was null");
                    return false;
                }

                // Restore UserSession
                var userInfo = new UserInfo
                {
                    IdWebUser = userInfoResponse.IdWebUser,
                    FullName = userInfoResponse.FullName,
                    PreferredClientId = userInfoResponse.Preferred_Client_idClient,
                    TimeZoneName = userInfoResponse.TimeZoneName,
                    PreferredTimezoneIdTimezone = userInfoResponse.Preferred_TimeZone_idTimeZone,
                    IdCompany = userInfoResponse.Preferred_Company_idCompany,
                    LoginTime = DateTime.UtcNow
                };

                context.Session.SetString("UserSession", JsonSerializer.Serialize(userInfo));
                _logger.LogInformation("UserSession restored for user {UserId}", userInfo.IdWebUser);

                // Restore UserPrivileges
                var privileges = await _privilegeService.LoadUserPrivilegesAsync();
                if (privileges != null)
                {
                    context.Session.SetPrivileges(privileges);
                    _logger.LogInformation("UserPrivileges restored: {ModuleCount} modules", privileges.Modules.Count);
                    return true;
                }

                _logger.LogWarning("Failed to load privileges during restore");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring session");
                return false;
            }
        }
    }
}
