/**
 * Inventory Management Page
 * Handles CRUD operations for inventory transactions
 */

$(document).ready(function () {
    // Initialize popovers
    $('[data-bs-toggle="popover"]').popover({
        container: 'body',
        placement: 'top',
        trigger: 'hover'
    });

    // Transaction Modal State
    let currentTransactionId = 0;
    let currentEditMode = 'create';
    let selectedMaterial = null;
    let materialSearchTimeout = null;
    let transactionStatuses = [];

    // ============================
    // Filters & Search
    // ============================

    $('#clearFiltersBtn').on('click', function() {
        $('#filterForm')[0].reset();
        $('#filterForm input[type="checkbox"]').prop('checked', false);

        var searchTerm = $('#mainSearchInput').val();
        var url = new URL(window.location.origin + '/Inventory/Index');
        if (searchTerm && searchTerm.trim() !== '') {
            url.searchParams.set('search', searchTerm.trim());
        }
        window.location.href = url.toString();
    });

    $('#applyFiltersBtn').on('click', function() {
        var movementTypes = $('input[name="movementTypes"]:checked')
            .map(function() { return this.value; }).get();

        var url = new URL(window.location.origin + '/Inventory/Index');

        // Add movement types filter
        if (movementTypes.length > 0) {
            url.searchParams.set('movementTypes', movementTypes.join(','));
        }

        // Add date filters
        const transactionDateStart = $('#transactionDateFrom').val();
        const transactionDateEnd = $('#transactionDateTo').val();
        if (transactionDateStart)
            url.searchParams.set('transactionDateStart', transactionDateStart);
        if (transactionDateEnd)
            url.searchParams.set('transactionDateEnd', transactionDateEnd);

        // Preserve search term
        var searchTerm = $('#mainSearchInput').val();
        if (searchTerm && searchTerm.trim() !== '') {
            url.searchParams.set('search', searchTerm.trim());
        }

        url.searchParams.set('page', '1');
        window.location.href = url.toString();
    });

    $('#searchButton').on('click', function() {
        var searchTerm = $('#mainSearchInput').val();
        var currentUrl = new URL(window.location.href);

        if (searchTerm && searchTerm.trim() !== '') {
            currentUrl.searchParams.set('search', searchTerm.trim());
        } else {
            currentUrl.searchParams.delete('search');
        }

        currentUrl.searchParams.delete('page');
        window.location.href = currentUrl.toString();
    });

    $('#mainSearchInput').on('keypress', function(e) {
        if (e.which === 13) {
            $('#searchButton').click();
        }
    });

    $('#clearSearchButton').on('click', function() {
        $('#mainSearchInput').val('');
        var currentUrl = new URL(window.location.href);
        currentUrl.searchParams.delete('search');
        currentUrl.searchParams.delete('page');
        window.location.href = currentUrl.toString();
    });

    // Restore date filters from URL
    function getQueryParam(name) {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(name);
    }

    const transactionDateStart = getQueryParam('transactionDateStart');
    if (transactionDateStart) $('#transactionDateFrom').val(transactionDateStart);

    const transactionDateEnd = getQueryParam('transactionDateEnd');
    if (transactionDateEnd) $('#transactionDateTo').val(transactionDateEnd);

    // ============================
    // Transaction Status Loading
    // ============================

    function loadTransactionStatuses() {
        $.ajax({
            url: '/Inventory/GetTransactionStatuses',
            method: 'GET',
            success: function(response) {
                if (response.success && response.data) {
                    transactionStatuses = response.data;
                    populateTransactionStatusDropdown();
                    populateTransactionStatusFilters();
                }
            },
            error: function(xhr, status, error) {
                console.error('Error loading transaction statuses:', error);
            }
        });
    }

    function populateTransactionStatusDropdown() {
        const $select = $('#stockMovementType');
        $select.empty().append('<option value="">Select Stock Movement Type</option>');

        transactionStatuses.forEach(status => {
            $select.append($('<option></option>')
                .val(status.idEnum)
                .text(status.enumName));
        });
    }

    function populateTransactionStatusFilters() {
        const $container = $('#transactionStatusFilters');
        $container.empty();

        transactionStatuses.forEach(status => {
            const sanitizedId = status.enumName.replace(/\s+/g, '');
            const $item = $(`
                <div class="filter-checkbox-item form-check">
                    <input class="form-check-input" type="checkbox"
                           value="${status.idEnum}"
                           id="status_${sanitizedId}"
                           name="movementTypes">
                    <label class="form-check-label" for="status_${sanitizedId}">
                        ${status.enumName}
                    </label>
                </div>
            `);
            $container.append($item);
        });

        // Restore selected filters from URL
        const urlParams = new URLSearchParams(window.location.search);
        const movementTypes = urlParams.get('movementTypes');
        if (movementTypes) {
            movementTypes.split(',').forEach(id => {
                $(`input[name="movementTypes"][value="${id}"]`).prop('checked', true);
            });
        }
    }

    // ============================
    // Transaction Modal
    // ============================

    // Open Add Modal
    $('a[href*="/Inventory/Add"]').on('click', function(e) {
        e.preventDefault();
        openTransactionModal('create');
    });

    // Open Edit Modal
    $(document).on('click', '.btn-edit-transaction', function() {
        const transactionId = $(this).data('id');
        openTransactionModal('edit', transactionId);
    });

    function openTransactionModal(mode, transactionId = 0) {
        currentEditMode = mode;
        currentTransactionId = transactionId;
        selectedMaterial = null;

        $('#transactionForm')[0].reset();
        $('#transactionId').val(transactionId);
        $('#transactionModalLabel').text(mode === 'create' ? 'Add Inventory Transaction' : 'Edit Inventory Transaction');

        // Reset material selection
        $('#materialSearchContainer').show();
        $('#materialSelected').hide();
        $('#materialSearch').val('');
        $('#materialDropdown').removeClass('show').empty();

        // Set today's date as default
        const today = new Date().toISOString().split('T')[0];
        $('#transactionDate').val(today);

        // Reset quantity unit
        $('#quantityUnit').text('PCS');

        if (mode === 'edit' && transactionId > 0) {
            loadTransactionData(transactionId);
        }

        $('#transactionModal').modal('show');
    }

    function loadTransactionData(id) {
        $.ajax({
            url: '/Inventory/GetById',
            method: 'GET',
            data: { id: id },
            success: function(response) {
                if (response.success && response.data) {
                    const data = response.data;

                    $('#stockMovementType').val(data.inventoryStatus_IdEnum);
                    $('#transactionDate').val(data.transactionDate.split('T')[0]);
                    $('#quantity').val(data.quantity);
                    $('#description').val(data.description);

                    // Set material (show as selected card)
                    selectedMaterial = {
                        id: data.material.id,
                        name: data.material.name
                    };
                    showSelectedMaterial();
                } else {
                    showNotification(response.message || 'Failed to load transaction data', 'error', 'Error');
                }
            },
            error: function(xhr, status, error) {
                console.error('Error loading transaction:', error);
                showNotification('Error loading transaction data', 'error', 'Error');
            }
        });
    }

    // ============================
    // Material Search
    // ============================

    $('#materialSearch').on('keyup', function() {
        clearTimeout(materialSearchTimeout);
        const term = $(this).val().trim();

        if (term.length === 0) {
            $('#materialDropdown').removeClass('show').empty();
            return;
        }

        materialSearchTimeout = setTimeout(function() {
            searchMaterials(term);
        }, 300);
    });

    function searchMaterials(term) {
        const $dropdown = $('#materialDropdown');

        // Show loading spinner
        $dropdown.empty().html(`
            <div class="typeahead-item text-center">
                <div class="spinner-border spinner-border-sm text-primary me-2" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <span class="text-muted">Searching materials...</span>
            </div>
        `).addClass('show');

        $.ajax({
            url: '/Inventory/SearchMaterials',
            method: 'GET',
            data: { term: term },
            success: function(response) {
                $dropdown.empty();

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function(index, material) {
                        const $item = $('<div></div>')
                            .addClass('typeahead-item')
                            .html(`
                                <div>
                                    <strong>${material.name}</strong><br>
                                    <small class="text-muted">Material ID: ${material.id}</small>
                                </div>
                            `)
                            .on('click', function() {
                                selectMaterial(material);
                            });
                        $dropdown.append($item);
                    });
                    $dropdown.addClass('show');
                } else {
                    $dropdown.append(
                        $('<div></div>')
                            .addClass('typeahead-item text-muted')
                            .text('No materials found')
                    );
                    $dropdown.addClass('show');
                }
            },
            error: function(xhr, status, error) {
                console.error('Error searching materials:', error);
                $dropdown.empty().append(
                    $('<div></div>')
                        .addClass('typeahead-item text-danger')
                        .html('<i class="ti ti-alert-circle me-2"></i>Error searching materials')
                );
                showNotification('Error searching materials', 'error', 'Error');
            }
        });
    }

    function selectMaterial(material) {
        selectedMaterial = material;
        showSelectedMaterial();
    }

    function showSelectedMaterial() {
        $('#materialSearchContainer').hide();
        $('#materialDropdown').removeClass('show').empty();

        $('#selectedMaterialName').text(selectedMaterial.name);
        $('#selectedMaterialCode').text('ID: ' + selectedMaterial.id);
        $('#selectedMaterialStock').text('N/A');
        $('#selectedMaterialId').val(selectedMaterial.id);

        $('#materialSelected').show();
    }

    $('#removeMaterialBtn').on('click', function() {
        selectedMaterial = null;
        $('#materialSelected').hide();
        $('#materialSearchContainer').show();
        $('#materialSearch').val('').focus();
        $('#selectedMaterialId').val('');
        $('#selectedMaterialUnit').val('');
        $('#quantityUnit').text('PCS');
    });

    // ============================
    // Save Transaction
    // ============================

    $('#saveTransactionBtn').on('click', function() {
        if (!validateTransactionForm()) {
            return;
        }

        const payload = {
            idInventoryTransactionHistory: currentEditMode === 'edit' ? currentTransactionId : 0,
            inventoryStatus_IdEnum: parseInt($('#stockMovementType').val()),
            transactionDate: $('#transactionDate').val(),
            material_IdJobCode: parseInt($('#selectedMaterialId').val()),
            quantity: parseFloat($('#quantity').val()),
            description: $('#description').val()
        };

        const url = currentEditMode === 'create' ? '/Inventory/Create' : '/Inventory/Update';

        // Debug: Log CSRF token info
        const token = $('input[name="__RequestVerificationToken"]').val();
        console.log('=== CSRF Token Debug ===');
        console.log('Token found:', token ? 'YES' : 'NO');
        console.log('Token value:', token);
        console.log('Token length:', token ? token.length : 0);
        console.log('Request URL:', url);
        console.log('Payload:', JSON.stringify(payload, null, 2));

        $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            beforeSend: function(xhr, settings) {
                // Manually add CSRF token (our beforeSend overwrites the global one)
                if (!settings.crossDomain && token) {
                    xhr.setRequestHeader('X-CSRF-TOKEN', token);
                    console.log('=== CSRF Token Added ===');
                    console.log('X-CSRF-TOKEN header:', token.substring(0, 20) + '...');
                }
            },
            success: function(response) {
                if (response.success) {
                    showNotification(response.message || 'Transaction saved successfully!', 'success', 'Success');
                    $('#transactionModal').modal('hide');
                    setTimeout(() => {
                        location.reload();
                    }, 1500);
                } else {
                    showNotification(response.message || 'Failed to save transaction', 'error', 'Error');
                }
            },
            error: function(xhr, status, error) {
                console.error('Error saving transaction:', error);
                showNotification('Error saving transaction', 'error', 'Error');
            }
        });
    });

    function validateTransactionForm() {
        const stockMovementType = $('#stockMovementType').val();
        if (!stockMovementType) {
            showNotification('Please select a stock movement type', 'error', 'Validation Error');
            return false;
        }

        const transactionDate = $('#transactionDate').val();
        if (!transactionDate) {
            showNotification('Please select a transaction date', 'error', 'Validation Error');
            return false;
        }

        if (!selectedMaterial || !$('#selectedMaterialId').val()) {
            showNotification('Please select a material', 'error', 'Validation Error');
            return false;
        }

        const quantity = parseFloat($('#quantity').val());
        if (!quantity || quantity <= 0) {
            showNotification('Please enter a valid quantity', 'error', 'Validation Error');
            return false;
        }

        return true;
    }

    // ============================
    // Delete Transaction
    // ============================

    let deleteTransactionId = 0;

    $(document).on('click', '.btn-delete-transaction', function() {
        deleteTransactionId = $(this).data('id');
        $('#deleteModal').modal('show');
    });

    $('#confirmDeleteBtn').on('click', function() {
        $.ajax({
            url: '/Inventory/Delete',
            method: 'POST',
            data: { id: deleteTransactionId },
            success: function(response) {
                if (response.success) {
                    showNotification(response.message || 'Transaction deleted successfully!', 'success', 'Success');
                    $('#deleteModal').modal('hide');
                    setTimeout(() => {
                        location.reload();
                    }, 1500);
                } else {
                    showNotification(response.message || 'Failed to delete transaction', 'error', 'Error');
                }
            },
            error: function(xhr, status, error) {
                console.error('Error deleting transaction:', error);
                showNotification('Error deleting transaction', 'error', 'Error');
            }
        });
    });

    // ============================
    // Initialize
    // ============================

    loadTransactionStatuses();
});
