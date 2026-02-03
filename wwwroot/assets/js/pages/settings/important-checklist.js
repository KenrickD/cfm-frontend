/**
 * Important Checklist Settings Page
 * Handles CRUD operations and reordering via up/down buttons
 * Uses server-side pagination
 */
(function ($) {
    'use strict';

    // Configuration from window.CategoryConfig set in View
    const CONFIG = {
        get apiEndpoints() {
            return window.CategoryConfig?.endpoints || {
                list: MvcEndpoints.Helpdesk.Settings.ImportantChecklist.List,
                create: MvcEndpoints.Helpdesk.Settings.ImportantChecklist.Create,
                update: MvcEndpoints.Helpdesk.Settings.ImportantChecklist.Update,
                delete: MvcEndpoints.Helpdesk.Settings.ImportantChecklist.Delete,
                updateOrder: MvcEndpoints.Helpdesk.Settings.ImportantChecklist.UpdateOrder
            };
        },
        get entityName() {
            return window.CategoryConfig?.entityName || 'Important Checklist Item';
        },
        get entityNamePlural() {
            return window.CategoryConfig?.entityNamePlural || 'Important Checklist Items';
        }
    };

    const state = {
        editingId: null,
        deleteModal: null,
        deleteItemId: null,
        deleteItemName: null
    };

    /**
     * Escape HTML to prevent XSS
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

    /**
     * Initialize the page
     */
    function init() {
        // Initialize delete modal
        const deleteModalEl = document.getElementById('deleteConfirmModal');
        if (deleteModalEl) {
            state.deleteModal = new bootstrap.Modal(deleteModalEl);
        }

        // Bind event handlers
        bindEvents();
    }

    /**
     * Bind all event handlers
     */
    function bindEvents() {
        // Search functionality - server-side search via page navigation
        $('#searchBtn').on('click', handleSearch);
        $('#searchInput').on('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                handleSearch();
            }
        });
        $('#clearSearchBtn').on('click', clearSearch);

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
     * Handle search - navigate to page with search parameter
     */
    function handleSearch() {
        const searchTerm = $('#searchInput').val().trim();
        const url = new URL(window.location.href);

        if (searchTerm) {
            url.searchParams.set('search', searchTerm);
        } else {
            url.searchParams.delete('search');
        }
        url.searchParams.set('page', '1'); // Reset to first page on search

        window.location.href = url.toString();
    }

    /**
     * Clear search and reload
     */
    function clearSearch() {
        const url = new URL(window.location.href);
        url.searchParams.delete('search');
        url.searchParams.set('page', '1');
        window.location.href = url.toString();
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
     * Creates TypePayloadDto: { Text, DisplayOrder }
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

        $input.removeClass('is-invalid');
        $error.text('');

        // Get max display order from current items on page
        let maxOrder = 0;
        $('.category-row').each(function() {
            const order = parseInt($(this).data('order')) || 0;
            if (order > maxOrder) maxOrder = order;
        });

        // Create TypePayloadDto structure
        const newItem = {
            text: name,
            displayOrder: maxOrder + 1
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
                    // Reload page to show new item
                    window.location.reload();
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
     * Start editing an item - transforms row to edit mode
     */
    window.editItem = function (id) {
        // Cancel any existing edit
        if (state.editingId !== null) {
            cancelEdit();
        }

        const $row = $(`.category-row[data-id="${id}"]`);
        if (!$row.length) return;

        const currentName = $row.find('.category-description').text().trim();
        const currentOrder = $row.data('order');

        state.editingId = id;
        $row.addClass('editing');

        // Replace description with input
        $row.find('.category-description').html(`
            <input type="text"
                   class="form-control form-control-sm"
                   id="editInput${id}"
                   value="${escapeHtml(currentName)}"
                   maxlength="200">
            <div class="invalid-feedback" id="editError${id}"></div>
        `);

        // Replace action buttons
        $row.find('.category-actions').html(`
            <button type="button"
                    class="btn btn-success btn-action btn-sm"
                    onclick="saveItem(${id}, ${currentOrder})">
                <i class="ti ti-check"></i>
            </button>
            <button type="button"
                    class="btn btn-secondary btn-action btn-sm"
                    onclick="cancelEdit()">
                <i class="ti ti-x"></i>
            </button>
        `);

        // Focus and select the input
        const $input = $(`#editInput${id}`);
        $input.focus();
        $input[0].select();

        // Bind keyboard handlers
        $input.on('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                window.saveItem(id, currentOrder);
            } else if (e.key === 'Escape') {
                e.preventDefault();
                cancelEdit();
            }
        });
    };

    /**
     * Save edited item
     */
    window.saveItem = function (id, displayOrder) {
        const $input = $(`#editInput${id}`);
        const $error = $(`#editError${id}`);
        const newName = $input.val().trim();

        // Validation
        if (!newName) {
            $input.addClass('is-invalid');
            $error.text('Please enter a checklist item name');
            return;
        }

        // Create TypePayloadDto structure
        const updatedItem = {
            idType: id,
            text: newName,
            displayOrder: displayOrder
        };

        updateItem(updatedItem);
    };

    /**
     * Cancel editing - reload page to restore original state
     */
    window.cancelEdit = function () {
        state.editingId = null;
        window.location.reload();
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
                    window.location.reload();
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
     * Move item up in order
     */
    window.moveItemUp = function (id) {
        const $currentRow = $(`.category-row[data-id="${id}"]`);
        const $prevRow = $currentRow.prev('.category-row');

        if (!$prevRow.length) return;

        const currentOrder = parseInt($currentRow.data('order'));
        const prevOrder = parseInt($prevRow.data('order'));
        const prevId = parseInt($prevRow.data('id'));

        // Swap orders
        const orderUpdates = [
            { idType: id, displayOrder: prevOrder },
            { idType: prevId, displayOrder: currentOrder }
        ];

        saveOrderChanges(orderUpdates);
    };

    /**
     * Move item down in order
     */
    window.moveItemDown = function (id) {
        const $currentRow = $(`.category-row[data-id="${id}"]`);
        const $nextRow = $currentRow.next('.category-row');

        if (!$nextRow.length) return;

        const currentOrder = parseInt($currentRow.data('order'));
        const nextOrder = parseInt($nextRow.data('order'));
        const nextId = parseInt($nextRow.data('id'));

        // Swap orders
        const orderUpdates = [
            { idType: id, displayOrder: nextOrder },
            { idType: nextId, displayOrder: currentOrder }
        ];

        saveOrderChanges(orderUpdates);
    };

    /**
     * Save order changes to server
     */
    function saveOrderChanges(orderUpdates) {
        showSpinner();

        $.ajax({
            url: CONFIG.apiEndpoints.updateOrder,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ items: orderUpdates }),
            success: function (response) {
                hideSpinner();

                if (response.success) {
                    showNotification('Order updated', 'success');
                    // Reload page to show updated order
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to update order', 'error');
                }
            },
            error: function () {
                hideSpinner();
                showNotification('Error updating order', 'error');
            }
        });
    }

    /**
     * Show delete confirmation modal
     */
    window.deleteItem = function (id, name) {
        state.deleteItemId = id;
        state.deleteItemName = name;
        $('#deleteItemName').text(name);
        state.deleteModal.show();
    };

    /**
     * Handle confirmed deletion
     */
    function handleDeleteConfirmed() {
        if (state.deleteItemId === null) return;

        deleteItemById(state.deleteItemId);
        state.deleteModal.hide();
    }

    /**
     * Delete item via API
     */
    function deleteItemById(id) {
        showSpinner();

        $.ajax({
            url: `${CONFIG.apiEndpoints.delete}?id=${id}`,
            method: 'DELETE',
            success: function (response) {
                hideSpinner();

                if (response.success) {
                    showNotification(response.message || `${CONFIG.entityName} deleted successfully`, 'success');
                    state.deleteItemId = null;
                    state.deleteItemName = null;
                    // Reload page to update list
                    window.location.reload();
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
     * Show loading spinner
     */
    function showSpinner() {
        if ($('#loadingSpinner').length === 0) {
            $('body').append(`
                <div id="loadingSpinner" class="spinner-overlay" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(255,255,255,0.7); display: flex; justify-content: center; align-items: center; z-index: 9999;">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </div>
            `);
        }
    }

    /**
     * Hide loading spinner
     */
    function hideSpinner() {
        $('#loadingSpinner').remove();
    }

    // Initialize on document ready
    $(document).ready(init);

})(jQuery);
