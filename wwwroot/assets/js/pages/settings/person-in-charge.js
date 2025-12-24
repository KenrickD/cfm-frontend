// Person in Charge Management
(function ($) {
    'use strict';

    // State management
    let picList = [];
    let allProperties = [];
    let selectedEmployee = null;
    let editingPICId = null;
    let employeeSearchTimeout = null;

    // Offcanvas instances
    let addOffcanvas;
    let editOffcanvas;

    // Initialize on document ready
    $(document).ready(function () {
        initializeOffcanvas();
        loadPICList();
        loadProperties();
        attachEventListeners();
    });

    // Initialize Bootstrap Offcanvas instances
    function initializeOffcanvas() {
        const addOffcanvasElement = document.getElementById('addPICOffcanvas');
        const editOffcanvasElement = document.getElementById('editPICOffcanvas');

        if (addOffcanvasElement) {
            addOffcanvas = new bootstrap.Offcanvas(addOffcanvasElement);
            addOffcanvasElement.addEventListener('hidden.bs.offcanvas', resetAddForm);
        }

        if (editOffcanvasElement) {
            editOffcanvas = new bootstrap.Offcanvas(editOffcanvasElement);
            editOffcanvasElement.addEventListener('hidden.bs.offcanvas', resetEditForm);
        }
    }

    // Attach all event listeners
    function attachEventListeners() {
        // Show Add Drawer
        $('#showAddDrawerBtn').on('click', showAddDrawer);

        // Employee Search
        $('#employeeSearchInput').on('input', handleEmployeeSearch);
        $(document).on('click', handleClickOutside);

        // Dual Listbox Controls - Add Mode
        $('#addPropertiesBtn').on('click', () => transferProperties('available', 'assigned'));
        $('#addAllPropertiesBtn').on('click', () => transferAllProperties('available', 'assigned'));
        $('#removePropertiesBtn').on('click', () => transferProperties('assigned', 'available'));
        $('#removeAllPropertiesBtn').on('click', () => transferAllProperties('assigned', 'available'));

        // Dual Listbox Controls - Edit Mode
        $('#editAddPropertiesBtn').on('click', () => transferProperties('editAvailable', 'editAssigned'));
        $('#editAddAllPropertiesBtn').on('click', () => transferAllProperties('editAvailable', 'editAssigned'));
        $('#editRemovePropertiesBtn').on('click', () => transferProperties('editAssigned', 'editAvailable'));
        $('#editRemoveAllPropertiesBtn').on('click', () => transferAllProperties('editAssigned', 'editAvailable'));

        // Listbox Search
        $('#availablePropertiesSearch').on('input', () => filterListbox('availablePropertiesListbox', $('#availablePropertiesSearch').val()));
        $('#assignedPropertiesSearch').on('input', () => filterListbox('assignedPropertiesListbox', $('#assignedPropertiesSearch').val()));
        $('#editAvailablePropertiesSearch').on('input', () => filterListbox('editAvailablePropertiesListbox', $('#editAvailablePropertiesSearch').val()));
        $('#editAssignedPropertiesSearch').on('input', () => filterListbox('editAssignedPropertiesListbox', $('#editAssignedPropertiesSearch').val()));

        // Save and Update buttons
        $('#savePICBtn').on('click', savePIC);
        $('#updatePICBtn').on('click', updatePIC);

        // Delete confirmation
        $('#confirmDeleteBtn').on('click', confirmDelete);

        // Search PIC list
        $('#searchInput').on('input', filterPICList);
    }

    // Load PIC list
    function loadPICList() {
        $.ajax({
            url: '/Helpdesk/Settings/GetPersonsInCharge',
            type: 'GET',
            success: function (response) {
                if (response.success) {
                    picList = response.data;
                    renderPICList(picList);
                    updateTotalCount(picList.length);
                } else {
                    showToast('Error loading persons in charge', 'error');
                }
            },
            error: function () {
                showToast('Failed to load persons in charge', 'error');
            }
        });
    }

    // Load properties list
    function loadProperties() {
        $.ajax({
            url: '/Helpdesk/Settings/GetProperties',
            type: 'GET',
            success: function (response) {
                if (response.success) {
                    allProperties = response.data;
                } else {
                    showToast('Error loading properties', 'error');
                }
            },
            error: function () {
                showToast('Failed to load properties', 'error');
            }
        });
    }

    // Render PIC list
    function renderPICList(list) {
        const tbody = $('#picTableBody');
        const emptyState = $('#emptyState');

        if (!list || list.length === 0) {
            tbody.html(emptyState);
            return;
        }

        emptyState.hide();
        tbody.empty();

        list.forEach(pic => {
            const propertyCount = pic.properties ? pic.properties.length : 0;
            const row = `
                <div class="pic-list-item">
                    <div class="pic-info">
                        <div class="pic-name">${escapeHtml(pic.employeeName)}</div>
                        <div class="pic-property-count">
                            <i class="ti ti-building me-1"></i>
                            <span class="badge">${propertyCount}</span> ${propertyCount === 1 ? 'property' : 'properties'}
                        </div>
                    </div>
                    <div class="pic-actions">
                        <button type="button" class="btn btn-edit btn-action btn-sm" onclick="editPIC(${pic.id})">
                            <i class="ti ti-edit me-1"></i>
                            Edit
                        </button>
                        <button type="button" class="btn btn-delete btn-action btn-sm" onclick="deletePIC(${pic.id}, '${escapeHtml(pic.employeeName)}')">
                            <i class="ti ti-trash me-1"></i>
                            Delete
                        </button>
                    </div>
                </div>
            `;
            tbody.append(row);
        });
    }

    // Filter PIC list
    function filterPICList() {
        const searchTerm = $('#searchInput').val().toLowerCase();
        const filtered = picList.filter(pic =>
            pic.employeeName.toLowerCase().includes(searchTerm)
        );
        renderPICList(filtered);
        updateTotalCount(filtered.length);
    }

    // Update total count
    function updateTotalCount(count) {
        $('#totalCount').text(count);
    }

    // Show Add Drawer
    function showAddDrawer() {
        resetAddForm();
        addOffcanvas.show();
    }

    // Reset Add Form
    function resetAddForm() {
        selectedEmployee = null;
        $('#employeeSearchInput').val('');
        $('#employeeSearchResults').removeClass('show').empty();
        $('#selectedEmployeeDisplay').empty();
        $('#availablePropertiesSearch').val('');
        $('#assignedPropertiesSearch').val('');
        populateListbox('availablePropertiesListbox', allProperties);
        populateListbox('assignedPropertiesListbox', []);
    }

    // Handle Employee Search
    function handleEmployeeSearch() {
        const searchTerm = $(this).val().trim();

        clearTimeout(employeeSearchTimeout);

        if (searchTerm.length < 2) {
            $('#employeeSearchResults').removeClass('show').empty();
            return;
        }

        employeeSearchTimeout = setTimeout(() => {
            $.ajax({
                url: '/Helpdesk/SearchEmployees',
                type: 'GET',
                data: { searchTerm: searchTerm },
                success: function (response) {
                    if (response.success && response.data) {
                        renderEmployeeSearchResults(response.data);
                    }
                },
                error: function () {
                    showToast('Failed to search employees', 'error');
                }
            });
        }, 300);
    }

    // Render employee search results
    function renderEmployeeSearchResults(employees) {
        const resultsContainer = $('#employeeSearchResults');
        resultsContainer.empty();

        if (employees.length === 0) {
            resultsContainer.html('<div class="employee-search-item">No employees found</div>');
        } else {
            employees.forEach(emp => {
                const item = `
                    <div class="employee-search-item" data-employee-id="${emp.id}" data-employee-name="${escapeHtml(emp.name)}">
                        <div class="employee-name">${escapeHtml(emp.name)}</div>
                        <div class="employee-details">${escapeHtml(emp.email || '')} ${emp.department ? '• ' + escapeHtml(emp.department) : ''}</div>
                    </div>
                `;
                resultsContainer.append(item);
            });

            // Attach click handlers
            resultsContainer.find('.employee-search-item').on('click', function () {
                const employeeId = $(this).data('employee-id');
                const employeeName = $(this).data('employee-name');
                selectEmployee(employeeId, employeeName);
            });
        }

        resultsContainer.addClass('show');
    }

    // Select employee
    function selectEmployee(id, name) {
        selectedEmployee = { id, name };
        $('#employeeSearchInput').val(name);
        $('#employeeSearchResults').removeClass('show').empty();

        const display = `
            <div class="selected-employee">
                <i class="ti ti-user-check me-2"></i>
                ${escapeHtml(name)}
                <i class="ti ti-x" onclick="clearSelectedEmployee()"></i>
            </div>
        `;
        $('#selectedEmployeeDisplay').html(display);
    }

    // Clear selected employee (exposed globally)
    window.clearSelectedEmployee = function () {
        selectedEmployee = null;
        $('#employeeSearchInput').val('');
        $('#selectedEmployeeDisplay').empty();
    };

    // Handle click outside to close search results
    function handleClickOutside(e) {
        if (!$(e.target).closest('.employee-search-container').length) {
            $('#employeeSearchResults').removeClass('show');
        }
    }

    // Populate listbox
    function populateListbox(listboxId, items) {
        const listbox = $(`#${listboxId}`);
        listbox.empty();

        if (!items || items.length === 0) {
            listbox.html('<div class="text-muted text-center p-3">No items</div>');
            return;
        }

        items.forEach(item => {
            const div = $('<div>')
                .addClass('listbox-item')
                .attr('data-id', item.id)
                .text(item.name)
                .on('click', function () {
                    $(this).toggleClass('selected');
                });
            listbox.append(div);
        });
    }

    // Filter listbox
    function filterListbox(listboxId, searchTerm) {
        const listbox = $(`#${listboxId}`);
        const items = listbox.find('.listbox-item');

        items.each(function () {
            const text = $(this).text().toLowerCase();
            const matches = text.includes(searchTerm.toLowerCase());
            $(this).toggle(matches);
        });
    }

    // Transfer properties between listboxes
    function transferProperties(fromPrefix, toPrefix) {
        const fromListboxId = fromPrefix === 'available' ? 'availablePropertiesListbox' :
            fromPrefix === 'assigned' ? 'assignedPropertiesListbox' :
                fromPrefix === 'editAvailable' ? 'editAvailablePropertiesListbox' :
                    'editAssignedPropertiesListbox';

        const toListboxId = toPrefix === 'available' ? 'availablePropertiesListbox' :
            toPrefix === 'assigned' ? 'assignedPropertiesListbox' :
                toPrefix === 'editAvailable' ? 'editAvailablePropertiesListbox' :
                    'editAssignedPropertiesListbox';

        const fromListbox = $(`#${fromListboxId}`);
        const toListbox = $(`#${toListboxId}`);

        const selectedItems = fromListbox.find('.listbox-item.selected');

        if (selectedItems.length === 0) {
            showToast('Please select items to transfer', 'warning');
            return;
        }

        selectedItems.each(function () {
            const item = $(this).clone();
            item.removeClass('selected');
            toListbox.append(item);
            item.on('click', function () {
                $(this).toggleClass('selected');
            });
            $(this).remove();
        });

        // Remove "No items" message if exists
        toListbox.find('.text-muted').remove();
        if (fromListbox.find('.listbox-item').length === 0) {
            fromListbox.html('<div class="text-muted text-center p-3">No items</div>');
        }
    }

    // Transfer all properties
    function transferAllProperties(fromPrefix, toPrefix) {
        const fromListboxId = fromPrefix === 'available' ? 'availablePropertiesListbox' :
            fromPrefix === 'assigned' ? 'assignedPropertiesListbox' :
                fromPrefix === 'editAvailable' ? 'editAvailablePropertiesListbox' :
                    'editAssignedPropertiesListbox';

        const toListboxId = toPrefix === 'available' ? 'availablePropertiesListbox' :
            toPrefix === 'assigned' ? 'assignedPropertiesListbox' :
                toPrefix === 'editAvailable' ? 'editAvailablePropertiesListbox' :
                    'editAssignedPropertiesListbox';

        const fromListbox = $(`#${fromListboxId}`);
        const toListbox = $(`#${toListboxId}`);

        const allItems = fromListbox.find('.listbox-item');

        if (allItems.length === 0) {
            showToast('No items to transfer', 'warning');
            return;
        }

        allItems.each(function () {
            const item = $(this).clone();
            item.removeClass('selected');
            toListbox.append(item);
            item.on('click', function () {
                $(this).toggleClass('selected');
            });
        });

        fromListbox.html('<div class="text-muted text-center p-3">No items</div>');
        toListbox.find('.text-muted').remove();
    }

    // Get assigned property IDs from listbox
    function getAssignedPropertyIds(listboxId) {
        const ids = [];
        $(`#${listboxId}`).find('.listbox-item').each(function () {
            ids.push(parseInt($(this).attr('data-id')));
        });
        return ids;
    }

    // Save PIC
    function savePIC() {
        if (!selectedEmployee) {
            showToast('Please select an employee', 'warning');
            return;
        }

        const assignedPropertyIds = getAssignedPropertyIds('assignedPropertiesListbox');

        if (assignedPropertyIds.length === 0) {
            showToast('Please assign at least one property', 'warning');
            return;
        }

        const payload = {
            idEmployee: selectedEmployee.id,
            propertyIds: assignedPropertyIds
        };

        $.ajax({
            url: '/Helpdesk/Settings/CreatePersonInCharge',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response.success) {
                    showToast('Person in charge added successfully', 'success');
                    addOffcanvas.hide();
                    loadPICList();
                } else {
                    showToast(response.message || 'Failed to add person in charge', 'error');
                }
            },
            error: function () {
                showToast('An error occurred while adding person in charge', 'error');
            }
        });
    }

    // Edit PIC (exposed globally)
    window.editPIC = function (id) {
        editingPICId = id;

        // Fetch full PIC details including properties from backend
        $.ajax({
            url: '/Helpdesk/Settings/GetPersonInChargeById',
            type: 'GET',
            data: { id: id },
            success: function (response) {
                if (response.success && response.data) {
                    const pic = response.data;

                    // Set employee name (locked field)
                    $('#editEmployeeName').val(pic.employeeName);

                    // Populate listboxes with assigned properties from backend
                    const assignedPropertyIds = pic.properties ? pic.properties.map(p => p.id) : [];
                    const availableProperties = allProperties.filter(p => !assignedPropertyIds.includes(p.id));
                    const assignedProperties = pic.properties || [];

                    populateListbox('editAvailablePropertiesListbox', availableProperties);
                    populateListbox('editAssignedPropertiesListbox', assignedProperties);

                    // Clear search inputs
                    $('#editAvailablePropertiesSearch').val('');
                    $('#editAssignedPropertiesSearch').val('');

                    editOffcanvas.show();
                } else {
                    showToast('Failed to load person in charge details', 'error');
                }
            },
            error: function () {
                showToast('An error occurred while loading person in charge details', 'error');
            }
        });
    };

    // Reset Edit Form
    function resetEditForm() {
        editingPICId = null;
        $('#editEmployeeName').val('');
        $('#editAvailablePropertiesSearch').val('');
        $('#editAssignedPropertiesSearch').val('');
    }

    // Update PIC
    function updatePIC() {
        if (!editingPICId) {
            showToast('Invalid operation', 'error');
            return;
        }

        const assignedPropertyIds = getAssignedPropertyIds('editAssignedPropertiesListbox');

        if (assignedPropertyIds.length === 0) {
            showToast('Please assign at least one property', 'warning');
            return;
        }

        const payload = {
            id: editingPICId,
            propertyIds: assignedPropertyIds
        };

        $.ajax({
            url: '/Helpdesk/Settings/UpdatePersonInCharge',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response.success) {
                    showToast('Person in charge updated successfully', 'success');
                    editOffcanvas.hide();
                    loadPICList();
                } else {
                    showToast(response.message || 'Failed to update person in charge', 'error');
                }
            },
            error: function () {
                showToast('An error occurred while updating person in charge', 'error');
            }
        });
    }

    // Delete PIC (exposed globally)
    window.deletePIC = function (id, name) {
        $('#deleteItemName').text(name);
        $('#confirmDeleteBtn').data('id', id);
        const deleteModal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
        deleteModal.show();
    };

    // Confirm Delete
    function confirmDelete() {
        const id = $(this).data('id');

        $.ajax({
            url: '/Helpdesk/Settings/DeletePersonInCharge',
            type: 'DELETE',
            contentType: 'application/json',
            data: JSON.stringify({ id: id }),
            success: function (response) {
                if (response.success) {
                    showToast('Person in charge deleted successfully', 'success');
                    const deleteModal = bootstrap.Modal.getInstance(document.getElementById('deleteConfirmModal'));
                    deleteModal.hide();
                    loadPICList();
                } else {
                    showToast(response.message || 'Failed to delete person in charge', 'error');
                }
            },
            error: function () {
                showToast('An error occurred while deleting person in charge', 'error');
            }
        });
    }

    // Utility: Escape HTML
    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }

    // Utility: Show Toast
    function showToast(message, type = 'info') {
        // Use your existing toast notification system
        // This is a placeholder - adjust based on your UI framework
        const iconMap = {
            success: 'ti-check',
            error: 'ti-x',
            warning: 'ti-alert-triangle',
            info: 'ti-info-circle'
        };

        const icon = iconMap[type] || iconMap.info;
        console.log(`[${type.toUpperCase()}] ${message}`);

        // If you have a toast library like Toastr or Bootstrap Toast, use it here
        if (typeof toastr !== 'undefined') {
            toastr[type](message);
        } else {
            alert(message);
        }
    }

})(jQuery);
