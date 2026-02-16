/**
 * =============================================================================
 * SNIPPET: Standard AJAX CRUD Pattern for Settings Pages
 * =============================================================================
 * Copy and modify for new settings pages with inline editing
 */

(function ($) {
    'use strict';

    // Configuration - Update endpoints for your feature
    const CONFIG = {
        apiEndpoints: {
            list: MvcEndpoints.Helpdesk.Settings.{Feature}.List,
            create: MvcEndpoints.Helpdesk.Settings.{Feature}.Create,
            update: MvcEndpoints.Helpdesk.Settings.{Feature}.Update,
            delete: MvcEndpoints.Helpdesk.Settings.{Feature}.Delete
        }
    };

    // Client context for multi-tab session safety (REQUIRED)
    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; }
    };

    // State management
    const state = {
        items: [],
        editingId: null,
        deleteId: null
    };

    /**
     * Initialize the module
     */
    function init() {
        initItemsFromDOM();
        bindEvents();
        console.log('{Feature} page initialized');
    }

    /**
     * Initialize items array from DOM
     */
    function initItemsFromDOM() {
        state.items = [];
        $('.item-row[data-id]').each(function () {
            const $row = $(this);
            state.items.push({
                id: parseInt($row.data('id')),
                name: $row.find('.item-name').text().trim()
            });
        });
    }

    /**
     * Bind event handlers
     */
    function bindEvents() {
        // Add form
        $('#showAddFormBtn').on('click', showAddForm);
        $('#saveNewBtn').on('click', saveNewItem);
        $('#cancelNewBtn').on('click', hideAddForm);

        // Enter key in add form
        $('#newItemName').on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                saveNewItem();
            }
        });

        // Escape key to cancel
        $('#newItemName').on('keydown', function (e) {
            if (e.which === 27) hideAddForm();
        });

        // Delete confirmation
        $('#confirmDeleteBtn').on('click', confirmDelete);
    }

    // ==========================================================================
    // CREATE
    // ==========================================================================

    function showAddForm() {
        if (state.editingId) cancelEdit();
        $('#showAddFormBtn').hide();
        $('#addNewForm').slideDown();
        $('#newItemName').focus();
    }

    function hideAddForm() {
        $('#addNewForm').slideUp();
        $('#showAddFormBtn').show();
        $('#newItemName').val('').removeClass('is-invalid');
    }

    function saveNewItem() {
        const name = $('#newItemName').val().trim();

        if (!name) {
            $('#newItemName').addClass('is-invalid');
            return;
        }

        $('#saveNewBtn').prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-1"></span>Saving...');

        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: CONFIG.apiEndpoints.create,
            method: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({
                text: name,
                idClient: clientContext.idClient  // REQUIRED: multi-tab safety
            }),
            success: function (response) {
                if (response.success) {
                    showNotification('Item created', 'success');
                    hideAddForm();
                    window.location.reload();
                }
                // Note: Errors auto-handled by global-ajax-handler.js
            },
            error: function (xhr, status, error) {
                console.error('Error creating item:', error);
                showNotification('Error creating item', 'error');
            },
            complete: function () {
                $('#saveNewBtn').prop('disabled', false)
                    .html('<i class="ti ti-check me-1"></i>Save');
            }
        });
    }

    // ==========================================================================
    // UPDATE
    // ==========================================================================

    window.editItem = function (id) {
        if (state.editingId && state.editingId !== id) cancelEdit();

        const item = state.items.find(i => i.id === id);
        if (!item) return;

        state.editingId = id;

        const $row = $(`.item-row[data-id="${id}"]`);
        $row.addClass('editing');
        $row.html(`
            <div class="item-input">
                <input type="text" class="form-control" id="edit-${id}"
                       value="${escapeHtml(item.name)}" maxlength="200">
            </div>
            <div class="item-actions">
                <button type="button" class="btn btn-success btn-sm" onclick="saveItem(${id})">
                    <i class="ti ti-check"></i>
                </button>
                <button type="button" class="btn btn-secondary btn-sm" onclick="cancelEdit()">
                    <i class="ti ti-x"></i>
                </button>
            </div>
        `);

        setTimeout(() => $(`#edit-${id}`).focus().select(), 100);

        $(`#edit-${id}`).on('keypress', function (e) {
            if (e.which === 13) { e.preventDefault(); saveItem(id); }
        });
        $(`#edit-${id}`).on('keydown', function (e) {
            if (e.which === 27) cancelEdit();
        });
    };

    window.saveItem = function (id) {
        const name = $(`#edit-${id}`).val().trim();
        const item = state.items.find(i => i.id === id);
        if (!item) return;

        if (!name) {
            $(`#edit-${id}`).addClass('is-invalid');
            return;
        }

        if (name === item.name) {
            cancelEdit();
            return;
        }

        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: CONFIG.apiEndpoints.update,
            method: 'PUT',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': token },
            data: JSON.stringify({
                idType: id,
                text: name,
                idClient: clientContext.idClient  // REQUIRED: multi-tab safety
            }),
            success: function (response) {
                if (response.success) {
                    showNotification('Item updated', 'success');
                    state.editingId = null;
                    window.location.reload();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error updating item:', error);
                showNotification('Error updating item', 'error');
            }
        });
    };

    window.cancelEdit = function () {
        if (!state.editingId) return;

        const item = state.items.find(i => i.id === state.editingId);
        if (!item) return;

        const $row = $(`.item-row[data-id="${state.editingId}"]`);
        $row.removeClass('editing');
        $row.html(`
            <div class="item-name">${escapeHtml(item.name)}</div>
            <div class="item-actions">
                <button type="button" class="btn btn-outline-primary btn-sm" onclick="editItem(${item.id})">
                    <i class="ti ti-edit"></i>
                </button>
                <button type="button" class="btn btn-outline-danger btn-sm" onclick="deleteItem(${item.id}, '${escapeHtml(item.name)}')">
                    <i class="ti ti-trash"></i>
                </button>
            </div>
        `);

        state.editingId = null;
    };

    // ==========================================================================
    // DELETE
    // ==========================================================================

    window.deleteItem = function (id, name) {
        state.deleteId = id;
        $('#deleteItemName').text(name);
        const modal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
        modal.show();
    };

    function confirmDelete() {
        if (!state.deleteId) return;

        const id = state.deleteId;
        $('#confirmDeleteBtn').prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-1"></span>Deleting...');

        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: `${CONFIG.apiEndpoints.delete}?id=${id}`,
            method: 'DELETE',
            headers: { 'RequestVerificationToken': token },
            success: function (response) {
                if (response.success) {
                    showNotification('Item deleted', 'success');
                    bootstrap.Modal.getInstance(document.getElementById('deleteConfirmModal')).hide();
                    state.deleteId = null;
                    window.location.reload();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error deleting item:', error);
                showNotification('Error deleting item', 'error');
            },
            complete: function () {
                $('#confirmDeleteBtn').prop('disabled', false).html('Delete');
            }
        });
    }

    // ==========================================================================
    // UTILITIES
    // ==========================================================================

    function escapeHtml(text) {
        if (!text) return '';
        const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return String(text).replace(/[&<>"']/g, m => map[m]);
    }

    // Initialize when document is ready
    $(document).ready(function () { init(); });

})(jQuery);
