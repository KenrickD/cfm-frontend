using cfm_frontend.Extensions;
using cfm_frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace cfm_frontend.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IPrivilegeService _privilegeService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IPrivilegeService privilegeService,
            ILogger<AccountController> logger)
            : base(privilegeService, logger)
        {
            _privilegeService = privilegeService;
            _logger = logger;
        }

        /// <summary>
        /// Manually refresh user privileges. Called when user clicks "Refresh Permissions" button.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RefreshPrivileges()
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                _logger.LogWarning("Privilege refresh attempted without active session");
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                _logger.LogInformation("Manual privilege refresh initiated");

                // Reload privileges from API
                var privileges = await _privilegeService.LoadUserPrivilegesAsync();

                if (privileges != null)
                {
                    HttpContext.Session.SetPrivileges(privileges);

                    _logger.LogInformation("User privileges manually refreshed successfully. Modules: {ModuleCount}, LoadedAt: {LoadedAt}",
                        privileges.Modules.Count,
                        privileges.LoadedAt);

                    return Json(new
                    {
                        success = true,
                        message = "Permissions refreshed successfully",
                        timestamp = privileges.LoadedAt
                    });
                }
                else
                {
                    _logger.LogWarning("Manual privilege refresh returned null");
                    return Json(new { success = false, message = "Failed to load privileges from server" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual privilege refresh");
                return Json(new { success = false, message = "An error occurred while refreshing privileges" });
            }
        }
    }
}
