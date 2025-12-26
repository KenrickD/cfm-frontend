(function ($) {
    'use strict';

    const CONFIG = {
        apiEndpoints: {
            list: '/Helpdesk/Settings/GetImportantChecklists',
            create: '/Helpdesk/Settings/CreateImportantChecklist',
            update: '/Helpdesk/Settings/UpdateImportantChecklist',
            delete: '/Helpdesk/Settings/DeleteImportantChecklist',
            updateOrder: '/Helpdesk/Settings/UpdateImportantChecklistOrder'
        },
        entityName: 'Important Checklist Item',
        entityNamePlural: 'Important Checklist Items'
    };

    const state = {
        items: [],
        filteredItems: [],
        editingId: null,
        deleteModal: null,
        deleteItemId: null,
        dragulaInstance: null
    };

    /**
     * Initialize the page
     */
    function init() {
        // Initialize delete modal
        state.deleteModal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));

        // Bind event handlers
        bindEvents();

        // Load initial data
        loadItems();
    }

    /**
     * Bind all event handlers
     */
    function bindEvents() {
        // Search functionality
        $('#searchInput').on('input', handleSearch);

        // Add new item
        $('#showAddFormBtn').on('click', showAddForm);
        $('#saveNewBtn').on('click', handleCreateItem);
        $('#cancelNewBtn').on('click', hideAddForm);

        // Enter key on new item input
        $('#newItemName').on('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                handleCreateItem();
            } else if (e.key === 'Escape') {
                e.preventDefault();
                hideAddForm();
            }
        });

        // Delete confirmation
        $('#confirmDeleteBtn').on('click', handleDeleteConfirmed);
    }

    /**
     * Load items from API
     */
    function loadItems() {
        showSpinner();

        $.ajax({
            url: CONFIG.apiEndpoints.list,
            method: 'GET',
            success: function (response) {
                hideSpinner();

                if (response.success && response.data) {
                    state.items = response.data;
                    state.filteredItems = [...state.items];
                    renderItems();
                    updateTotalCount();
                    initializeDragula();
                } else {
                    showNotification(response.message || 'Failed to load items', 'error');
                }
            },
            error: function () {
                hideSpinner();
                showNotification('Error loading items', 'error');
            }
        });
    }

    /**
     * Initialize Dragula for drag-and-drop reordering
     */
    function initializeDragula() {
        // Destroy existing instance if any
        if (state.dragulaInstance) {
            state.dragulaInstance.destroy();
        }

        const container = document.getElementById('checklistTableBody');
        if (!container) return;

        // Initialize Dragula
        state.dragulaInstance = dragula([container], {
            moves: function (el, container, handle) {
                // Only allow dragging from the drag handle
                return handle.classList.contains('drag-handle') || handle.closest('.drag-handle');
            },
            accepts: function (el, target, source, sibling) {
                // Don't allow dropping on empty state or editing rows
                return !el.classList.contains('empty-state') &&
                       !el.classList.contains('editing') &&
                       !el.id.includes('emptyState');
            }
        });

        // Handle drop event
        state.dragulaInstance.on('drop', function (el, target, source, sibling) {
            handleReorder();
        });
    }

    /**
     * Handle reordering after drag-and-drop
     */
    function handleReorder() {
        const container = document.getElementById('checklistTableBody');
        const rows = Array.from(container.querySelectorAll('.category-row'));

        // Build new order array
        const orderUpdates = rows.map((row, index) => ({
            id: parseInt(row.dataset.id),
            displayOrder: index + 1
        }));

        // Update local state
        orderUpdates.forEach(update => {
            const item = state.items.find(i => i.id === update.id);
            if (item) {
                item.displayOrder = update.displayOrder;
            }
        });

        // Re-render with new order numbers
        renderItems();

        // Send update to server
        saveOrder(orderUpdates);
    }

    /**
     * Save new order to server
     */
    function saveOrder(orderUpdates) {
        $.ajax({
            url: CONFIG.apiEndpoints.updateOrder,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ items: orderUpdates }),
            success: function (response) {
                if (response.success) {
                    showNotification('Order updated successfully', 'success');
                } else {
                    showNotification(response.message || 'Failed to update order', 'error');
                    // Reload to get correct order
                    loadItems();
                }
            },
            error: function () {
                showNotification('Error updating order', 'error');
                // Reload to get correct order
                loadItems();
            }
        });
    }

    /**
     * Render items to the table
     */
    function renderItems() {
        const $container = $('#checklistTableBody');
        const $emptyState = $('#emptyState');

        // Clear non-empty-state content
        $container.children('.category-row').remove();

        if (state.filteredItems.length === 0) {
            $emptyState.show();
            return;
        }

        $emptyState.hide();

        // Sort by displayOrder
        const sortedItems = [...state.filteredItems].sort((a, b) => a.displayOrder - b.displayOrder);

        sortedItems.forEach(item => {
            const row = createItemRow(item);
            $emptyState.before(row);
        });
    }

    /**
     * Create HTML for a single item row
     */
    function createItemRow(item) {
        const isEditing = state.editingId === item.id;
        const escapedName = escapeHtml(item.name || item.label || '');

        if (isEditing) {
            return `
                <div class="category-row editing" data-id="${item.id}">
                    <div class="row align-items-center">
                        <div class="col-1 text-center">
                            <span class="display-order-badge">${item.displayOrder || ''}</span>
                        </div>
                        <div class="col-10">
                            <input type="text"
                                   class="form-control form-control-sm"
                                   id="editInput${item.id}"
                                   value="${escapedName}"
                                   maxlength="200">
                            <div class="invalid-feedback" id="editError${item.id}"></div>
                        </div>
                        <div class="col-1">
                            <div class="category-actions">
                                <button type="button"
                                        class="btn btn-save btn-sm"
                                        onclick="window.saveItem(${item.id})">
                                    <i class="ti ti-check"></i>
                                </button>
                                <button type="button"
                                        class="btn btn-cancel btn-sm"
                                        onclick="window.cancelEdit()">
                                    <i class="ti ti-x"></i>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        }

        return `
            <div class="category-row" data-id="${item.id}">
                <div class="row align-items-center">
                    <div class="col-1 text-center">
                        <i class="ti ti-grip-vertical drag-handle"></i>
                    </div>
                    <div class="col-10">
                        <span class="category-name">${escapedName}</span>
                    </div>
                    <div class="col-1">
                        <div class="category-actions">
                            <button type="button"
                                    class="btn btn-edit btn-sm"
                                    onclick="window.editItem(${item.id})"
                                    title="Edit">
                                <i class="ti ti-edit"></i>
                            </button>
                            <button type="button"
                                    class="btn btn-delete btn-sm"
                                    onclick="window.deleteItem(${item.id})"
                                    title="Delete">
                                <i class="ti ti-trash"></i>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Handle search input
     */
    function handleSearch() {
        const searchTerm = $(this).val().toLowerCase().trim();

        if (!searchTerm) {
            state.filteredItems = [...state.items];
        } else {
            state.filteredItems = state.items.filter(item => {
                const name = (item.name || item.label || '').toLowerCase();
                return name.includes(searchTerm);
            });
        }

        renderItems();
        updateTotalCount();
    }

    /**
     * Show add new form
     */
    function showAddForm() {
        $('#addNewForm').slideDown(200);
        $('#showAddFormBtn').hide();
        $('#newItemName').val('').removeClass('is-invalid').focus();
        $('#addValidationError').text('');
    }

    /**
     * Hide add new form
     */
    function hideAddForm() {
        $('#addNewForm').slideUp(200);
        $('#showAddFormBtn').show();
        $('#newItemName').val('').removeClass('is-invalid');
        $('#addValidationError').text('');
    }

    /**
     * Handle creating new item
     */
    function handleCreateItem() {
        const name = $('#newItemName').val().trim();
        const $input = $('#newItemName');
        const $error = $('#addValidationError');

        // Validation
        if (!name) {
            $input.addClass('is-invalid');
            $error.text('Please enter a checklist item name');
            return;
        }

        // Check for duplicates
        const isDuplicate = state.items.some(item =>
            (item.name || item.label || '').toLowerCase() === name.toLowerCase()
        );

        if (isDuplicate) {
            $input.addClass('is-invalid');
            $error.text('This checklist item already exists');
            return;
        }

        $input.removeClass('is-invalid');
        $error.text('');

        // Calculate next display order
        const maxOrder = state.items.length > 0
            ? Math.max(...state.items.map(i => i.displayOrder || 0))
            : 0;

        const newItem = {
            name: name,
            label: name,
            displayOrder: maxOrder + 1,
            isActive: true
        };

        createItem(newItem);
    }

    /**
     * Create item via API
     */
    function createItem(item) {
        showSpinner();

        $.ajax({
            url: CONFIG.apiEndpoints.create,
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(item),
            success: function (response) {
                hideSpinner();

                if (response.success) {
                    showNotification(response.message || `${CONFIG.entityName} created successfully`, 'success');
                    hideAddForm();
                    loadItems();
                } else {
                    showNotification(response.message || `Failed to create ${CONFIG.entityName}`, 'error');
                }
            },
            error: function () {
                hideSpinner();
                showNotification(`Error creating ${CONFIG.entityName}`, 'error');
            }
        });
    }

    /**
     * Start editing an item
     */
    window.editItem = function (id) {
        // Cancel any existing edit
        if (state.editingId !== null) {
            cancelEdit();
        }

        state.editingId = id;
        renderItems();

        // Focus and select the input
        const $input = $(`#editInput${id}`);
        $input.focus();
        $input[0].select();

        // Bind keyboard handlers
        $input.off('keydown').on('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                window.saveItem(id);
            } else if (e.key === 'Escape') {
                e.preventDefault();
                cancelEdit();
            }
        });
    };

    /**
     * Save edited item
     */
    window.saveItem = function (id) {
        const $input = $(`#editInput${id}`);
        const $error = $(`#editError${id}`);
        const newName = $input.val().trim();

        // Validation
        if (!newName) {
            $input.addClass('is-invalid');
            $error.text('Please enter a checklist item name');
            return;
        }

        // Check for duplicates (excluding current item)
        const isDuplicate = state.items.some(item =>
            item.id !== id && (item.name || item.label || '').toLowerCase() === newName.toLowerCase()
        );

        if (isDuplicate) {
            $input.addClass('is-invalid');
            $error.text('This checklist item already exists');
            return;
        }

        const item = state.items.find(i => i.id === id);
        if (!item) return;

        const updatedItem = {
            ...item,
            name: newName,
            label: newName
        };

        updateItem(updatedItem);
    };

    /**
     * Cancel editing
     */
    window.cancelEdit = function () {
        state.editingId = null;
        renderItems();
    };

    /**
     * Update item via API
     */
    function updateItem(item) {
        showSpinner();

        $.ajax({
            url: CONFIG.apiEndpoints.update,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(item),
            success: function (response) {
                hideSpinner();

                if (response.success) {
                    showNotification(response.message || `${CONFIG.entityName} updated successfully`, 'success');
                    state.editingId = null;
                    loadItems();
                } else {
                    showNotification(response.message || `Failed to update ${CONFIG.entityName}`, 'error');
                }
            },
            error: function () {
                hideSpinner();
                showNotification(`Error updating ${CONFIG.entityName}`, 'error');
            }
        });
    }

    /**
     * Show delete confirmation modal
     */
    window.deleteItem = function (id) {
        const item = state.items.find(i => i.id === id);
        if (!item) return;

        state.deleteItemId = id;
        $('#deleteItemName').text(item.name || item.label || '');
        state.deleteModal.show();
    };

    /**
     * Handle confirmed deletion
     */
    function handleDeleteConfirmed() {
        if (state.deleteItemId === null) return;

        const item = state.items.find(i => i.id === state.deleteItemId);
        if (!item) return;

        deleteItem(item);
        state.deleteModal.hide();
    }

    /**
     * Delete item via API
     */
    function deleteItem(item) {
        showSpinner();

        $.ajax({
            url: CONFIG.apiEndpoints.delete,
            method: 'DELETE',
            contentType: 'application/json',
            data: JSON.stringify(item),
            success: function (response) {
                hideSpinner();

                if (response.success) {
                    showNotification(response.message || `${CONFIG.entityName} deleted successfully`, 'success');
                    state.deleteItemId = null;
                    loadItems();
                } else {
                    showNotification(response.message || `Failed to delete ${CONFIG.entityName}`, 'error');
                }
            },
            error: function () {
                hideSpinner();
                showNotification(`Error deleting ${CONFIG.entityName}`, 'error');
            }
        });
    }

    /**
     * Update total count display
     */
    function updateTotalCount() {
        $('#totalCount').text(state.filteredItems.length);
    }

    /**
     * Show loading spinner
     */
    function showSpinner() {
        // Use existing spinner implementation or create a simple overlay
        $('body').append('<div id="loadingSpinner" class="spinner-overlay"><div class="spinner-border text-primary" role="status"></div></div>');
    }

    /**
     * Hide loading spinner
     */
    function hideSpinner() {
        $('#loadingSpinner').remove();
    }

    /**
     * Note: showNotification() is now a global function defined in _Layout.cshtml
     * It uses toastr library for consistent notifications across the application
     * Toastr already handles HTML escaping for XSS protection
     */

    // Initialize on document ready
    $(document).ready(init);

})(jQuery);
