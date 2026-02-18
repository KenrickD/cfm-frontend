// Person in Charge Management (Settings)
(function ($) {
    'use strict';

    // Configuration
    const CONFIG = {
        debounceDelay: 300,
        minSearchLength: 1,
        apiEndpoints: {
            list: MvcEndpoints.Helpdesk.Settings.PersonInCharge.List,
            getDetails: MvcEndpoints.Helpdesk.Settings.PersonInCharge.GetDetails,
            create: MvcEndpoints.Helpdesk.Settings.PersonInCharge.Create,
            update: MvcEndpoints.Helpdesk.Settings.PersonInCharge.Update,
            delete: MvcEndpoints.Helpdesk.Settings.PersonInCharge.Delete,
            searchEmployees: MvcEndpoints.Helpdesk.Search.CompanyContacts
        }
    };

    // Client context for multi-tab session safety
    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; },
        get idCompany() { return window.PageContext?.idCompany || 0; }
    };

    function getClientParams() {
        const params = {};
        if (clientContext.idClient) params.idClient = clientContext.idClient;
        if (clientContext.idCompany) params.idCompany = clientContext.idCompany;
        return params;
    }

    function withClientParams(data) {
        return { ...getClientParams(), ...data };
    }

    // State management
    const state = {
        selectedEmployee: null,
        editingEmployeeId: null,
        deleteEmployeeId: null,
        deleteEmployeeName: '',
        isExistingPic: false
    };

    // Offcanvas instances
    let addOffcanvas, editOffcanvas;
    let employeeSearchTimeout = null;

    // Initialize on document ready
    $(document).ready(function () {
        initializeOffcanvas();
        attachEventListeners();
        initializeClientSessionMonitor();
    });

    // Initialize Bootstrap Offcanvas instances
    function initializeOffcanvas() {
        const addEl = document.getElementById('addPICOffcanvas');
        const editEl = document.getElementById('editPICOffcanvas');

        if (addEl) {
            addOffcanvas = new bootstrap.Offcanvas(addEl);
            addEl.addEventListener('hidden.bs.offcanvas', resetAddForm);
        }

        if (editEl) {
            editOffcanvas = new bootstrap.Offcanvas(editEl);
            editEl.addEventListener('hidden.bs.offcanvas', resetEditForm);
        }
    }

    // Initialize ClientSessionMonitor
    function initializeClientSessionMonitor() {
        if (typeof ClientSessionMonitor !== 'undefined') {
            var monitor = new ClientSessionMonitor({
                pageLoadClientId: clientContext.idClient,
                pageLoadCompanyId: clientContext.idCompany
            });
            monitor.start();
        }
    }

    // Attach all event listeners
    function attachEventListeners() {
        // Show Add Drawer
        $('#showAddDrawerBtn').on('click', function () {
            resetAddForm();
            addOffcanvas.show();
        });

        // Employee Search (Add mode)
        $('#employeeSearchInput').on('keyup', handleEmployeeSearch);
        $('#clearEmployeeBtn').on('click', clearEmployeeSelection);

        // Close search dropdown when clicking outside
        $(document).on('click', function (e) {
            if (!$(e.target).closest('#employeeSearchInput, #employeeSearchResults').length) {
                $('#employeeSearchResults').removeClass('show').empty();
            }
        });

        // Dual Listbox Controls - Add Mode
        $('#addPropertiesBtn').on('click', function () { transferProperties('availablePropertiesListbox', 'assignedPropertiesListbox'); });
        $('#addAllPropertiesBtn').on('click', function () { transferAllProperties('availablePropertiesListbox', 'assignedPropertiesListbox'); });
        $('#removePropertiesBtn').on('click', function () { transferProperties('assignedPropertiesListbox', 'availablePropertiesListbox'); });
        $('#removeAllPropertiesBtn').on('click', function () { transferAllProperties('assignedPropertiesListbox', 'availablePropertiesListbox'); });

        // Dual Listbox Controls - Edit Mode
        $('#editAddPropertiesBtn').on('click', function () { transferProperties('editAvailablePropertiesListbox', 'editAssignedPropertiesListbox'); });
        $('#editAddAllPropertiesBtn').on('click', function () { transferAllProperties('editAvailablePropertiesListbox', 'editAssignedPropertiesListbox'); });
        $('#editRemovePropertiesBtn').on('click', function () { transferProperties('editAssignedPropertiesListbox', 'editAvailablePropertiesListbox'); });
        $('#editRemoveAllPropertiesBtn').on('click', function () { transferAllProperties('editAssignedPropertiesListbox', 'editAvailablePropertiesListbox'); });

        // Listbox Search
        $('#availablePropertiesSearch').on('input', function () { filterListbox('availablePropertiesListbox', $(this).val()); });
        $('#assignedPropertiesSearch').on('input', function () { filterListbox('assignedPropertiesListbox', $(this).val()); });
        $('#editAvailablePropertiesSearch').on('input', function () { filterListbox('editAvailablePropertiesListbox', $(this).val()); });
        $('#editAssignedPropertiesSearch').on('input', function () { filterListbox('editAssignedPropertiesListbox', $(this).val()); });

        // Save and Update buttons
        $('#savePICBtn').on('click', savePIC);
        $('#updatePICBtn').on('click', updatePIC);

        // Delete - event delegation for server-rendered list
        $(document).on('click', '.btn-edit-pic', function () {
            var employeeId = parseInt($(this).data('employee-id'));
            editPIC(employeeId);
        });

        $(document).on('click', '.btn-delete-pic', function () {
            state.deleteEmployeeId = parseInt($(this).data('employee-id'));
            state.deleteEmployeeName = $(this).data('employee-name');
            $('#deleteItemName').text(state.deleteEmployeeName);
            var deleteModal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
            deleteModal.show();
        });

        $('#confirmDeleteBtn').on('click', confirmDelete);

        // Search button and Enter key
        $('#searchBtn').on('click', performSearch);
        $('#searchInput').on('keyup', function (e) {
            if (e.key === 'Enter') {
                performSearch();
            }
        });

        // Clear search
        $('#clearSearchBtn').on('click', function () {
            window.location.href = buildPageUrl(1, '');
        });
    }

    // -- Search / Navigation --

    function performSearch() {
        var keyword = $('#searchInput').val().trim();
        window.location.href = buildPageUrl(1, keyword);
    }

    function buildPageUrl(page, keyword) {
        var search = (keyword !== undefined) ? keyword : (window.searchKeyword || '');
        var url = '/Helpdesk/PersonInCharge?page=' + page;
        if (search) {
            url += '&search=' + encodeURIComponent(search);
        }
        return url;
    }

    // -- Employee Search (Add mode) --

    function handleEmployeeSearch() {
        var term = $(this).val().trim();
        clearTimeout(employeeSearchTimeout);

        var $results = $('#employeeSearchResults');

        if (term.length < CONFIG.minSearchLength) {
            $results.removeClass('show').empty();
            return;
        }

        // Show loading spinner
        $results.empty().html(
            '<div class="employee-search-item text-center">' +
            '<span class="spinner-border spinner-border-sm me-2" role="status"></span>' +
            'Searching...' +
            '</div>'
        ).addClass('show');

        employeeSearchTimeout = setTimeout(function () {
            $.ajax({
                url: CONFIG.apiEndpoints.searchEmployees,
                method: 'GET',
                data: withClientParams({ term: term }),
                success: function (response) {
                    $results.empty();

                    if (response.success && response.data && response.data.length > 0) {
                        $.each(response.data, function (index, employee) {
                            var name = employee.employeeName || employee.EmployeeName || '';
                            var title = employee.title || employee.Title || '';
                            var department = employee.department || employee.Department || '';

                            var $item = $('<div></div>')
                                .addClass('employee-search-item')
                                .html(
                                    '<strong>' + escapeHtml(name) + '</strong>' +
                                    '<small class="text-muted">' +
                                    (title ? escapeHtml(title) + '<br>' : '') +
                                    escapeHtml(department) +
                                    '</small>'
                                )
                                .on('click', function () {
                                    onEmployeeSelected({
                                        id: employee.idEmployee || employee.IdEmployee,
                                        fullName: name,
                                        title: title,
                                        departmentName: department,
                                        emailAddress: employee.email || employee.Email || '',
                                        phoneNumber: employee.phone || employee.Phone || ''
                                    });
                                });
                            $results.append($item);
                        });
                        $results.addClass('show');
                    } else {
                        $results.append(
                            $('<div></div>')
                                .addClass('employee-search-item text-muted')
                                .text('No employees found')
                        );
                        $results.addClass('show');
                    }
                },
                error: function () {
                    showNotification('Error searching employees', 'error');
                    $('#employeeSearchResults').removeClass('show').empty();
                }
            });
        }, CONFIG.debounceDelay);
    }

    function onEmployeeSelected(employee) {
        // Close search dropdown
        $('#employeeSearchResults').removeClass('show').empty();

        // Show employee card
        showEmployeeCard(employee);

        // Load PIC details to get property assignments
        loadPicDetailsForEmployee(employee.id);
    }

    function showEmployeeCard(employee) {
        state.selectedEmployee = employee;
        $('#selectedEmployeeId').val(employee.id);

        var initials = generateInitials(employee.fullName);
        var avatarColor = generateAvatarColor(employee.fullName);

        $('#employeeInitials').text(initials);
        $('#employeeAvatar').css('background-color', avatarColor);
        $('#employeeCardName').text(employee.fullName);

        var detailsHtml = '';
        if (employee.title) {
            detailsHtml += '<div><i class="ti ti-briefcase me-1"></i>' + escapeHtml(employee.title) + '</div>';
        }
        if (employee.departmentName) {
            detailsHtml += '<div><i class="ti ti-building me-1"></i>' + escapeHtml(employee.departmentName) + '</div>';
        }
        if (employee.emailAddress) {
            detailsHtml += '<div><i class="ti ti-mail me-1"></i>' + escapeHtml(employee.emailAddress) + '</div>';
        }
        if (employee.phoneNumber) {
            detailsHtml += '<div><i class="ti ti-phone me-1"></i>' + escapeHtml(employee.phoneNumber) + '</div>';
        }

        $('#employeeCardDetails').html(detailsHtml || '<div class="text-muted fst-italic">No details available</div>');

        // Toggle visibility
        $('#employeeSearchContainer').hide();
        $('#employeeCard').fadeIn(300);
    }

    function clearEmployeeSelection() {
        state.selectedEmployee = null;
        state.isExistingPic = false;
        $('#selectedEmployeeId').val('');
        $('#employeeSearchInput').val('');
        $('#employeeCardName').text('');
        $('#employeeCardDetails').empty();

        // Hide card, show search
        $('#employeeCard').hide();
        $('#employeeSearchContainer').fadeIn(300);

        // Hide property section
        $('#addPropertySection').addClass('property-section-hidden');

        // Clear listboxes
        populateListbox('availablePropertiesListbox', []);
        populateListbox('assignedPropertiesListbox', []);

        setTimeout(function () {
            $('#employeeSearchInput').focus();
        }, 350);
    }

    // -- Load PIC Details --

    function loadPicDetailsForEmployee(employeeId) {
        $.ajax({
            url: CONFIG.apiEndpoints.getDetails,
            method: 'GET',
            data: withClientParams({ employeeId: employeeId }),
            success: function (response) {
                if (response.success && response.data) {
                    var details = response.data;
                    var hasAssigned = details.assignedProperties && details.assignedProperties.length > 0;

                    if (hasAssigned) {
                        // Employee already has PIC assignments - block creation
                        state.isExistingPic = true;
                        showNotification('This employee is already assigned as a Person in Charge. Use the Edit button from the list instead.', 'warning');
                    } else {
                        state.isExistingPic = false;
                    }

                    // Populate listboxes from API response
                    populateListbox('availablePropertiesListbox', details.availableProperties || []);
                    populateListbox('assignedPropertiesListbox', details.assignedProperties || []);

                    // Show property section
                    $('#addPropertySection').removeClass('property-section-hidden');
                } else {
                    showNotification('Failed to load property details', 'error');
                }
            },
            error: function () {
                showNotification('Error loading property details', 'error');
            }
        });
    }

    // -- Dual Listbox --

    function populateListbox(listboxId, items) {
        var $listbox = $('#' + listboxId);
        $listbox.empty();

        if (!items || items.length === 0) {
            $listbox.html('<div class="text-muted text-center p-3">No items</div>');
            return;
        }

        items.forEach(function (item) {
            var id = item.idProperty || item.IdProperty;
            var name = item.propertyName || item.PropertyName || '';
            var $div = $('<div>')
                .addClass('listbox-item')
                .attr('data-id', id)
                .text(name)
                .on('click', function () {
                    $(this).toggleClass('selected');
                });
            $listbox.append($div);
        });
    }

    function filterListbox(listboxId, searchTerm) {
        var $listbox = $('#' + listboxId);
        var $items = $listbox.find('.listbox-item');
        var lowerTerm = (searchTerm || '').toLowerCase();

        $items.each(function () {
            var text = $(this).text().toLowerCase();
            $(this).toggle(text.includes(lowerTerm));
        });
    }

    function transferProperties(fromListboxId, toListboxId) {
        var $from = $('#' + fromListboxId);
        var $to = $('#' + toListboxId);
        var $selected = $from.find('.listbox-item.selected');

        if ($selected.length === 0) {
            showNotification('Please select items to transfer', 'warning');
            return;
        }

        $selected.each(function () {
            var $item = $(this).clone().removeClass('selected');
            $item.on('click', function () {
                $(this).toggleClass('selected');
            });
            $to.append($item);
            $(this).remove();
        });

        // Remove "No items" message if it exists in target
        $to.find('.text-muted').remove();

        // Add "No items" message if source is now empty
        if ($from.find('.listbox-item').length === 0) {
            $from.html('<div class="text-muted text-center p-3">No items</div>');
        }
    }

    function transferAllProperties(fromListboxId, toListboxId) {
        var $from = $('#' + fromListboxId);
        var $to = $('#' + toListboxId);
        var $allItems = $from.find('.listbox-item');

        if ($allItems.length === 0) {
            showNotification('No items to transfer', 'warning');
            return;
        }

        $allItems.each(function () {
            var $item = $(this).clone().removeClass('selected');
            $item.on('click', function () {
                $(this).toggleClass('selected');
            });
            $to.append($item);
        });

        $from.html('<div class="text-muted text-center p-3">No items</div>');
        $to.find('.text-muted').remove();
    }

    function getListboxItemIds(listboxId) {
        var ids = [];
        $('#' + listboxId).find('.listbox-item').each(function () {
            ids.push(parseInt($(this).attr('data-id')));
        });
        return ids;
    }

    // -- CRUD Operations --

    function savePIC() {
        if (!state.selectedEmployee) {
            showNotification('Please select an employee', 'warning');
            return;
        }

        if (state.isExistingPic) {
            showNotification('This employee is already a Person in Charge. Please use the Edit button from the list.', 'warning');
            return;
        }

        var assignedIds = getListboxItemIds('assignedPropertiesListbox');
        if (assignedIds.length === 0) {
            showNotification('Please assign at least one property', 'warning');
            return;
        }

        var unassignedIds = getListboxItemIds('availablePropertiesListbox');

        var payload = {
            idEmployee: state.selectedEmployee.id,
            idClient: clientContext.idClient,
            assignedProperties: assignedIds,
            unassignedProperties: unassignedIds
        };

        $.ajax({
            url: CONFIG.apiEndpoints.create,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response.success) {
                    showNotification('Person in charge added', 'success');
                    addOffcanvas.hide();
                    window.location.href = buildPageUrl(1);
                } else {
                    showNotification(response.message || 'Failed to add person in charge', 'error');
                }
            },
            error: function () {
                showNotification('An error occurred while adding person in charge', 'error');
            }
        });
    }

    function editPIC(employeeId) {
        state.editingEmployeeId = employeeId;

        $.ajax({
            url: CONFIG.apiEndpoints.getDetails,
            type: 'GET',
            data: withClientParams({ employeeId: employeeId }),
            success: function (response) {
                if (response.success && response.data) {
                    var details = response.data;

                    // Set employee name (locked field)
                    $('#editEmployeeName').val(details.fullName || details.FullName || '');

                    // Populate listboxes from API response
                    populateListbox('editAvailablePropertiesListbox', details.availableProperties || []);
                    populateListbox('editAssignedPropertiesListbox', details.assignedProperties || []);

                    // Clear search inputs
                    $('#editAvailablePropertiesSearch').val('');
                    $('#editAssignedPropertiesSearch').val('');

                    editOffcanvas.show();
                } else {
                    showNotification('Failed to load PIC details', 'error');
                }
            },
            error: function () {
                showNotification('An error occurred while loading PIC details', 'error');
            }
        });
    }

    function updatePIC() {
        if (!state.editingEmployeeId) {
            showNotification('Invalid operation', 'error');
            return;
        }

        var assignedIds = getListboxItemIds('editAssignedPropertiesListbox');
        if (assignedIds.length === 0) {
            showNotification('Please assign at least one property', 'warning');
            return;
        }

        var unassignedIds = getListboxItemIds('editAvailablePropertiesListbox');

        var payload = {
            idEmployee: state.editingEmployeeId,
            idClient: clientContext.idClient,
            assignedProperties: assignedIds,
            unassignedProperties: unassignedIds
        };

        $.ajax({
            url: CONFIG.apiEndpoints.update,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response.success) {
                    showNotification('Person in charge updated', 'success');
                    editOffcanvas.hide();
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to update person in charge', 'error');
                }
            },
            error: function () {
                showNotification('An error occurred while updating person in charge', 'error');
            }
        });
    }

    function confirmDelete() {
        var employeeId = state.deleteEmployeeId;
        if (!employeeId) return;

        $.ajax({
            url: CONFIG.apiEndpoints.delete,
            type: 'DELETE',
            data: withClientParams({ employeeId: employeeId }),
            success: function (response) {
                if (response.success) {
                    showNotification('Person in charge deleted', 'success');
                    var deleteModal = bootstrap.Modal.getInstance(document.getElementById('deleteConfirmModal'));
                    if (deleteModal) deleteModal.hide();
                    window.location.reload();
                } else {
                    showNotification(response.message || 'Failed to delete person in charge', 'error');
                }
            },
            error: function () {
                showNotification('An error occurred while deleting person in charge', 'error');
            }
        });
    }

    // -- Form Reset --

    function resetAddForm() {
        state.selectedEmployee = null;
        state.isExistingPic = false;
        $('#selectedEmployeeId').val('');
        $('#employeeSearchInput').val('');
        $('#employeeSearchResults').removeClass('show').empty();
        $('#employeeCardName').text('');
        $('#employeeCardDetails').empty();
        $('#employeeCard').hide();
        $('#employeeSearchContainer').show();
        $('#addPropertySection').addClass('property-section-hidden');
        $('#availablePropertiesSearch').val('');
        $('#assignedPropertiesSearch').val('');
        populateListbox('availablePropertiesListbox', []);
        populateListbox('assignedPropertiesListbox', []);
    }

    function resetEditForm() {
        state.editingEmployeeId = null;
        $('#editEmployeeName').val('');
        $('#editAvailablePropertiesSearch').val('');
        $('#editAssignedPropertiesSearch').val('');
    }

    // -- Utility Functions --

    function generateInitials(fullName) {
        if (!fullName) return '??';
        var nameParts = fullName.trim().split(' ').filter(function (part) { return part.length > 0; });
        if (nameParts.length === 0) return '??';
        if (nameParts.length === 1) return nameParts[0].substring(0, 2).toUpperCase();
        var firstInitial = nameParts[0].charAt(0);
        var lastInitial = nameParts[nameParts.length - 1].charAt(0);
        return (firstInitial + lastInitial).toUpperCase();
    }

    function generateAvatarColor(fullName) {
        var colors = [
            '#3498db', '#e74c3c', '#2ecc71', '#f39c12', '#9b59b6',
            '#1abc9c', '#e67e22', '#34495e', '#16a085', '#c0392b'
        ];
        if (!fullName) return colors[0];
        var hash = 0;
        for (var i = 0; i < fullName.length; i++) {
            hash = fullName.charCodeAt(i) + ((hash << 5) - hash);
        }
        var index = Math.abs(hash) % colors.length;
        return colors[index];
    }

    function escapeHtml(text) {
        if (!text) return '';
        var map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return String(text).replace(/[&<>"']/g, function (m) { return map[m]; });
    }

})(jQuery);
