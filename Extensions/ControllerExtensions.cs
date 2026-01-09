using Microsoft.AspNetCore.Mvc;

namespace cfm_frontend.Extensions
{
    /// <summary>
    /// Extension methods for controller authorization checks
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// Check if user can view a specific page. Returns redirect to AccessDenied if not authorized.
        /// </summary>
        public static IActionResult? CheckViewAccess(this Controller controller, string moduleName, string pageName)
        {
            var privileges = controller.HttpContext.Session.GetPrivileges();

            if (privileges == null || !privileges.CanViewPage(moduleName, pageName))
            {
                controller.TempData["AccessDeniedMessage"] = $"You do not have permission to view {pageName} in {moduleName}.";

                // Capture current URL for redirect after restore
                var returnUrl = controller.HttpContext.Request.Path + controller.HttpContext.Request.QueryString;

                return controller.RedirectToAction("AccessDenied", "Error", new { returnUrl });
            }

            return null;
        }

        /// <summary>
        /// Check if user can add to a specific page. Returns redirect to AccessDenied if not authorized.
        /// </summary>
        public static IActionResult? CheckAddAccess(this Controller controller, string moduleName, string pageName)
        {
            var privileges = controller.HttpContext.Session.GetPrivileges();

            if (privileges == null || !privileges.CanAddToPage(moduleName, pageName))
            {
                controller.TempData["AccessDeniedMessage"] = $"You do not have permission to add to {pageName} in {moduleName}.";

                // Capture current URL for redirect after restore
                var returnUrl = controller.HttpContext.Request.Path + controller.HttpContext.Request.QueryString;

                return controller.RedirectToAction("AccessDenied", "Error", new { returnUrl });
            }

            return null;
        }

        /// <summary>
        /// Check if user can edit a specific page. Returns redirect to AccessDenied if not authorized.
        /// </summary>
        public static IActionResult? CheckEditAccess(this Controller controller, string moduleName, string pageName)
        {
            var privileges = controller.HttpContext.Session.GetPrivileges();

            if (privileges == null || !privileges.CanEditPage(moduleName, pageName))
            {
                controller.TempData["AccessDeniedMessage"] = $"You do not have permission to edit {pageName} in {moduleName}.";

                // Capture current URL for redirect after restore
                var returnUrl = controller.HttpContext.Request.Path + controller.HttpContext.Request.QueryString;

                return controller.RedirectToAction("AccessDenied", "Error", new { returnUrl });
            }

            return null;
        }

        /// <summary>
        /// Check if user can delete from a specific page. Returns redirect to AccessDenied if not authorized.
        /// </summary>
        public static IActionResult? CheckDeleteAccess(this Controller controller, string moduleName, string pageName)
        {
            var privileges = controller.HttpContext.Session.GetPrivileges();

            if (privileges == null || !privileges.CanDeleteFromPage(moduleName, pageName))
            {
                controller.TempData["AccessDeniedMessage"] = $"You do not have permission to delete from {pageName} in {moduleName}.";

                // Capture current URL for redirect after restore
                var returnUrl = controller.HttpContext.Request.Path + controller.HttpContext.Request.QueryString;

                return controller.RedirectToAction("AccessDenied", "Error", new { returnUrl });
            }

            return null;
        }
    }
}
