/**
 * Work Category Settings Page - Inline Editing
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

    // State management
    const state = {
        categories: [],
        filteredCategories: [],
        editingId: null,
        deleteId: null
    };

    /**
     * Initialize the module
     */
    function init() {
        bindEvents();
        loadCategories();
        console.log('Work Category page initialized');
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

        // Search
        $('#searchInput').on('keyup', debounce(handleSearch, 300));

        // Delete confirmation
        $('#confirmDeleteBtn').on('click', confirmDelete);
    }

    /**
     * Load categories from API
     */
    function loadCategories() {
        $.ajax({
            url: CONFIG.apiEndpoints.list,
            method: 'GET',
            success: function (response) {
                if (response.success) {
                    state.categories = response.data || [];
                    state.filteredCategories = [...state.categories];
                    renderCategories();
                    updateTotalCount();
                } else {
                    showNotification(response.message || 'Failed to load work categories', 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading categories:', error);
                showNotification('Error loading work categories. Please refresh the page.', 'error');
            }
        });
    }

    /**
     * Render categories list
     */
    function renderCategories() {
        const $tbody = $('#categoryTableBody');
        const $emptyState = $('#emptyState');

        if (state.filteredCategories.length === 0) {
            $tbody.html('');
            $emptyState.show();
            return;
        }

        $emptyState.hide();
        $tbody.html('');

        state.filteredCategories.forEach(category => {
            const row = createCategoryRow(category);
            $tbody.append(row);
        });
    }

    /**
     * Create category row HTML
     */
    function createCategoryRow(category) {
        const isEditing = state.editingId === category.id;

        if (isEditing) {
            return `
                <div class="category-row editing" data-id="${category.id}">
                    <div class="category-input">
                        <input type="text"
                               class="form-control"
                               id="edit-${category.id}"
                               value="${escapeHtml(category.name)}"
                               maxlength="200">
                        <div class="invalid-feedback" id="edit-error-${category.id}"></div>
                    </div>
                    <div class="category-actions">
                        <button type="button"
                                class="btn btn-save btn-action"
                                onclick="saveCategory(${category.id})">
                            <i class="ti ti-check me-1"></i>
                            Save
                        </button>
                        <button type="button"
                                class="btn btn-cancel btn-action"
                                onclick="cancelEdit()">
                            <i class="ti ti-x me-1"></i>
                            Cancel
                        </button>
                    </div>
                </div>
            `;
        }

        return `
            <div class="category-row" data-id="${category.id}">
                <div class="category-description">${escapeHtml(category.name)}</div>
                <div class="category-actions">
                    <button type="button"
                            class="btn btn-edit btn-action"
                            onclick="editCategory(${category.id})">
                        <i class="ti ti-edit"></i>
                    </button>
                    <button type="button"
                            class="btn btn-delete btn-action"
                            onclick="deleteCategory(${category.id})">
                        <i class="ti ti-trash"></i>
                    </button>
                </div>
            </div>
        `;
    }

    /**
     * Show add form
     */
    function showAddForm() {
        // Cancel any ongoing edit
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

        // Validation
        if (!name) {
            $('#newCategoryName').addClass('is-invalid');
            $('#addValidationError').text('Category name is required').addClass('d-block');
            return;
        }

        // Check for duplicates
        if (state.categories.some(c => c.name.toLowerCase() === name.toLowerCase())) {
            $('#newCategoryName').addClass('is-invalid');
            $('#addValidationError').text('This category already exists').addClass('d-block');
            return;
        }

        // Clear validation
        $('#newCategoryName').removeClass('is-invalid');
        $('#addValidationError').text('').removeClass('d-block');

        // Disable button
        $('#saveNewBtn').prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Saving...');

        // Send to API
        $.ajax({
            url: CONFIG.apiEndpoints.create,
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ name: name }),
            success: function (response) {
                if (response.success) {
                    showNotification('Work category created successfully', 'success');
                    hideAddForm();
                    loadCategories(); // Reload list
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
        // Cancel any ongoing edit
        if (state.editingId && state.editingId !== id) {
            cancelEdit();
        }

        state.editingId = id;
        renderCategories();

        // Focus on input
        setTimeout(() => {
            $(`#edit-${id}`).focus().select();
        }, 100);

        // Bind enter key
        $(`#edit-${id}`).on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                saveCategory(id);
            }
        });

        // Bind escape key
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
        const category = state.categories.find(c => c.id === id);

        if (!category) return;

        // Validation
        if (!name) {
            $(`#edit-${id}`).addClass('is-invalid');
            $(`#edit-error-${id}`).text('Category name is required').addClass('d-block');
            return;
        }

        // Check for duplicates (excluding current)
        if (state.categories.some(c => c.id !== id && c.name.toLowerCase() === name.toLowerCase())) {
            $(`#edit-${id}`).addClass('is-invalid');
            $(`#edit-error-${id}`).text('This category already exists').addClass('d-block');
            return;
        }

        // No changes
        if (name === category.name) {
            cancelEdit();
            return;
        }

        // Clear validation
        $(`#edit-${id}`).removeClass('is-invalid');
        $(`#edit-error-${id}`).text('').removeClass('d-block');

        // Send to API
        $.ajax({
            url: CONFIG.apiEndpoints.update,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ id: id, name: name }),
            success: function (response) {
                if (response.success) {
                    showNotification('Work category updated successfully', 'success');
                    state.editingId = null;
                    loadCategories(); // Reload list
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
        state.editingId = null;
        renderCategories();
    };

    /**
     * Delete category - make function global
     */
    window.deleteCategory = function (id) {
        const category = state.categories.find(c => c.id === id);
        if (!category) return;

        state.deleteId = id;
        $('#deleteItemName').text(category.name);
        const modal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
        modal.show();
    };

    /**
     * Confirm delete
     */
    function confirmDelete() {
        if (!state.deleteId) return;

        const id = state.deleteId;

        // Disable button
        $('#confirmDeleteBtn').prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Deleting...');

        $.ajax({
            url: CONFIG.apiEndpoints.delete,
            method: 'DELETE',
            contentType: 'application/json',
            data: JSON.stringify({ id: id }),
            success: function (response) {
                if (response.success) {
                    showNotification('Work category deleted successfully', 'success');
                    bootstrap.Modal.getInstance(document.getElementById('deleteConfirmModal')).hide();
                    state.deleteId = null;
                    loadCategories(); // Reload list
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
     * Handle search
     */
    function handleSearch() {
        const searchTerm = $('#searchInput').val().toLowerCase().trim();

        if (!searchTerm) {
            state.filteredCategories = [...state.categories];
        } else {
            state.filteredCategories = state.categories.filter(category =>
                category.name.toLowerCase().includes(searchTerm)
            );
        }

        renderCategories();
        updateTotalCount();
    }

    /**
     * Update total count
     */
    function updateTotalCount() {
        $('#totalCount').text(state.filteredCategories.length);
    }

    /**
     * Utility: Escape HTML
     */
    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }

    /**
     * Utility: Debounce
     */
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    /**
     * Note: showNotification() is now a global function defined in _Layout.cshtml
     * It uses toastr library for consistent notifications across the application
     */

    // Initialize when document is ready
    $(document).ready(function () {
        init();
    });

})(jQuery);
