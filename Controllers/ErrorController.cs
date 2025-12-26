using cfm_frontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace cfm_frontend.Controllers
{
    public class ErrorController : BaseController
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(IPrivilegeService privilegeService, ILogger<ErrorController> logger)
            : base(privilegeService, logger)
        {
            _logger = logger;
        }

        public IActionResult AccessDenied()
        {
            _logger.LogWarning("Access denied for user. Path: {Path}, Message: {Message}",
                HttpContext.Request.Path,
                TempData["AccessDeniedMessage"] ?? "No specific message");

            return View();
        }
    }
}
