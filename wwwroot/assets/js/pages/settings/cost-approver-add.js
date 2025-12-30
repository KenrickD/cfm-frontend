// Cost Approver Add Page JavaScript
(function () {
    'use strict';

    const CONFIG = {
        apiEndpoints: {
            properties: MvcEndpoints.Helpdesk.Settings.GetProperties,
            workCategories: MvcEndpoints.Helpdesk.WorkRequest.GetWorkCategoriesByClient,
            currencies: MvcEndpoints.Helpdesk.Extended.GetCurrencies,
            searchEmployees: MvcEndpoints.Helpdesk.Search.Employees,
            createCostApproverGroup: MvcEndpoints.Helpdesk.Settings.CostApproverGroup.Create
        }
    };

    let approvalFlowModal;
    let approverModal;
    let approvalFlows = [];
    let currentFlowId = null;
    let currentEditingFlow = null;
    let flowIdCounter = 0;
    let allEmployees = [];
    let filteredEmployees = [];
    let idClient = null;
    let idCompany = null;

    document.addEventListener('DOMContentLoaded', function () {
        initializeComponents();
        loadInitialData();
    });

    function initializeComponents() {
        const approvalFlowModalElement = document.getElementById('approvalFlowModal');
        const approverModalElement = document.getElementById('approverModal');

        if (approvalFlowModalElement) {
            approvalFlowModal = new bootstrap.Modal(approvalFlowModalElement);
        }

        if (approverModalElement) {
            approverModal = new bootstrap.Modal(approverModalElement);
        }

        document.getElementById('addApprovalFlowBtn').addEventListener('click', showApprovalFlowModal);
        document.getElementById('saveApprovalFlowBtn').addEventListener('click', saveApprovalFlow);
        document.getElementById('saveApproversBtn').addEventListener('click', saveApprovers);
        document.getElementById('addApproverToListBtn').addEventListener('click', addApproverToList);
        document.getElementById('approverSearch').addEventListener('input', filterApprovers);
        document.getElementById('approverSelect').addEventListener('change', handleApproverSelect);
        document.getElementById('selectAllProperties').addEventListener('change', toggleAllProperties);
        document.getElementById('selectAllCategories').addEventListener('change', toggleAllCategories);
        document.getElementById('costApproverForm').addEventListener('submit', handleFormSubmit);
    }

    async function loadInitialData() {
        idClient = await getIdClient();
        idCompany = await getIdCompany();

        await Promise.all([
            loadProperties(),
            loadWorkCategories(),
            loadCurrencies(),
            loadEmployees()
        ]);
    }

    async function getIdClient() {
        return 1;
    }

    async function getIdCompany() {
        return 1;
    }

    async function loadProperties() {
        try {
            const response = await fetch(CONFIG.apiEndpoints.properties);
            const result = await response.json();

            if (result.success) {
                renderProperties(result.data || []);
            } else {
                showError('Failed to load properties: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error loading properties:', error);
            showError('An error occurred while loading properties');
        }
    }

    function renderProperties(properties) {
        const container = document.getElementById('propertiesGrid');
        if (properties.length === 0) {
            container.innerHTML = '<p class="text-muted small">No properties available</p>';
            return;
        }

        const html = properties.map(prop => `
            <div class="checkbox-item">
                <input class="form-check-input property-checkbox" type="checkbox" value="${prop.id}" id="prop_${prop.id}">
                <label class="form-check-label" for="prop_${prop.id}">${escapeHtml(prop.name)}</label>
            </div>
        `).join('');

        container.innerHTML = html;
    }

    async function loadWorkCategories() {
        try {
            const url = `${CONFIG.apiEndpoints.workCategories}?categoryType=workCategory`;
            const response = await fetch(url);
            const result = await response.json();

            if (result.success) {
                renderWorkCategories(result.data || []);
            } else {
                showError('Failed to load work categories: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error loading work categories:', error);
            showError('An error occurred while loading work categories');
        }
    }

    function renderWorkCategories(categories) {
        const container = document.getElementById('categoriesGrid');
        if (categories.length === 0) {
            container.innerHTML = '<p class="text-muted small">No work categories available</p>';
            return;
        }

        const html = categories.map(cat => `
            <div class="checkbox-item">
                <input class="form-check-input category-checkbox" type="checkbox" value="${cat.id}" id="cat_${cat.id}">
                <label class="form-check-label" for="cat_${cat.id}">${escapeHtml(cat.description)}</label>
            </div>
        `).join('');

        container.innerHTML = html;
    }

    async function loadCurrencies() {
        try {
            const response = await fetch(CONFIG.apiEndpoints.currencies);
            const result = await response.json();

            if (result.success && result.data) {
                renderCurrencies(result.data);
            }
        } catch (error) {
            console.error('Error loading currencies:', error);
        }
    }

    function renderCurrencies(currencies) {
        const select = document.getElementById('currency');
        const html = currencies.map(curr =>
            `<option value="${curr.code || curr.id}">${curr.code || curr.description}</option>`
        ).join('');
        select.innerHTML = html;
    }

    async function loadEmployees() {
        try {
            const url = `${CONFIG.apiEndpoints.searchEmployees}?term=&idClient=${idClient}`;
            const response = await fetch(url);
            const result = await response.json();

            if (result.success) {
                allEmployees = result.data || [];
                filteredEmployees = [...allEmployees];
            }
        } catch (error) {
            console.error('Error loading employees:', error);
        }
    }

    function toggleAllProperties(e) {
        const checkboxes = document.querySelectorAll('.property-checkbox');
        checkboxes.forEach(cb => cb.checked = e.target.checked);
    }

    function toggleAllCategories(e) {
        const checkboxes = document.querySelectorAll('.category-checkbox');
        checkboxes.forEach(cb => cb.checked = e.target.checked);
    }

    function showApprovalFlowModal() {
        currentEditingFlow = null;
        document.getElementById('approvalFlowName').value = '';
        if (approvalFlowModal) {
            approvalFlowModal.show();
        }
    }

    function saveApprovalFlow() {
        const flowName = document.getElementById('approvalFlowName').value.trim();

        if (!flowName) {
            showError('Please enter approval flow name');
            return;
        }

        if (currentEditingFlow) {
            currentEditingFlow.name = flowName;
            renderApprovalFlows();
        } else {
            const flow = {
                id: ++flowIdCounter,
                name: flowName,
                approvers: []
            };

            approvalFlows.push(flow);
            renderApprovalFlows();
        }

        if (approvalFlowModal) {
            approvalFlowModal.hide();
        }
    }

    function renderApprovalFlows() {
        const container = document.getElementById('approvalFlowsContainer');

        if (approvalFlows.length === 0) {
            container.innerHTML = '<p class="text-muted">No approval flows added yet. Click "Add New Approval Flow" to get started.</p>';
            return;
        }

        const html = approvalFlows.map(flow => {
            const priorityApprovers = flow.approvers.filter(a => a.isPriority);
            const approverText = priorityApprovers.length > 0
                ? priorityApprovers.map(a => `<span class="clickable-approver" onclick="editFlowApprovers(${flow.id})">${escapeHtml(a.name)}</span>`).join(', ')
                : '<span class="text-muted">No Approver</span>';

            return `
                <div class="approval-flow-card">
                    <div class="approval-flow-header">
                        <div class="approval-flow-name">${escapeHtml(flow.name)}</div>
                        <div>
                            <button type="button" class="btn btn-sm btn-info me-1" onclick="editApprovalFlow(${flow.id})" title="Edit Flow Name">
                                <i class="ti ti-edit"></i>
                                Edit
                            </button>
                            <button type="button" class="btn btn-sm btn-danger" onclick="deleteApprovalFlow(${flow.id})" title="Delete Flow">
                                <i class="ti ti-trash"></i>
                                Delete
                            </button>
                        </div>
                    </div>
                    <div class="approver-section">
                        <div class="mb-2">
                            <strong>Approver <span class="required">*</span></strong>
                        </div>
                        <button type="button" class="btn btn-sm btn-primary mb-2" onclick="addNewApprover(${flow.id})">
                            <i class="ti ti-plus me-1"></i>
                            Add New Approver
                        </button>
                        <div>
                            ${flow.approvers.length === 0
                                ? '<p class="text-muted small mb-0">→ Add a new approver first</p>'
                                : `
                                    <table class="table table-sm approver-table">
                                        <thead>
                                            <tr>
                                                <th>Name</th>
                                                <th width="150">Authority Level</th>
                                                <th width="50">Action</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            ${flow.approvers.map((approver, index) => `
                                                <tr class="approver-row">
                                                    <td>
                                                        <span class="approver-name">${escapeHtml(approver.name)}</span>
                                                        ${approver.isPriority ? '<span class="priority-badge">Priority</span>' : ''}
                                                    </td>
                                                    <td>
                                                        <span class="authority-level">${approver.authorityLevel || 1}</span>
                                                    </td>
                                                    <td>
                                                        <button type="button" class="btn btn-danger btn-icon" onclick="removeApprover(${flow.id}, ${index})">
                                                            <i class="ti ti-trash"></i>
                                                        </button>
                                                    </td>
                                                </tr>
                                            `).join('')}
                                        </tbody>
                                    </table>
                                `
                            }
                        </div>
                    </div>
                </div>
            `;
        }).join('');

        container.innerHTML = html;
    }

    window.editApprovalFlow = function(flowId) {
        const flow = approvalFlows.find(f => f.id === flowId);
        if (!flow) return;

        currentEditingFlow = flow;
        document.getElementById('approvalFlowName').value = flow.name;
        if (approvalFlowModal) {
            approvalFlowModal.show();
        }
    };

    window.deleteApprovalFlow = function(flowId) {
        if (confirm('Are you sure you want to delete this approval flow?')) {
            approvalFlows = approvalFlows.filter(f => f.id !== flowId);
            renderApprovalFlows();
        }
    };

    window.addNewApprover = function(flowId) {
        currentFlowId = flowId;
        const flow = approvalFlows.find(f => f.id === flowId);

        document.getElementById('approverSearch').value = '';
        renderApproverDropdown();
        renderApproverTable(flow ? flow.approvers : []);

        if (approverModal) {
            approverModal.show();
        }
    };

    window.editFlowApprovers = function(flowId) {
        window.addNewApprover(flowId);
    };

    function renderApproverDropdown() {
        const select = document.getElementById('approverSelect');
        const html = '<option value="">Select an approver...</option>' +
            filteredEmployees.map(emp =>
                `<option value="${emp.id}" data-name="${escapeHtml(emp.name || emp.fullName)}">${escapeHtml(emp.name || emp.fullName)}</option>`
            ).join('');
        select.innerHTML = html;
    }

    function filterApprovers(e) {
        const searchTerm = e.target.value.toLowerCase();

        if (!searchTerm) {
            filteredEmployees = [...allEmployees];
        } else {
            filteredEmployees = allEmployees.filter(emp =>
                (emp.name || emp.fullName || '').toLowerCase().includes(searchTerm)
            );
        }

        renderApproverDropdown();
    }

    function handleApproverSelect(e) {
        if (e.target.value) {
            addApproverToList();
        }
    }

    function addApproverToList() {
        const select = document.getElementById('approverSelect');
        const selectedId = select.value;

        if (!selectedId) {
            showError('Please select an approver');
            return;
        }

        const flow = approvalFlows.find(f => f.id === currentFlowId);
        if (!flow) return;

        const selectedOption = select.options[select.selectedIndex];
        const approverName = selectedOption.getAttribute('data-name');

        if (flow.approvers.some(a => a.id == selectedId)) {
            showError('This approver is already added');
            return;
        }

        const approver = {
            id: selectedId,
            name: approverName,
            isPriority: false,
            authorityLevel: flow.approvers.length + 1
        };

        flow.approvers.push(approver);
        renderApproverTable(flow.approvers);
        select.value = '';
    }

    function renderApproverTable(approvers) {
        const tbody = document.getElementById('approverTableBody');

        if (approvers.length === 0) {
            tbody.innerHTML = `
                <tr class="empty-approvers">
                    <td colspan="3">No Approver</td>
                </tr>
            `;
            return;
        }

        const html = approvers.map((approver, index) => `
            <tr class="approver-row">
                <td>
                    <span class="approver-name">${escapeHtml(approver.name)}</span>
                </td>
                <td>
                    <div class="form-check">
                        <input class="form-check-input" type="checkbox" ${approver.isPriority ? 'checked' : ''}
                               onchange="togglePriorityApprover(${index})">
                    </div>
                </td>
                <td>
                    <button type="button" class="btn btn-danger btn-icon" onclick="removeApproverFromModal(${index})">
                        <i class="ti ti-trash"></i>
                    </button>
                </td>
            </tr>
        `).join('');

        tbody.innerHTML = html;
    }

    window.togglePriorityApprover = function(index) {
        const flow = approvalFlows.find(f => f.id === currentFlowId);
        if (!flow) return;

        flow.approvers[index].isPriority = !flow.approvers[index].isPriority;
        renderApproverTable(flow.approvers);
    };

    window.removeApproverFromModal = function(index) {
        const flow = approvalFlows.find(f => f.id === currentFlowId);
        if (!flow) return;

        flow.approvers.splice(index, 1);
        flow.approvers.forEach((a, i) => a.authorityLevel = i + 1);
        renderApproverTable(flow.approvers);
    };

    window.removeApprover = function(flowId, index) {
        const flow = approvalFlows.find(f => f.id === flowId);
        if (!flow) return;

        if (confirm('Are you sure you want to remove this approver?')) {
            flow.approvers.splice(index, 1);
            flow.approvers.forEach((a, i) => a.authorityLevel = i + 1);
            renderApprovalFlows();
        }
    };

    function saveApprovers() {
        const flow = approvalFlows.find(f => f.id === currentFlowId);
        if (!flow) return;

        const hasPriorityApprover = flow.approvers.some(a => a.isPriority);

        if (flow.approvers.length > 0 && !hasPriorityApprover) {
            showError('Please select at least one priority approver');
            return;
        }

        renderApprovalFlows();

        if (approverModal) {
            approverModal.hide();
        }

        showSuccess('Approvers saved successfully');
    }

    async function handleFormSubmit(e) {
        e.preventDefault();

        const groupName = document.getElementById('groupName').value.trim();
        const description = document.getElementById('description').value.trim();
        const currency = document.getElementById('currency').value;
        const minValue = parseFloat(document.getElementById('minValue').value) || 0;
        const maxValue = parseFloat(document.getElementById('maxValue').value) || 0;

        const selectedProperties = Array.from(document.querySelectorAll('.property-checkbox:checked'))
            .map(cb => parseInt(cb.value));

        const selectedCategories = Array.from(document.querySelectorAll('.category-checkbox:checked'))
            .map(cb => parseInt(cb.value));

        if (!groupName) {
            showError('Please enter cost approver group name');
            return;
        }

        if (selectedProperties.length === 0) {
            showError('Please select at least one property');
            return;
        }

        if (selectedCategories.length === 0) {
            showError('Please select at least one work category');
            return;
        }

        if (approvalFlows.length === 0) {
            showError('Please add at least one approval flow');
            return;
        }

        for (const flow of approvalFlows) {
            if (flow.approvers.length === 0) {
                showError(`Approval flow "${flow.name}" must have at least one approver`);
                return;
            }

            const hasPriority = flow.approvers.some(a => a.isPriority);
            if (!hasPriority) {
                showError(`Approval flow "${flow.name}" must have at least one priority approver`);
                return;
            }
        }

        const payload = {
            name: groupName,
            description: description,
            currency: currency,
            minValue: minValue,
            maxValue: maxValue,
            propertyIds: selectedProperties,
            workCategoryIds: selectedCategories,
            approvalFlows: approvalFlows.map(flow => ({
                name: flow.name,
                approvers: flow.approvers.map(a => ({
                    employeeId: a.id,
                    isPriority: a.isPriority,
                    authorityLevel: a.authorityLevel
                }))
            }))
        };

        try {
            const response = await fetch(CONFIG.apiEndpoints.createCostApproverGroup, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            if (result.success) {
                showSuccess('Cost approver group created successfully');
                setTimeout(() => {
                    window.location.href = '/Helpdesk/CostApprover';
                }, 1500);
            } else {
                showError('Failed to create cost approver group: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error creating cost approver group:', error);
            showError('An error occurred while creating cost approver group');
        }
    }

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function showSuccess(message) {
        showNotification(message, 'success', 'Success');
    }

    function showError(message) {
        showNotification(message, 'error', 'Error');
    }
})();
