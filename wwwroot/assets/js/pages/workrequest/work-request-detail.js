/**
 * Work Request Detail Page JavaScript
 * Handles interactions and functionality for the work request detail view
 */

(function () {
    'use strict';

    // Initialize when document is ready
    $(document).ready(function () {
        initializePage();
    });

    function initializePage() {
        initializePopovers();
        initializeDownloadButton();
        initializeDeleteModal();
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
     */
    function downloadChangeHistoryAsExcel() {
        var workRequestId = window.workRequestId;

        if (!workRequestId) {
            showNotification('Work request ID not found', 'error', 'Error');
            return;
        }

        // Show notification that feature is coming
        showNotification('Excel export will be available when the backend API is ready', 'info', 'Info');
        console.log('Download Change History for Work Request ID:', workRequestId);
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
        $('#confirmDeleteBtn').on('click', function () {
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
     */
    function handleDeleteWorkRequest() {
        var workRequestId = window.workRequestId;
        var workRequestCode = window.workRequestCode;

        if (!workRequestId) {
            showNotification('Work request ID not found', 'error', 'Error');
            return;
        }

        // Show loading state on button
        var $btn = $('#confirmDeleteBtn');
        var originalText = $btn.html();
        $btn.html('<i class="ti ti-loader me-2"></i>Deleting...').prop('disabled', true);

        // Make API call to delete work request
        $.ajax({
            url: '/Helpdesk/DeleteWorkRequest/' + workRequestId,
            type: 'DELETE',
            headers: {
                'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (result) {
                if (result.success) {
                    showNotification('Work Request ' + workRequestCode + ' deleted successfully', 'success', 'Success');
                    $('#deleteModal').modal('hide');
                    // Redirect to list after short delay
                    setTimeout(function () {
                        window.location.href = '/Helpdesk/Index';
                    }, 1500);
                } else {
                    showNotification(result.message || 'Failed to delete work request', 'error', 'Error');
                    $btn.html(originalText).prop('disabled', false);
                }
            },
            error: function (xhr, status, error) {
                console.error('Delete error:', error);
                showNotification('An error occurred while deleting the work request', 'error', 'Error');
                $btn.html(originalText).prop('disabled', false);
            }
        });
    }

})();
