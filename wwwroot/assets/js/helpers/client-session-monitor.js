/**
 * Client Session Monitor
 * Monitors client context changes across browser tabs and alerts users
 * when the session client differs from page-load client
 *
 * Usage:
 *   const monitor = new ClientSessionMonitor({
 *       pageLoadClientId: clientContext.idClient,
 *       onMismatch: (sessionClient, pageLoadClient) => { ... }
 *   });
 *   monitor.start();
 */
(function (window) {
    'use strict';

    class ClientSessionMonitor {
        /**
         * Initialize the monitor
         * @param {Object} options - Configuration options
         * @param {number} options.pageLoadClientId - Client ID captured at page load (required)
         * @param {number} options.pageLoadCompanyId - Company ID captured at page load (optional)
         * @param {string} options.checkEndpoint - API endpoint to check session (default: /Helpdesk/CheckSessionClient)
         * @param {function} options.onMismatch - Callback when client mismatch detected (sessionClient, pageLoadClient)
         * @param {function} options.onSessionExpired - Callback when session expired
         * @param {function} options.onCheckError - Callback when check fails
         * @param {boolean} options.enableBanner - Show default warning banner (default: true)
         * @param {boolean} options.checkOnFocus - Check when window gains focus (default: true)
         * @param {boolean} options.checkOnVisibility - Check when tab becomes visible (default: true)
         */
        constructor(options = {}) {
            // Configuration with defaults
            this.options = {
                checkEndpoint: '/Helpdesk/CheckSessionClient',
                pageLoadClientId: null,
                pageLoadCompanyId: null,
                onMismatch: null,
                onSessionExpired: null,
                onCheckError: null,
                enableBanner: true,
                checkOnFocus: true,
                checkOnVisibility: true,
                ...options
            };

            this.isMonitoring = false;
            this.bannerVisible = false;

            // Bind event handlers to preserve context
            this._boundVisibilityHandler = this._handleVisibilityChange.bind(this);
            this._boundFocusHandler = this._handleFocus.bind(this);
        }

        /**
         * Start monitoring for client changes
         */
        start() {
            if (this.isMonitoring) {
                console.warn('ClientSessionMonitor: Already monitoring');
                return this;
            }

            if (!this.options.pageLoadClientId) {
                console.warn('ClientSessionMonitor: No pageLoadClientId provided, monitoring disabled');
                return this;
            }

            this.isMonitoring = true;

            // Set up event listeners
            if (this.options.checkOnVisibility) {
                document.addEventListener('visibilitychange', this._boundVisibilityHandler);
            }

            if (this.options.checkOnFocus) {
                window.addEventListener('focus', this._boundFocusHandler);
            }

            console.log('ClientSessionMonitor: Started monitoring client context');
            return this;
        }

        /**
         * Stop monitoring
         */
        stop() {
            if (!this.isMonitoring) {
                return this;
            }

            this.isMonitoring = false;
            document.removeEventListener('visibilitychange', this._boundVisibilityHandler);
            window.removeEventListener('focus', this._boundFocusHandler);
            this._removeBanner();

            console.log('ClientSessionMonitor: Stopped monitoring');
            return this;
        }

        /**
         * Handle visibility change event
         * @private
         */
        _handleVisibilityChange() {
            if (document.visibilityState === 'visible') {
                this.checkSession();
            }
        }

        /**
         * Handle window focus event
         * @private
         */
        _handleFocus() {
            this.checkSession();
        }

        /**
         * Check if session client matches page-load client
         * @returns {Promise} Promise that resolves when check is complete
         */
        checkSession() {
            if (!this.isMonitoring) {
                return Promise.resolve();
            }

            return $.ajax({
                url: this.options.checkEndpoint,
                method: 'GET',
                success: (response) => {
                    if (response.sessionExpired) {
                        this._handleSessionExpired(response);
                    } else if (response.success && response.idClient !== this.options.pageLoadClientId) {
                        this._handleMismatch(response.idClient);
                    } else if (response.success) {
                        // Match - remove banner if visible
                        this._removeBanner();
                    }
                },
                error: (xhr, status, error) => {
                    this._handleError(error);
                }
            });
        }

        /**
         * Handle client mismatch
         * @private
         */
        _handleMismatch(sessionClientId) {
            console.warn('ClientSessionMonitor: Client mismatch detected', {
                pageLoad: this.options.pageLoadClientId,
                session: sessionClientId
            });

            // Call custom callback if provided
            if (typeof this.options.onMismatch === 'function') {
                this.options.onMismatch(sessionClientId, this.options.pageLoadClientId);
            }

            // Show default banner if enabled
            if (this.options.enableBanner) {
                this._showBanner(sessionClientId);
            }
        }

        /**
         * Handle session expiration
         * @private
         */
        _handleSessionExpired(response) {
            console.warn('ClientSessionMonitor: Session expired');

            if (typeof this.options.onSessionExpired === 'function') {
                this.options.onSessionExpired(response);
            } else {
                // Default behavior: show notification and redirect
                if (typeof showNotification === 'function') {
                    showNotification(
                        'Your session has expired. Please log in again.',
                        'error',
                        'Session Expired'
                    );
                }
                setTimeout(() => {
                    window.location.href = '/Login/SignIn';
                }, 2000);
            }
        }

        /**
         * Handle check error
         * @private
         */
        _handleError(error) {
            if (typeof this.options.onCheckError === 'function') {
                this.options.onCheckError(error);
            } else {
                console.error('ClientSessionMonitor: Error checking session', error);
            }
        }

        /**
         * Show mismatch warning banner
         * @private
         */
        _showBanner(sessionClientId) {
            if (this.bannerVisible) {
                return;
            }

            const banner = $('<div>', {
                id: 'client-mismatch-banner',
                class: 'alert alert-warning alert-dismissible fade show',
                role: 'alert',
                css: {
                    position: 'fixed',
                    top: '0',
                    left: '0',
                    right: '0',
                    zIndex: '9999',
                    margin: '0',
                    borderRadius: '0',
                    textAlign: 'center',
                    boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
                },
                html: `
                    <strong><i class="feather icon-alert-triangle"></i> Warning:</strong>
                    Your client context has changed in another tab or window.
                    The data on this page may be outdated. Please refresh to continue.
                    <button type="button" class="btn btn-sm btn-warning ms-2" onclick="location.reload()">
                        <i class="feather icon-refresh-cw"></i> Refresh Now
                    </button>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                `
            });

            $('body').prepend(banner);
            this.bannerVisible = true;

            // Remove banner when dismissed
            banner.on('closed.bs.alert', () => {
                this.bannerVisible = false;
            });
        }

        /**
         * Remove banner if visible
         * @private
         */
        _removeBanner() {
            if (this.bannerVisible) {
                const $banner = $('#client-mismatch-banner');
                if ($banner.length) {
                    $banner.alert('close');
                }
                this.bannerVisible = false;
            }
        }

        /**
         * Manually trigger a session check
         * Useful for testing or manual verification
         */
        forceCheck() {
            return this.checkSession();
        }
    }

    // Expose to global scope
    window.ClientSessionMonitor = ClientSessionMonitor;

})(window);
