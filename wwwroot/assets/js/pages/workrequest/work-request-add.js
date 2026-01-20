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
            // Location cascade
            locations: MvcEndpoints.Helpdesk.Location.GetByClient,
            floors: MvcEndpoints.Helpdesk.Location.GetFloorsByLocation,
            rooms: MvcEndpoints.Helpdesk.Location.GetRoomsByFloor,

            // Dropdowns
            workCategories: MvcEndpoints.Helpdesk.WorkRequest.GetWorkCategoriesByTypes,
            otherCategories: MvcEndpoints.Helpdesk.WorkRequest.GetOtherCategoriesByTypes,
            importantChecklist: MvcEndpoints.Helpdesk.WorkRequest.GetImportantChecklistByTypes,
            serviceProviders: MvcEndpoints.Helpdesk.WorkRequest.GetServiceProvidersByClient,
            priorityLevels: MvcEndpoints.Helpdesk.WorkRequest.GetPriorityLevels,
            feedbackTypes: MvcEndpoints.Helpdesk.WorkRequest.GetFeedbackTypesByEnums,
            getCurrencies: MvcEndpoints.Helpdesk.Extended.GetCurrencies,

            // Radio buttons
            requestMethods: MvcEndpoints.Helpdesk.WorkRequest.GetWorkRequestMethodsByEnums,
            statuses: MvcEndpoints.Helpdesk.WorkRequest.GetWorkRequestStatusesByEnums,

            // Search/autocomplete
            searchRequestors: MvcEndpoints.Helpdesk.Search.Requestors,
            searchWorkersByCompany: MvcEndpoints.Helpdesk.Search.WorkersByCompany,
            searchWorkersByServiceProvider: MvcEndpoints.Helpdesk.Search.WorkersByServiceProvider,
            personsInCharge: MvcEndpoints.Helpdesk.WorkRequest.GetPersonsInChargeByFilters,

            // Business day calculation
            officeHours: MvcEndpoints.Helpdesk.Extended.GetOfficeHours,
            publicHolidays: MvcEndpoints.Helpdesk.Extended.GetPublicHolidays
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
        targetOverrides: {},
        workers: [],
        importantChecklistData: [],
        priorityLevelsCache: {} // Cache full priority level data by ID
    };

    /**
     * Initialize the module
     */
    function init() {
        initializeDateTimePickers();

        // Check if data is pre-loaded server-side
        const hasServerData = $('#locationSelect option').length > 2; // More than just empty + "Not Specified"

        if (!hasServerData) {
            // Fallback: Load client-side if server-side loading failed
            console.warn('Server-side data not loaded, falling back to client-side loading');
            loadLocations();
            loadWorkCategories();
            loadOtherCategories();
            loadServiceProviders();
            loadPriorityLevels();
            loadFeedbackTypes();
            loadCurrencies();
            loadRequestMethods();
            loadStatuses();
            loadImportantChecklist();
        } else {
            // Data already loaded server-side, just cache priority levels from DOM
            console.log('Server-side data detected, caching priority level details');
            cachePriorityLevelsFromDOM();
        }

        // Initialize interactive features (these always run)
        initializeLocationSearch();
        initializeLocationCascade();
        initializeRequestorSearch();
        initializeWorkerSearch();
        initializePersonInCharge();
        initializeServiceProviderChange();
        initializePriorityLevel();
        initializeTargetOverrides();
        initializeLaborMaterial();
        initializeWorkers();
        initializeFormSubmission();
        setCurrentDateTime();

        // Auto-select first priority level after a short delay to ensure DOM is ready
        setTimeout(function () {
            autoSelectFirstPriorityLevel();
        }, 100);

        console.log('Work Request Add page initialized');
    }

    /**
     * Cache priority level data from server-rendered DOM
     */
    function cachePriorityLevelsFromDOM() {
        try {
            $('#priorityLevelSelect option[value!=""]').each(function () {
                const $option = $(this);
                const priorityId = $option.val();
                const priorityDetailsJson = $option.attr('data-priority-details');

                console.log('Processing priority option:', priorityId, 'Has data-priority-details:', !!priorityDetailsJson);

                if (priorityDetailsJson) {
                    try {
                        const priorityData = JSON.parse(priorityDetailsJson);
                        state.priorityLevelsCache[priorityId] = priorityData;
                        console.log('Cached priority data for ID', priorityId, ':', priorityData);
                    } catch (e) {
                        console.error('Error parsing priority level data for ID:', priorityId, e);
                        console.error('Raw JSON:', priorityDetailsJson);
                    }
                }
            });

            console.log('Priority levels cached from DOM:', Object.keys(state.priorityLevelsCache).length);
            console.log('Cache contents:', state.priorityLevelsCache);
        } catch (error) {
            console.error('Error caching priority levels from DOM:', error);
        }
    }

    /**
     * Initialize date and time pickers
     */
    function initializeDateTimePickers() {
        // Set min date to today for all date inputs
        const today = new Date().toISOString().split('T')[0];
        $('input[type="date"]').attr('min', today);

        // Setup real-time calculation triggers
        $('#requestDate').on('change', triggerTargetDateCalculation);
        $('#requestTime').on('change', triggerTargetDateCalculation);
        $('#priorityLevelSelect').on('change', triggerTargetDateCalculation);
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
     * Auto-select first priority level on page load and trigger calculation
     */
    function autoSelectFirstPriorityLevel() {
        const $prioritySelect = $('#priorityLevelSelect');
        const $firstOption = $prioritySelect.find('option[value!=""]').first();

        if ($firstOption.length > 0) {
            const firstValue = $firstOption.val();
            $prioritySelect.val(firstValue);

            // If using searchable dropdown, update it as well
            const selectElement = document.getElementById('priorityLevelSelect');
            if (selectElement && selectElement._searchableDropdown) {
                selectElement._searchableDropdown.setValue(firstValue, $firstOption.text(), true);
            }

            // Trigger the target date calculation
            triggerTargetDateCalculation();

            console.log('Auto-selected first priority level:', firstValue, $firstOption.text());
        }
    }

    /**
     * Load locations from API (replaces server-side rendering)
     */
    function loadLocations() {
        $.ajax({
            url: CONFIG.apiEndpoints.locations,
            method: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    const $select = $('#locationSelect');
                    $select.empty()
                        .append('<option value="">Select Location</option>')
                        .append('<option value="-1">Not Specified</option>');
                    $.each(response.data, function (index, location) {
                        $select.append(
                            $('<option></option>')
                                .val(location.idProperty)
                                .text(location.propertyName)
                                .attr('data-property-group', location.idPropertyType || '')
                        );
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading locations:', error);
                showNotification('Error loading locations', 'error');
            }
        });
    }

    /**
     * Load work categories from API
     */
    function loadWorkCategories() {
        $.ajax({
            url: CONFIG.apiEndpoints.workCategories,
            method: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    const $select = $('#workCategorySelect');
                    $select.empty().append('<option value="">Select Work Category</option>');
                    $.each(response.data, function (index, category) {
                        $select.append(
                            $('<option></option>')
                                .val(category.idType)
                                .text(category.typeName)
                        );
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading work categories:', error);
            }
        });
    }

    /**
     * Load other categories from API
     */
    function loadOtherCategories() {
        // Load Other Category 1
        $.ajax({
            url: CONFIG.apiEndpoints.otherCategories,
            method: 'GET',
            data: { categoryType: 'workRequestCustomCategory' },
            success: function (response) {
                if (response.success && response.data) {
                    const $select = $('#otherCategorySelect');
                    $select.empty().append('<option value="">Select Other Category</option>');
                    $.each(response.data, function (index, category) {
                        $select.append(
                            $('<option></option>')
                                .val(category.idType)
                                .text(category.typeName)
                        );
                    });
                }
            }
        });

        // Load Other Category 2
        $.ajax({
            url: CONFIG.apiEndpoints.otherCategories,
            method: 'GET',
            data: { categoryType: 'workRequestCustomCategory2' },
            success: function (response) {
                if (response.success && response.data) {
                    const $container = $('#otherCategory2Container');

                    if (response.data.length === 0) {
                        // Hide container if no data
                        $container.hide();
                    } else {
                        $container.show();
                        const $select = $('#otherCategory2Select');
                        $select.empty().append('<option value="">Select Other Category 2</option>');
                        $.each(response.data, function (index, category) {
                            $select.append(
                                $('<option></option>')
                                    .val(category.idType)
                                    .text(category.typeName)
                            );
                        });
                    }
                } else {
                    // Hide if API fails
                    $('#otherCategory2Container').hide();
                }
            },
            error: function () {
                // Hide on error
                $('#otherCategory2Container').hide();
            }
        });
    }

    /**
     * Load service providers from API
     */
    function loadServiceProviders() {
        $.ajax({
            url: CONFIG.apiEndpoints.serviceProviders,
            method: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    const $select = $('#serviceProviderSelect');
                    $select.empty()
                        .append('<option value="-1">Not Specified</option>')
                        .append('<option value="-2">Self-Performed</option>');
                    $.each(response.data, function (index, provider) {
                        $select.append(
                            $('<option></option>')
                                .val(provider.idServiceProvider)
                                .text(provider.aliasCompanyName)
                                .attr('data-id-company', provider.idCompany)
                        );
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading service providers:', error);
            }
        });
    }

    /**
     * Load priority levels from API with full details
     * Caches the full priority level data for target date calculations
     */
    async function loadPriorityLevels() {
        try {
            // Fetch the list of priority levels first (for dropdown)
            const listResponse = await fetch(CONFIG.apiEndpoints.priorityLevels);
            const listData = await listResponse.json();

            if (!listData.success || !listData.data) {
                throw new Error('Failed to load priority levels list');
            }

            // Populate the dropdown
            const $select = $('#priorityLevelSelect');
            $select.empty().append('<option value="">Select Priority Level</option>');

            // Fetch full details for each priority level and cache
            const detailPromises = listData.data.map(async (priority) => {
                const priorityId = priority.value || priority.id;

                // Fetch full priority level details
                const detailResponse = await fetch(MvcEndpoints.Helpers.buildUrl(MvcEndpoints.Helpdesk.WorkRequest.GetPriorityLevelById, { id: priorityId }));
                const detailData = await detailResponse.json();

                if (detailData.success && detailData.data) {
                    // Cache the full priority level data
                    state.priorityLevelsCache[priorityId] = detailData.data;

                    // Add to dropdown
                    $select.append(
                        $('<option></option>')
                            .val(priorityId)
                            .text(priority.label || priority.name)
                            .attr('data-description', priority.description || '')
                    );
                }
            });

            // Wait for all detail fetches to complete
            await Promise.all(detailPromises);

            console.log('Priority levels loaded and cached:', Object.keys(state.priorityLevelsCache).length);

        } catch (error) {
            console.error('Error loading priority levels:', error);
            showNotification('Error loading priority levels', 'error');
        }
    }

    /**
     * Load feedback types from API
     */
    function loadFeedbackTypes() {
        $.ajax({
            url: CONFIG.apiEndpoints.feedbackTypes,
            method: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    const $select = $('#feedbackStatus');
                    $select.empty().append('<option value="">No Feedback</option>');
                    $.each(response.data, function (index, type) {
                        $select.append(
                            $('<option></option>')
                                .val(type.idEnum)
                                .text(type.enumName)
                        );
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading feedback types:', error);
            }
        });
    }

    /**
     * Load currencies from API
     */
    function loadCurrencies() {
        $.ajax({
            url: CONFIG.apiEndpoints.getCurrencies,
            method: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    const $select = $('#costEstimationCurrencySelect');
                    $select.empty().append('<option value="">Select Currency</option>');
                    $.each(response.data, function (index, currency) {
                        $select.append(
                            $('<option></option>')
                                .val(currency.id || currency.value)
                                .text(currency.code || currency.name)
                        );
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading currencies:', error);
            }
        });
    }

    /**
     * Load request methods from API (replaces hardcoded radio buttons)
     */
    function loadRequestMethods() {
        $.ajax({
            url: CONFIG.apiEndpoints.requestMethods,
            method: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    const $container = $('.col-md-6 .form-label:contains("Request Method")').siblings('.mt-2');
                    $container.empty();
                    $.each(response.data, function (index, method) {
                        const radioId = 'method' + method.idEnum;
                        const isFirst = index === 0;
                        $container.append(`
                            <div class="form-check form-check-inline">
                                <input class="form-check-input" type="radio"
                                       name="RequestMethod"
                                       id="${radioId}"
                                       value="${method.idEnum}"
                                       ${isFirst ? 'required' : ''}>
                                <label class="form-check-label" for="${radioId}">
                                    ${method.enumName}
                                </label>
                            </div>
                        `);
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading request methods:', error);
            }
        });
    }

    /**
     * Load statuses from API (replaces hardcoded radio buttons)
     */
    function loadStatuses() {
        $.ajax({
            url: CONFIG.apiEndpoints.statuses,
            method: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    const $container = $('.col-md-6 .form-label:contains("Status")').siblings('.mt-2');
                    $container.empty();
                    $.each(response.data, function (index, status) {
                        const radioId = 'status' + status.idEnum;
                        const isNew = status.enumName === 'New';
                        $container.append(`
                            <div class="form-check form-check-inline">
                                <input class="form-check-input" type="radio"
                                       name="Status"
                                       id="${radioId}"
                                       value="${status.idEnum}"
                                       ${isNew ? 'checked' : ''}
                                       ${index === 0 ? 'required' : ''}>
                                <label class="form-check-label" for="${radioId}">
                                    ${status.enumName}
                                </label>
                            </div>
                        `);
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading statuses:', error);
            }
        });
    }

    /**
     * Load important checklist from API
     */
    function loadImportantChecklist() {
        $.ajax({
            url: CONFIG.apiEndpoints.importantChecklist,
            method: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    state.importantChecklistData = response.data;
                    const $container = $('#importantChecklistContainer');
                    $container.empty();

                    $.each(response.data, function (index, item) {
                        $container.append(`
                            <div class="col-md-4 mb-2">
                                <div class="form-check">
                                    <input class="form-check-input important-checklist-item"
                                           type="checkbox"
                                           id="checklist${item.idType}"
                                           data-type-id="${item.idType}"
                                           value="false">
                                    <label class="form-check-label" for="checklist${item.idType}">
                                        ${item.typeName}
                                    </label>
                                </div>
                            </div>
                        `);
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading important checklist:', error);
            }
        });
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

            // Reset floor and room dropdowns
            $floorSelect.prop('disabled', true).empty().append('<option value="">Loading floors...</option>');
            $roomSelect.prop('disabled', true).empty().append('<option value="">Select Room/Area/Zone</option>');
            state.selectedFloor = null;
            state.selectedRoom = null;

            // Reset searchable dropdown components
            const floorElement = document.getElementById('floorSelect');
            if (floorElement && floorElement._searchableDropdown) {
                floorElement._searchableDropdown.clear();
                floorElement._searchableDropdown.loadFromSelect();
                floorElement._searchableDropdown.disable();
            }
            const roomElement = document.getElementById('roomSelect');
            if (roomElement && roomElement._searchableDropdown) {
                roomElement._searchableDropdown.clear();
                roomElement._searchableDropdown.loadFromSelect();
                roomElement._searchableDropdown.disable();
            }

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

            // Reset room dropdown
            $roomSelect.prop('disabled', true).empty().append('<option value="">Loading rooms...</option>');
            state.selectedRoom = null;

            // Reset searchable dropdown component
            const roomElement = document.getElementById('roomSelect');
            if (roomElement && roomElement._searchableDropdown) {
                roomElement._searchableDropdown.clear();
                roomElement._searchableDropdown.loadFromSelect();
                roomElement._searchableDropdown.disable();
            }

            if (floorId && state.selectedLocation) {
                loadRooms(state.selectedLocation, floorId);
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
     * Load floors for selected property
     */
    function loadFloors(propertyId) {
        const $floorSelect = $('#floorSelect');

        $.ajax({
            url: CONFIG.apiEndpoints.floors,
            method: 'GET',
            data: { locationId: propertyId },
            success: function (response) {
                $floorSelect.empty()
                    .append('<option value="">Select Floor</option>')
                    .append('<option value="-1">Not Specified</option>');

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function (index, floor) {
                        $floorSelect.append(
                            $('<option></option>')
                                .val(floor.idPropertyFloor)
                                .text(floor.floorUnitName)
                        );
                    });
                    $floorSelect.prop('disabled', false);

                    // Refresh and enable the searchable dropdown component
                    const selectElement = document.getElementById('floorSelect');
                    if (selectElement && selectElement._searchableDropdown) {
                        selectElement._searchableDropdown.loadFromSelect();
                        selectElement._searchableDropdown.enable();
                    }
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
     * Load room zones for selected property and floor
     */
    function loadRooms(propertyId, floorId) {
        const $roomSelect = $('#roomSelect');

        $.ajax({
            url: CONFIG.apiEndpoints.rooms,
            method: 'GET',
            data: { propertyId: propertyId, floorId: floorId },
            success: function (response) {
                $roomSelect.empty()
                    .append('<option value="">Select Room/Area/Zone</option>')
                    .append('<option value="-1">Not Specified</option>');

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function (index, room) {
                        $roomSelect.append(
                            $('<option></option>')
                                .val(room.idRoomZone)
                                .text(room.roomZoneName)
                        );
                    });
                    $roomSelect.prop('disabled', false);

                    // Refresh and enable the searchable dropdown component
                    const selectElement = document.getElementById('roomSelect');
                    if (selectElement && selectElement._searchableDropdown) {
                        selectElement._searchableDropdown.loadFromSelect();
                        selectElement._searchableDropdown.enable();
                    }
                } else {
                    $roomSelect.append('<option value="">No room zones available</option>');
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
     * Initialize requestor search with typeahead and card display
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
                    // Show card instead of just updating text
                    showRequestorCard(employee);
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

        // Delete button click handler
        $('#deleteRequestorBtn').on('click', function () {
            resetRequestorSelection();
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
     * Search requestors via API (updated for new backend structure)
     * Response model: RequestorFormDetailResponse
     * Fields: IdEmployee, FullName, DepartmentName, Title, EmailAddress, PhoneNumber
     */
    function searchEmployees(term, $dropdown, onSelectCallback) {
        $.ajax({
            url: CONFIG.apiEndpoints.searchRequestors,
            method: 'GET',
            data: { term: term },
            success: function (response) {
                $dropdown.empty();

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function (index, employee) {
                        const name = employee.fullName || employee.FullName;
                        const title = employee.title || employee.Title || '';
                        const department = employee.departmentName || employee.DepartmentName || '';

                        const $item = $('<div></div>')
                            .addClass('typeahead-item')
                            .html(`
                                <strong>${name}</strong>
                                <small class="text-muted">
                                    ${title ? title + '<br>' : ''}${department}
                                </small>
                            `)
                            .on('click', function () {
                                onSelectCallback({
                                    id: employee.idEmployee || employee.IdEmployee,
                                    fullName: name,
                                    departmentName: department,
                                    title: title,
                                    emailAddress: employee.emailAddress || employee.EmailAddress || '',
                                    phoneNumber: employee.phoneNumber || employee.PhoneNumber || ''
                                });
                            });
                        $dropdown.append($item);
                    });
                    $dropdown.addClass('show');
                } else {
                    $dropdown.append(
                        $('<div></div>')
                            .addClass('typeahead-item text-muted')
                            .text('No requestors found')
                    );
                    $dropdown.addClass('show');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error searching requestors:', error);
                showNotification('Error searching requestors. Please try again.', 'error');
            }
        });
    }

    /**
     * Show requestor card with employee information
     * Displays card with avatar, name, title, department, email, phone
     * Model: RequestorFormDetailResponse
     */
    function showRequestorCard(requestor) {
        // Store selected requestor in state
        state.selectedRequestor = requestor;

        // Update hidden ID field
        $('#requestorId').val(requestor.id);

        // Generate avatar with initials
        const initials = generateInitials(requestor.fullName);
        const avatarColor = generateAvatarColor(requestor.fullName);

        // Update card content
        $('#requestorInitials').text(initials);
        $('#requestorAvatar').css('background-color', avatarColor);
        $('#requestorCardName').text(requestor.fullName);

        // Build details HTML
        let detailsHtml = '';
        if (requestor.title) {
            detailsHtml += `<div><i class="ti ti-briefcase me-1"></i>${requestor.title}</div>`;
        }
        if (requestor.departmentName) {
            detailsHtml += `<div><i class="ti ti-building me-1"></i>${requestor.departmentName}</div>`;
        }
        if (requestor.emailAddress) {
            detailsHtml += `<div><i class="ti ti-mail me-1"></i>${requestor.emailAddress}</div>`;
        }
        if (requestor.phoneNumber) {
            detailsHtml += `<div><i class="ti ti-phone me-1"></i>${requestor.phoneNumber}</div>`;
        }

        $('#requestorCardDetails').html(detailsHtml || '<div class="text-muted fst-italic">No details available</div>');

        // Toggle visibility: hide search, show card
        $('#requestorSearchContainer').hide();
        $('#requestorCard').fadeIn(300);

        // Mark the hidden input as having a value (for form validation)
        $('#requestorSearch').removeAttr('required');
    }

    /**
     * Reset requestor selection
     * Hides card and shows search box again
     */
    function resetRequestorSelection() {
        // Clear state
        state.selectedRequestor = null;

        // Clear hidden ID field
        $('#requestorId').val('');

        // Clear search input
        $('#requestorSearch').val('').attr('required', 'required');

        // Clear card content
        $('#requestorCardName').text('');
        $('#requestorCardDetails').empty();

        // Toggle visibility: show search, hide card
        $('#requestorCard').hide();
        $('#requestorSearchContainer').fadeIn(300);

        // Focus back on search input
        setTimeout(function() {
            $('#requestorSearch').focus();
        }, 350);

        showNotification('Requestor selection cleared', 'info');
    }

    /**
     * Generate initials from full name (first letter of first and last name)
     * Examples: "Adi Hidayat" -> "AH", "John Doe Smith" -> "JS"
     */
    function generateInitials(fullName) {
        if (!fullName) return '??';

        const nameParts = fullName.trim().split(' ').filter(part => part.length > 0);

        if (nameParts.length === 0) return '??';
        if (nameParts.length === 1) return nameParts[0].substring(0, 2).toUpperCase();

        // First letter of first name + first letter of last name
        const firstInitial = nameParts[0].charAt(0);
        const lastInitial = nameParts[nameParts.length - 1].charAt(0);

        return (firstInitial + lastInitial).toUpperCase();
    }

    /**
     * Generate consistent avatar color based on name
     * Uses a predefined color palette for professional appearance
     */
    function generateAvatarColor(fullName) {
        const colors = [
            '#3498db', // Blue
            '#e74c3c', // Red
            '#2ecc71', // Green
            '#f39c12', // Orange
            '#9b59b6', // Purple
            '#1abc9c', // Turquoise
            '#e67e22', // Dark Orange
            '#34495e', // Dark Blue Gray
            '#16a085', // Dark Turquoise
            '#c0392b'  // Dark Red
        ];

        if (!fullName) return colors[0];

        // Generate consistent index based on name hash
        let hash = 0;
        for (let i = 0; i < fullName.length; i++) {
            hash = fullName.charCodeAt(i) + ((hash << 5) - hash);
        }

        const index = Math.abs(hash) % colors.length;
        return colors[index];
    }

    /**
     * Search workers from company via API
     * Uses backend endpoint: /api/v1/employee/worker
     * Response includes: IdEmployee, FullName, DepartmentName, Title
     */
    function searchWorkers(term, serviceProviderId, $dropdown, onSelectCallback) {
        const idLocation = state.selectedLocation;

        if (!idLocation) {
            showNotification('Please select a location first', 'warning');
            return;
        }

        $.ajax({
            url: CONFIG.apiEndpoints.searchWorkersByCompany,
            method: 'GET',
            data: {
                term: term,
                idLocation: idLocation
            },
            success: function (response) {
                $dropdown.empty();

                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function (index, worker) {
                        const $item = $('<div></div>')
                            .addClass('typeahead-item')
                            .html(`
                                <strong>${worker.fullName || worker.FullName}</strong>
                                <small class="text-muted">
                                    ${worker.title || worker.Title || ''}<br>
                                    ${worker.departmentName || worker.DepartmentName || ''}
                                </small>
                            `)
                            .on('click', function () {
                                onSelectCallback({
                                    id: worker.idEmployee || worker.IdEmployee,
                                    name: worker.fullName || worker.FullName
                                });
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
     * Initialize Person in Charge dropdown with dynamic loading
     */
    function initializePersonInCharge() {
        // Load PIC when work category or location changes
        $('#workCategorySelect, #locationSelect').on('change', function () {
            const idWorkCategory = $('#workCategorySelect').val();
            const idLocation = $('#locationSelect').val();

            if (idWorkCategory || idLocation) {
                loadPersonsInCharge(idWorkCategory, idLocation);
            }
        });
    }

    /**
     * Load persons in charge with filters
     */
    function loadPersonsInCharge(idWorkCategory, idLocation) {
        const $select = $('#personInChargeSelect');

        const params = {};
        if (idWorkCategory) params.idWorkCategory = idWorkCategory;
        if (idLocation) params.idLocation = idLocation;

        $.ajax({
            url: CONFIG.apiEndpoints.personsInCharge,
            method: 'GET',
            data: params,
            success: function (response) {
                $select.empty().append('<option value="">Select Person in Charge</option>');
                if (response.success && response.data && response.data.length > 0) {
                    $.each(response.data, function (index, person) {
                        $select.append(
                            $('<option></option>')
                                .val(person.id)
                                .text((person.fullName || person.name) + (person.position ? ' - ' + person.position : ''))
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
     * Initialize service provider change to show/hide worker from service provider
     */
    function initializeServiceProviderChange() {
        $('#serviceProviderSelect').on('change', function () {
            const value = $(this).val();
            const idLocation = state.selectedLocation;

            // Show worker from service provider if not "Not Specified" or "Self-Performed"
            if (value && value !== '-1' && value !== '-2') {
                showWorkerFromServiceProvider(value, idLocation);
                // Show Service Provider Worker option in Add Worker modal
                updateWorkerSourceOptions(true);
            } else {
                hideWorkerFromServiceProvider();
                // Hide Service Provider Worker option in Add Worker modal
                updateWorkerSourceOptions(false);
            }
        });
    }

    /**
     * Update worker source options visibility in Add Worker modal
     * @param {boolean} showServiceProvider - Whether to show Service Provider Worker option
     */
    function updateWorkerSourceOptions(showServiceProvider) {
        const $serviceProviderOption = $('#serviceProviderSourceOption');

        if (showServiceProvider) {
            $serviceProviderOption.show();
        } else {
            $serviceProviderOption.hide();
            // If Service Provider was selected, switch back to Company
            if ($('#sourceServiceProvider').is(':checked')) {
                $('#sourceCompany').prop('checked', true).trigger('change');
            }
        }
    }

    /**
     * Show worker from service provider searchbox
     */
    function showWorkerFromServiceProvider(idServiceProvider, idLocation) {
        if ($('#workerServiceProviderSearch').length === 0) {
            const html = `
                <div class="col-md-12 mb-3" id="workerServiceProviderContainer">
                    <label class="form-label fw-semibold">Worker from Service Provider</label>
                    <input type="text" class="form-control form-control-sm"
                           id="workerServiceProviderSearch"
                           placeholder="Type to search worker from service provider..."
                           autocomplete="off">
                    <div id="workerServiceProviderDropdown" class="typeahead-dropdown"></div>
                    <input type="hidden" id="workerServiceProviderId" name="IdWorkerServiceProvider">
                </div>
            `;
            $('#workerSearch').closest('.col-md-12').after(html);

            // Setup autocomplete
            let timeout;
            $('#workerServiceProviderSearch').on('keyup', function () {
                clearTimeout(timeout);
                const term = $(this).val().trim();

                if (term.length < CONFIG.minSearchLength) {
                    $('#workerServiceProviderDropdown').removeClass('show').empty();
                    return;
                }

                if (!idLocation) {
                    showNotification('Please select a location first', 'warning');
                    $('#workerServiceProviderDropdown').removeClass('show').empty();
                    return;
                }

                timeout = setTimeout(function () {
                    $.ajax({
                        url: CONFIG.apiEndpoints.searchWorkersByServiceProvider,
                        method: 'GET',
                        data: {
                            term: term,
                            idLocation: idLocation,
                            idServiceProvider: idServiceProvider
                        },
                        success: function (response) {
                            const $dropdown = $('#workerServiceProviderDropdown');
                            $dropdown.empty();

                            if (response.success && response.data && response.data.length > 0) {
                                $.each(response.data, function (index, worker) {
                                    const $item = $('<div></div>')
                                        .addClass('typeahead-item')
                                        .html(`
                                            <strong>${worker.fullName || worker.name}</strong>
                                            ${worker.position ? `<br><small class="text-muted">${worker.position}</small>` : ''}
                                        `)
                                        .on('click', function () {
                                            $('#workerServiceProviderSearch').val(worker.fullName || worker.name);
                                            $('#workerServiceProviderId').val(worker.id);
                                            $dropdown.removeClass('show').empty();
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
                            console.error('Error searching workers from service provider:', error);
                            showNotification('Error searching workers. Please try again.', 'error');
                        }
                    });
                }, CONFIG.debounceDelay);
            });

            // Close dropdown when clicking outside
            $(document).on('click', function (e) {
                if (!$(e.target).closest('#workerServiceProviderSearch, #workerServiceProviderDropdown').length) {
                    $('#workerServiceProviderDropdown').removeClass('show').empty();
                }
            });
        }
    }

    /**
     * Hide worker from service provider searchbox
     */
    function hideWorkerFromServiceProvider() {
        $('#workerServiceProviderContainer').remove();
    }

    /**
     * Initialize priority level dropdown (legacy compatibility)
     */
    function initializePriorityLevel() {
        // Priority level change is now handled by triggerTargetDateCalculation
        // This function kept for backward compatibility
    }

    /**
     * Trigger target date calculation (real-time on change)
     * Uses cached priority data and fetches fresh office hours/holidays
     */
    async function triggerTargetDateCalculation() {
        const requestDate = $('#requestDate').val();
        const requestTime = $('#requestTime').val();
        const priorityId = $('#priorityLevelSelect').val();

        // Only calculate if all required fields are filled
        if (!requestDate || !requestTime || !priorityId) {
            clearAllTargetDates();
            return;
        }

        // Check if priority level is cached
        if (!state.priorityLevelsCache[priorityId]) {
            console.error('Priority level not found in cache:', priorityId);
            hideAllTargetDates();
            showNotification('Priority level data not loaded. Please refresh the page.', 'error', 'Error');
            return;
        }

        // Show loading state
        showTargetDatesLoading();

        try {
            // Fetch fresh office hours and public holidays (per user requirement)
            const [officeHoursResponse, publicHolidaysResponse] = await Promise.all([
                fetch(CONFIG.apiEndpoints.officeHours),
                fetch(CONFIG.apiEndpoints.publicHolidays)
            ]);

            const officeHoursData = await officeHoursResponse.json();
            const publicHolidaysData = await publicHolidaysResponse.json();

            // Check if data loaded successfully
            if (!officeHoursData.success || !publicHolidaysData.success) {
                hideAllTargetDates();
                showNotification('Failed to load business day configuration. Cannot calculate target dates.', 'error', 'Error');
                return;
            }

            // Initialize calculator with fresh data
            const calculator = new BusinessDateCalculator(
                officeHoursData.data,
                publicHolidaysData.data
            );

            // Get cached priority level data
            const priorityData = state.priorityLevelsCache[priorityId];

            // Perform calculations
            calculateAndDisplayTargets(priorityData, calculator, requestDate, requestTime);

        } catch (error) {
            console.error('Error calculating target dates:', error);
            hideAllTargetDates();
            showNotification('Error calculating target dates. Please try again.', 'error', 'Error');
        }
    }

    /**
     * Show loading state for target dates
     */
    function showTargetDatesLoading() {
        $('.target-time-display .target-date').text('Calculating...');
    }

    /**
     * Hide all target dates
     */
    function hideAllTargetDates() {
        $('.target-time-display').hide();
        $('.target-time-display .target-date').text('-');
    }

    /**
     * Clear all target dates
     */
    function clearAllTargetDates() {
        $('.target-time-display .target-date').text('-');
    }

    /**
     * Helper function to get property value supporting both PascalCase and camelCase
     * C# serializes with PascalCase, but API may return camelCase
     */
    function getPriorityProperty(priorityLevel, pascalName) {
        // Try PascalCase first (from C# JsonSerializer.Serialize in Razor)
        if (priorityLevel[pascalName] !== undefined) {
            return priorityLevel[pascalName];
        }
        // Try camelCase (from API responses)
        const camelName = pascalName.charAt(0).toLowerCase() + pascalName.slice(1);
        if (priorityLevel[camelName] !== undefined) {
            return priorityLevel[camelName];
        }
        return null;
    }

    /**
     * Reference enum mapping - maps enum IDs to their meaning
     * These values come from the backend enum for target calculation references
     * Note: Each target type has its own set of enum values
     */
    // Reference text values from backend API (matching *TargetCalculationText fields)
    const REFERENCE_TEXT = {
        AFTER_REQUEST_DATE: 'After Request Date',
        AFTER_HELPDESK_RESPONSE_TARGET: 'After Helpdesk Response Target',
        AFTER_INITIAL_FOLLOWUP_TARGET: 'After Initial Follow Up Target',
        AFTER_INITIAL_FOLLOWUP: 'After Initial Follow Up',
        AFTER_QUOTATION_SUBMISSION_TARGET: 'After Quotation Submission Target',
        AFTER_QUOTATION_SUBMISSION: 'After Quotation Submission',
        AFTER_COST_APPROVAL_TARGET: 'After Cost Approval Target',
        AFTER_COST_APPROVAL: 'After Cost Approval',
        AFTER_WORK_COMPLETION_TARGET: 'After Work Completion Target',
        AFTER_WORK_COMPLETION: 'After Work Completion'
    };

    /**
     * Calculate and display all 6 target dates
     * Targets are calculated in sequence because some targets depend on the result of previous targets
     */
    function calculateAndDisplayTargets(priorityLevel, calculator, requestDate, requestTime) {
        const requestDateTime = new Date(`${requestDate}T${requestTime}`);

        // Store calculated target dates for chaining
        const calculatedTargets = {
            helpdesk: null,
            initialFollowUp: null,
            quotation: null,
            costApproval: null,
            workCompletion: null,
            afterWork: null
        };

        // Define all 6 target types with their priority level field mappings
        const targets = [
            {
                type: 'helpdesk',
                label: 'Helpdesk Response',
                days: getPriorityProperty(priorityLevel, 'HelpdeskResponseTargetDays'),
                hours: getPriorityProperty(priorityLevel, 'HelpdeskResponseTargetHours'),
                minutes: getPriorityProperty(priorityLevel, 'HelpdeskResponseTargetMinutes'),
                withinOfficeHours: getPriorityProperty(priorityLevel, 'HelpdeskResponseTargetWithinOfficeHours'),
                reference: getPriorityProperty(priorityLevel, 'HelpdeskResponseTargetReference'),
                // Helpdesk Response always starts from Request Date
                getBaseDate: () => requestDateTime
            },
            {
                type: 'initialFollowUp',
                label: 'Initial Follow Up',
                days: getPriorityProperty(priorityLevel, 'InitialFollowUpTargetDays'),
                hours: getPriorityProperty(priorityLevel, 'InitialFollowUpTargetHours'),
                minutes: getPriorityProperty(priorityLevel, 'InitialFollowUpTargetMinutes'),
                withinOfficeHours: getPriorityProperty(priorityLevel, 'InitialFollowUpTargetWithinOfficeHours'),
                reference: getPriorityProperty(priorityLevel, 'InitialFollowUpTargetReference'),
                getBaseDate: (ref) => {
                    // After Helpdesk Response Target
                    if (ref === REFERENCE_TEXT.AFTER_HELPDESK_RESPONSE_TARGET) {
                        return calculatedTargets.helpdesk || requestDateTime;
                    }
                    // Default: After Request Date
                    return requestDateTime;
                }
            },
            {
                type: 'quotation',
                label: 'Quotation Submission',
                days: getPriorityProperty(priorityLevel, 'QuotationSubmissionTargetDays'),
                hours: getPriorityProperty(priorityLevel, 'QuotationSubmissionTargetHours'),
                minutes: getPriorityProperty(priorityLevel, 'QuotationSubmissionTargetMinutes'),
                withinOfficeHours: getPriorityProperty(priorityLevel, 'QuotationSubmissionTargetWithinOfficeHours'),
                reference: getPriorityProperty(priorityLevel, 'QuotationSubmissionTargetReference'),
                getBaseDate: (ref) => {
                    // After Initial Follow Up Target or After Initial Follow Up
                    if (ref === REFERENCE_TEXT.AFTER_INITIAL_FOLLOWUP_TARGET ||
                        ref === REFERENCE_TEXT.AFTER_INITIAL_FOLLOWUP) {
                        return calculatedTargets.initialFollowUp || requestDateTime;
                    }
                    // Default: After Request Date
                    return requestDateTime;
                }
            },
            {
                type: 'costApproval',
                label: 'Cost Approval',
                days: getPriorityProperty(priorityLevel, 'CostApprovalTargetDays'),
                hours: getPriorityProperty(priorityLevel, 'CostApprovalTargetHours'),
                minutes: getPriorityProperty(priorityLevel, 'CostApprovalTargetMinutes'),
                withinOfficeHours: getPriorityProperty(priorityLevel, 'CostApprovalTargetWithinOfficeHours'),
                reference: getPriorityProperty(priorityLevel, 'CostApprovalTargetReference'),
                getBaseDate: (ref) => {
                    // After Quotation Submission Target or After Quotation Submission
                    if (ref === REFERENCE_TEXT.AFTER_QUOTATION_SUBMISSION_TARGET ||
                        ref === REFERENCE_TEXT.AFTER_QUOTATION_SUBMISSION) {
                        return calculatedTargets.quotation || requestDateTime;
                    }
                    // Default: After Request Date
                    return requestDateTime;
                }
            },
            {
                type: 'workCompletion',
                label: 'Work Completion',
                days: getPriorityProperty(priorityLevel, 'WorkCompletionTargetDays'),
                hours: getPriorityProperty(priorityLevel, 'WorkCompletionTargetHours'),
                minutes: getPriorityProperty(priorityLevel, 'WorkCompletionTargetMinutes'),
                withinOfficeHours: getPriorityProperty(priorityLevel, 'WorkCompletionTargetWithinOfficeHours'),
                reference: getPriorityProperty(priorityLevel, 'WorkCompletionTargetReference'),
                getBaseDate: (ref) => {
                    // After Initial Follow Up Target or After Initial Follow Up
                    if (ref === REFERENCE_TEXT.AFTER_INITIAL_FOLLOWUP_TARGET ||
                        ref === REFERENCE_TEXT.AFTER_INITIAL_FOLLOWUP) {
                        return calculatedTargets.initialFollowUp || requestDateTime;
                    }
                    // After Cost Approval Target or After Cost Approval
                    if (ref === REFERENCE_TEXT.AFTER_COST_APPROVAL_TARGET ||
                        ref === REFERENCE_TEXT.AFTER_COST_APPROVAL) {
                        return calculatedTargets.costApproval || requestDateTime;
                    }
                    // Default: After Request Date
                    return requestDateTime;
                }
            },
            {
                type: 'afterWork',
                label: 'After Work Follow Up',
                days: getPriorityProperty(priorityLevel, 'AfterWorkFollowUpTargetDays'),
                hours: getPriorityProperty(priorityLevel, 'AfterWorkFollowUpTargetHours'),
                minutes: getPriorityProperty(priorityLevel, 'AfterWorkFollowUpTargetMinutes'),
                withinOfficeHours: getPriorityProperty(priorityLevel, 'AfterWorkFollowUpTargetWithinOfficeHours'),
                reference: getPriorityProperty(priorityLevel, 'AfterWorkFollowUpTargetReference'),
                // After Work Follow Up always starts from Work Completion Target (legacy behavior)
                getBaseDate: () => {
                    return calculatedTargets.workCompletion || requestDateTime;
                }
            }
        ];

        // Calculate and display each target in sequence (order matters for chaining)
        targets.forEach(target => {
            // Skip if there's a manual override
            if (state.targetOverrides[target.type]) {
                return;
            }

            const days = target.days ?? 0;
            const hours = target.hours ?? 0;
            const minutes = target.minutes ?? 0;
            const hasDuration = days > 0 || hours > 0 || minutes > 0;

            if (hasDuration) {
                // Get the base date for this target (may depend on previous calculated targets)
                const baseDate = target.getBaseDate(target.reference);

                // Calculate target date using BusinessDateCalculator
                const targetDate = calculator.calculateTargetDate(
                    baseDate,
                    days,
                    hours,
                    minutes,
                    target.withinOfficeHours || false
                );

                // Store the calculated target for use by subsequent targets
                calculatedTargets[target.type] = targetDate;

                // Build tooltip text
                const tooltip = buildTooltipText(target, target.reference);

                // Update display
                updateTargetDateDisplay(target.type, targetDate, tooltip);
                $(`#${target.type}Target`).show();
            } else {
                // No target defined - show "No Target"
                $(`#${target.type}Target .target-date`).text('No Target');
                $(`#${target.type}Target`).attr('title', '');
                $(`#${target.type}Target`).show();
            }
        });
    }

    /**
     * Get human-readable reference description based on reference text
     * The reference is now a text string from the backend (e.g., "After Request Date")
     */
    function getReferenceDescription(reference, targetType) {
        // Special case for After Work Follow Up - always uses Work Completion
        if (targetType === 'afterWork') {
            return 'after Work Completion Target';
        }

        // The reference text from backend is already human-readable, just lowercase the first letter
        if (reference && typeof reference === 'string' && reference.length > 0) {
            return reference.charAt(0).toLowerCase() + reference.slice(1);
        }

        return 'after Request Date';
    }

    /**
     * Build tooltip text for target date
     */
    function buildTooltipText(target, reference) {
        let tooltip = 'Max ';

        if (target.days > 0) {
            tooltip += `${target.days} ${target.days === 1 ? 'day' : 'days'} `;
        }
        if (target.hours > 0) {
            tooltip += `${target.hours} ${target.hours === 1 ? 'hour' : 'hours'} `;
        }
        if (target.minutes > 0) {
            tooltip += `${target.minutes} ${target.minutes === 1 ? 'minute' : 'minutes'} `;
        }

        tooltip += getReferenceDescription(reference, target.type);

        if (target.withinOfficeHours) {
            tooltip += ' (within office hours only)';
        }

        tooltip += '. Click to change this target';

        return tooltip;
    }

    /**
     * Update target date display element
     */
    function updateTargetDateDisplay(type, targetDate, tooltip) {
        const $targetElement = $(`#${type}Target`);
        const formattedDate = formatDisplayDate(targetDate);

        $targetElement.find('.target-date').text(formattedDate);
        $targetElement.attr('title', tooltip);
        $targetElement.data('calculated-target', targetDate.toISOString());
    }

    /**
     * Format date for display (dd MMM yyyy hh:mm tt)
     */
    function formatDisplayDate(date) {
        const options = {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            hour12: true
        };

        return date.toLocaleString('en-US', options);
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
     * Initialize workers management
     */
    function initializeWorkers() {
        $('#addWorkerBtn').on('click', function () {
            resetWorkerModal();
            $('#addWorkerModal').modal('show');
        });

        $('input[name="workerSource"]').on('change', function () {
            $('#workerSearchModal').val('').removeData('selectedWorker');
            $('#selectedWorkerId').val('');
            $('#selectedWorkerSide').val('');
        });

        let workerSearchTimeout;
        $('#workerSearchModal').on('keyup', function () {
            clearTimeout(workerSearchTimeout);
            const term = $(this).val().trim();
            const source = $('input[name="workerSource"]:checked').val();

            // Clear selected worker data if user is typing new search
            $(this).removeData('selectedWorker');
            $('#selectedWorkerId').val('');

            if (term.length < CONFIG.minSearchLength) {
                $('#workerSearchDropdownModal').removeClass('show').empty();
                return;
            }

            workerSearchTimeout = setTimeout(() => searchWorkersForModal(term, source), CONFIG.debounceDelay);
        });

        $('#saveWorkerBtn').on('click', saveWorker);

        // Close dropdown when clicking outside
        $(document).on('click', function (e) {
            if (!$(e.target).closest('#workerSearchModal, #workerSearchDropdownModal').length) {
                $('#workerSearchDropdownModal').removeClass('show').empty();
            }
        });
    }

    function searchWorkersForModal(term, source) {
        const idLocation = state.selectedLocation;
        if (!idLocation) {
            showNotification('Please select a location first', 'warning');
            return;
        }

        const endpoint = source === 'company'
            ? CONFIG.apiEndpoints.searchWorkersByCompany
            : CONFIG.apiEndpoints.searchWorkersByServiceProvider;

        const params = { term, idLocation };
        if (source === 'serviceProvider') {
            const $selectedOption = $('#serviceProviderSelect option:selected');
            const idServiceProvider = $selectedOption.val();
            const idCompany = $selectedOption.attr('data-id-company');

            // Only validate that it's not the special "Not Specified" value
            if (!idServiceProvider || idServiceProvider === '-1') {
                showNotification('Please select a service provider first', 'warning');
                return;
            }

            params.idServiceProvider = idServiceProvider;
            if (idCompany) {
                params.idCompany = idCompany;
            }
        }

        $.ajax({
            url: endpoint,
            method: 'GET',
            data: params,
            success: function (response) {
                const $dropdown = $('#workerSearchDropdownModal');
                $dropdown.empty();

                if (response.success && response.data?.length > 0) {
                    $.each(response.data, function (index, worker) {
                        // Build display text with title and department
                        const name = worker.fullName || worker.FullName || worker.name;
                        const title = worker.title || worker.Title || '';
                        const department = worker.departmentName || worker.DepartmentName || '';

                        let detailsHtml = '';
                        if (title || department) {
                            detailsHtml = '<small class="text-muted">';
                            if (title) detailsHtml += title;
                            if (title && department) detailsHtml += '<br>';
                            if (department) detailsHtml += department;
                            detailsHtml += '</small>';
                        }

                        $dropdown.append(
                            $('<div></div>')
                                .addClass('typeahead-item')
                                .html(`<strong>${name}</strong>${detailsHtml ? '<br>' + detailsHtml : ''}`)
                                .on('click', () => selectWorkerForModal(worker, source))
                        );
                    });
                    $dropdown.addClass('show');
                } else {
                    $dropdown.append($('<div></div>').addClass('typeahead-item text-muted').text('No workers found'));
                    $dropdown.addClass('show');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error searching workers:', error);
                showNotification('Failed to search workers', 'error');
            }
        });
    }

    function selectWorkerForModal(worker, source) {
        const workerName = worker.fullName || worker.FullName || worker.name;
        const workerId = worker.idEmployee || worker.IdEmployee || worker.id || worker.Employee_idEmployee;
        const department = worker.departmentName || worker.DepartmentName || '';
        const title = worker.title || worker.Title || '';
        const email = worker.emailAddress || worker.EmailAddress || '';
        const phone = worker.phoneNumber || worker.PhoneNumber || '';

        $('#workerSearchModal').val(workerName);
        $('#selectedWorkerId').val(workerId);
        $('#selectedWorkerSide').val(worker.side_Enum_idEnum || (source === 'company' ? 1 : 2));
        $('#workerSearchDropdownModal').removeClass('show').empty();

        // Store full worker data for later use
        $('#workerSearchModal').data('selectedWorker', {
            id: workerId,
            fullName: workerName,
            departmentName: department,
            title: title,
            emailAddress: email,
            phoneNumber: phone
        });

        console.log('Worker selected:', { id: workerId, name: workerName, source: source });
    }

    function resetWorkerModal() {
        $('#sourceCompany').prop('checked', true);
        $('#workerSearchModal').val('').removeData('selectedWorker');
        $('#selectedWorkerId').val('');
        $('#selectedWorkerSide').val('');
        $('#joinChatRoomCheck').prop('checked', false);
        $('#workerSearchDropdownModal').removeClass('show').empty();

        // Update Service Provider Worker option visibility based on current selection
        const serviceProviderValue = $('#serviceProviderSelect').val();
        const showServiceProvider = serviceProviderValue && serviceProviderValue !== '-1' && serviceProviderValue !== '-2';
        updateWorkerSourceOptions(showServiceProvider);
    }

    function saveWorker() {
        const workerId = $('#selectedWorkerId').val();
        const workerSide = $('#selectedWorkerSide').val();
        const joinChatRoom = $('#joinChatRoomCheck').is(':checked');
        const source = $('input[name="workerSource"]:checked').val();

        // Get full worker data stored during selection
        const selectedWorkerData = $('#workerSearchModal').data('selectedWorker');

        if (!workerId || !selectedWorkerData) {
            showNotification('Please select a worker from the search results', 'error');
            return;
        }

        const worker = {
            Employee_idEmployee: parseInt(workerId),
            fullName: selectedWorkerData.fullName,
            departmentName: selectedWorkerData.departmentName,
            title: selectedWorkerData.title,
            emailAddress: selectedWorkerData.emailAddress,
            phoneNumber: selectedWorkerData.phoneNumber,
            side_Enum_idEnum: parseInt(workerSide),
            source: source === 'company' ? 'Company' : 'Service Provider',
            isJoinToExternalChatRoom: joinChatRoom
        };

        state.workers.push(worker);
        addWorkerCard(worker, state.workers.length - 1);

        $('#addWorkerModal').modal('hide');
        showNotification('Worker added successfully', 'success');
    }

    /**
     * Add worker card to the workers container
     */
    function addWorkerCard(worker, index) {
        // Hide empty state, show card list
        $('#workersEmptyState').hide();
        $('#workersCardList').show();

        const isCompany = worker.source === 'Company';
        const initials = generateInitials(worker.fullName);
        const avatarColor = generateAvatarColor(worker.fullName);

        // Build details lines
        let detailsHtml = '';
        if (worker.title) {
            detailsHtml += `<div><i class="ti ti-briefcase me-1"></i>${worker.title}</div>`;
        }
        if (worker.departmentName) {
            detailsHtml += `<div><i class="ti ti-building me-1"></i>${worker.departmentName}</div>`;
        }
        if (worker.emailAddress) {
            detailsHtml += `<div><i class="ti ti-mail me-1"></i>${worker.emailAddress}</div>`;
        }
        if (worker.phoneNumber) {
            detailsHtml += `<div><i class="ti ti-phone me-1"></i>${worker.phoneNumber}</div>`;
        }

        const cardHtml = `
            <div class="col-md-6 col-lg-4" data-worker-index="${index}">
                <div class="worker-card ${isCompany ? 'worker-company' : 'worker-provider'}">
                    <div class="d-flex align-items-start">
                        <div class="worker-avatar me-2" style="background-color: ${avatarColor};">
                            ${initials}
                        </div>
                        <div class="flex-grow-1 min-width-0">
                            <div class="d-flex justify-content-between align-items-start mb-1">
                                <div class="worker-name text-truncate">${worker.fullName}</div>
                                <button type="button" class="btn btn-sm btn-link text-danger p-0 btn-remove-worker"
                                        onclick="removeWorkerCard(${index})" title="Remove worker">
                                    <i class="ti ti-x"></i>
                                </button>
                            </div>
                            <div class="worker-details mb-2">
                                ${detailsHtml || '<div class="text-muted fst-italic">No details available</div>'}
                            </div>
                            <div class="d-flex flex-wrap gap-1">
                                <span class="worker-source-badge ${isCompany ? 'badge-company' : 'badge-provider'}">
                                    <i class="ti ${isCompany ? 'ti-building-community' : 'ti-truck-delivery'} me-1"></i>
                                    ${worker.source}
                                </span>
                                ${worker.isJoinToExternalChatRoom ? `
                                    <span class="worker-chat-badge">
                                        <i class="ti ti-message-circle me-1"></i>Chat
                                    </span>
                                ` : ''}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;

        $('#workersCardList').append(cardHtml);
    }

    /**
     * Remove worker card
     */
    window.removeWorkerCard = function (index) {
        // Remove from state
        state.workers.splice(index, 1);

        // Rebuild cards
        rebuildWorkerCards();

        showNotification('Worker removed', 'info');
    };

    /**
     * Rebuild all worker cards (after removal)
     */
    function rebuildWorkerCards() {
        const $cardList = $('#workersCardList');
        $cardList.empty();

        if (state.workers.length === 0) {
            $cardList.hide();
            $('#workersEmptyState').show();
        } else {
            state.workers.forEach((worker, index) => {
                addWorkerCard(worker, index);
            });
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

            // Important Checklist
            const importantChecklist = [];
            $('.important-checklist-item').each(function () {
                importantChecklist.push({
                    Type_idType: $(this).data('type-id'),
                    value: $(this).is(':checked')
                });
            });

            if (importantChecklist.length > 0) {
                if ($form.find('input[name="ImportantChecklistJson"]').length === 0) {
                    $('<input>').attr({ type: 'hidden', name: 'ImportantChecklistJson' }).appendTo($form);
                }
                $form.find('input[name="ImportantChecklistJson"]').val(JSON.stringify(importantChecklist));
            }

            // Workers
            if (state.workers.length > 0) {
                if ($form.find('input[name="WorkersJson"]').length === 0) {
                    $('<input>').attr({ type: 'hidden', name: 'WorkersJson' }).appendTo($form);
                }
                $form.find('input[name="WorkersJson"]').val(JSON.stringify(state.workers));
            }

            // Target Change Notes - map to hidden fields
            $('#helpdeskResponseTargetChangeNote').val(state.targetOverrides.helpdesk?.remark || '');
            $('#onsiteResponseTargetChangeNote').val(state.targetOverrides.initialFollowUp?.remark || '');
            $('#quotationSubmissionTargetChangeNote').val(state.targetOverrides.quotation?.remark || '');
            $('#costApprovalTargetChangeNote').val(state.targetOverrides.costApproval?.remark || '');
            $('#workCompletionTargetChangeNote').val(state.targetOverrides.workCompletion?.remark || '');
            $('#followUpTargetChangeNote').val(state.targetOverrides.afterWork?.remark || '');

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