/**
 * Work Request Detail Page JavaScript
 * Handles interactions and functionality for the work request detail view
 */

(function () {
    'use strict';

    // Configuration
    const CONFIG = {
        apiEndpoints: {
            locations: MvcEndpoints.Helpdesk.Extended.GetLocationsByClient,
            workCategories: MvcEndpoints.Helpdesk.Extended.GetAllWorkCategory,
            otherCategories: MvcEndpoints.Helpdesk.Extended.GetAllOtherCategory,
            serviceProviders: MvcEndpoints.Helpdesk.Extended.GetServiceProvidersByClient
        }
    };

    // Initialize when document is ready
    $(document).ready(function () {
        initializePage();
    });

    async function initializePage() {
        try {
            await loadDropdownData();
            initializePopovers();
            initializeDownloadButton();
            initializeDeleteModal();
        } catch (error) {
            console.error('Error initializing page:', error);
            showNotification('Failed to initialize page', 'error', 'Error');
        }
    }

    /**
     * Load all dropdown data in parallel
     */
    async function loadDropdownData() {
        try {
            await Promise.all([
                loadLocations(),
                loadWorkCategories(),
                loadOtherCategories(),
                loadServiceProviders()
            ]);
        } catch (error) {
            console.error('Error loading dropdown data:', error);
            showNotification('Failed to load dropdown data', 'error', 'Error');
        }
    }

    /**
     * Load locations dropdown
     */
    async function loadLocations() {
        try {
            const response = await fetch(CONFIG.apiEndpoints.locations);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            if (data.success && data.data) {
                populateDropdown('#locationSelect', data.data, 'value', 'text');
            }
        } catch (error) {
            console.error('Error loading locations:', error);
        }
    }

    /**
     * Load work categories dropdown
     */
    async function loadWorkCategories() {
        try {
            const response = await fetch(CONFIG.apiEndpoints.workCategories);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            if (data.success && data.data) {
                populateDropdown('#workCategorySelect', data.data, 'value', 'text');
            }
        } catch (error) {
            console.error('Error loading work categories:', error);
        }
    }

    /**
     * Load other categories dropdown
     */
    async function loadOtherCategories() {
        try {
            const response = await fetch(CONFIG.apiEndpoints.otherCategories);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            if (data.success && data.data) {
                populateDropdown('#otherCategorySelect', data.data, 'value', 'text');
            }
        } catch (error) {
            console.error('Error loading other categories:', error);
        }
    }

    /**
     * Load service providers dropdown
     */
    async function loadServiceProviders() {
        try {
            const response = await fetch(CONFIG.apiEndpoints.serviceProviders);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            if (data.success && data.data) {
                populateDropdown('#serviceProviderSelect', data.data, 'value', 'text');
            }
        } catch (error) {
            console.error('Error loading service providers:', error);
        }
    }

    /**
     * Populate dropdown with data
     */
    function populateDropdown(selector, data, valueField, textField) {
        const $select = $(selector);
        if ($select.length === 0) return;

        // Keep the first option (placeholder)
        const $firstOption = $select.find('option:first');

        // Clear existing options except first
        $select.find('option:not(:first)').remove();

        // Add new options
        data.forEach(item => {
            const option = new Option(item[textField], item[valueField], false, false);
            $select.append(option);
        });
    }

    /**
     * Initialize Bootstrap popovers for tooltips
     */
    function initializePopovers() {
        $('[data-bs-toggle="popover"]').popover({
            container: 'body',
            placement: 'top',
            trigger: 'hover'
        });
    }

    /**
     * Initialize download change history button
     */
    function initializeDownloadButton() {
        $('#downloadChangeHistoryBtn').on('click', function () {
            downloadChangeHistoryAsExcel();
        });
    }

    /**
     * Download change history table as Excel file
     * TODO: Implement actual Excel export when backend API is ready
     */
    function downloadChangeHistoryAsExcel() {
        // For now, just show a message
        console.log('Download Change History as Excel - Feature coming soon');

        // TODO: When backend is ready, make API call to generate Excel
        // Example:
        // const workRequestId = getWorkRequestIdFromUrl();
        // window.location.href = `/api/workrequest/${workRequestId}/changehistory/export`;

        // Show temporary message
        alert('Excel export functionality will be available once the backend API is implemented.');
    }

    /**
     * Initialize delete modal animations and functionality
     */
    function initializeDeleteModal() {
        var deleteModal = $('#deleteModal');

        deleteModal.on('show.bs.modal', function (event) {
            var button = $(event.relatedTarget);
            var recipient = button.data('pc-animate');

            if (recipient) {
                deleteModal.addClass('anim-' + recipient);
                if (recipient === 'let-me-in' || recipient === 'make-way' || recipient === 'slip-from-top') {
                    $('body').addClass('anim-' + recipient);
                }
            }
        });

        deleteModal.on('hidden.bs.modal', function () {
            removeClassByPrefix(deleteModal, 'anim-');
            removeClassByPrefix($('body'), 'anim-');
        });

        // Handle delete button click
        $('#deleteModal .btn-danger').on('click', function () {
            handleDeleteWorkRequest();
        });
    }

    /**
     * Remove classes with a specific prefix from an element
     */
    function removeClassByPrefix(node, prefix) {
        node.removeClass(function (index, className) {
            return (className.match(new RegExp('\\b' + prefix + '\\S+', 'g')) || []).join(' ');
        });
    }

    /**
     * Handle work request deletion
     * TODO: Implement actual delete API call when backend is ready
     */
    function handleDeleteWorkRequest() {
        // TODO: Get work request ID from URL or data attribute
        // const workRequestId = getWorkRequestIdFromUrl();

        console.log('Delete Work Request - Feature coming soon');

        // TODO: When backend is ready, make API call to delete work request
        // Example:
        // $.ajax({
        //     url: `/api/workrequest/${workRequestId}`,
        //     type: 'DELETE',
        //     success: function(result) {
        //         $('#deleteModal').modal('hide');
        //         window.location.href = '/Helpdesk/Index';
        //     },
        //     error: function(xhr, status, error) {
        //         alert('Failed to delete work request: ' + error);
        //     }
        // });

        // For now, just close the modal
        $('#deleteModal').modal('hide');
        alert('Delete functionality will be available once the backend API is implemented.');
    }

    /**
     * Get work request ID from current URL
     */
    function getWorkRequestIdFromUrl() {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get('id');
    }

})();
