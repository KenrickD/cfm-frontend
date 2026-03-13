/**
 * Send New Work Request Page - JavaScript Module
 * Handles all interactive functionality for the simplified Send Work Request form
 * Dependencies: jQuery
 */

(function ($) {
    'use strict';

    // Configuration
    const CONFIG = {
        apiEndpoints: {
            floors: '/Helpdesk/GetFloorsByLocation',
            rooms: '/Helpdesk/GetRoomsByFloor',
            workCategories: MvcEndpoints.Helpdesk.WorkRequest.GetWorkCategoriesByTypes
        },
        maxFileSize: 5 * 1024 * 1024, // 5MB per file
        allowedFileTypes: ['image/jpeg', 'image/jpg', 'image/png', 'image/gif']
    };

    // State management
    const state = {
        selectedLocation: null,
        selectedFloor: null,
        selectedRoom: null,
        selectedWorkCategory: null,
        uploadedFiles: [],
        selectedRequestor: null,
        requestorSearchTimeout: null,
        workRequestList: [],
        workRequestMetadata: null,
        currentPage: 1,
        pageSize: 10,
        isAutoSelecting: false  // Flag to prevent cascade interference during auto-selection
    };

    /**
     * Initialize the module
     */
    function init() {
        // Check if user has employee profile
        if (!window.PageContext || !window.PageContext.hasEmployeeProfile) {
            return;
        }

        // Remove 'required' attribute from searchable dropdowns to prevent browser validation errors
        // (searchable dropdown hides native select, making it unfocusable)
        $('#locationSelect, #floorSelect, #roomSelect, #workCategorySelect').removeAttr('required');

        loadWorkCategories();
        initializeLocationCascade();
        initializeFileUpload();
        initializeRequestorToggle();
        initializeRequestorSearch();
        initializeFormSubmission();
        loadWorkRequestList();
        autoSelectFirstLocation();

    }

    /**
     * Auto-select first location on page load
     */
    function autoSelectFirstLocation() {
        const $locationSelect = $('#locationSelect');
        const locationElement = document.getElementById('locationSelect');


        // Function to perform auto-selection
        function performAutoSelect() {

            // Get first valid location option (skip "Select Location" and "Not Specified")
            const firstOption = $locationSelect.find('option').filter(function() {
                const val = $(this).val();
                return val && val !== '' && val !== '-1';
            }).first();


            if (firstOption.length > 0) {
                const firstLocationId = firstOption.val();
                const firstLocationText = firstOption.text();


                // Set flag to prevent cascade interference
                state.isAutoSelecting = true;

                // Update native select
                $locationSelect.val(firstLocationId);
                state.selectedLocation = firstLocationId;


                // Update searchable dropdown component WITHOUT triggering change (third param = true)
                if (locationElement && locationElement._searchableDropdown) {
                    locationElement._searchableDropdown.setValue(firstLocationId, firstLocationText, true);
                }

                // Manually trigger change ONCE after updating both native select and searchable dropdown
                $locationSelect.trigger('change');
            } else {
            }
        }

        let attemptCount = 0;
        let hasExecuted = false;

        // Wait for searchable dropdown to be initialized
        const checkInterval = setInterval(function() {
            attemptCount++;
            const hasDropdown = locationElement && locationElement._searchableDropdown;

            if (attemptCount % 10 === 0) {
            }

            if (hasDropdown && !hasExecuted) {
                clearInterval(checkInterval);
                hasExecuted = true;
                performAutoSelect();
            }

            // Stop checking after 100 attempts (5 seconds)
            if (attemptCount >= 100) {
                clearInterval(checkInterval);
                if (!hasExecuted) {
                    hasExecuted = true;
                    performAutoSelect();
                }
            }
        }, 50);
    }

    /**
     * Load work categories from API
     */
    function loadWorkCategories() {
        const $select = $('#workCategorySelect');
        const selectElement = document.getElementById('workCategorySelect');

        // Show loading state
        $select.empty().append('<option value="">Loading work categories...</option>');
        $select.prop('disabled', true);

        // Add loading class to searchable dropdown if exists
        if (selectElement && selectElement._searchableDropdown) {
            const dropdownWrapper = selectElement.closest('.searchable-dropdown');
            if (dropdownWrapper) {
                dropdownWrapper.classList.add('loading');
            }
        }

        $.ajax({
            url: CONFIG.apiEndpoints.workCategories,
            method: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    $select.empty()
                        .append('<option value="">Select Work Category</option>')
                        .append('<option value="null">Not Specified</option>');

                    $.each(response.data, function (index, category) {
                        $select.append(
                            $('<option></option>')
                                .val(category.idType)
                                .text(category.typeName)
                        );
                    });

                    // Auto-select "Not Specified" on page load
                    $select.val('null');
                    state.selectedWorkCategory = 'null';

                    // Enable and refresh searchable dropdown
                    $select.prop('disabled', false);
                    if (selectElement && selectElement._searchableDropdown) {
                        selectElement._searchableDropdown.loadFromSelect();
                        selectElement._searchableDropdown.enable();
                        selectElement._searchableDropdown.setValue('null', 'Not Specified', true);
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading work categories:', error);
                $select.empty().append('<option value="">Error loading categories</option>');
                showNotification('Error loading work categories', 'error');
                $select.prop('disabled', false);
            },
            complete: function() {
                // Remove loading state
                if (selectElement && selectElement._searchableDropdown) {
                    const dropdownWrapper = selectElement.closest('.searchable-dropdown');
                    if (dropdownWrapper) {
                        dropdownWrapper.classList.remove('loading');
                    }
                }
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
        const $workCategorySelect = $('#workCategorySelect');


        // When location changes, load floors
        $locationSelect.on('change', function () {
            const locationId = $(this).val();
            state.selectedLocation = locationId;

            // Only reset dropdowns if NOT during auto-selection
            if (!state.isAutoSelecting) {
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

            // Only reset dropdowns if NOT during auto-selection
            if (!state.isAutoSelecting) {
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
            } else {
            }

            if (floorId && floorId !== '' && state.selectedLocation) {
                loadRooms(state.selectedLocation, floorId);
            } else {
                if (!state.isAutoSelecting) {
                    $roomSelect.empty()
                        .append('<option value="">Select Room/Area/Zone</option>')
                        .append('<option value="-1">Not Specified</option>');
                }
            }
        });

        // When room changes, update state
        $roomSelect.on('change', function () {
            const roomId = $(this).val();
            state.selectedRoom = roomId;
        });

        // When work category changes, update state
        $workCategorySelect.on('change', function () {
            const workCategoryId = $(this).val();
            state.selectedWorkCategory = workCategoryId;
        });
    }

    /**
     * Load floors for selected property
     */
    function loadFloors(propertyId) {
        const $floorSelect = $('#floorSelect');
        const selectElement = document.getElementById('floorSelect');

        // Show loading state
        $floorSelect.empty().append('<option value="">Loading floors...</option>');
        $floorSelect.prop('disabled', true);

        // Add loading class to searchable dropdown if exists
        if (selectElement && selectElement._searchableDropdown) {
            const dropdownWrapper = selectElement.closest('.searchable-dropdown');
            if (dropdownWrapper) {
                dropdownWrapper.classList.add('loading');
            }
        }

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
                    if (selectElement && selectElement._searchableDropdown) {
                        selectElement._searchableDropdown.loadFromSelect();
                        selectElement._searchableDropdown.enable();
                    }

                    // Auto-select first floor (skip "Select Floor" and "Not Specified")
                    if (response.data.length > 0 && state.isAutoSelecting) {
                        const firstFloorId = response.data[0].idPropertyFloor;
                        const firstFloorName = response.data[0].floorUnitName;

                        $floorSelect.val(firstFloorId);
                        state.selectedFloor = firstFloorId;

                        // Update searchable dropdown component WITHOUT triggering change (third param = true)
                        if (selectElement && selectElement._searchableDropdown) {
                            selectElement._searchableDropdown.setValue(firstFloorId, firstFloorName, true);
                        }

                        // Manually trigger change ONCE after updating both native select and searchable dropdown
                        $floorSelect.trigger('change');
                    }
                } else {
                    // No floors available - ALWAYS auto-select "Not Specified"

                    // Make sure "Not Specified" option exists
                    if ($floorSelect.find('option[value="-1"]').length === 0) {
                        $floorSelect.append('<option value="-1">Not Specified</option>');
                    }

                    $floorSelect.val('-1');
                    state.selectedFloor = '-1';
                    $floorSelect.prop('disabled', false);


                    // Update searchable dropdown component
                    if (selectElement && selectElement._searchableDropdown) {
                        selectElement._searchableDropdown.loadFromSelect();
                        selectElement._searchableDropdown.enable();
                        selectElement._searchableDropdown.setValue('-1', 'Not Specified', !state.isAutoSelecting);
                    }

                    // Trigger change to load rooms (will also select "Not Specified" for rooms if empty)
                    if (state.isAutoSelecting) {
                        $floorSelect.trigger('change');
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading floors:', error);
                $floorSelect.empty().append('<option value="">Error loading floors</option>');
                showNotification('Error loading floors. Please try again.', 'error');
            },
            complete: function() {
                // Remove loading state
                if (selectElement && selectElement._searchableDropdown) {
                    const dropdownWrapper = selectElement.closest('.searchable-dropdown');
                    if (dropdownWrapper) {
                        dropdownWrapper.classList.remove('loading');
                    }
                }
            }
        });
    }

    /**
     * Load room zones for selected property and floor
     */
    function loadRooms(propertyId, floorId) {
        const $roomSelect = $('#roomSelect');
        const selectElement = document.getElementById('roomSelect');

        // Only show loading state if NOT during auto-selection (to preserve options)
        if (!state.isAutoSelecting) {
            $roomSelect.empty().append('<option value="">Loading rooms...</option>');
            $roomSelect.prop('disabled', true);

            // Add loading class to searchable dropdown if exists
            if (selectElement && selectElement._searchableDropdown) {
                const dropdownWrapper = selectElement.closest('.searchable-dropdown');
                if (dropdownWrapper) {
                    dropdownWrapper.classList.add('loading');
                }
            }
        } else {
        }

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
                    if (selectElement && selectElement._searchableDropdown) {
                        selectElement._searchableDropdown.loadFromSelect();
                        selectElement._searchableDropdown.enable();
                    }

                    // Auto-select first room (skip "Select Room/Area/Zone" and "Not Specified")
                    if (response.data.length > 0 && state.isAutoSelecting) {
                        const firstRoomId = response.data[0].idRoomZone;
                        const firstRoomName = response.data[0].roomZoneName;

                        $roomSelect.val(firstRoomId);
                        state.selectedRoom = firstRoomId;

                        // Update searchable dropdown component WITHOUT triggering change (third param = true)
                        if (selectElement && selectElement._searchableDropdown) {
                            selectElement._searchableDropdown.setValue(firstRoomId, firstRoomName, true);
                        }

                        // Clear auto-selecting flag after final room is selected
                        state.isAutoSelecting = false;
                    }
                } else {
                    // No rooms available - ALWAYS auto-select "Not Specified"

                    // Make sure "Not Specified" option exists
                    if ($roomSelect.find('option[value="-1"]').length === 0) {
                        $roomSelect.append('<option value="-1">Not Specified</option>');
                    }

                    $roomSelect.val('-1');
                    state.selectedRoom = '-1';
                    $roomSelect.prop('disabled', false);


                    // Update searchable dropdown component
                    if (selectElement && selectElement._searchableDropdown) {
                        selectElement._searchableDropdown.loadFromSelect();
                        selectElement._searchableDropdown.enable();
                        selectElement._searchableDropdown.setValue('-1', 'Not Specified', !state.isAutoSelecting);
                    }

                    // Clear auto-selecting flag if it was set
                    if (state.isAutoSelecting) {
                        state.isAutoSelecting = false;
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading rooms:', error);
                $roomSelect.empty().append('<option value="">Error loading rooms</option>');
                showNotification('Error loading rooms. Please try again.', 'error');
            },
            complete: function() {
                // Remove loading state
                if (selectElement && selectElement._searchableDropdown) {
                    const dropdownWrapper = selectElement.closest('.searchable-dropdown');
                    if (dropdownWrapper) {
                        dropdownWrapper.classList.remove('loading');
                    }
                }
            }
        });
    }

    /**
     * Initialize file upload functionality
     */
    function initializeFileUpload() {
        const $fileInput = $('#relatedPhotos');

        $fileInput.on('change', function (e) {
            const files = e.target.files;

            if (files.length === 0) {
                return;
            }

            // Process each file
            Array.from(files).forEach(file => {
                // Validate file type
                if (!CONFIG.allowedFileTypes.includes(file.type)) {
                    showNotification(`File "${file.name}" is not a valid image type.`, 'error');
                    return;
                }

                // Validate file size
                if (file.size > CONFIG.maxFileSize) {
                    showNotification(`File "${file.name}" exceeds the maximum size of 5MB.`, 'error');
                    return;
                }

                // Add to state
                const fileId = Date.now() + '_' + Math.random().toString(36).substr(2, 9);
                state.uploadedFiles.push({
                    id: fileId,
                    name: file.name,
                    size: file.size,
                    type: file.type,
                    file: file
                });

                // Update table
                updateUploadedFilesTable();
            });

            // Clear input for next selection
            $fileInput.val('');
        });

        // Make remove function global
        window.removeUploadedFile = function (fileId) {
            // Remove from state
            state.uploadedFiles = state.uploadedFiles.filter(f => f.id !== fileId);

            // Update table
            updateUploadedFilesTable();
        };
    }

    /**
     * Update uploaded files table
     */
    function updateUploadedFilesTable() {
        const $tbody = $('#uploadedFilesTable tbody');
        $tbody.empty();

        if (state.uploadedFiles.length === 0) {
            $tbody.html(`
                <tr>
                    <td class="text-center text-muted">
                        <em>No file yet</em>
                    </td>
                </tr>
            `);
            return;
        }

        state.uploadedFiles.forEach((file, index) => {
            const fileSizeKB = (file.size / 1024).toFixed(2);
            const row = `
                <tr>
                    <td style="width: 50px;" class="text-center">${index + 1}</td>
                    <td>
                        <i class="ti ti-file-photo me-2"></i>
                        ${file.name}
                        <small class="text-muted ms-2">(${fileSizeKB} KB)</small>
                    </td>
                    <td style="width: 100px;" class="text-center">
                        <button type="button"
                                class="btn btn-sm btn-danger"
                                onclick="removeUploadedFile('${file.id}')"
                                title="Remove file">
                            <i class="ti ti-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
            $tbody.append(row);
        });
    }

    /**
     * Initialize requestor toggle (Myself vs On behalf of other)
     */
    function initializeRequestorToggle() {
        const $forMyself = $('#forMyself');
        const $forOther = $('#forOther');
        const $requestorSearchContainer = $('#requestorSearchContainer');
        const $selectedRequestorCard = $('#selectedRequestorCard');

        // Handle radio button change
        $('input[name="ForWhom"]').on('change', function() {
            if ($forOther.is(':checked')) {
                // Show requestor search
                $requestorSearchContainer.slideDown();
                // Clear any previous selection
                clearSelectedRequestor();
            } else {
                // Hide requestor search and clear selection
                $requestorSearchContainer.slideUp();
                $selectedRequestorCard.slideUp();
                clearSelectedRequestor();
            }
        });

        // Handle remove requestor button
        $('#removeRequestorBtn').on('click', function() {
            clearSelectedRequestor();
            $('#requestorSearch').val('').focus();
        });
    }

    /**
     * Initialize requestor search functionality
     */
    function initializeRequestorSearch() {
        const $searchInput = $('#requestorSearch');
        const $searchResults = $('#requestorSearchResults');

        // Handle search input
        $searchInput.on('input', function() {
            const searchTerm = $(this).val().trim();

            // Clear previous timeout
            if (state.requestorSearchTimeout) {
                clearTimeout(state.requestorSearchTimeout);
            }

            if (searchTerm.length < 1) {
                $searchResults.removeClass('show').empty();
                return;
            }

            // Debounce search
            state.requestorSearchTimeout = setTimeout(() => {
                searchRequestors(searchTerm);
            }, 300);
        });

        // Close dropdown when clicking outside
        $(document).on('click', function(e) {
            if (!$(e.target).closest('#requestorSearch, #requestorSearchResults').length) {
                $searchResults.removeClass('show');
            }
        });
    }

    /**
     * Search for requestors via API
     */
    function searchRequestors(searchTerm) {
        const $searchResults = $('#requestorSearchResults');

        // Show loading state
        $searchResults.html('<div class="dropdown-item text-muted"><i class="ti ti-loader spinning me-2"></i>Searching...</div>').addClass('show');

        $.ajax({
            url: MvcEndpoints.Helpdesk.Search.Requestors,
            method: 'GET',
            data: {
                term: searchTerm,
                idCompany: window.PageContext.idCompany
            },
            success: function(response) {
                if (response.success && response.data && response.data.length > 0) {
                    let html = '';
                    response.data.forEach(function(requestor) {
                        html += `
                            <a href="#" class="dropdown-item requestor-item" data-id="${requestor.idEmployee}" data-name="${requestor.fullName}" data-dept="${requestor.departmentName || 'N/A'}">
                                <div>
                                    <strong>${requestor.fullName}</strong>
                                    <br>
                                    <small class="text-muted">${requestor.departmentName || 'N/A'}</small>
                                </div>
                            </a>
                        `;
                    });
                    $searchResults.html(html).addClass('show');

                    // Handle requestor selection
                    $('.requestor-item').on('click', function(e) {
                        e.preventDefault();
                        const requestorId = $(this).data('id');
                        const requestorName = $(this).data('name');
                        const requestorDept = $(this).data('dept');

                        selectRequestor(requestorId, requestorName, requestorDept);
                        $searchResults.removeClass('show');
                        $('#requestorSearch').val('');
                    });
                } else {
                    $searchResults.html('<div class="dropdown-item text-muted">No results found</div>').addClass('show');
                }
            },
            error: function() {
                $searchResults.html('<div class="dropdown-item text-danger">Error searching requestors</div>').addClass('show');
            }
        });
    }

    /**
     * Select a requestor
     */
    function selectRequestor(id, name, department) {
        state.selectedRequestor = { id, name, department };

        // Update UI
        $('#selectedRequestorName').text(name);
        $('#selectedRequestorDept').text(department);
        $('#idRequestorInput').val(id);

        // Show card and hide search
        $('#requestorSearchContainer').slideUp();
        $('#selectedRequestorCard').slideDown();
    }

    /**
     * Clear selected requestor
     */
    function clearSelectedRequestor() {
        state.selectedRequestor = null;
        $('#idRequestorInput').val('');
        $('#selectedRequestorCard').slideUp();
        $('#requestorSearchContainer').slideDown();
    }

    /**
     * Load work request list for current user
     */
    function loadWorkRequestList() {
        const idEmployee = window.PageContext.idEmployee;
        const idClient = window.PageContext.idClient;

        if (!idEmployee || !idClient) {
            $('#workRequestListLoading').hide();
            $('#workRequestListError').show();
            return;
        }

        $.ajax({
            url: MvcEndpoints.Helpdesk.WorkRequest.GetSendWorkRequestList,
            method: 'GET',
            data: {
                idEmployee: idEmployee,
                idClient: idClient
            },
            success: function(response) {
                $('#workRequestListLoading').hide();

                if (response.success && response.data && response.data.length > 0) {
                    // Sort by request date descending (most recent first)
                    const sortedData = response.data.sort(function(a, b) {
                        return new Date(b.requestDate) - new Date(a.requestDate);
                    });

                    // Store full dataset and metadata
                    state.workRequestList = sortedData;
                    state.workRequestMetadata = response.metadata || null;
                    state.currentPage = 1;

                    // Render first page
                    renderWorkRequestListPage();
                    $('#workRequestListContainer').show();
                } else {
                    $('#workRequestListEmpty').show();
                }
            },
            error: function(xhr, status, error) {
                console.error('Error loading work request list:', error);
                $('#workRequestListLoading').hide();
                $('#workRequestListErrorMessage').text('Failed to load your work requests');
                $('#workRequestListError').show();
            }
        });
    }

    /**
     * Render work request list for current page
     */
    function renderWorkRequestListPage() {
        const $tbody = $('#workRequestListBody');
        $tbody.empty();

        // Calculate pagination
        const totalRecords = state.workRequestList.length;
        const totalPages = Math.ceil(totalRecords / state.pageSize);
        const startIndex = (state.currentPage - 1) * state.pageSize;
        const endIndex = Math.min(startIndex + state.pageSize, totalRecords);
        const pageData = state.workRequestList.slice(startIndex, endIndex);

        // Render rows for current page
        pageData.forEach((wr, index) => {
            const requestDate = new Date(wr.requestDate).toLocaleDateString();
            const location = `${wr.propertyName || ''} ${wr.floor || ''} ${wr.roomZone || ''}`.trim() || 'N/A';
            const globalIndex = startIndex + index + 1;

            const row = `
                <tr>
                    <td>${globalIndex}</td>
                    <td><strong>${wr.workRequestCode || 'N/A'}</strong></td>
                    <td>${wr.workTitle || 'N/A'}</td>
                    <td><span class="badge bg-info">${wr.status || 'N/A'}</span></td>
                    <td>${location}</td>
                    <td>${requestDate}</td>
                </tr>
            `;
            $tbody.append(row);
        });

        // Render pagination controls
        renderPagination(totalPages, totalRecords);
    }

    /**
     * Render pagination controls
     */
    function renderPagination(totalPages, totalRecords) {
        const $paginationContainer = $('#workRequestListPagination');

        if (totalPages <= 1) {
            $paginationContainer.hide();
            return;
        }

        $paginationContainer.show();
        let paginationHtml = '<nav><ul class="pagination pagination-sm justify-content-center mb-0">';

        // Previous button
        paginationHtml += `
            <li class="page-item ${state.currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${state.currentPage - 1}">Previous</a>
            </li>
        `;

        // Page numbers (show max 5 pages)
        const maxPageButtons = 5;
        let startPage = Math.max(1, state.currentPage - Math.floor(maxPageButtons / 2));
        let endPage = Math.min(totalPages, startPage + maxPageButtons - 1);

        if (endPage - startPage < maxPageButtons - 1) {
            startPage = Math.max(1, endPage - maxPageButtons + 1);
        }

        // First page button
        if (startPage > 1) {
            paginationHtml += `<li class="page-item"><a class="page-link" href="#" data-page="1">1</a></li>`;
            if (startPage > 2) {
                paginationHtml += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            }
        }

        // Page number buttons
        for (let i = startPage; i <= endPage; i++) {
            paginationHtml += `
                <li class="page-item ${i === state.currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>
            `;
        }

        // Last page button
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                paginationHtml += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            }
            paginationHtml += `<li class="page-item"><a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a></li>`;
        }

        // Next button
        paginationHtml += `
            <li class="page-item ${state.currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${state.currentPage + 1}">Next</a>
            </li>
        `;

        paginationHtml += '</ul></nav>';

        // Add info text
        const startRecord = (state.currentPage - 1) * state.pageSize + 1;
        const endRecord = Math.min(state.currentPage * state.pageSize, totalRecords);
        paginationHtml += `
            <div class="text-center text-muted mt-2 small">
                Showing ${startRecord} to ${endRecord} of ${totalRecords} work requests
            </div>
        `;

        $paginationContainer.html(paginationHtml);

        // Attach click handlers
        $paginationContainer.find('.page-link').on('click', function(e) {
            e.preventDefault();
            const $this = $(this);
            if ($this.parent().hasClass('disabled') || $this.parent().hasClass('active')) {
                return;
            }

            const newPage = parseInt($this.data('page'));
            if (newPage >= 1 && newPage <= totalPages) {
                state.currentPage = newPage;
                renderWorkRequestListPage();

                // Scroll to table
                $('html, body').animate({
                    scrollTop: $('#workRequestListContainer').offset().top - 100
                }, 300);
            }
        });
    }

    /**
     * Initialize form submission handling
     */
    function initializeFormSubmission() {
        const $form = $('#sendWorkRequestForm');

        $form.on('submit', function (e) {
            e.preventDefault();

            // Custom validation for searchable dropdowns (since they don't have 'required' attribute)
            // Note: -1 (Not Specified) is a VALID selection for floor and room
            // Use state values for validation as they are reliably updated during cascade
            const locationValue = state.selectedLocation || $('#locationSelect').val();
            const floorValue = state.selectedFloor || $('#floorSelect').val();
            const roomValue = state.selectedRoom || $('#roomSelect').val();
            const workCategoryValue = state.selectedWorkCategory || $('#workCategorySelect').val();

            console.log('[VALIDATION] Location:', locationValue, '(state:', state.selectedLocation, ')');
            console.log('[VALIDATION] Floor:', floorValue, '(state:', state.selectedFloor, ')');
            console.log('[VALIDATION] Room:', roomValue, '(state:', state.selectedRoom, ')');
            console.log('[VALIDATION] Work Category:', workCategoryValue, '(state:', state.selectedWorkCategory, ')');

            // Location must be selected (not empty, not "Not Specified")
            if (!locationValue || locationValue === '' || locationValue === '-1') {
                showNotification('Please select a valid location', 'error');
                return false;
            }

            // Floor must be selected (empty is invalid, but "Not Specified" -1 is valid)
            if (!floorValue || floorValue === '') {
                showNotification('Please select a floor', 'error');
                return false;
            }

            // Room must be selected (empty is invalid, but "Not Specified" -1 is valid)
            if (!roomValue || roomValue === '') {
                showNotification('Please select a room/area/zone', 'error');
                return false;
            }

            // Work Category must be selected (not empty)
            if (!workCategoryValue || workCategoryValue === '') {
                showNotification('Please select a work category', 'error');
                return false;
            }

            // Validate form
            if (!$form[0].checkValidity()) {
                $form[0].reportValidity();
                return false;
            }

            // Create FormData for file upload
            const formData = new FormData($form[0]);

            // Remove the default file input and add individual files
            formData.delete('RelatedPhotos');
            state.uploadedFiles.forEach((fileObj, index) => {
                formData.append(`RelatedPhotos`, fileObj.file);
            });

            // Show loading indicator
            showLoadingOverlay();

            // Submit form via AJAX
            $.ajax({
                url: $form.attr('action'),
                method: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                success: function (response) {
                    hideLoadingOverlay();

                    if (response.success) {
                        showNotification(`Work request ${response.workRequestCode || ''} sent successfully!`, 'success');

                        // Reset form
                        $form[0].reset();
                        state.uploadedFiles = [];
                        updateUploadedFilesTable();

                        // Reset state
                        state.selectedLocation = null;
                        state.selectedFloor = null;
                        state.selectedRoom = null;
                        state.selectedWorkCategory = null;

                        // Reset dropdowns
                        $('#floorSelect').prop('disabled', true).empty().append('<option value="">Select Floor</option>');
                        $('#roomSelect').prop('disabled', true).empty().append('<option value="">Select Room/Area/Zone</option>');

                        // Reset searchable dropdown components
                        const locationElement = document.getElementById('locationSelect');
                        const floorElement = document.getElementById('floorSelect');
                        const roomElement = document.getElementById('roomSelect');
                        const workCategoryElement = document.getElementById('workCategorySelect');

                        if (locationElement && locationElement._searchableDropdown) {
                            locationElement._searchableDropdown.clear();
                        }
                        if (floorElement && floorElement._searchableDropdown) {
                            floorElement._searchableDropdown.clear();
                            floorElement._searchableDropdown.disable();
                        }
                        if (roomElement && roomElement._searchableDropdown) {
                            roomElement._searchableDropdown.clear();
                            roomElement._searchableDropdown.disable();
                        }
                        if (workCategoryElement && workCategoryElement._searchableDropdown) {
                            workCategoryElement._searchableDropdown.clear();
                        }

                        // Re-trigger auto-selection for location
                        autoSelectFirstLocation();

                        // Reset requestor selection
                        if ($('#forOther').is(':checked')) {
                            $('#forMyself').prop('checked', true).trigger('change');
                        }

                        // Reload work request list
                        $('#workRequestListContainer').hide();
                        $('#workRequestListEmpty').hide();
                        $('#workRequestListError').hide();
                        $('#workRequestListLoading').show();
                        loadWorkRequestList();

                        // Scroll to top
                        $('html, body').animate({ scrollTop: 0 }, 300);
                    } else {
                        showNotification(response.message || 'Failed to send work request. Please try again.', 'error');
                    }
                },
                error: function (xhr, status, error) {
                    hideLoadingOverlay();
                    console.error('Error submitting work request:', error);
                    showNotification('An error occurred while sending the work request. Please try again.', 'error');
                }
            });

            return false;
        });
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
        if ($('#loading-overlay').length > 0) {
            return;
        }

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

    /**
     * Utility: Hide loading overlay
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
