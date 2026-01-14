using cfm_frontend.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace cfm_frontend.Helpers
{
    /// <summary>
    /// Centralized helper for authentication-related operations.
    /// Used by both middleware and exception filters for consistent session cleanup and request detection.
    /// </summary>
    public static class AuthenticationHelper
    {
        /// <summary>
        /// Clears all authentication data (session + cookie) for the current user.
        /// Call this when tokens have expired or authentication has failed.
        /// </summary>
        /// <param name="context">The HTTP context</param>
        public static async Task ClearAuthenticationAsync(HttpContext context)
        {
            // Clear session data
            context.Session.Remove("UserSession");
            context.Session.Remove("UserPrivileges");
            await context.Session.CommitAsync();

            // Sign out authentication cookie
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Determines if the current request is an AJAX/API request.
        /// Used to decide between returning JSON (AJAX) or redirecting (full page).
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <returns>True if AJAX request, false if full page request</returns>
        public static bool IsAjaxRequest(HttpRequest request)
        {
            // Check for XMLHttpRequest header (standard AJAX detection)
            if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return true;

            // Check if client explicitly requests JSON
            if (request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if path starts with /api (API endpoints)
            if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if both access token and refresh token are expired.
        /// This means the user's session cannot be recovered and they must log in again.
        /// </summary>
        /// <param name="accessToken">The access token</param>
        /// <param name="refreshToken">The refresh token</param>
        /// <returns>True if both tokens are expired/missing, false otherwise</returns>
        public static bool IsTokenExpiredAndUnrefreshable(string? accessToken, string? refreshToken)
        {
            // If no access token, session is invalid
            if (string.IsNullOrEmpty(accessToken))
                return true;

            // Check if access token is expired
            if (!JwtTokenHelper.IsTokenExpired(accessToken))
                return false; // Access token still valid

            // Access token is expired, check if we can refresh it
            return string.IsNullOrEmpty(refreshToken) ||
                   JwtTokenHelper.IsTokenExpired(refreshToken);
        }
    }
}
