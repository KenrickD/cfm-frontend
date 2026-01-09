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
        uploadedFiles: []
    };

    /**
     * Initialize the module
     */
    function init() {
        loadWorkCategories();
        initializeLocationCascade();
        initializeFileUpload();
        initializeFormSubmission();

        console.log('Send Work Request page initialized');
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
                showNotification('Error loading work categories', 'error');
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
     * Initialize form submission handling
     */
    function initializeFormSubmission() {
        const $form = $('#sendWorkRequestForm');

        $form.on('submit', function (e) {
            e.preventDefault();

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
                        showNotification('Work request sent successfully!', 'success');

                        // Redirect after 2 seconds
                        setTimeout(function () {
                            window.location.href = response.redirectUrl || '/Helpdesk/Index';
                        }, 2000);
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
