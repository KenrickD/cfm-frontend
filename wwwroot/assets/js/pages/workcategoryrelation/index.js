/**
 * Work Category Relation - List Page
 * Handles search and delete operations
 */

(function ($) {
    'use strict';

    // Configuration
    const CONFIG = {
        apiEndpoints: {
            delete: MvcEndpoints.WorkCategoryRelation.Delete
        }
    };

    // Client context for multi-tab session safety
    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; }
    };

    // State management
    const state = {
        deleteId: null,
        deleteName: ''
    };

    /**
     * Initialize the module
     */
    function init() {
        bindEvents();
        console.log('Work Category Relation list page initialized');
    }

    /**
     * Bind event handlers
     */
    function bindEvents() {
        // Search with Enter key
        $('#searchInput').on('keypress', function (e) {
            if (e.which === 13) {
                handleSearch();
            }
        });

        // Search button click
        $('#searchBtn').on('click', handleSearch);

        // Delete confirmation
        $('#confirmDeleteBtn').on('click', confirmDelete);
    }

    /**
     * Handle search - navigate with query param
     */
    function handleSearch() {
        const searchValue = $('#searchInput').val().trim();
        const url = new URL(window.location.href);

        if (searchValue) {
            url.searchParams.set('search', searchValue);
        } else {
            url.searchParams.delete('search');
        }
        url.searchParams.set('page', '1');

        window.location.href = url.toString();
    }

    /**
     * Delete relation - make function global
     */
    window.deleteRelation = function (id, name) {
        state.deleteId = id;
        state.deleteName = name;
        $('#deleteItemName').text(name);
        const modal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
        modal.show();
    };

    /**
     * Confirm delete
     */
    function confirmDelete() {
        if (!state.deleteId) return;

        const id = state.deleteId;

        $('#confirmDeleteBtn').prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-1"></span>Deleting...');

        // Get CSRF token
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: `${CONFIG.apiEndpoints.delete}?id=${id}`,
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': token
            },
            success: function (response) {
                if (response.success) {
                    showNotification('Work category relation deleted successfully', 'success');
                    bootstrap.Modal.getInstance(document.getElementById('deleteConfirmModal')).hide();
                    state.deleteId = null;
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to delete relation', 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error deleting relation:', error);
                let errorMessage = 'An error occurred while deleting the relation.';

                if (xhr.responseJSON) {
                    const apiResponse = xhr.responseJSON;
                    if (apiResponse.errors && apiResponse.errors.length > 0) {
                        errorMessage = apiResponse.errors.join(', ');
                    } else if (apiResponse.message) {
                        errorMessage = apiResponse.message;
                    }
                }

                showNotification(errorMessage, 'error');
            },
            complete: function () {
                $('#confirmDeleteBtn').prop('disabled', false).html('Delete');
            }
        });
    }

    // Initialize when document is ready
    $(document).ready(function () {
        init();
    });

})(jQuery);
