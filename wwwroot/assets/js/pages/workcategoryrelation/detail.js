/**
 * Work Category Relation - Detail Page
 * Handles PIC target management (add, delete, reorder)
 */

(function ($) {
    'use strict';

    // Configuration
    const CONFIG = {
        apiEndpoints: {
            addPICToTarget: MvcEndpoints.WorkCategoryRelation.AddPICToTarget,
            removePICFromTarget: MvcEndpoints.WorkCategoryRelation.RemovePICFromTarget,
            movePICUp: MvcEndpoints.WorkCategoryRelation.MovePICUp,
            movePICDown: MvcEndpoints.WorkCategoryRelation.MovePICDown,
            getPICs: MvcEndpoints.WorkCategoryRelation.GetPICs
        }
    };

    // Client context for multi-tab session safety
    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; },
        get idWorkCategoryRelation() { return window.PageContext?.idWorkCategoryRelation || 0; }
    };

    // State management
    const state = {
        currentTargetType: '',
        deletePICId: null,
        deletePICName: '',
        deleteTargetType: ''
    };

    // Component instances
    let modalPICDropdown;
    let addPICModal, deletePICModal;

    /**
     * Initialize the module
     */
    function init() {
        initializeModals();
        initializeSearchableDropdowns();
        loadPICs();
        console.log('Work Category Relation detail page initialized');
    }

    /**
     * Initialize Bootstrap modals
     */
    function initializeModals() {
        const addModalEl = document.getElementById('addPICModal');
        const deleteModalEl = document.getElementById('deletePICModal');

        if (addModalEl) {
            addPICModal = new bootstrap.Modal(addModalEl);
            addModalEl.addEventListener('hidden.bs.modal', resetAddPICForm);
        }

        if (deleteModalEl) {
            deletePICModal = new bootstrap.Modal(deleteModalEl);
        }

        // Bind modal buttons
        $('#confirmAddPICBtn').on('click', confirmAddPIC);
        $('#confirmDeletePICBtn').on('click', confirmDeletePIC);
    }

    /**
     * Initialize searchable dropdowns
     */
    function initializeSearchableDropdowns() {
        const modalPICEl = document.getElementById('modalPICSelect');
        if (modalPICEl) {
            modalPICDropdown = new SearchableDropdown(modalPICEl);
        }
    }

    /**
     * Load PICs for dropdown
     */
    function loadPICs() {
        // TODO: Load from API when implemented
        console.log('Loading PICs - API not yet implemented');

        // Mock implementation:
        // $.ajax({
        //     url: CONFIG.apiEndpoints.getPICs,
        //     method: 'GET',
        //     success: function(response) {
        //         if (response.success && response.data) {
        //             const options = response.data.map(pic => ({
        //                 value: pic.idEmployee,
        //                 label: pic.employeeName
        //             }));
        //             modalPICDropdown.loadOptions(options);
        //         }
        //     },
        //     error: function(xhr, status, error) {
        //         console.error('Error loading PICs:', error);
        //     }
        // });
    }

    /**
     * Show Add PIC Modal - make function global
     */
    window.showAddPICModal = function (targetType) {
        state.currentTargetType = targetType;
        $('#modalTargetType').val(targetType);
        addPICModal.show();
    };

    /**
     * Reset Add PIC Form
     */
    function resetAddPICForm() {
        modalPICDropdown.clear();
        $('#modalTargetType').val('');
        state.currentTargetType = '';
    }

    /**
     * Confirm Add PIC
     */
    function confirmAddPIC() {
        const idPIC = $('#modalPICSelect').val();
        const targetType = $('#modalTargetType').val();

        if (!idPIC) {
            showNotification('Please select a PIC', 'warning');
            return;
        }

        $('#confirmAddPICBtn').prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-1"></span>Adding...');

        // Get CSRF token
        const token = $('input[name="__RequestVerificationToken"]').val();

        const payload = {
            idWorkCategoryRelation: clientContext.idWorkCategoryRelation,
            idPIC: parseInt(idPIC),
            targetType: targetType,
            displayOrder: 0
        };

        $.ajax({
            url: CONFIG.apiEndpoints.addPICToTarget,
            method: 'POST',
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': token
            },
            data: JSON.stringify(payload),
            success: function (response) {
                if (response.success) {
                    showNotification('PIC added successfully', 'success');
                    addPICModal.hide();
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to add PIC', 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error adding PIC:', error);

                let errorMessage = 'An error occurred while adding the PIC.';

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
                $('#confirmAddPICBtn').prop('disabled', false).html('Add PIC');
            }
        });
    }

    /**
     * Delete PIC - make function global
     */
    window.deletePIC = function (idPIC, picName, targetType) {
        state.deletePICId = idPIC;
        state.deletePICName = picName;
        state.deleteTargetType = targetType;

        $('#deletePICName').text(picName);
        $('#deletePICId').val(idPIC);
        $('#deleteTargetType').val(targetType);

        deletePICModal.show();
    };

    /**
     * Confirm Delete PIC
     */
    function confirmDeletePIC() {
        const idPIC = parseInt($('#deletePICId').val());
        const targetType = $('#deleteTargetType').val();

        $('#confirmDeletePICBtn').prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-1"></span>Removing...');

        // Get CSRF token
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: `${CONFIG.apiEndpoints.removePICFromTarget}?id=${clientContext.idWorkCategoryRelation}&idPIC=${idPIC}&targetType=${targetType}`,
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': token
            },
            success: function (response) {
                if (response.success) {
                    showNotification('PIC removed successfully', 'success');
                    deletePICModal.hide();
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to remove PIC', 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error removing PIC:', error);

                let errorMessage = 'An error occurred while removing the PIC.';

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
                $('#confirmDeletePICBtn').prop('disabled', false).html('Remove');
            }
        });
    }

    /**
     * Move PIC Up - make function global
     */
    window.movePICUp = function (idPIC, targetType) {
        performPICMove(idPIC, targetType, 'up');
    };

    /**
     * Move PIC Down - make function global
     */
    window.movePICDown = function (idPIC, targetType) {
        performPICMove(idPIC, targetType, 'down');
    };

    /**
     * Perform PIC move operation
     */
    function performPICMove(idPIC, targetType, direction) {
        const url = direction === 'up'
            ? `${CONFIG.apiEndpoints.movePICUp}?id=${clientContext.idWorkCategoryRelation}&idPIC=${idPIC}&targetType=${targetType}`
            : `${CONFIG.apiEndpoints.movePICDown}?id=${clientContext.idWorkCategoryRelation}&idPIC=${idPIC}&targetType=${targetType}`;

        // Get CSRF token
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: url,
            method: 'PUT',
            headers: {
                'RequestVerificationToken': token
            },
            success: function (response) {
                if (response.success) {
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to reorder PIC', 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error reordering PIC:', error);

                let errorMessage = 'An error occurred while reordering the PIC.';

                if (xhr.responseJSON) {
                    const apiResponse = xhr.responseJSON;
                    if (apiResponse.errors && apiResponse.errors.length > 0) {
                        errorMessage = apiResponse.errors.join(', ');
                    } else if (apiResponse.message) {
                        errorMessage = apiResponse.message;
                    }
                }

                showNotification(errorMessage, 'error');
            }
        });
    }

    /**
     * View PIC - make function global
     */
    window.viewPIC = function (idPIC) {
        // TODO: Navigate to PIC detail page when implemented
        console.log('View PIC', idPIC, '- Navigation not yet implemented');
        showNotification('PIC view navigation not yet implemented', 'info');
    };

    /**
     * Edit PIC - make function global
     */
    window.editPIC = function (idPIC) {
        // TODO: Navigate to PIC edit page when implemented
        console.log('Edit PIC', idPIC, '- Navigation not yet implemented');
        showNotification('PIC edit navigation not yet implemented', 'info');
    };

    // Initialize when document is ready
    $(document).ready(function () {
        init();
    });

})(jQuery);
