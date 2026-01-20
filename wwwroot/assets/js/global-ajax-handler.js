/**
 * Global AJAX Event Handlers
 * Intercepts all AJAX responses to provide consistent error handling.
 */
(function ($) {
    'use strict';

    $(document).ajaxSuccess(function (event, xhr, settings) {
        try {
            // Check if response is JSON and has specific success: false indicator
            if (xhr.responseJSON && xhr.responseJSON.success === false) {
                // Ensure there is a message to show
                if (xhr.responseJSON.message) {
                    showNotification(xhr.responseJSON.message, 'error');
                    console.warn('API Logic Error:', xhr.responseJSON.message);
                }
            }
        } catch (e) {
            console.error('Global AJAX handler error:', e);
        }
    });

})(jQuery);
