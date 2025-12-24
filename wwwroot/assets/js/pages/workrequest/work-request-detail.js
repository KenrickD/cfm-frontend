/**
 * Work Request Detail Page JavaScript
 * Handles interactions and functionality for the work request detail view
 */

(function () {
    'use strict';

    // Initialize when document is ready
    $(document).ready(function () {
        initializePopovers();
        initializeDownloadButton();
        initializeDeleteModal();
    });

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
     * @param {jQuery} node - The jQuery element
     * @param {string} prefix - The class prefix to remove
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
     * @returns {string|null} The work request ID or null if not found
     */
    function getWorkRequestIdFromUrl() {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get('id');
    }

})();
