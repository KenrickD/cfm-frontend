using cfm_frontend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace cfm_frontend.Filters
{
    /// <summary>
    /// Global exception filter that catches authentication-related exceptions.
    /// This is the SAFETY NET (Layer 3) that catches any 401 errors that slip through middleware.
    ///
    /// How it works:
    /// 1. Registered globally in Program.cs via AddControllersWithViews()
    /// 2. Runs when an exception is thrown during controller action execution
    /// 3. Catches HttpRequestException with 401 status (e.g., from backend API calls)
    /// 4. Clears session and auth cookie
    /// 5. Returns redirect or JSON based on request type
    ///
    /// Example scenarios:
    /// - Backend API returns 401 during controller action
    /// - HttpClient "BackendAPI" throws HttpRequestException with StatusCode 401
    /// - Any other auth-related exception during request processing
    ///
    /// This complements TokenExpirationMiddleware by catching exceptions that occur
    /// AFTER the middleware has run (during controller execution).
    /// </summary>
    public class AuthenticationExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<AuthenticationExceptionFilter> _logger;

        public AuthenticationExceptionFilter(ILogger<AuthenticationExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            // Check if exception is authentication-related
            if (IsAuthenticationException(context.Exception))
            {
                _logger.LogWarning(
                    context.Exception,
                    "Authentication exception caught for user {User} on path {Path}",
                    context.HttpContext.User.Identity?.Name ?? "Unknown",
                    context.HttpContext.Request.Path
                );

                // Clear authentication data
                AuthenticationHelper.ClearAuthenticationAsync(context.HttpContext).Wait();

                // Determine response based on request type
                if (AuthenticationHelper.IsAjaxRequest(context.HttpContext.Request))
                {
                    // AJAX request: Return 401 JSON
                    _logger.LogInformation("Returning 401 JSON response for AJAX request");

                    context.Result = new JsonResult(new
                    {
                        success = false,
                        error = "Session expired",
                        message = "Your session has expired. Please log in again."
                    })
                    {
                        StatusCode = 401
                    };
                }
                else
                {
                    // Full page request: Redirect to login with return URL
                    var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;

                    _logger.LogInformation(
                        "Redirecting to login page with return URL: {ReturnUrl}",
                        returnUrl
                    );

                    context.Result = new RedirectToActionResult(
                        "Index",
                        "Login",
                        new { returnUrl = returnUrl, sessionExpired = true }
                    );
                }

                // Mark exception as handled so it doesn't propagate further
                context.ExceptionHandled = true;
            }
        }

        /// <summary>
        /// Determines if an exception is authentication-related and should trigger logout.
        /// </summary>
        private bool IsAuthenticationException(Exception exception)
        {
            // Check for HttpRequestException with 401 status
            if (exception is HttpRequestException httpEx)
            {
                return httpEx.StatusCode == HttpStatusCode.Unauthorized;
            }

            // Check for UnauthorizedAccessException
            if (exception is UnauthorizedAccessException)
            {
                return true;
            }

            // Add more auth-related exception types here if needed
            return false;
        }
    }
}
