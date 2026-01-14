/**
 * Global Session Expiration Handler
 * Intercepts 401 responses from AJAX calls and redirects to login
 *
 * IMPORTANT: This ONLY triggers when BOTH access token AND refresh token are expired.
 * When only the access token expires, AuthTokenHandler automatically refreshes it
 * and the AJAX call succeeds (no 401 reaches the client).
 */
(function() {
    'use strict';

    // Track if we're already redirecting to prevent multiple toasts
    let isRedirecting = false;

    $(document).ajaxError(function(event, xhr, settings, thrownError) {
        // Only handle 401 Unauthorized from AuthTokenHandler session expiration
        if (xhr.status === 401 && !isRedirecting) {
            // Parse response to check if it's from AuthTokenHandler (session expired)
            let isSessionExpired = false;
            let message = 'Your session has expired. Please log in again.';

            try {
                const response = JSON.parse(xhr.responseText);
                // AuthTokenHandler returns: {"error":"Session expired","message":"..."}
                if (response && response.error === 'Session expired') {
                    isSessionExpired = true;
                    if (response.message) {
                        message = response.message;
                    }
                }
            } catch (e) {
                // Not JSON or parse error - ignore this 401 (might be from backend API)
                return;
            }

            // Only redirect if this is confirmed session expiration from AuthTokenHandler
            if (isSessionExpired) {
                isRedirecting = true;

                // Show toast notification
                showNotification(message, 'warning', 'Session Expired', {
                    timeOut: 3000,
                    onHidden: function() {
                        // Redirect after toast hides
                        window.location.href = '/Login/Index';
                    }
                });

                // Fallback redirect in case onHidden doesn't fire
                setTimeout(function() {
                    window.location.href = '/Login/Index';
                }, 3500);
            }
        }
    });
})();
