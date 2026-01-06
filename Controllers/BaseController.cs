using cfm_frontend.Extensions;
using cfm_frontend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
    }
}
