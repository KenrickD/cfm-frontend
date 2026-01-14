using cfm_frontend.Helpers;
using Microsoft.AspNetCore.Authentication;

namespace cfm_frontend.Middleware
{
    /// <summary>
    /// Middleware that intercepts requests BEFORE controllers execute to validate token expiration.
    /// This is the PRIMARY FIX for the bug where full page loads show empty data when tokens expire.
    ///
    /// How it works:
    /// 1. Runs after UseAuthentication() middleware
    /// 2. Checks if user is authenticated
    /// 3. If authenticated, validates both access_token and refresh_token
    /// 4. If BOTH tokens are expired:
    ///    - Clears session and auth cookie
    ///    - For AJAX requests: Returns 401 JSON (intercepted by global-session-handler.js)
    ///    - For full page requests: Redirects to login with return URL
    ///    - SHORT CIRCUITS the pipeline (controller never executes)
    /// 5. If tokens are valid or user is not authenticated, continues to next middleware
    ///
    /// IMPORTANT:
    /// - This middleware does NOT attempt to refresh tokens (that's AuthTokenHandler's job)
    /// - It only VALIDATES if tokens are expired and forces logout if both are gone
    /// - This prevents the bug where controllers execute with expired tokens and return empty views
    /// </summary>
    public class TokenExpirationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenExpirationMiddleware> _logger;

        public TokenExpirationMiddleware(RequestDelegate next, ILogger<TokenExpirationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only check authenticated users
            if (context.User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    // Retrieve tokens from authentication cookie
                    var accessToken = await context.GetTokenAsync("access_token");
                    var refreshToken = await context.GetTokenAsync("refresh_token");

                    // Check if both tokens are expired (session is unrecoverable)
                    if (AuthenticationHelper.IsTokenExpiredAndUnrefreshable(accessToken, refreshToken))
                    {
                        _logger.LogWarning(
                            "Both access and refresh tokens expired for user {User} on path {Path}. Forcing logout.",
                            context.User.Identity.Name ?? "Unknown",
                            context.Request.Path
                        );

                        // Clear all authentication data
                        await AuthenticationHelper.ClearAuthenticationAsync(context);

                        // Determine response based on request type
                        if (AuthenticationHelper.IsAjaxRequest(context.Request))
                        {
                            // AJAX request: Return 401 JSON
                            // This will be intercepted by global-session-handler.js
                            _logger.LogInformation("Returning 401 JSON for AJAX request to {Path}", context.Request.Path);

                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(new
                            {
                                error = "Session expired",
                                message = "Your session has expired. Please log in again."
                            });
                        }
                        else
                        {
                            // Full page request: Redirect to login with return URL
                            // This is the PRIMARY FIX for the bug
                            var returnUrl = context.Request.Path + context.Request.QueryString;
                            var loginUrl = $"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}&sessionExpired=true";

                            _logger.LogInformation(
                                "Redirecting full page request to login. Original URL: {ReturnUrl}",
                                returnUrl
                            );

                            context.Response.Redirect(loginUrl);
                        }

                        // SHORT CIRCUIT: Do not call next middleware (controller never executes)
                        return;
                    }

                    // Tokens are valid or user not authenticated, continue to next middleware
                }
                catch (Exception ex)
                {
                    // Log error but don't block the request
                    // Let it continue to controller/filter error handling
                    _logger.LogError(ex, "Error in TokenExpirationMiddleware for path {Path}", context.Request.Path);
                }
            }

            // Continue to next middleware (controller execution)
            await _next(context);
        }
    }

    /// <summary>
    /// Extension method for easy middleware registration in Program.cs
    /// </summary>
    public static class TokenExpirationMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenExpiration(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenExpirationMiddleware>();
        }
    }
}
