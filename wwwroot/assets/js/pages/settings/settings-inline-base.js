/**
 * Settings Inline Editing Base Module
 * Reusable inline editing functionality for simple settings pages
 */

window.SettingsInlineEditor = (function ($) {
    'use strict';

    /**
     * Create inline editor instance
     */
    function create(config) {
        // State management
        const state = {
            items: [],
            filteredItems: [],
            editingId: null,
            deleteId: null
        };

        /**
         * Initialize the module
         */
        function init() {
            bindEvents();
            loadItems();
            console.log(`${config.entityName} page initialized`);
        }

        /**
         * Bind event handlers
         */
        function bindEvents() {
            $('#showAddFormBtn').on('click', showAddForm);
            $('#saveNewBtn').on('click', saveNewItem);
            $('#cancelNewBtn').on('click', hideAddForm);

            $('#newItemName').on('keypress', function (e) {
                if (e.which === 13) {
                    e.preventDefault();
                    saveNewItem();
                }
            });

            $('#newItemName').on('keydown', function (e) {
                if (e.which === 27) {
                    hideAddForm();
                }
            });

            $('#searchInput').on('keyup', debounce(handleSearch, 300));
            $('#confirmDeleteBtn').on('click', confirmDelete);
        }

        /**
         * Load items from API
         */
        function loadItems() {
            $.ajax({
                url: config.apiEndpoints.list,
                method: 'GET',
                success: function (response) {
                    if (response.success) {
                        state.items = response.data || [];
                        state.filteredItems = [...state.items];
                        renderItems();
                        updateTotalCount();
                    } else {
                        showNotification(response.message || `Failed to load ${config.entityNamePlural}`, 'error');
                    }
                },
                error: function (xhr, status, error) {
                    console.error(`Error loading ${config.entityNamePlural}:`, error);
                    showNotification(`Error loading ${config.entityNamePlural}. Please refresh the page.`, 'error');
                }
            });
        }

        /**
         * Render items list
         */
        function renderItems() {
            const $tbody = $('#categoryTableBody');
            const $emptyState = $('#emptyState');

            if (state.filteredItems.length === 0) {
                $tbody.html('');
                $emptyState.show();
                return;
            }

            $emptyState.hide();
            $tbody.html('');

            state.filteredItems.forEach(item => {
                const row = createItemRow(item);
                $tbody.append(row);
            });
        }

        /**
         * Create item row HTML
         */
        function createItemRow(item) {
            const isEditing = state.editingId === item.id;

            if (isEditing) {
                return `
                    <div class="category-row editing" data-id="${item.id}">
                        <div class="category-input">
                            <input type="text"
                                   class="form-control"
                                   id="edit-${item.id}"
                                   value="${escapeHtml(item.name)}"
                                   maxlength="200">
                            <div class="invalid-feedback" id="edit-error-${item.id}"></div>
                        </div>
                        <div class="category-actions">
                            <button type="button"
                                    class="btn btn-save btn-action"
                                    onclick="saveItem(${item.id})">
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
                <div class="category-row" data-id="${item.id}">
                    <div class="category-description">${escapeHtml(item.name)}</div>
                    <div class="category-actions">
                        <button type="button"
                                class="btn btn-edit btn-action"
                                onclick="editItem(${item.id})">
                            <i class="ti ti-edit"></i>
                        </button>
                        <button type="button"
                                class="btn btn-delete btn-action"
                                onclick="deleteItem(${item.id})">
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
            if (state.editingId) {
                cancelEdit();
            }

            $('#showAddFormBtn').hide();
            $('#addNewForm').slideDown();
            $('#newItemName').focus();
            $('#addValidationError').text('').removeClass('d-block');
        }

        /**
         * Hide add form
         */
        function hideAddForm() {
            $('#addNewForm').slideUp();
            $('#showAddFormBtn').show();
            $('#newItemName').val('').removeClass('is-invalid');
            $('#addValidationError').text('').removeClass('d-block');
        }

        /**
         * Save new item
         */
        function saveNewItem() {
            const name = $('#newItemName').val().trim();

            if (!name) {
                $('#newItemName').addClass('is-invalid');
                $('#addValidationError').text(`${config.entityName} name is required`).addClass('d-block');
                return;
            }

            if (state.items.some(c => c.name.toLowerCase() === name.toLowerCase())) {
                $('#newItemName').addClass('is-invalid');
                $('#addValidationError').text(`This ${config.entityName.toLowerCase()} already exists`).addClass('d-block');
                return;
            }

            $('#newItemName').removeClass('is-invalid');
            $('#addValidationError').text('').removeClass('d-block');
            $('#saveNewBtn').prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Saving...');

            $.ajax({
                url: config.apiEndpoints.create,
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ name: name }),
                success: function (response) {
                    if (response.success) {
                        showNotification(`${config.entityName} created successfully`, 'success');
                        hideAddForm();
                        loadItems();
                    } else {
                        showNotification(response.message || `Failed to create ${config.entityName.toLowerCase()}`, 'error');
                    }
                },
                error: function (xhr, status, error) {
                    console.error(`Error creating ${config.entityName.toLowerCase()}:`, error);
                    showNotification(`Error creating ${config.entityName.toLowerCase()}. Please try again.`, 'error');
                },
                complete: function () {
                    $('#saveNewBtn').prop('disabled', false).html('<i class="ti ti-check me-1"></i>Save');
                }
            });
        }

        /**
         * Edit item
         */
        window.editItem = function (id) {
            if (state.editingId && state.editingId !== id) {
                cancelEdit();
            }

            state.editingId = id;
            renderItems();

            setTimeout(() => {
                $(`#edit-${id}`).focus().select();
            }, 100);

            $(`#edit-${id}`).on('keypress', function (e) {
                if (e.which === 13) {
                    e.preventDefault();
                    saveItem(id);
                }
            });

            $(`#edit-${id}`).on('keydown', function (e) {
                if (e.which === 27) {
                    cancelEdit();
                }
            });
        };

        /**
         * Save item
         */
        window.saveItem = function (id) {
            const name = $(`#edit-${id}`).val().trim();
            const item = state.items.find(c => c.id === id);

            if (!item) return;

            if (!name) {
                $(`#edit-${id}`).addClass('is-invalid');
                $(`#edit-error-${id}`).text(`${config.entityName} name is required`).addClass('d-block');
                return;
            }

            if (state.items.some(c => c.id !== id && c.name.toLowerCase() === name.toLowerCase())) {
                $(`#edit-${id}`).addClass('is-invalid');
                $(`#edit-error-${id}`).text(`This ${config.entityName.toLowerCase()} already exists`).addClass('d-block');
                return;
            }

            if (name === item.name) {
                cancelEdit();
                return;
            }

            $(`#edit-${id}`).removeClass('is-invalid');
            $(`#edit-error-${id}`).text('').removeClass('d-block');

            $.ajax({
                url: config.apiEndpoints.update,
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({ id: id, name: name }),
                success: function (response) {
                    if (response.success) {
                        showNotification(`${config.entityName} updated successfully`, 'success');
                        state.editingId = null;
                        loadItems();
                    } else {
                        showNotification(response.message || `Failed to update ${config.entityName.toLowerCase()}`, 'error');
                    }
                },
                error: function (xhr, status, error) {
                    console.error(`Error updating ${config.entityName.toLowerCase()}:`, error);
                    showNotification(`Error updating ${config.entityName.toLowerCase()}. Please try again.`, 'error');
                }
            });
        };

        /**
         * Cancel edit
         */
        window.cancelEdit = function () {
            state.editingId = null;
            renderItems();
        };

        /**
         * Delete item
         */
        window.deleteItem = function (id) {
            const item = state.items.find(c => c.id === id);
            if (!item) return;

            state.deleteId = id;
            $('#deleteItemName').text(item.name);
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

            $.ajax({
                url: config.apiEndpoints.delete,
                method: 'DELETE',
                contentType: 'application/json',
                data: JSON.stringify({ id: id }),
                success: function (response) {
                    if (response.success) {
                        showNotification(`${config.entityName} deleted successfully`, 'success');
                        bootstrap.Modal.getInstance(document.getElementById('deleteConfirmModal')).hide();
                        state.deleteId = null;
                        loadItems();
                    } else {
                        showNotification(response.message || `Failed to delete ${config.entityName.toLowerCase()}`, 'error');
                    }
                },
                error: function (xhr, status, error) {
                    console.error(`Error deleting ${config.entityName.toLowerCase()}:`, error);
                    showNotification(`Error deleting ${config.entityName.toLowerCase()}. Please try again.`, 'error');
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
                state.filteredItems = [...state.items];
            } else {
                state.filteredItems = state.items.filter(item =>
                    item.name.toLowerCase().includes(searchTerm)
                );
            }

            renderItems();
            updateTotalCount();
        }

        /**
         * Update total count
         */
        function updateTotalCount() {
            $('#totalCount').text(state.filteredItems.length);
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

        // Initialize
        init();
    }

    return {
        create: create
    };

})(jQuery);
