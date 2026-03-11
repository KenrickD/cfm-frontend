/**
 * Work Category Relation - Edit Page
 * Handles form submission with pre-populated data
 */

(function ($) {
    'use strict';

    // Configuration
    const CONFIG = {
        apiEndpoints: {
            update: MvcEndpoints.WorkCategoryRelation.Update,
            getWorkCategories: MvcEndpoints.WorkCategoryRelation.GetWorkCategories,
            getPriorityLevels: MvcEndpoints.WorkCategoryRelation.GetPriorityLevels,
            getPICs: MvcEndpoints.WorkCategoryRelation.GetPICs,
            getAllProperties: MvcEndpoints.WorkCategoryRelation.GetAllProperties,
            getPropertiesByPIC: MvcEndpoints.WorkCategoryRelation.GetPropertiesByPIC
        }
    };

    // Client context for multi-tab session safety
    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; }
    };

    // Component instances
    let workCategoryDropdown, priorityLevelDropdown, picDropdown;
    let dualListbox;

    /**
     * Initialize the module
     */
    function init() {
        initializeSearchableDropdowns();
        initializeDualListbox();
        bindEvents();
        loadDropdownData();
        console.log('Work Category Relation edit page initialized');
    }

    /**
     * Initialize searchable dropdowns
     */
    function initializeSearchableDropdowns() {
        const workCategoryEl = document.getElementById('workCategorySelect');
        const priorityLevelEl = document.getElementById('priorityLevelSelect');
        const picEl = document.getElementById('picSelect');

        // Use existing instances if already initialized by data-searchable auto-init
        if (workCategoryEl) {
            workCategoryDropdown = workCategoryEl._searchableDropdown || new SearchableDropdown(workCategoryEl);
        }
        if (priorityLevelEl) {
            priorityLevelDropdown = priorityLevelEl._searchableDropdown || new SearchableDropdown(priorityLevelEl);
        }
        if (picEl) {
            picDropdown = picEl._searchableDropdown;
            if (!picDropdown) {
                picDropdown = new SearchableDropdown(picEl, {
                    onChange: handlePICChange
                });
            } else {
                // If already initialized, update the onChange handler
                picDropdown.options.onChange = handlePICChange;
            }
        }
    }

    /**
     * Initialize dual listbox
     */
    function initializeDualListbox() {
        dualListbox = new DualListbox('#propertiesListbox', {
            leftLabel: 'All Properties',
            rightLabel: 'Accessible by PIC',
            leftItems: [],
            rightItems: [],
            searchable: true,
            idProperty: 'idProperty',
            nameProperty: 'propertyName',
            disabled: false
        });
    }

    /**
     * Bind event handlers
     */
    function bindEvents() {
        $('#editForm').on('submit', handleSubmit);
    }

    /**
     * Load dropdown data and pre-select values
     */
    function loadDropdownData() {
        // TODO: Load dropdown data from API when implemented
        console.log('Loading dropdown data - API not yet implemented');

        // After loading dropdowns, set selected values:
        // workCategoryDropdown.setValue(window.PageContext.selectedWorkCategoryId);
        // priorityLevelDropdown.setValue(window.PageContext.selectedPriorityLevelId);
        // picDropdown.setValue(window.PageContext.selectedPICId);

        // Mock data for testing UI
        // Uncomment when API is ready:
        // loadWorkCategories();
        // loadPriorityLevels();
        // loadPICs();
        // loadAllProperties();
    }

    /**
     * Handle PIC selection change
     */
    function handlePICChange(value) {
        if (!value) {
            dualListbox.setRightItems([]);
            return;
        }

        loadPropertiesByPIC(parseInt(value));
    }

    /**
     * Load properties accessible by selected PIC
     */
    function loadPropertiesByPIC(idPIC) {
        // TODO: Load from API when implemented
        console.log('Loading properties for PIC', idPIC, '- API not yet implemented');

        // Mock implementation:
        // const url = `${CONFIG.apiEndpoints.getPropertiesByPIC}?idPIC=${idPIC}&idClient=${clientContext.idClient}`;
        // $.ajax({
        //     url: url,
        //     method: 'GET',
        //     success: function(response) {
        //         if (response.success && response.data) {
        //             // Filter out already assigned properties
        //             const assignedIds = window.PageContext.assignedPropertyIds || [];
        //             const leftItems = response.data.filter(p => !assignedIds.includes(p.idProperty));
        //             const rightItems = response.data.filter(p => assignedIds.includes(p.idProperty));
        //
        //             dualListbox.setLeftItems(leftItems);
        //             dualListbox.setRightItems(rightItems);
        //         }
        //     },
        //     error: function(xhr, status, error) {
        //         console.error('Error loading properties for PIC:', error);
        //         showNotification('Failed to load properties', 'error');
        //     }
        // });
    }

    /**
     * Handle form submission
     */
    function handleSubmit(e) {
        e.preventDefault();

        // Validate form
        if (!validateForm()) {
            return false;
        }

        // Prepare payload
        const payload = {
            idWorkCategoryRelation: parseInt($('#idWorkCategoryRelation').val()),
            idWorkCategory: parseInt($('#workCategorySelect').val()),
            idPriorityLevel: parseInt($('#priorityLevelSelect').val()),
            idPIC: parseInt($('#picSelect').val()),
            propertyIds: dualListbox.getRightItemIds(),
            idClient: clientContext.idClient
        };

        // Show loading overlay
        showLoadingOverlay();

        // Get CSRF token
        const token = $('input[name="__RequestVerificationToken"]').val();

        // Submit to API
        $.ajax({
            url: CONFIG.apiEndpoints.update,
            method: 'PUT',
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': token
            },
            data: JSON.stringify(payload),
            success: function (response) {
                if (response.success) {
                    showNotification('Work category relation updated successfully', 'success');
                    setTimeout(() => {
                        window.location.href = MvcEndpoints.WorkCategoryRelation.Index;
                    }, 1000);
                } else {
                    hideLoadingOverlay();
                    showNotification(response.message || 'Failed to update relation', 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error updating relation:', error);
                hideLoadingOverlay();

                let errorMessage = 'An error occurred while updating the relation.';

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

        return false;
    }

    /**
     * Validate form
     */
    function validateForm() {
        let isValid = true;

        // Clear previous errors
        $('.is-invalid').removeClass('is-invalid');
        $('.invalid-feedback').text('');

        // Validate Work Category
        const workCategory = $('#workCategorySelect').val();
        if (!workCategory) {
            $('#workCategorySelect').addClass('is-invalid');
            $('#workCategoryError').text('Please select a work category');
            isValid = false;
        }

        // Validate Priority Level
        const priorityLevel = $('#priorityLevelSelect').val();
        if (!priorityLevel) {
            $('#priorityLevelSelect').addClass('is-invalid');
            $('#priorityLevelError').text('Please select a priority level');
            isValid = false;
        }

        // Validate PIC
        const pic = $('#picSelect').val();
        if (!pic) {
            $('#picSelect').addClass('is-invalid');
            $('#picError').text('Please select a PIC');
            isValid = false;
        }

        return isValid;
    }

    /**
     * Show loading overlay
     */
    function showLoadingOverlay() {
        if ($('#loading-overlay').length > 0) return;

        const $overlay = $(`
            <div id="loading-overlay" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%;
                 background: rgba(0, 0, 0, 0.5); display: flex; justify-content: center; align-items: center; z-index: 9999;">
                <div class="spinner-border text-light" role="status" style="width: 3rem; height: 3rem;">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        `);
        $('body').append($overlay);
    }

    /**
     * Hide loading overlay
     */
    function hideLoadingOverlay() {
        $('#loading-overlay').fadeOut(function () {
            $(this).remove();
        });
    }

    // Initialize when document is ready
    $(document).ready(function () {
        init();
    });

})(jQuery);
