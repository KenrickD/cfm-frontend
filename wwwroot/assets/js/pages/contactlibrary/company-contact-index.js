/**
 * Company Contact Index Page
 * Handles contact list filtering, search, and pagination
 */

(function ($) {
    'use strict';

    // Configuration
    const CONFIG = {
        endpoints: MvcEndpoints.CompanyContact
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
        initializePopovers();
        initializeDeleteModal();
        restoreFilterState();
        console.log('Company Contact Index page initialized');
    }

    /**
     * Initialize popovers
     */
    function initializePopovers() {
        $('[data-bs-toggle="popover"]').popover({
            container: 'body',
            placement: 'top',
            trigger: 'hover'
        });
    }

    /**
     * Initialize delete modal animation
     */
    function initializeDeleteModal() {
        var deleteModal = $('#deleteModal');
        deleteModal.on('show.bs.modal', function (event) {
            var button = $(event.relatedTarget);
            var recipient = button.data('pc-animate');
            if (recipient) {
                deleteModal.addClass('anim-' + recipient);
                if (recipient == 'let-me-in' || recipient == 'make-way' || recipient == 'slip-from-top') {
                    $('body').addClass('anim-' + recipient);
                }
            }
        });

        deleteModal.on('hidden.bs.modal', function () {
            removeClassByPrefix(deleteModal, 'anim-');
            removeClassByPrefix($('body'), 'anim-');
        });

        function removeClassByPrefix(node, prefix) {
            node.removeClass(function (index, className) {
                return (className.match(new RegExp('\\b' + prefix + '\\S+', 'g')) || []).join(' ');
            });
        }
    }

    /**
     * Bind event handlers
     */
    function bindEvents() {
        // Search functionality
        $('#searchButton').on('click', handleSearch);
        $('#mainSearchInput').on('keypress', function (e) {
            if (e.which === 13) {
                handleSearch();
            }
        });
        $('#clearSearchButton').on('click', clearSearch);

        // Filter modal
        $('#clearFiltersBtn').on('click', clearFilters);
        $('#applyFiltersBtn').on('click', applyFilters);

        // Delete confirmation
        $('#confirmDeleteBtn').on('click', confirmDelete);
    }

    /**
     * Handle search
     */
    function handleSearch() {
        var searchTerm = $('#mainSearchInput').val();
        var currentUrl = new URL(window.location.href);

        if (searchTerm && searchTerm.trim() !== '') {
            currentUrl.searchParams.set('search', searchTerm.trim());
        } else {
            currentUrl.searchParams.delete('search');
        }

        // Reset to page 1 when searching
        currentUrl.searchParams.delete('page');

        window.location.href = currentUrl.toString();
    }

    /**
     * Clear search
     */
    function clearSearch() {
        $('#mainSearchInput').val('');
        var currentUrl = new URL(window.location.href);
        currentUrl.searchParams.delete('search');
        currentUrl.searchParams.delete('page');
        window.location.href = currentUrl.toString();
    }

    /**
     * Clear all filters
     */
    function clearFilters() {
        $('#filterForm')[0].reset();
        $('#filterForm input[type="checkbox"]').prop('checked', false);

        // Redirect to clean URL (preserving search if exists)
        var searchTerm = $('#mainSearchInput').val();
        var url = new URL(window.location.origin + '/CompanyContact/Index');
        if (searchTerm && searchTerm.trim() !== '') {
            url.searchParams.set('search', searchTerm.trim());
        }
        window.location.href = url.toString();
    }

    /**
     * Apply filters
     */
    function applyFilters() {
        // Collect all filter values
        var filters = {
            departments: $('input[name="departments"]:checked').map(function() { return this.value; }).get(),
            showDeleted: $('#showDeleted').is(':checked')
        };

        // Build query string from filters
        var url = new URL(window.location.origin + '/CompanyContact/Index');

        // Add multi-value filters (arrays)
        if (filters.departments.length > 0) {
            url.searchParams.set('departments', filters.departments.join(','));
        }
        if (filters.showDeleted) {
            url.searchParams.set('showDeleted', 'true');
        }

        // Preserve search term if exists
        var searchTerm = $('#mainSearchInput').val();
        if (searchTerm && searchTerm.trim() !== '') {
            url.searchParams.set('search', searchTerm.trim());
        }

        // Reset to page 1 when filters change
        url.searchParams.set('page', '1');

        // Close modal and navigate
        bootstrap.Modal.getInstance(document.getElementById('filterModal')).hide();
        window.location.href = url.toString();
    }

    /**
     * Restore filter state from URL query string
     */
    function restoreFilterState() {
        const urlParams = new URLSearchParams(window.location.search);

        // Restore department checkboxes
        const departments = urlParams.get('departments');
        if (departments) {
            departments.split(',').forEach(id => {
                $(`input[name="departments"][value="${id}"]`).prop('checked', true);
            });
        }

        // Restore show deleted checkbox
        const showDeleted = urlParams.get('showDeleted');
        if (showDeleted === 'true') {
            $('#showDeleted').prop('checked', true);
        }
    }

    /**
     * Delete contact - make function global
     */
    window.deleteContact = function (id, name) {
        state.deleteId = id;
        state.deleteName = name;
        $('#deleteContactName').text(name);
        const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
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
            url: `${CONFIG.endpoints.Delete}?id=${id}&cid=${clientContext.idClient}`,
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': token
            },
            success: function (response) {
                if (response.success) {
                    showNotification('Contact deleted successfully', 'success');
                    bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
                    state.deleteId = null;
                    // Reload page to refresh list
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to delete contact', 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error deleting contact:', error);

                let errorMessage = 'Error deleting contact. Please try again.';

                if (xhr.responseText) {
                    try {
                        const response = JSON.parse(xhr.responseText);
                        if (response.errors && Array.isArray(response.errors) && response.errors.length > 0) {
                            errorMessage = response.errors.join(', ');
                        } else if (response.message) {
                            errorMessage = response.message;
                        }
                    } catch (e) {
                        console.error('Error parsing response:', xhr.responseText);
                    }
                }

                showNotification(errorMessage, 'error');
            },
            complete: function () {
                $('#confirmDeleteBtn').prop('disabled', false)
                    .html('<i class="ti ti-trash me-2"></i>Delete');
            }
        });
    }

    // Initialize when document is ready
    $(document).ready(function () {
        init();
    });

})(jQuery);
