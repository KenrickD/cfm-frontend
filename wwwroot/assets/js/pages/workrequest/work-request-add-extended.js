/**
 * Work Request Add Page - Extended functionality for Labor/Material and Assets
 * This file contains additional handlers for modals and complex interactions
 */

(function ($) {
    'use strict';

    // Extended API endpoints
    const EXTENDED_CONFIG = {
        apiEndpoints: {
            searchJobCode: MvcEndpoints.Helpdesk.Search.JobCode,
            getCurrencies: MvcEndpoints.Helpdesk.Extended.GetCurrencies,
            getMeasurementUnits: MvcEndpoints.Helpdesk.Extended.GetMeasurementUnits,
            getLaborMaterialLabels: MvcEndpoints.Helpdesk.Extended.GetLaborMaterialLabels,
            getDocumentLabels: MvcEndpoints.Helpdesk.Extended.GetDocumentLabels,
            searchAsset: MvcEndpoints.Helpdesk.Search.Asset,
            searchAssetGroup: MvcEndpoints.Helpdesk.Search.AssetGroup
        },
        debounceDelay: 300,
        minSearchLength: 1
    };

    // Extended state for labor/material and assets
    const extendedState = {
        laborMaterialItems: {
            jobCode: [],
            adHoc: []
        },
        relatedAssets: {
            individual: [],
            groups: []
        },
        relatedDocuments: [],
        currentJobCodeSelection: null,
        currentAssetSelection: null,
        laborMaterialLabels: [],
        currencies: [],
        measurementUnits: [],
        documentLabels: [],
        editingItemIndex: null,
        editingItemType: null
    };

    // ========================================
    // Client Context Helpers (from main module)
    // ========================================

    /**
     * Get client parameters from main module.
     * Falls back to reading from window.WorkRequestContext if main module helpers not available.
     */
    const getClientParams = window.getClientParams || function() {
        const params = {};
        if (window.WorkRequestContext?.idClient) {
            params.idClient = window.WorkRequestContext.idClient;
        }
        if (window.WorkRequestContext?.idCompany) {
            params.idCompany = window.WorkRequestContext.idCompany;
        }
        return params;
    };

    /**
     * Merge client params with existing data object.
     */
    const withClientParams = window.withClientParams || function(data) {
        return { ...getClientParams(), ...data };
    };

    // ========================================
    // Loading State Helper Functions
    // ========================================

    /**
     * Show loading message in a typeahead dropdown
     * @param {jQuery} $dropdown - The dropdown element
     */
    function showTypeaheadLoading($dropdown) {
        $dropdown.empty().append(
            $('<div></div>')
                .addClass('typeahead-loading')
                .html('<i class="ti ti-loader me-1"></i>Searching...')
        ).addClass('show');
    }

    /**
     * Hide input loading state
     * @param {string} selector - The input selector
     */
    function hideInputLoading(selector) {
        $(selector).parent().removeClass('input-loading');
    }

    /**
     * Initialize extended functionality
     */
    function initExtended() {
        initializeLaborMaterialModal();
        initializeRelatedAssetModal();
        initializeRelatedDocuments();
        loadDropdownData();

        console.log('Extended Work Request Add functionality initialized');
    }

    /**
     * Load dropdown data for modals
     */
    function loadDropdownData() {
        // Load currencies
        $.ajax({
            url: EXTENDED_CONFIG.apiEndpoints.getCurrencies,
            method: 'GET',
            success: function(response) {
                if (response.success && response.data) {
                    extendedState.currencies = response.data;
                    const $select = $('#adHocCurrency');
                    $select.empty();
                    $.each(response.data, function(index, currency) {
                        $select.append(
                            $('<option></option>')
                                .val(currency.idEnum)
                                .text(currency.enumName)
                        );
                    });
                }
            },
            error: function(xhr, status, error) {
                console.error('Error loading currencies:', error);
            }
        });

        // Load measurement units
        $.ajax({
            url: EXTENDED_CONFIG.apiEndpoints.getMeasurementUnits,
            method: 'GET',
            success: function(response) {
                if (response.success && response.data) {
                    extendedState.measurementUnits = response.data;
                    const $select = $('#adHocMeasurementUnit');
                    $select.empty().append('<option value="">Select unit</option>');
                    $.each(response.data, function(index, unit) {
                        $select.append(
                            $('<option></option>')
                                .val(unit.idEnum)
                                .text(unit.enumName)
                        );
                    });
                }
            },
            error: function(xhr, status, error) {
                console.error('Error loading measurement units:', error);
            }
        });

        // Load labor/material labels (from Masters Enum API with 'materialLabel')
        $.ajax({
            url: EXTENDED_CONFIG.apiEndpoints.getLaborMaterialLabels,
            method: 'GET',
            success: function(response) {
                if (response.success && response.data) {
                    extendedState.laborMaterialLabels = response.data;
                    // Populate the Ad Hoc label radio buttons
                    const $container = $('#adHocLabelContainer');
                    $container.empty();
                    $.each(response.data, function(index, label) {
                        const radioId = 'label' + label.idEnum;
                        const isFirst = index === 0;
                        $container.append(`
                            <div class="form-check form-check-inline">
                                <input class="form-check-input" type="radio" name="adHocLabel"
                                       id="${radioId}" value="${label.idEnum}" ${isFirst ? 'checked' : ''}>
                                <label class="form-check-label" for="${radioId}">${label.enumName}</label>
                            </div>
                        `);
                    });
                }
            },
            error: function(xhr, status, error) {
                console.error('Error loading labor/material labels:', error);
            }
        });

        // Load document labels for Related Documents dropdown
        $.ajax({
            url: EXTENDED_CONFIG.apiEndpoints.getDocumentLabels,
            method: 'GET',
            success: function(response) {
                if (response.success && response.data) {
                    extendedState.documentLabels = response.data;
                }
            },
            error: function(xhr, status, error) {
                console.error('Error loading document labels:', error);
            }
        });
    }

    /**
     * Initialize Labor/Material Modal
     */
    function initializeLaborMaterialModal() {
        const $modal = $('#laborMaterialModal');

        // Open modal when Add Labor/Material button is clicked
        $('#addLaborMaterialBtn').on('click', function() {
            resetLaborMaterialModal();
            $modal.modal('show');
        });

        // Toggle between Job Code and Ad Hoc modes
        $('input[name="laborMaterialMode"]').on('change', function() {
            const mode = $(this).val();
            if (mode === 'jobCode') {
                $('#jobCodeMode').show();
                $('#adHocMode').hide();
            } else {
                $('#jobCodeMode').hide();
                $('#adHocMode').show();
            }
        });

        // Job Code search
        let jobCodeSearchTimeout;
        $('#jobCodeSearch').on('keyup', function() {
            clearTimeout(jobCodeSearchTimeout);
            const term = $(this).val().trim();
            const $input = $(this);

            if (term.length < EXTENDED_CONFIG.minSearchLength) {
                $('#jobCodeDropdown').removeClass('show').empty();
                return;
            }

            jobCodeSearchTimeout = setTimeout(function() {
                // Show loading state
                $input.parent().addClass('input-loading');
                showTypeaheadLoading($('#jobCodeDropdown'));
                searchJobCode(term);
            }, EXTENDED_CONFIG.debounceDelay);
        });

        // Remove selected job code
        $('#removeJobCodeBtn').on('click', function() {
            $('#jobCodeSearch').val('').show();
            $('#jobCodeSelected').hide();
            extendedState.currentJobCodeSelection = null;
        });

        // Set default transaction date to today
        const today = new Date().toISOString().split('T')[0];
        $('#jobCodeTransactionDate').val(today);

        // Save Labor/Material
        $('#saveLaborMaterialBtn').on('click', function() {
            saveLaborMaterial();
        });

        // Update unit display when measurement unit changes (Ad Hoc mode)
        $('#adHocMeasurementUnit').on('change', function() {
            const selectedUnit = extendedState.measurementUnits.find(u => u.idEnum == $(this).val());
            $('#adHocUnitDisplay').text(selectedUnit ? selectedUnit.enumName : 'UNIT');
        });

        // Close dropdown when clicking outside
        $(document).on('click', function(e) {
            if (!$(e.target).closest('#jobCodeSearch, #jobCodeDropdown').length) {
                $('#jobCodeDropdown').removeClass('show').empty();
            }
        });
    }

    /**
     * Search Job Code via API
     * API Response: JobCodeFormDetailResponse { IdJobCode, Name, Description, MinimumStock, UnitPrice, UnitPriceCurrency, LatestStock, LaborMaterialMeasurementUnit }
     */
    function searchJobCode(term) {
        $.ajax({
            url: EXTENDED_CONFIG.apiEndpoints.searchJobCode,
            method: 'GET',
            data: withClientParams({ term: term }),
            success: function(response) {
                const $dropdown = $('#jobCodeDropdown');
                $dropdown.empty();

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function(index, jobCode) {
                        // Use correct property names from JobCodeFormDetailResponse
                        const name = jobCode.Name || jobCode.name;
                        const latestStock = jobCode.LatestStock ?? jobCode.latestStock ?? 0;
                        const minimumStock = jobCode.MinimumStock ?? jobCode.minimumStock ?? 0;
                        const unitPrice = jobCode.UnitPrice ?? jobCode.unitPrice ?? 0;
                        const currency = jobCode.UnitPriceCurrency || jobCode.unitPriceCurrency || 'IDR';
                        const stockClass = latestStock > minimumStock ? 'text-success' : 'text-danger';

                        const priceDisplay = unitPrice > 0 ? `${currency} ${unitPrice.toLocaleString()}` : '-';

                        const $item = $('<div></div>')
                            .addClass('typeahead-item')
                            .html(`
                                <strong>${name}</strong><br>
                                <small class="${stockClass}">Stock: ${latestStock}</small>
                                <small class="text-muted ms-2">| Price: ${priceDisplay}</small>
                            `)
                            .on('click', function() {
                                selectJobCode(jobCode);
                            });
                        $dropdown.append($item);
                    });
                    $dropdown.addClass('show');
                } else {
                    $dropdown.append(
                        $('<div></div>')
                            .addClass('typeahead-item text-muted')
                            .text('No job codes found')
                    );
                    $dropdown.addClass('show');
                }
            },
            error: function(xhr, status, error) {
                console.error('Error searching job codes:', error);
                showNotification('Error searching job codes', 'error', 'Error');
                $('#jobCodeDropdown').removeClass('show').empty();
            },
            complete: function() {
                // Hide loading state
                hideInputLoading('#jobCodeSearch');
            }
        });
    }

    /**
     * Select a Job Code
     * Uses JobCodeFormDetailResponse properties: IdJobCode, Name, Description, MinimumStock, UnitPrice, UnitPriceCurrency, LatestStock, LaborMaterialMeasurementUnit
     */
    function selectJobCode(jobCode) {
        extendedState.currentJobCodeSelection = jobCode;

        $('#jobCodeSearch').hide();
        $('#jobCodeDropdown').removeClass('show').empty();

        // Use correct property names from JobCodeFormDetailResponse
        const name = jobCode.Name || jobCode.name;
        const latestStock = jobCode.LatestStock ?? jobCode.latestStock ?? 0;
        const minimumStock = jobCode.MinimumStock ?? jobCode.minimumStock ?? 0;
        const unit = jobCode.LaborMaterialMeasurementUnit || jobCode.laborMaterialMeasurementUnit || 'PCS';
        const unitPrice = jobCode.UnitPrice ?? jobCode.unitPrice ?? 0;
        const unitPriceCurrency = jobCode.UnitPriceCurrency || jobCode.unitPriceCurrency || 'IDR';

        // Apply stock color coding: green if > min, red if <= min
        const stockClass = latestStock > minimumStock ? 'text-success' : 'text-danger';

        $('#selectedJobCodeName').text(name);
        $('#selectedJobCodeStock')
            .text(latestStock)
            .removeClass('text-success text-danger')
            .addClass(stockClass);
        $('#jobCodeUnit').text(unit);
        $('#jobCodeUnitDisplay').text(unit);
        $('#selectedJobCodeCurrency').text(unitPriceCurrency);
        $('#selectedJobCodeUnitPrice').text(unitPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
        $('#jobCodeSelected').show();
    }

    /**
     * Reset Labor/Material Modal
     */
    function resetLaborMaterialModal() {
        // Reset mode to Job Code
        $('#modeJobCode').prop('checked', true);
        $('#jobCodeMode').show();
        $('#adHocMode').hide();

        // Re-enable mode switching
        $('input[name="laborMaterialMode"]').prop('disabled', false);

        // Reset Job Code fields
        $('#jobCodeSearch').val('').show();
        $('#jobCodeSelected').hide();
        $('#removeJobCodeBtn').show(); // Show remove button
        $('#jobCodeDropdown').removeClass('show').empty();
        const today = new Date().toISOString().split('T')[0];
        $('#jobCodeTransactionDate').val(today);
        $('#jobCodeQuantity').val('1.00');
        $('#jobCodeUnit').text('PCS');
        $('#jobCodeUnitDisplay').text('PCS');
        $('#selectedJobCodeCurrency').text('');
        $('#selectedJobCodeUnitPrice').text('');
        extendedState.currentJobCodeSelection = null;

        // Reset Ad Hoc fields
        $('#adHocName').val('');
        // Select first radio button if available
        $('input[name="adHocLabel"]').first().prop('checked', true);
        $('#adHocUnitPrice').val('');
        $('#adHocQuantity').val('1.00');
        if ($('#adHocMeasurementUnit option').length > 1) {
            $('#adHocMeasurementUnit').val('');
        }
        $('#adHocUnitDisplay').text('UNIT');

        // Reset edit mode
        extendedState.editingItemIndex = null;
        extendedState.editingItemType = null;
        $('#saveLaborMaterialBtn').html('<i class="ti ti-device-floppy"></i> Save').data('edit-mode', false);
        $('#laborMaterialModalLabel').text('Labor/Material');
    }

    /**
     * Save Labor/Material (from modal)
     */
    function saveLaborMaterial() {
        const mode = $('input[name="laborMaterialMode"]:checked').val();
        const isEditMode = $('#saveLaborMaterialBtn').data('edit-mode') === true;

        if (mode === 'jobCode') {
            saveLaborMaterialJobCode(isEditMode);
        } else {
            saveLaborMaterialAdHoc(isEditMode);
        }
    }

    /**
     * Save Labor/Material - Job Code Mode
     * Uses JobCodeFormDetailResponse properties: IdJobCode, Name, Description, MinimumStock, UnitPrice, UnitPriceCurrency, LatestStock, LaborMaterialMeasurementUnit
     */
    function saveLaborMaterialJobCode(isEditMode) {
        if (!extendedState.currentJobCodeSelection) {
            showNotification('Please select a job code', 'error', 'Validation Error');
            return;
        }

        const transactionDate = $('#jobCodeTransactionDate').val();
        if (!transactionDate) {
            showNotification('Please select a transaction date', 'error', 'Validation Error');
            return;
        }

        const quantity = parseFloat($('#jobCodeQuantity').val());
        if (!quantity || quantity <= 0) {
            showNotification('Please enter a valid quantity', 'error', 'Validation Error');
            return;
        }

        // Use correct property names from JobCodeFormDetailResponse
        // UnitPrice and UnitPriceCurrency come from the API, not user input
        const selection = extendedState.currentJobCodeSelection;
        const unitPrice = selection.UnitPrice ?? selection.unitPrice ?? 0;
        const currencyCode = selection.UnitPriceCurrency || selection.unitPriceCurrency || 'IDR';

        const item = {
            type: 'jobCode',
            idJobCode: selection.IdJobCode ?? selection.idJobCode,
            name: selection.Name || selection.name,
            description: selection.Description || selection.description || '',
            transactionDate: transactionDate,
            quantity: quantity,
            unitPrice: unitPrice,
            unit: selection.LaborMaterialMeasurementUnit || selection.laborMaterialMeasurementUnit || 'PCS',
            minimumStock: selection.MinimumStock ?? selection.minimumStock ?? 0,
            latestStock: selection.LatestStock ?? selection.latestStock ?? 0,
            currencyCode: currencyCode,
            currencyId: null // No longer selecting from dropdown
        };

        if (isEditMode && extendedState.editingItemIndex !== null) {
            // Update existing item
            extendedState.laborMaterialItems.jobCode[extendedState.editingItemIndex] = item;
            rebuildLaborMaterialTable();
            showNotification('Labor/Material updated successfully', 'success', 'Success');
        } else {
            // Add new item
            extendedState.laborMaterialItems.jobCode.push(item);
            addLaborMaterialToTable(item);
            showNotification('Labor/Material added successfully', 'success', 'Success');
        }

        $('#laborMaterialModal').modal('hide');
        resetLaborMaterialModal();
        updateCostEstimation();
    }

    /**
     * Save Labor/Material - Ad Hoc Mode
     */
    function saveLaborMaterialAdHoc(isEditMode) {
        const name = $('#adHocName').val().trim();
        if (!name) {
            showNotification('Please enter a name', 'error', 'Validation Error');
            return;
        }

        // Get selected label from radio button (value is the idEnum from API)
        const labelId = $('input[name="adHocLabel"]:checked').val();
        if (!labelId) {
            showNotification('Please select a label', 'error', 'Validation Error');
            return;
        }

        // Find the label name for display purposes
        const selectedLabel = extendedState.laborMaterialLabels.find(l => l.idEnum == labelId);
        const labelName = selectedLabel ? selectedLabel.enumName : 'Unknown';

        const currencyId = $('#adHocCurrency').val();
        const unitPrice = parseFloat($('#adHocUnitPrice').val());
        if (!unitPrice || unitPrice < 0) {
            showNotification('Please enter a valid unit price', 'error', 'Validation Error');
            return;
        }

        const measurementUnitId = $('#adHocMeasurementUnit').val();
        if (!measurementUnitId) {
            showNotification('Please select a measurement unit', 'error', 'Validation Error');
            return;
        }

        const quantity = parseFloat($('#adHocQuantity').val());
        if (!quantity || quantity <= 0) {
            showNotification('Please enter a valid quantity', 'error', 'Validation Error');
            return;
        }

        const measurementUnit = extendedState.measurementUnits.find(u => u.idEnum == measurementUnitId);
        const currency = extendedState.currencies.find(c => c.idEnum == currencyId);

        // Create item data
        const item = {
            type: 'adHoc',
            name: name,
            labelValue: labelName, // For display
            label_Enum_idEnum: parseInt(labelId),
            unitPriceCurrency_Enum_idEnum: currencyId ? parseInt(currencyId) : 1,
            unitPrice: unitPrice,
            quantity: quantity,
            measurementUnit_Enum_idEnum: parseInt(measurementUnitId),
            unit: measurementUnit ? measurementUnit.enumName : 'UNIT',
            currencyCode: currency ? currency.enumName : 'IDR',
            transactionDate: new Date().toISOString().split('T')[0]
        };

        if (isEditMode && extendedState.editingItemIndex !== null) {
            // Update existing item in state
            extendedState.laborMaterialItems.adHoc[extendedState.editingItemIndex] = item;
            rebuildLaborMaterialTable();
            $('#laborMaterialModal').modal('hide');
            resetLaborMaterialModal();
            updateCostEstimation();
            showNotification('Labor/Material updated successfully', 'success', 'Success');
        } else {
            // Add new item
            extendedState.laborMaterialItems.adHoc.push(item);
            addLaborMaterialToTable(item);
            $('#laborMaterialModal').modal('hide');
            resetLaborMaterialModal();
            updateCostEstimation();
            showNotification('Labor/Material added successfully', 'success', 'Success');
        }
    }

    /**
     * Add Labor/Material item to the table
     */
    function addLaborMaterialToTable(item) {
        const $tbody = $('#laborMaterialTable tbody');

        // Remove empty message if present
        if ($tbody.find('td[colspan]').length > 0) {
            $tbody.empty();
        }

        const rowIndex = $tbody.find('tr').length;

        // Determine prefix based on type
        const typePrefix = item.type === 'jobCode' ? '[Job Code]' : '[AdHoc]';
        const displayName = `${typePrefix} ${item.name}`;

        // Format unit price display
        const unitPriceDisplay = item.unitPrice ?
            `${item.currencyCode || 'IDR'} ${item.unitPrice.toFixed(2)}` :
            '-';

        // Format quantity with unit
        const qtyDisplay = `${item.quantity.toFixed(2)} ${item.unit || ''}`.trim();

        const row = `
            <tr data-labor-index="${rowIndex}" data-labor-type="${item.type}" data-item-data='${JSON.stringify(item).replace(/'/g, "&#39;")}'>
                <td>${escapeHtml(displayName)}</td>
                <td>${qtyDisplay}</td>
                <td>${unitPriceDisplay}</td>
                <td class="text-center">
                    <button type="button"
                            class="btn btn-sm btn-outline-primary me-1"
                            onclick="window.editLaborMaterialRow(this)"
                            title="Edit this item">
                        <i class="ti ti-pencil"></i>
                    </button>
                    <button type="button"
                            class="btn btn-sm btn-danger"
                            onclick="window.removeLaborMaterialRow(this)"
                            title="Remove this item">
                        <i class="ti ti-trash"></i>
                    </button>
                </td>
            </tr>
        `;

        $tbody.append(row);
        updateCostEstimation();
    }

    /**
     * Remove Labor/Material row (global function)
     */
    window.removeLaborMaterialRow = function(button) {
        const $row = $(button).closest('tr');
        const type = $row.data('labor-type');
        const index = $row.data('labor-index');

        // Remove from state arrays
        if (type === 'jobCode') {
            extendedState.laborMaterialItems.jobCode = extendedState.laborMaterialItems.jobCode.filter((_, i) => i !== index);
        } else {
            extendedState.laborMaterialItems.adHoc = extendedState.laborMaterialItems.adHoc.filter((_, i) => i !== index);
        }

        // Remove row
        $row.remove();

        // Re-index remaining rows and update state array indices
        rebuildLaborMaterialTable();

        updateCostEstimation();
        showNotification('Labor/Material removed', 'info', 'Info');
    };

    /**
     * Edit Labor/Material row (global function)
     */
    window.editLaborMaterialRow = function(button) {
        const $row = $(button).closest('tr');
        const type = $row.data('labor-type');
        const index = $row.data('labor-index');
        const itemData = $row.data('item-data');

        // Store which item is being edited
        extendedState.editingItemIndex = index;
        extendedState.editingItemType = type;

        // Open modal in edit mode
        if (type === 'jobCode') {
            openEditJobCodeModal(itemData);
        } else {
            openEditAdHocModal(itemData);
        }
    };

    /**
     * Open edit modal for Job Code item
     * Job Code items can only edit date and quantity (price comes from API)
     */
    function openEditJobCodeModal(itemData) {
        const $modal = $('#laborMaterialModal');

        // Set mode to Job Code
        $('#modeJobCode').prop('checked', true);
        $('#jobCodeMode').show();
        $('#adHocMode').hide();

        // Disable mode switching during edit
        $('input[name="laborMaterialMode"]').prop('disabled', true);

        // Hide job code search, show selected item (locked)
        $('#jobCodeSearch').hide();
        $('#jobCodeDropdown').removeClass('show').empty();

        // Show selected job code info (read-only)
        $('#selectedJobCodeName').text(itemData.name);
        $('#selectedJobCodeStock').text(itemData.latestStock || '-').removeClass('text-success text-danger');
        $('#jobCodeUnit').text(itemData.unit || 'PCS');
        $('#jobCodeUnitDisplay').text(itemData.unit || 'PCS');
        $('#selectedJobCodeCurrency').text(itemData.currencyCode || 'IDR');
        $('#selectedJobCodeUnitPrice').text((itemData.unitPrice || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
        $('#jobCodeSelected').show();

        // Hide remove button during edit (can't change job code)
        $('#removeJobCodeBtn').hide();

        // Set editable fields (only date and quantity)
        $('#jobCodeTransactionDate').val(itemData.transactionDate || new Date().toISOString().split('T')[0]);
        $('#jobCodeQuantity').val(itemData.quantity || 1);

        // Store the job code selection for update (including UnitPrice and UnitPriceCurrency)
        extendedState.currentJobCodeSelection = {
            IdJobCode: itemData.idJobCode,
            Name: itemData.name,
            Description: itemData.description,
            MinimumStock: itemData.minimumStock,
            LatestStock: itemData.latestStock,
            LaborMaterialMeasurementUnit: itemData.unit,
            UnitPrice: itemData.unitPrice,
            UnitPriceCurrency: itemData.currencyCode
        };

        // Change save button to update mode
        $('#saveLaborMaterialBtn').text('Update').data('edit-mode', true);

        // Update modal title
        $('#laborMaterialModalLabel').text('Edit Labor/Material');

        $modal.modal('show');
    }

    /**
     * Open edit modal for AdHoc item
     * AdHoc items can edit all fields
     */
    function openEditAdHocModal(itemData) {
        const $modal = $('#laborMaterialModal');

        // Set mode to Ad Hoc
        $('#modeAdHoc').prop('checked', true);
        $('#jobCodeMode').hide();
        $('#adHocMode').show();

        // Disable mode switching during edit
        $('input[name="laborMaterialMode"]').prop('disabled', true);

        // Populate fields
        $('#adHocName').val(itemData.name);

        // Set label radio by idEnum value
        const labelId = itemData.label_Enum_idEnum;
        if (labelId) {
            $(`input[name="adHocLabel"][value="${labelId}"]`).prop('checked', true);
        }

        // Set currency
        if (itemData.unitPriceCurrency_Enum_idEnum) {
            $('#adHocCurrency').val(itemData.unitPriceCurrency_Enum_idEnum);
        }

        $('#adHocUnitPrice').val(itemData.unitPrice || '');
        $('#adHocQuantity').val(itemData.quantity || 1);

        // Set measurement unit
        if (itemData.measurementUnit_Enum_idEnum) {
            $('#adHocMeasurementUnit').val(itemData.measurementUnit_Enum_idEnum);
        }

        // Update the unit display
        const measurementUnit = extendedState.measurementUnits.find(u => u.idEnum == itemData.measurementUnit_Enum_idEnum);
        if (measurementUnit) {
            $('#adHocUnitDisplay').text(measurementUnit.enumName);
        }

        // Change save button to update mode
        $('#saveLaborMaterialBtn').text('Update').data('edit-mode', true);

        // Update modal title
        $('#laborMaterialModalLabel').text('Edit Labor/Material');

        $modal.modal('show');
    }

    /**
     * Rebuild labor/material table from state
     */
    function rebuildLaborMaterialTable() {
        const $tbody = $('#laborMaterialTable tbody');
        $tbody.empty();

        const allItems = [
            ...extendedState.laborMaterialItems.jobCode.map(item => ({ ...item, type: 'jobCode' })),
            ...extendedState.laborMaterialItems.adHoc.map(item => ({ ...item, type: 'adHoc' }))
        ];

        if (allItems.length === 0) {
            $tbody.html(`
                <tr>
                    <td colspan="4" class="text-center text-muted">
                        <em>No Labor/Material added yet</em>
                    </td>
                </tr>
            `);
            return;
        }

        // Re-add all items
        allItems.forEach((item, index) => {
            const typePrefix = item.type === 'jobCode' ? '[Job Code]' : '[AdHoc]';
            const displayName = `${typePrefix} ${item.name}`;
            const unitPriceDisplay = item.unitPrice ?
                `${item.currencyCode || 'IDR'} ${item.unitPrice.toFixed(2)}` :
                '-';
            const qtyDisplay = `${item.quantity.toFixed(2)} ${item.unit || ''}`.trim();

            const row = `
                <tr data-labor-index="${index}" data-labor-type="${item.type}" data-item-data='${JSON.stringify(item).replace(/'/g, "&#39;")}'>
                    <td>${escapeHtml(displayName)}</td>
                    <td>${qtyDisplay}</td>
                    <td>${unitPriceDisplay}</td>
                    <td class="text-center">
                        <button type="button"
                                class="btn btn-sm btn-outline-primary me-1"
                                onclick="window.editLaborMaterialRow(this)"
                                title="Edit this item">
                            <i class="ti ti-pencil"></i>
                        </button>
                        <button type="button"
                                class="btn btn-sm btn-danger"
                                onclick="window.removeLaborMaterialRow(this)"
                                title="Remove this item">
                            <i class="ti ti-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
            $tbody.append(row);
        });
    }

    /**
     * Update cost estimation based on labor/material total
     * Calculates: sum of (quantity * unitPrice) for all items
     */
    function updateCostEstimation() {
        let total = 0;

        // Calculate from job code items
        extendedState.laborMaterialItems.jobCode.forEach(item => {
            const qty = parseFloat(item.quantity) || 0;
            const price = parseFloat(item.unitPrice) || 0;
            total += qty * price;
        });

        // Calculate from ad-hoc items
        extendedState.laborMaterialItems.adHoc.forEach(item => {
            const qty = parseFloat(item.quantity) || 0;
            const price = parseFloat(item.unitPrice) || 0;
            total += qty * price;
        });

        // Always update cost estimation with the calculated total
        const $costEstimation = $('#costEstimation');
        $costEstimation.val(total.toFixed(0));
    }

    /**
     * Initialize Related Asset Modal
     */
    function initializeRelatedAssetModal() {
        const $modal = $('#relatedAssetModal');

        // Open modal when Add Asset button is clicked
        $('#addAssetBtn').on('click', function() {
            resetAssetModal();
            $modal.modal('show');
        });

        // Toggle between Individual and Group search modes
        $('input[name="assetSearchMode"]').on('change', function() {
            const mode = $(this).val();
            if (mode === 'individual') {
                $('#individualSearchMode').show();
                $('#groupSearchMode').hide();
            } else {
                $('#individualSearchMode').hide();
                $('#groupSearchMode').show();
            }
        });

        // Asset Individual search
        let assetSearchTimeout;
        $('#assetIndividualSearch').on('keyup', function() {
            clearTimeout(assetSearchTimeout);
            const term = $(this).val().trim();
            const $input = $(this);

            if (term.length < EXTENDED_CONFIG.minSearchLength) {
                $('#assetIndividualDropdown').removeClass('show').empty();
                return;
            }

            assetSearchTimeout = setTimeout(function() {
                // Show loading state
                $input.parent().addClass('input-loading');
                showTypeaheadLoading($('#assetIndividualDropdown'));
                searchAssetIndividual(term);
            }, EXTENDED_CONFIG.debounceDelay);
        });

        // Asset Group search
        let assetGroupSearchTimeout;
        $('#assetGroupSearch').on('keyup', function() {
            clearTimeout(assetGroupSearchTimeout);
            const term = $(this).val().trim();
            const $input = $(this);

            if (term.length < EXTENDED_CONFIG.minSearchLength) {
                $('#assetGroupDropdown').removeClass('show').empty();
                return;
            }

            assetGroupSearchTimeout = setTimeout(function() {
                // Show loading state
                $input.parent().addClass('input-loading');
                showTypeaheadLoading($('#assetGroupDropdown'));
                searchAssetGroup(term);
            }, EXTENDED_CONFIG.debounceDelay);
        });

        // Remove selected individual asset
        $('#removeAssetIndividualBtn').on('click', function() {
            $('#assetIndividualSearch').val('').show();
            $('#assetIndividualSelected').hide();
            extendedState.currentAssetSelection = null;
        });

        // Remove selected asset group
        $('#removeAssetGroupBtn').on('click', function() {
            $('#assetGroupSearch').val('').show();
            $('#assetGroupSelected').hide();
            extendedState.currentAssetSelection = null;
        });

        // Save Asset
        $('#saveAssetBtn').on('click', function() {
            saveAsset();
        });

        // Close dropdowns when clicking outside
        $(document).on('click', function(e) {
            if (!$(e.target).closest('#assetIndividualSearch, #assetIndividualDropdown').length) {
                $('#assetIndividualDropdown').removeClass('show').empty();
            }
            if (!$(e.target).closest('#assetGroupSearch, #assetGroupDropdown').length) {
                $('#assetGroupDropdown').removeClass('show').empty();
            }
        });
    }

    /**
     * Search Asset Individual via API
     */
    function searchAssetIndividual(term) {
        const propertyId = $('#locationSelect').val();
        if (!propertyId) {
            showNotification('Please select a location first', 'warning', 'Warning');
            hideInputLoading('#assetIndividualSearch');
            $('#assetIndividualDropdown').removeClass('show').empty();
            return;
        }

        $.ajax({
            url: EXTENDED_CONFIG.apiEndpoints.searchAsset,
            method: 'GET',
            data: withClientParams({ term: term, propertyId: propertyId }),
            success: function(response) {
                const $dropdown = $('#assetIndividualDropdown');
                $dropdown.empty();

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function(index, asset) {
                        const $item = $('<div></div>')
                            .addClass('typeahead-item')
                            .html(`
                                <strong>${asset.label}</strong><br>
                                <small class="text-muted">${asset.name || ''}</small>
                            `)
                            .on('click', function() {
                                selectAssetIndividual(asset);
                            });
                        $dropdown.append($item);
                    });
                    $dropdown.addClass('show');
                } else {
                    $dropdown.append(
                        $('<div></div>')
                            .addClass('typeahead-item text-muted')
                            .text('No assets found')
                    );
                    $dropdown.addClass('show');
                }
            },
            error: function(xhr, status, error) {
                console.error('Error searching assets:', error);
                showNotification('Error searching assets', 'error', 'Error');
                $('#assetIndividualDropdown').removeClass('show').empty();
            },
            complete: function() {
                // Hide loading state
                hideInputLoading('#assetIndividualSearch');
            }
        });
    }

    /**
     * Select an Individual Asset
     */
    function selectAssetIndividual(asset) {
        extendedState.currentAssetSelection = {
            type: 'individual',
            data: asset
        };

        $('#assetIndividualSearch').hide();
        $('#assetIndividualDropdown').removeClass('show').empty();

        $('#selectedAssetName').text(`${asset.label} - ${asset.name || ''}`);
        $('#assetIndividualSelected').show();
    }

    /**
     * Search Asset Group via API
     */
    function searchAssetGroup(term) {
        const propertyId = $('#locationSelect').val();
        if (!propertyId) {
            showNotification('Please select a location first', 'warning', 'Warning');
            hideInputLoading('#assetGroupSearch');
            $('#assetGroupDropdown').removeClass('show').empty();
            return;
        }

        $.ajax({
            url: EXTENDED_CONFIG.apiEndpoints.searchAssetGroup,
            method: 'GET',
            data: withClientParams({ term: term, propertyId: propertyId }),
            success: function(response) {
                const $dropdown = $('#assetGroupDropdown');
                $dropdown.empty();

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function(index, group) {
                        const assetCount = group.asset ? group.asset.length : 0;
                        const $item = $('<div></div>')
                            .addClass('typeahead-item')
                            .html(`
                                <strong>${group.assetGroupName}</strong><br>
                                <small class="text-muted">${assetCount} asset(s)</small>
                            `)
                            .on('click', function() {
                                selectAssetGroup(group);
                            });
                        $dropdown.append($item);
                    });
                    $dropdown.addClass('show');
                } else {
                    $dropdown.append(
                        $('<div></div>')
                            .addClass('typeahead-item text-muted')
                            .text('No asset groups found')
                    );
                    $dropdown.addClass('show');
                }
            },
            error: function(xhr, status, error) {
                console.error('Error searching asset groups:', error);
                showNotification('Error searching asset groups', 'error', 'Error');
                $('#assetGroupDropdown').removeClass('show').empty();
            },
            complete: function() {
                // Hide loading state
                hideInputLoading('#assetGroupSearch');
            }
        });
    }

    /**
     * Select an Asset Group
     */
    function selectAssetGroup(group) {
        extendedState.currentAssetSelection = {
            type: 'group',
            data: group
        };

        $('#assetGroupSearch').hide();
        $('#assetGroupDropdown').removeClass('show').empty();

        const $list = $('#selectedAssetGroupList');
        $list.empty();

        if (group.asset && group.asset.length > 0) {
            $.each(group.asset, function(index, asset) {
                $list.append(`<li>${asset.label} - ${asset.name || ''}</li>`);
            });
        } else {
            $list.append('<li class="text-muted">No assets in this group</li>');
        }

        $('#assetGroupSelected').show();
    }

    /**
     * Reset Asset Modal
     */
    function resetAssetModal() {
        // Reset mode to Individual
        $('#searchIndividual').prop('checked', true);
        $('#individualSearchMode').show();
        $('#groupSearchMode').hide();

        // Reset Individual search
        $('#assetIndividualSearch').val('').show();
        $('#assetIndividualSelected').hide();
        $('#assetIndividualDropdown').removeClass('show').empty();

        // Reset Group search
        $('#assetGroupSearch').val('').show();
        $('#assetGroupSelected').hide();
        $('#assetGroupDropdown').removeClass('show').empty();

        extendedState.currentAssetSelection = null;
    }

    /**
     * Save Asset (from modal)
     */
    function saveAsset() {
        if (!extendedState.currentAssetSelection) {
            showNotification('Please select an asset or asset group', 'error', 'Validation Error');
            return;
        }

        const selection = extendedState.currentAssetSelection;

        if (selection.type === 'individual') {
            extendedState.relatedAssets.individual.push(selection.data);
            addAssetToTable(selection.data);
        } else {
            if (selection.data.asset && selection.data.asset.length > 0) {
                $.each(selection.data.asset, function(index, asset) {
                    extendedState.relatedAssets.individual.push(asset);
                    addAssetToTable(asset);
                });
                extendedState.relatedAssets.groups.push(selection.data);
            } else {
                showNotification('No assets found in this group', 'warning', 'Warning');
                return;
            }
        }

        $('#relatedAssetModal').modal('hide');
        showNotification('Asset(s) added successfully', 'success', 'Success');
    }

    /**
     * Add Asset to the table
     */
    function addAssetToTable(asset) {
        const $tbody = $('#relatedAssetTable tbody');

        // Remove empty message if present
        if ($tbody.find('td[colspan]').length > 0) {
            $tbody.empty();
        }

        const rowCount = $tbody.find('tr').length + 1;

        const row = `
            <tr data-asset-id="${asset.idAsset}">
                <td>${rowCount}</td>
                <td>${asset.label || '-'}</td>
                <td>${asset.name || '-'}</td>
                <td class="text-center">
                    <button type="button"
                            class="btn btn-sm btn-danger"
                            onclick="window.removeAssetRow(this)"
                            title="Remove this asset">
                        <i class="ti ti-trash"></i>
                    </button>
                </td>
            </tr>
        `;

        $tbody.append(row);
    }

    /**
     * Remove Asset row (global function)
     */
    window.removeAssetRow = function(button) {
        const $row = $(button).closest('tr');
        const assetId = $row.data('asset-id');

        // Remove from state
        extendedState.relatedAssets.individual = extendedState.relatedAssets.individual.filter(a => a.idAsset !== assetId);

        // Remove row
        $row.remove();

        // Re-number rows
        $('#relatedAssetTable tbody tr').each(function(i) {
            $(this).find('td').first().text(i + 1);
        });

        // Check if table is empty
        if ($('#relatedAssetTable tbody tr').length === 0) {
            $('#relatedAssetTable tbody').html(`
                <tr>
                    <td colspan="4" class="text-center text-muted">
                        <em>No Related Asset</em>
                    </td>
                </tr>
            `);
        }

        showNotification('Asset removed', 'info', 'Info');
    };

    /**
     * Initialize Related Documents
     */
    function initializeRelatedDocuments() {
        // Upload button click handler
        $('#uploadDocumentBtn').on('click', function() {
            const fileInput = document.getElementById('relatedDocuments');
            const files = fileInput.files;

            if (files.length === 0) {
                showNotification('Please select at least one file', 'warning', 'Warning');
                return;
            }

            // Add files to state and table
            for (let i = 0; i < files.length; i++) {
                const file = files[i];
                addDocumentToTable(file);
            }

            // Clear file input
            fileInput.value = '';
            showNotification(`${files.length} file(s) added successfully`, 'success', 'Success');
        });

        // Also allow Enter key or change event to trigger upload
        $('#relatedDocuments').on('change', function() {
            if (this.files.length > 0) {
                $('#uploadDocumentBtn').click();
            }
        });
    }

    /**
     * Add document to the table
     */
    function addDocumentToTable(file) {
        const $tbody = $('#relatedDocumentsTable tbody');

        // Remove empty message if present
        if ($tbody.find('td[colspan]').length > 0) {
            $tbody.empty();
        }

        const documentId = Date.now() + '_' + Math.random().toString(36).substr(2, 9);
        const fileSize = formatFileSize(file.size);

        // Add to state
        extendedState.relatedDocuments.push({
            id: documentId,
            file: file,
            label: file.name,
            fileName: file.name,
            fileSize: fileSize
        });

        // Build document label dropdown options
        const labelOptions = extendedState.documentLabels.map(label =>
            `<option value="${label.idType}" data-type-name="${escapeHtml(label.typeName)}">${escapeHtml(label.typeName)}</option>`
        ).join('');

        const row = `
            <tr data-document-id="${documentId}">
                <td>
                    <select class="form-select form-select-sm document-label">
                        <option value="">Select Document Label</option>
                        ${labelOptions}
                    </select>
                </td>
                <td>
                    <span class="text-muted">${escapeHtml(file.name)}</span>
                    <small class="text-muted d-block">${fileSize}</small>
                </td>
                <td class="text-center">
                    <button type="button"
                            class="btn btn-sm btn-danger"
                            onclick="window.removeDocumentRow(this)"
                            title="Remove this document">
                        <i class="ti ti-trash"></i>
                    </button>
                </td>
            </tr>
        `;

        $tbody.append(row);
    }

    /**
     * Remove document row (global function)
     */
    window.removeDocumentRow = function(button) {
        const $row = $(button).closest('tr');
        const documentId = $row.data('document-id');

        // Remove from state
        extendedState.relatedDocuments = extendedState.relatedDocuments.filter(doc => doc.id !== documentId);

        // Remove row
        $row.remove();

        // Check if table is empty
        if ($('#relatedDocumentsTable tbody tr').length === 0) {
            $('#relatedDocumentsTable tbody').html(`
                <tr>
                    <td colspan="3" class="text-center text-muted">
                        <em>No File</em>
                    </td>
                </tr>
            `);
        }

        showNotification('Document removed', 'info', 'Info');
    };

    /**
     * Format file size
     */
    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
    }

    /**
     * Escape HTML to prevent XSS
     */
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Extend form submission to include labor/material, assets, and documents
     * Integrates with the main form submission in work-request-add.js
     */
    function extendFormSubmission() {
        const $form = $('#workRequestForm');

        // Store the original buildWorkRequestPayload function
        const originalBuildPayload = window.buildWorkRequestPayload;
        const originalSubmitWorkRequest = window.submitWorkRequest;

        // Override buildWorkRequestPayload to add extended data
        window.buildWorkRequestPayload = function() {
            // Get the base payload from original function
            const payload = originalBuildPayload();

            // Add Material_Jobcode array
            payload.Material_Jobcode = extendedState.laborMaterialItems.jobCode.map(item => ({
                idJobCode: item.idJobCode,
                jobCode: item.name || '', // Include job code name
                quantity: item.quantity,
                unitPrice: item.unitPrice || 0
            }));

            // Add Material_Adhoc array
            payload.Material_Adhoc = extendedState.laborMaterialItems.adHoc.map(item => ({
                name: item.name,
                label_Enum_idEnum: item.label_Enum_idEnum,
                unitPriceCurrency_Enum_idEnum: item.unitPriceCurrency_Enum_idEnum,
                unitPrice: item.unitPrice,
                quantity: item.quantity,
                measurementUnit_Enum_idEnum: item.measurementUnit_Enum_idEnum
            }));

            // Add Assets array
            payload.Assets = extendedState.relatedAssets.individual.map(asset => ({
                idAsset: asset.idAsset,
                asset: asset.label || asset.name || '' // Include asset label/name
            }));

            return payload;
        };

        // Override submitWorkRequest to handle document conversion
        window.submitWorkRequest = async function(payload) {
            // If there are documents, convert them to base64 first
            if (extendedState.relatedDocuments.length > 0) {
                try {
                    payload.RelatedDocuments = await convertDocumentsToBase64();
                } catch (error) {
                    console.error('Error converting documents:', error);
                    showNotification('Error processing documents. Please try again.', 'error');
                    hideLoadingOverlay();
                    return;
                }
            }

            // Call original submit function
            originalSubmitWorkRequest(payload);
        };
    }

    /**
     * Convert uploaded documents to base64 format for API submission
     * Returns a promise that resolves to an array of document objects
     */
    async function convertDocumentsToBase64() {
        const documentPromises = extendedState.relatedDocuments.map(async (doc) => {
            // Get selected document label from dropdown
            const $row = $(`tr[data-document-id="${doc.id}"]`);
            const $select = $row.find('.document-label');
            const label = $select.find('option:selected').data('type-name') || doc.fileName;

            // Read file as base64
            const base64Content = await readFileAsBase64(doc.file);

            // Extract extension from filename
            const extension = doc.fileName.split('.').pop() || '';

            return {
                idDocument: 0, // New document
                documentName: label,
                fileName: doc.fileName,
                fileSize: doc.file.size,
                extension: extension,
                documentUrl: '', // Will be set by backend
                base64: base64Content,
                documentType: doc.file.type || 'application/octet-stream'
            };
        });

        return Promise.all(documentPromises);
    }

    /**
     * Read a file and return its base64 content (without data URL prefix)
     */
    function readFileAsBase64(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();

            reader.onload = function(e) {
                // Remove the data URL prefix (e.g., "data:application/pdf;base64,")
                const base64String = e.target.result.split(',')[1];
                resolve(base64String);
            };

            reader.onerror = function(error) {
                reject(error);
            };

            reader.readAsDataURL(file);
        });
    }

    /**
     * Hide loading overlay (utility function)
     */
    function hideLoadingOverlay() {
        $('#loading-overlay').remove();
    }

    // Initialize when document is ready
    $(document).ready(function() {
        initExtended();
        extendFormSubmission();
    });

})(jQuery);
