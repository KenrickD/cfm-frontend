/**
 * Work Request Add Page - JavaScript Module
 * Handles all interactive functionality for the Work Request Add form
 * Dependencies: jQuery, Select2, DatePicker
 */

(function ($) {
    'use strict';

    // Configuration
    const CONFIG = {
        debounceDelay: 300,
        minSearchLength: 2,
        apiEndpoints: {
            floors: '/Helpdesk/GetFloorsByLocation',
            rooms: '/Helpdesk/GetRoomsByFloor',
            searchEmployees: '/Helpdesk/SearchEmployees',
            personsInCharge: '/Helpdesk/GetPersonsInCharge',
            searchWorkers: '/Helpdesk/SearchWorkers'
        },
        priorityLevels: {
            'Critical': {
                helpdeskResponse: { hours: 0 },
                initialFollowUp: { hours: 2 },
                quotation: { hours: 4 },
                costApproval: { hours: 6 },
                workCompletion: { hours: 12 },
                afterWorkFollowUp: { hours: 24 }
            },
            'High': {
                helpdeskResponse: { hours: 4 },
                initialFollowUp: { days: 1 },
                quotation: { days: 2 },
                costApproval: { days: 3 },
                workCompletion: { days: 5 },
                afterWorkFollowUp: { days: 7 }
            },
            'Medium': {
                helpdeskResponse: { days: 1 },
                initialFollowUp: { days: 2 },
                quotation: { days: 4 },
                costApproval: { days: 6 },
                workCompletion: { days: 10 },
                afterWorkFollowUp: { days: 14 }
            },
            'Low': {
                helpdeskResponse: { days: 2 },
                initialFollowUp: { days: 4 },
                quotation: { days: 7 },
                costApproval: { days: 10 },
                workCompletion: { days: 21 },
                afterWorkFollowUp: { days: 30 }
            }
        }
    };

    // State management
    const state = {
        selectedLocation: null,
        selectedFloor: null,
        selectedRoom: null,
        selectedRequestor: null,
        selectedWorker: null,
        laborMaterialItems: [],
        targetOverrides: {}
    };

    /**
     * Initialize the module
     */
    function init() {
        initializeDateTimePickers();
        initializeLocationSearch();
        initializeLocationCascade();
        initializeRequestorSearch();
        initializeWorkerSearch();
        initializePersonInCharge();
        initializePriorityLevel();
        initializeTargetOverrides();
        initializeLaborMaterial();
        initializeFormSubmission();
        setCurrentDateTime();

        console.log('Work Request Add page initialized');
    }

    /**
     * Initialize date and time pickers
     */
    function initializeDateTimePickers() {
        // Set min date to today for all date inputs
        const today = new Date().toISOString().split('T')[0];
        $('input[type="date"]').attr('min', today);

        // Add change event listeners for date/time calculations
        $('#requestDate, #requestTime, #priorityLevelSelect').on('change', function () {
            calculateTargetDates();
        });
    }

    /**
     * Set current date and time in request date/time fields
     */
    function setCurrentDateTime() {
        const now = new Date();
        const dateStr = now.toISOString().split('T')[0];
        const timeStr = now.toTimeString().split(' ')[0].substring(0, 5);

        $('#requestDate').val(dateStr);
        $('#requestTime').val(timeStr);
    }

    /**
     * Initialize location search functionality
     */
    function initializeLocationSearch() {
        const $locationSearch = $('#locationSearch');
        const $locationSelect = $('#locationSelect');

        // Store original options
        const originalOptions = $locationSelect.find('option').clone();

        $locationSearch.on('keyup', debounce(function () {
            const searchTerm = $(this).val().toLowerCase().trim();

            if (searchTerm === '') {
                // Restore all options
                $locationSelect.empty().append(originalOptions.clone());
                $locationSelect.val('').trigger('change');
            } else {
                // Filter options
                $locationSelect.find('option').each(function () {
                    const optionText = $(this).text().toLowerCase();
                    const optionValue = $(this).val();

                    if (optionValue === '' || optionText.indexOf(searchTerm) > -1) {
                        $(this).show();
                    } else {
                        $(this).hide();
                    }
                });
            }
        }, CONFIG.debounceDelay));

        // Clear search when location is selected
        $locationSelect.on('change', function () {
            if ($(this).val()) {
                $locationSearch.val('');
            }
        });
    }

    /**
     * Initialize location cascade (location -> floor -> room)
     */
    function initializeLocationCascade() {
        const $locationSelect = $('#locationSelect');
        const $floorSelect = $('#floorSelect');
        const $roomSelect = $('#roomSelect');

        // When location changes, load floors
        $locationSelect.on('change', function () {
            const locationId = $(this).val();
            state.selectedLocation = locationId;

            // Reset floor and room
            $floorSelect.prop('disabled', true).empty().append('<option value="">Loading floors...</option>');
            $roomSelect.prop('disabled', true).empty().append('<option value="">Select Room/Area/Zone</option>');
            state.selectedFloor = null;
            state.selectedRoom = null;

            if (locationId) {
                loadFloors(locationId);
            } else {
                $floorSelect.empty().append('<option value="">Select Floor</option>');
            }
        });

        // When floor changes, load rooms
        $floorSelect.on('change', function () {
            const floorId = $(this).val();
            state.selectedFloor = floorId;

            // Reset room
            $roomSelect.prop('disabled', true).empty().append('<option value="">Loading rooms...</option>');
            state.selectedRoom = null;

            if (floorId) {
                loadRooms(floorId);
            } else {
                $roomSelect.empty().append('<option value="">Select Room/Area/Zone</option>');
            }
        });

        // When room changes, update state
        $roomSelect.on('change', function () {
            state.selectedRoom = $(this).val();
        });
    }

    /**
     * Load floors for selected location
     */
    function loadFloors(locationId) {
        const $floorSelect = $('#floorSelect');

        $.ajax({
            url: CONFIG.apiEndpoints.floors,
            method: 'GET',
            data: { locationId: locationId },
            success: function (response) {
                $floorSelect.empty().append('<option value="">Select Floor</option>');

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function (index, floor) {
                        $floorSelect.append(
                            $('<option></option>')
                                .val(floor.id)
                                .text(floor.name)
                        );
                    });
                    $floorSelect.prop('disabled', false);
                } else {
                    $floorSelect.append('<option value="">No floors available</option>');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading floors:', error);
                $floorSelect.empty().append('<option value="">Error loading floors</option>');
                showNotification('Error loading floors. Please try again.', 'error');
            }
        });
    }

    /**
     * Load rooms for selected floor
     */
    function loadRooms(floorId) {
        const $roomSelect = $('#roomSelect');

        $.ajax({
            url: CONFIG.apiEndpoints.rooms,
            method: 'GET',
            data: { floorId: floorId },
            success: function (response) {
                $roomSelect.empty().append('<option value="">Select Room/Area/Zone</option>');

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function (index, room) {
                        $roomSelect.append(
                            $('<option></option>')
                                .val(room.id)
                                .text(room.name)
                        );
                    });
                    $roomSelect.prop('disabled', false);
                } else {
                    $roomSelect.append('<option value="">No rooms available</option>');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading rooms:', error);
                $roomSelect.empty().append('<option value="">Error loading rooms</option>');
                showNotification('Error loading rooms. Please try again.', 'error');
            }
        });
    }

    /**
     * Initialize requestor search with typeahead
     */
    function initializeRequestorSearch() {
        const $requestorSearch = $('#requestorSearch');
        const $requestorDropdown = $('#requestorDropdown');
        const $requestorId = $('#requestorId');

        let searchTimeout;

        $requestorSearch.on('keyup', function () {
            clearTimeout(searchTimeout);
            const term = $(this).val().trim();

            if (term.length < CONFIG.minSearchLength) {
                $requestorDropdown.removeClass('show').empty();
                return;
            }

            searchTimeout = setTimeout(function () {
                searchEmployees(term, $requestorDropdown, function (employee) {
                    $requestorSearch.val(employee.name);
                    $requestorId.val(employee.id);
                    state.selectedRequestor = employee;
                    $requestorDropdown.removeClass('show').empty();
                });
            }, CONFIG.debounceDelay);
        });

        // Close dropdown when clicking outside
        $(document).on('click', function (e) {
            if (!$(e.target).closest('#requestorSearch, #requestorDropdown').length) {
                $requestorDropdown.removeClass('show').empty();
            }
        });
    }

    /**
     * Initialize worker search with typeahead
     */
    function initializeWorkerSearch() {
        const $workerSearch = $('#workerSearch');
        const $workerDropdown = $('#workerDropdown');
        const $workerId = $('#workerId');
        const $serviceProviderSelect = $('#serviceProviderSelect');

        let searchTimeout;

        $workerSearch.on('keyup', function () {
            clearTimeout(searchTimeout);
            const term = $(this).val().trim();

            if (term.length < CONFIG.minSearchLength) {
                $workerDropdown.removeClass('show').empty();
                return;
            }

            searchTimeout = setTimeout(function () {
                const serviceProviderId = $serviceProviderSelect.val();
                searchWorkers(term, serviceProviderId, $workerDropdown, function (worker) {
                    $workerSearch.val(worker.name);
                    $workerId.val(worker.id);
                    state.selectedWorker = worker;
                    $workerDropdown.removeClass('show').empty();
                });
            }, CONFIG.debounceDelay);
        });

        // Close dropdown when clicking outside
        $(document).on('click', function (e) {
            if (!$(e.target).closest('#workerSearch, #workerDropdown').length) {
                $workerDropdown.removeClass('show').empty();
            }
        });
    }

    /**
     * Search employees via API
     */
    function searchEmployees(term, $dropdown, onSelectCallback) {
        const idClient = 1; // TODO: Get from session

        $.ajax({
            url: CONFIG.apiEndpoints.searchEmployees,
            method: 'GET',
            data: {
                term: term,
                idClient: idClient
            },
            success: function (response) {
                $dropdown.empty();

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function (index, employee) {
                        const $item = $('<div></div>')
                            .addClass('typeahead-item')
                            .html(`
                                <strong>${employee.name}</strong>
                                ${employee.position ? `<br><small class="text-muted">${employee.position}</small>` : ''}
                            `)
                            .on('click', function () {
                                onSelectCallback(employee);
                            });
                        $dropdown.append($item);
                    });
                    $dropdown.addClass('show');
                } else {
                    $dropdown.append(
                        $('<div></div>')
                            .addClass('typeahead-item text-muted')
                            .text('No employees found')
                    );
                    $dropdown.addClass('show');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error searching employees:', error);
                showNotification('Error searching employees. Please try again.', 'error');
            }
        });
    }

    /**
     * Search workers via API
     */
    function searchWorkers(term, serviceProviderId, $dropdown, onSelectCallback) {
        $.ajax({
            url: CONFIG.apiEndpoints.searchWorkers,
            method: 'GET',
            data: {
                term: term,
                idServiceProvider: serviceProviderId || null
            },
            success: function (response) {
                $dropdown.empty();

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function (index, worker) {
                        const $item = $('<div></div>')
                            .addClass('typeahead-item')
                            .html(`
                                <strong>${worker.name}</strong>
                                ${worker.position ? `<br><small class="text-muted">${worker.position}</small>` : ''}
                            `)
                            .on('click', function () {
                                onSelectCallback(worker);
                            });
                        $dropdown.append($item);
                    });
                    $dropdown.addClass('show');
                } else {
                    $dropdown.append(
                        $('<div></div>')
                            .addClass('typeahead-item text-muted')
                            .text('No workers found')
                    );
                    $dropdown.addClass('show');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error searching workers:', error);
                showNotification('Error searching workers. Please try again.', 'error');
            }
        });
    }

    /**
     * Initialize Person in Charge dropdown
     */
    function initializePersonInCharge() {
        const $personInChargeSelect = $('#personInChargeSelect');
        const idClient = 1; // TODO: Get from session

        // Load persons in charge on page load
        $.ajax({
            url: CONFIG.apiEndpoints.personsInCharge,
            method: 'GET',
            data: { idClient: idClient },
            success: function (response) {
                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function (index, person) {
                        $personInChargeSelect.append(
                            $('<option></option>')
                                .val(person.id)
                                .text(person.name + (person.position ? ' - ' + person.position : ''))
                        );
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading persons in charge:', error);
            }
        });
    }

    /**
     * Initialize priority level and target date calculation
     */
    function initializePriorityLevel() {
        $('#priorityLevelSelect').on('change', function () {
            calculateTargetDates();
        });
    }

    /**
     * Calculate target dates based on priority level and request date
     */
    function calculateTargetDates() {
        const priorityLevel = $('#priorityLevelSelect').val();
        const requestDate = $('#requestDate').val();
        const requestTime = $('#requestTime').val();

        if (!priorityLevel || !requestDate || !requestTime) {
            return;
        }

        const baseDate = new Date(requestDate + 'T' + requestTime);
        const targets = CONFIG.priorityLevels[priorityLevel];

        if (!targets) {
            return;
        }

        // Calculate and update each target
        updateTargetDate('helpdesk', baseDate, targets.helpdeskResponse);
        updateTargetDate('initialFollowUp', baseDate, targets.initialFollowUp);
        updateTargetDate('quotation', baseDate, targets.quotation);
        updateTargetDate('costApproval', baseDate, targets.costApproval);
        updateTargetDate('workCompletion', baseDate, targets.workCompletion);
        updateTargetDate('afterWork', baseDate, targets.afterWorkFollowUp);
    }

    /**
     * Update target date display
     */
    function updateTargetDate(targetType, baseDate, offset) {
        // Check if there's a manual override
        if (state.targetOverrides[targetType]) {
            return; // Don't overwrite manual overrides
        }

        const targetDate = new Date(baseDate);

        if (offset.hours !== undefined) {
            targetDate.setHours(targetDate.getHours() + offset.hours);
        }
        if (offset.days !== undefined) {
            targetDate.setDate(targetDate.getDate() + offset.days);
        }

        const formattedDate = formatDateTime(targetDate);
        $(`#${targetType}Target .target-date`).text(formattedDate);

        // Store the calculated target
        $(`#${targetType}Target`).data('calculated-target', targetDate.toISOString());
    }

    /**
     * Initialize target override functionality
     */
    function initializeTargetOverrides() {
        // Click on target display to show override form
        $('.target-time-display').on('click', function () {
            const targetType = $(this).data('target-type');
            const currentTarget = $(this).data('calculated-target');

            // Hide all other forms
            $('.target-change-form').slideUp();

            // Set current target value
            if (currentTarget) {
                const targetDateTime = new Date(currentTarget);
                const dateTimeLocal = targetDateTime.toISOString().slice(0, 16);
                $(`#${targetType}NewTarget`).val(dateTimeLocal);
            }

            // Show this form
            $(`#${targetType}TargetForm`).slideDown();
        });

        // Make save and cancel functions global
        window.saveTarget = function (targetType) {
            const newTarget = $(`#${targetType}NewTarget`).val();
            const remark = $(`#${targetType}Remark`).val();

            if (!newTarget) {
                showNotification('Please select a new target date and time', 'error');
                return;
            }

            if (!remark) {
                showNotification('Please provide a remark for changing the target', 'error');
                return;
            }

            // Update display
            const targetDate = new Date(newTarget);
            const formattedDate = formatDateTime(targetDate);
            $(`#${targetType}Target .target-date`).text(formattedDate);
            $(`#${targetType}RemarkDisplay`).val(remark);

            // Store override
            state.targetOverrides[targetType] = {
                newTarget: newTarget,
                remark: remark
            };

            // Hide form
            $(`#${targetType}TargetForm`).slideUp();

            showNotification('Target date updated successfully', 'success');
        };

        window.cancelTarget = function (targetType) {
            // Clear form
            $(`#${targetType}NewTarget`).val('');
            $(`#${targetType}Remark`).val('');

            // Hide form
            $(`#${targetType}TargetForm`).slideUp();
        };
    }

    /**
     * Initialize labor/material management
     */
    function initializeLaborMaterial() {
        let itemCounter = 0;

        $('#addLaborMaterialBtn').on('click', function () {
            addLaborMaterialRow(itemCounter++);
        });

        // Make remove function global
        window.removeLaborRow = function (button) {
            const $row = $(button).closest('tr');
            const index = $row.data('index');

            // Remove from state
            state.laborMaterialItems = state.laborMaterialItems.filter(item => item.index !== index);

            // Remove row
            $row.remove();

            // Check if table is empty
            if ($('#laborMaterialTable tbody tr').length === 0) {
                $('#laborMaterialTable tbody').html(`
                    <tr>
                        <td colspan="4" class="text-center text-muted">
                            <em>No Labor/Material added yet</em>
                        </td>
                    </tr>
                `);
            }

            updateLaborMaterialTotal();
        };
    }

    /**
     * Add labor/material row to table
     */
    function addLaborMaterialRow(index) {
        const $tbody = $('#laborMaterialTable tbody');

        // Remove empty message if present
        if ($tbody.find('td[colspan]').length > 0) {
            $tbody.empty();
        }

        const row = `
            <tr data-index="${index}">
                <td>
                    <input type="text" 
                           class="form-control form-control-sm labor-name" 
                           name="LaborMaterial[${index}].Name" 
                           placeholder="Material or labor description"
                           required>
                </td>
                <td>
                    <input type="number" 
                           class="form-control form-control-sm labor-qty" 
                           name="LaborMaterial[${index}].Quantity" 
                           min="0" 
                           step="0.01"
                           value="1"
                           required>
                </td>
                <td>
                    <input type="number" 
                           class="form-control form-control-sm labor-price" 
                           name="LaborMaterial[${index}].UnitPrice" 
                           min="0" 
                           step="0.01"
                           placeholder="0.00"
                           required>
                </td>
                <td class="text-center">
                    <button type="button" 
                            class="btn btn-sm btn-danger" 
                            onclick="removeLaborRow(this)"
                            title="Remove this item">
                        <i class="ti ti-trash"></i>
                    </button>
                </td>
            </tr>
        `;

        $tbody.append(row);

        // Add to state
        state.laborMaterialItems.push({ index: index });

        // Add change listeners for calculation
        $(`tr[data-index="${index}"]`).find('.labor-qty, .labor-price').on('input', function () {
            updateLaborMaterialTotal();
        });
    }

    /**
     * Update labor/material total (if needed)
     */
    function updateLaborMaterialTotal() {
        let total = 0;

        $('#laborMaterialTable tbody tr[data-index]').each(function () {
            const qty = parseFloat($(this).find('.labor-qty').val()) || 0;
            const price = parseFloat($(this).find('.labor-price').val()) || 0;
            total += qty * price;
        });

        // Update cost estimation if it's empty
        const $costEstimation = $('#costEstimation');
        if (!$costEstimation.val() || parseFloat($costEstimation.val()) === 0) {
            $costEstimation.val(total.toFixed(2));
        }
    }

    /**
     * Initialize form submission handling
     */
    function initializeFormSubmission() {
        const $form = $('#workRequestForm');

        // Save as draft button
        $('#saveDraftBtn').on('click', function () {
            // Add hidden field for draft
            if ($form.find('input[name="IsDraft"]').length === 0) {
                $('<input>').attr({
                    type: 'hidden',
                    name: 'IsDraft',
                    value: 'true'
                }).appendTo($form);
            }

            // Submit form
            $form.submit();
        });

        // Form submit event
        $form.on('submit', function (e) {
            // Serialize labor/material items
            const laborMaterialItems = [];
            $('#laborMaterialTable tbody tr[data-index]').each(function () {
                const name = $(this).find('.labor-name').val();
                const qty = parseFloat($(this).find('.labor-qty').val()) || 0;
                const price = parseFloat($(this).find('.labor-price').val()) || 0;

                if (name && qty > 0 && price > 0) {
                    laborMaterialItems.push({
                        name: name,
                        quantity: qty,
                        unitPrice: price,
                        totalPrice: qty * price
                    });
                }
            });

            // Add labor/material JSON to form
            if (laborMaterialItems.length > 0) {
                if ($form.find('input[name="LaborMaterialJson"]').length === 0) {
                    $('<input>').attr({
                        type: 'hidden',
                        name: 'LaborMaterialJson'
                    }).appendTo($form);
                }
                $form.find('input[name="LaborMaterialJson"]').val(JSON.stringify(laborMaterialItems));
            }

            // Add target overrides to form
            $.each(state.targetOverrides, function (targetType, override) {
                const capitalizedType = targetType.charAt(0).toUpperCase() + targetType.slice(1);

                // Add target date
                if ($form.find(`input[name="${capitalizedType}ResponseTarget"]`).length === 0) {
                    $('<input>').attr({
                        type: 'hidden',
                        name: `${capitalizedType}ResponseTarget`,
                        value: override.newTarget
                    }).appendTo($form);
                }

                // Add remark
                if ($form.find(`input[name="${capitalizedType}ResponseRemark"]`).length === 0) {
                    $('<input>').attr({
                        type: 'hidden',
                        name: `${capitalizedType}ResponseRemark`,
                        value: override.remark
                    }).appendTo($form);
                }
            });

            // Show loading indicator
            showLoadingOverlay();
        });
    }

    /**
     * Utility: Debounce function
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
     * Utility: Format date time for display
     */
    function formatDateTime(date) {
        const options = {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        };
        return date.toLocaleDateString('en-US', options);
    }

    /**
     * Utility: Show notification
     */
    function showNotification(message, type = 'info') {
        // Check if notification area exists
        let $notificationArea = $('#notification-area');
        if ($notificationArea.length === 0) {
            $notificationArea = $('<div id="notification-area" style="position: fixed; top: 20px; right: 20px; z-index: 9999;"></div>');
            $('body').append($notificationArea);
        }

        const alertClass = type === 'success' ? 'alert-success' :
            type === 'error' ? 'alert-danger' :
                type === 'warning' ? 'alert-warning' :
                    'alert-info';

        const icon = type === 'success' ? 'ti-check' :
            type === 'error' ? 'ti-x' :
                type === 'warning' ? 'ti-alert-triangle' :
                    'ti-info-circle';

        const $notification = $(`
            <div class="alert ${alertClass} alert-dismissible fade show" role="alert" style="min-width: 300px;">
                <i class="ti ${icon} me-2"></i>
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `);

        $notificationArea.append($notification);

        // Auto dismiss after 5 seconds
        setTimeout(function () {
            $notification.fadeOut(function () {
                $(this).remove();
            });
        }, 5000);
    }

    /**
     * Utility: Show loading overlay
     */
    function showLoadingOverlay() {
        const $overlay = $(`
            <div id="loading-overlay" style="
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.5);
                display: flex;
                justify-content: center;
                align-items: center;
                z-index: 9999;
            ">
                <div class="spinner-border text-light" role="status" style="width: 3rem; height: 3rem;">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        `);

        $('body').append($overlay);
    }

    // Initialize when document is ready
    $(document).ready(function () {
        init();
    });

})(jQuery);