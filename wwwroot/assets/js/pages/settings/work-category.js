/**
 * Work Category Settings Page - Inline Editing with Server-Side Pagination
 * Handles CRUD operations for work categories with inline editing
 */

(function ($) {
    'use strict';

    // Configuration
    const CONFIG = {
        apiEndpoints: {
            list: MvcEndpoints.Helpdesk.Settings.WorkCategory.List,
            create: MvcEndpoints.Helpdesk.Settings.WorkCategory.Create,
            update: MvcEndpoints.Helpdesk.Settings.WorkCategory.Update,
            delete: MvcEndpoints.Helpdesk.Settings.WorkCategory.Delete
        }
    };

    // Client context for multi-tab session safety
    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; }
    };

    // State management
    const state = {
        categories: [],
        editingId: null,
        deleteId: null,
        deleteName: ''
    };

    /**
     * Initialize the module
     */
    function init() {
        // Initialize categories from server-rendered data
        initCategoriesFromDOM();
        bindEvents();
        console.log('Work Category page initialized');
    }

    /**
     * Initialize categories array from DOM for inline editing
     */
    function initCategoriesFromDOM() {
        state.categories = [];
        $('.category-row[data-id]').each(function () {
            const $row = $(this);
            state.categories.push({
                idType: parseInt($row.data('id')),
                typeName: $row.find('.category-description').text().trim()
            });
        });
    }

    /**
     * Bind event handlers
     */
    function bindEvents() {
        // Show add form
        $('#showAddFormBtn').on('click', showAddForm);

        // Save new category
        $('#saveNewBtn').on('click', saveNewCategory);

        // Cancel new category
        $('#cancelNewBtn').on('click', hideAddForm);

        // Enter key in add form
        $('#newCategoryName').on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                saveNewCategory();
            }
        });

        // Escape key in add form
        $('#newCategoryName').on('keydown', function (e) {
            if (e.which === 27) {
                hideAddForm();
            }
        });

        // Search with URL navigation (server-side)
        $('#searchInput').on('keypress', function (e) {
            if (e.which === 13) {
                handleSearch();
            }
        });

        // Search button click
        $('#searchBtn').on('click', handleSearch);

        // Clear search button
        $('#clearSearchBtn').on('click', clearSearch);

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
     * Clear search - navigate without search param
     */
    function clearSearch() {
        const url = new URL(window.location.href);
        url.searchParams.delete('search');
        url.searchParams.set('page', '1');
        window.location.href = url.toString();
    }

    /**
     * Show add form
     */
    function showAddForm() {
        if (state.editingId) {
            cancelEdit();
        }

        $('#showAddFormBtn').hide();
        $('#addNewForm').slideDown();
        $('#newCategoryName').focus();
        $('#addValidationError').text('').removeClass('d-block');
    }

    /**
     * Hide add form
     */
    function hideAddForm() {
        $('#addNewForm').slideUp();
        $('#showAddFormBtn').show();
        $('#newCategoryName').val('').removeClass('is-invalid');
        $('#addValidationError').text('').removeClass('d-block');
    }

    /**
     * Save new category
     */
    function saveNewCategory() {
        const name = $('#newCategoryName').val().trim();

        if (!name) {
            $('#newCategoryName').addClass('is-invalid');
            $('#addValidationError').text('Category name is required').addClass('d-block');
            return;
        }

        // Check for duplicates in current page
        if (state.categories.some(c => c.typeName.toLowerCase() === name.toLowerCase())) {
            $('#newCategoryName').addClass('is-invalid');
            $('#addValidationError').text('This category already exists').addClass('d-block');
            return;
        }

        $('#newCategoryName').removeClass('is-invalid');
        $('#addValidationError').text('').removeClass('d-block');

        $('#saveNewBtn').prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Saving...');

        // Get CSRF token
        const token = $('input[name="__RequestVerificationToken"]').val();

        // New payload structure matching WorkCategoryPayloadDto
        $.ajax({
            url: CONFIG.apiEndpoints.create,
            method: 'POST',
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': token
            },
            data: JSON.stringify({
                text: name,
                idType: 0,
                idClient: clientContext.idClient
            }),
            success: function (response) {
                if (response.success) {
                    showNotification('Work category created successfully', 'success');
                    hideAddForm();
                    // Reload page to refresh server-rendered list
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to create category', 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error creating category:', error);
                showNotification('Error creating category. Please try again.', 'error');
            },
            complete: function () {
                $('#saveNewBtn').prop('disabled', false).html('<i class="ti ti-check me-1"></i>Save');
            }
        });
    }

    /**
     * Edit category - make function global
     */
    window.editCategory = function (id) {
        if (state.editingId && state.editingId !== id) {
            cancelEdit();
        }

        const category = state.categories.find(c => c.idType === id);
        if (!category) return;

        state.editingId = id;

        const $row = $(`.category-row[data-id="${id}"]`);
        $row.addClass('editing');

        const currentName = category.typeName;
        $row.html(`
            <div class="category-input">
                <input type="text"
                       class="form-control"
                       id="edit-${id}"
                       value="${escapeHtml(currentName)}"
                       maxlength="200">
                <div class="invalid-feedback" id="edit-error-${id}"></div>
            </div>
            <div class="category-actions">
                <button type="button"
                        class="btn btn-success btn-action"
                        onclick="saveCategory(${id})">
                    <i class="ti ti-check me-1"></i>
                    Save
                </button>
                <button type="button"
                        class="btn btn-secondary btn-action"
                        onclick="cancelEdit()">
                    <i class="ti ti-x me-1"></i>
                    Cancel
                </button>
            </div>
        `);

        setTimeout(() => {
            $(`#edit-${id}`).focus().select();
        }, 100);

        $(`#edit-${id}`).on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                saveCategory(id);
            }
        });

        $(`#edit-${id}`).on('keydown', function (e) {
            if (e.which === 27) {
                cancelEdit();
            }
        });
    };

    /**
     * Save category - make function global
     */
    window.saveCategory = function (id) {
        const name = $(`#edit-${id}`).val().trim();
        const category = state.categories.find(c => c.idType === id);

        if (!category) return;

        if (!name) {
            $(`#edit-${id}`).addClass('is-invalid');
            $(`#edit-error-${id}`).text('Category name is required').addClass('d-block');
            return;
        }

        if (state.categories.some(c => c.idType !== id && c.typeName.toLowerCase() === name.toLowerCase())) {
            $(`#edit-${id}`).addClass('is-invalid');
            $(`#edit-error-${id}`).text('This category already exists').addClass('d-block');
            return;
        }

        if (name === category.typeName) {
            cancelEdit();
            return;
        }

        $(`#edit-${id}`).removeClass('is-invalid');
        $(`#edit-error-${id}`).text('').removeClass('d-block');

        // Get CSRF token
        const token = $('input[name="__RequestVerificationToken"]').val();

        // New payload structure matching WorkCategoryPayloadDto
        $.ajax({
            url: CONFIG.apiEndpoints.update,
            method: 'PUT',
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': token
            },
            data: JSON.stringify({
                idType: id,
                text: name,
                idClient: clientContext.idClient
            }),
            success: function (response) {
                if (response.success) {
                    showNotification('Work category updated successfully', 'success');
                    state.editingId = null;
                    // Reload to get fresh data
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to update category', 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error updating category:', error);
                showNotification('Error updating category. Please try again.', 'error');
            }
        });
    };

    /**
     * Cancel edit - make function global
     */
    window.cancelEdit = function () {
        if (!state.editingId) return;

        const category = state.categories.find(c => c.idType === state.editingId);
        if (!category) return;

        const $row = $(`.category-row[data-id="${state.editingId}"]`);
        $row.removeClass('editing');
        $row.html(`
            <div class="category-description">${escapeHtml(category.typeName)}</div>
            <div class="category-actions">
                <button type="button"
                        class="btn btn-outline-primary btn-action"
                        onclick="editCategory(${category.idType})">
                    <i class="ti ti-edit"></i>
                </button>
                <button type="button"
                        class="btn btn-outline-danger btn-action"
                        onclick="deleteCategory(${category.idType}, '${escapeHtml(category.typeName).replace(/'/g, "\\'")}')">
                    <i class="ti ti-trash"></i>
                </button>
            </div>
        `);

        state.editingId = null;
    };

    /**
     * Delete category - make function global
     */
    window.deleteCategory = function (id, name) {
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

        $('#confirmDeleteBtn').prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Deleting...');

        // Get CSRF token
        const token = $('input[name="__RequestVerificationToken"]').val();

        // Delete endpoint uses query param for ID
        $.ajax({
            url: `${CONFIG.apiEndpoints.delete}?id=${id}`,
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': token
            },
            success: function (response) {
                if (response.success) {
                    showNotification('Work category deleted successfully', 'success');
                    bootstrap.Modal.getInstance(document.getElementById('deleteConfirmModal')).hide();
                    state.deleteId = null;
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to delete category', 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error deleting category:', error);
                showNotification('Error deleting category. Please try again.', 'error');
            },
            complete: function () {
                $('#confirmDeleteBtn').prop('disabled', false).html('Delete');
            }
        });
    }

    /**
     * Utility: Escape HTML
     */
    function escapeHtml(text) {
        if (!text) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return String(text).replace(/[&<>"']/g, m => map[m]);
    }

    // Initialize when document is ready
    $(document).ready(function () {
        init();
    });

})(jQuery);
