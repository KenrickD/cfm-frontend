using cfm_frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace cfm_frontend.Controllers
{
    public class ErrorController : BaseController
    {
        private readonly ISessionRestoreService _sessionRestoreService;
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(
            ISessionRestoreService sessionRestoreService,
            IPrivilegeService privilegeService,
            ILogger<ErrorController> logger)
            : base(privilegeService, logger)
        {
            _sessionRestoreService = sessionRestoreService;
            _logger = logger;
        }

        public async Task<IActionResult> AccessDenied(string returnUrl = null)
        {
            // Only attempt restore ONCE to prevent infinite loops
            // Check if we've already tried (using TempData flag)
            var alreadyAttemptedRestore = TempData["SessionRestoreAttempted"] as bool? ?? false;

            if (!alreadyAttemptedRestore)
            {
                _logger.LogInformation("Access denied - attempting automatic session restore");
                TempData["SessionRestoreAttempted"] = true; // Set flag BEFORE restore

                var restored = await _sessionRestoreService.TryRestoreSessionAsync();

                if (restored)
                {
                    _logger.LogInformation("Session restored successfully, redirecting to: {ReturnUrl}", returnUrl ?? "/Dashboard");

                    // Clear the flag on success
                    TempData.Remove("SessionRestoreAttempted");

                    // Redirect to original URL or Dashboard
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Dashboard");
                }

                _logger.LogWarning("Session restore failed - showing AccessDenied page");
            }
            else
            {
                _logger.LogInformation("Session restore already attempted - showing AccessDenied page");
            }

            // Clear flag for next time
            TempData.Remove("SessionRestoreAttempted");

            // Show AccessDenied page (restore failed or already attempted)
            _logger.LogWarning("Access denied for user. Path: {Path}, ReturnUrl: {ReturnUrl}, Message: {Message}",
                HttpContext.Request.Path,
                returnUrl,
                TempData["AccessDeniedMessage"] ?? "No specific message");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
    }
}
