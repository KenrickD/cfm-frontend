// Cost Approver Add/Edit Page JavaScript
(function () {
    'use strict';

    const CONFIG = {
        apiEndpoints: {
            locations: MvcEndpoints.Helpdesk.Location.GetByClient,
            workCategories: MvcEndpoints.Helpdesk.Settings.CostApproverGroup.GetWorkCategoriesForSettings,
            currencies: MvcEndpoints.Helpdesk.Extended.GetCurrencies,
            companyUsers: MvcEndpoints.Helpdesk.Settings.CostApproverGroup.GetCompanyUsers,
            getCostApproverGroupById: MvcEndpoints.Helpdesk.Settings.CostApproverGroup.GetById,
            createCostApproverGroup: MvcEndpoints.Helpdesk.Settings.CostApproverGroup.Create,
            updateCostApproverGroup: MvcEndpoints.Helpdesk.Settings.CostApproverGroup.Update
        }
    };

    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; },
        get idCostApproverGroup() { return window.PageContext?.idCostApproverGroup || 0; }
    };

    let subGroupModal;
    let approverModal;
    let subGroups = [];
    let currentSubGroupIndex = null;
    let currentEditingLevel = null;
    let subGroupIdCounter = 0;
    let allEmployees = [];
    let filteredEmployees = [];
    let isEditMode = false;
    let searchTimeout = null;
    let selectedApproverFromSearch = null;
    let pendingCurrencyValue = null;

    document.addEventListener('DOMContentLoaded', function () {
        isEditMode = clientContext.idCostApproverGroup > 0;
        initializeComponents();
        loadInitialData();
    });

    function initializeComponents() {
        const subGroupModalElement = document.getElementById('approvalFlowModal');
        const approverModalElement = document.getElementById('approverModal');

        if (subGroupModalElement) {
            subGroupModal = new bootstrap.Modal(subGroupModalElement);
        }

        if (approverModalElement) {
            approverModal = new bootstrap.Modal(approverModalElement);
        }

        document.getElementById('addApprovalFlowBtn').addEventListener('click', showSubGroupModal);
        document.getElementById('saveApprovalFlowBtn').addEventListener('click', saveSubGroup);
        document.getElementById('saveApproversBtn').addEventListener('click', saveApprovers);
        document.getElementById('approverSearch').addEventListener('input', handleApproverSearch);
        document.getElementById('approverSearch').addEventListener('focus', handleSearchFocus);
        document.getElementById('selectAllProperties').addEventListener('change', toggleAllProperties);
        document.getElementById('selectAllCategories').addEventListener('change', toggleAllCategories);
        document.getElementById('costApproverForm').addEventListener('submit', handleFormSubmit);

        document.addEventListener('click', function(e) {
            const searchContainer = document.querySelector('.approver-search-container');
            if (searchContainer && !searchContainer.contains(e.target)) {
                hideSearchResults();
            }
        });
    }

    async function loadInitialData() {
        await Promise.all([
            loadLocations(),
            loadWorkCategories(),
            loadCurrencies(),
            loadEmployees()
        ]);

        if (isEditMode) {
            await loadCostApproverGroupDetails();
        }
    }

    async function loadCostApproverGroupDetails() {
        try {
            const response = await fetch(`${CONFIG.apiEndpoints.getCostApproverGroupById}?id=${clientContext.idCostApproverGroup}`);
            const result = await response.json();

            if (result.success && result.data) {
                populateFormWithData(result.data);
            } else {
                showError('Failed to load cost approver group: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error loading cost approver group:', error);
            showError('An error occurred while loading cost approver group');
        }
    }

    function populateFormWithData(data) {
        console.log('populateFormWithData called with data:', data);
        console.log('rangeValue:', data.rangeValue);

        document.getElementById('groupName').value = data.name || '';
        document.getElementById('description').value = data.description || '';

        pendingCurrencyValue = data.rangeValue?.currency_IdEnum || data.rangeValue?.Currency_IdEnum || '';
        console.log('Pending currency value set to:', pendingCurrencyValue);

        setCurrencyValue();

        document.getElementById('minValue').value = data.rangeValue?.amountStart || data.rangeValue?.AmountStart || 0;
        document.getElementById('maxValue').value = data.rangeValue?.amountEnd || data.rangeValue?.AmountEnd || 0;

        if (data.properties && data.properties.length > 0) {
            data.properties.forEach(prop => {
                const checkbox = document.getElementById(`prop_${prop.idProperty}`);
                if (checkbox) checkbox.checked = true;
            });
        }

        if (data.workCategories && data.workCategories.length > 0) {
            data.workCategories.forEach(cat => {
                const checkbox = document.getElementById(`cat_${cat.workCategory_IdType}`);
                if (checkbox) checkbox.checked = true;
            });
        }

        if (data.subGroups && data.subGroups.length > 0) {
            subGroups = data.subGroups.map((subGroup, index) => {
                const approvers = [];
                if (subGroup.levels && subGroup.levels.length > 0) {
                    subGroup.levels.forEach(level => {
                        if (level.approvers && level.approvers.length > 0) {
                            level.approvers.forEach(approver => {
                                approvers.push({
                                    id: approver.idEmployee,
                                    name: approver.fullName,
                                    isPriority: approver.isPriorityApprover,
                                    level: level.level
                                });
                            });
                        }
                    });
                }

                return {
                    id: ++subGroupIdCounter,
                    idCostApproverSubGroup: subGroup.idCostApproverSubGroup || 0,
                    name: subGroup.name,
                    approvers: approvers
                };
            });

            renderSubGroups();
        }
    }

    async function loadLocations() {
        const spinner = document.getElementById('propertiesSpinner');
        const grid = document.getElementById('propertiesGrid');

        try {
            const response = await fetch(`${CONFIG.apiEndpoints.locations}?idClient=${clientContext.idClient}`);
            const result = await response.json();

            if (result.success && result.data) {
                renderLocations(result.data);
            } else {
                showError('Failed to load locations: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error loading locations:', error);
            showError('An error occurred while loading locations');
        } finally {
            spinner.style.display = 'none';
            grid.style.display = 'block';
        }
    }

    function renderLocations(locations) {
        const container = document.getElementById('propertiesGrid');
        if (!locations || locations.length === 0) {
            container.innerHTML = '<p class="text-muted small">No properties available</p>';
            return;
        }

        const html = locations.map(loc => `
            <div class="checkbox-item">
                <input class="form-check-input property-checkbox" type="checkbox" value="${loc.idProperty}" id="prop_${loc.idProperty}">
                <label class="form-check-label" for="prop_${loc.idProperty}">${escapeHtml(loc.propertyName)}</label>
            </div>
        `).join('');

        container.innerHTML = html;
    }

    async function loadWorkCategories() {
        const spinner = document.getElementById('categoriesSpinner');
        const grid = document.getElementById('categoriesGrid');

        try {
            const response = await fetch(`${CONFIG.apiEndpoints.workCategories}?idClient=${clientContext.idClient}`);
            const result = await response.json();

            if (result.success && result.data) {
                renderWorkCategories(result.data);
            } else {
                showError('Failed to load work categories: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error loading work categories:', error);
            showError('An error occurred while loading work categories');
        } finally {
            spinner.style.display = 'none';
            grid.style.display = 'block';
        }
    }

    function renderWorkCategories(categories) {
        const container = document.getElementById('categoriesGrid');
        if (!categories || categories.length === 0) {
            container.innerHTML = '<p class="text-muted small">No work categories available</p>';
            return;
        }

        const html = categories.map(cat => `
            <div class="checkbox-item">
                <input class="form-check-input category-checkbox" type="checkbox" value="${cat.idType}" id="cat_${cat.idType}">
                <label class="form-check-label" for="cat_${cat.idType}">${escapeHtml(cat.typeName)}</label>
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
        const html = '<option value="">Select Currency</option>' + currencies.map(curr =>
            `<option value="${curr.idEnum}">${curr.enumName}</option>`
        ).join('');
        select.innerHTML = html;

        setCurrencyValue();
    }

    function setCurrencyValue() {
        if (!pendingCurrencyValue) {
            console.log('setCurrencyValue: No pending currency value');
            return;
        }

        const currencySelect = document.getElementById('currency');
        if (!currencySelect) {
            console.log('setCurrencyValue: Currency select element not found');
            return;
        }

        if (currencySelect.options.length <= 1) {
            console.log('setCurrencyValue: Options not loaded yet, length:', currencySelect.options.length);
            return;
        }

        console.log('setCurrencyValue: Setting currency to', pendingCurrencyValue);
        console.log('Available options:', Array.from(currencySelect.options).map(o => ({ value: o.value, text: o.text })));

        // Convert to string for comparison
        const valueAsString = pendingCurrencyValue.toString();

        // Find the option to get both value and label
        let optionLabel = '';
        for (let i = 0; i < currencySelect.options.length; i++) {
            if (currencySelect.options[i].value === valueAsString) {
                optionLabel = currencySelect.options[i].textContent;
                break;
            }
        }

        if (!optionLabel) {
            console.warn('Failed to find currency option. Value:', valueAsString);
            return;
        }

        // Check if SearchableDropdown is initialized
        if (currencySelect._searchableDropdown) {
            console.log('Using SearchableDropdown.setValue()');
            currencySelect._searchableDropdown.setValue(valueAsString, optionLabel, false);
            console.log('Currency set successfully via SearchableDropdown');
        } else {
            // Fallback to direct select value (shouldn't happen for currency)
            console.log('SearchableDropdown not initialized, using direct value assignment');
            currencySelect.value = valueAsString;
        }

        // Verify the value persists
        setTimeout(() => {
            const currentValue = document.getElementById('currency')?.value;
            console.log('Currency value after 100ms:', currentValue);
            if (currentValue !== valueAsString) {
                console.warn('Currency value was changed! Expected:', valueAsString, 'Got:', currentValue);
            }
        }, 100);
    }

    async function loadEmployees() {
        try {
            const response = await fetch(`${CONFIG.apiEndpoints.companyUsers}?prefix=`);
            const result = await response.json();

            if (result.success && result.data) {
                allEmployees = result.data.map(emp => ({
                    id: emp.idEmployee,
                    name: emp.employeeName,
                    title: emp.title,
                    department: emp.department
                }));
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

    function showSubGroupModal() {
        currentSubGroupIndex = null;
        document.getElementById('approvalFlowName').value = '';
        if (subGroupModal) {
            subGroupModal.show();
        }
    }

    function saveSubGroup() {
        const flowName = document.getElementById('approvalFlowName').value.trim();

        if (!flowName) {
            showError('Please enter sub group name');
            return;
        }

        if (currentSubGroupIndex !== null) {
            subGroups[currentSubGroupIndex].name = flowName;
        } else {
            const subGroup = {
                id: ++subGroupIdCounter,
                idCostApproverSubGroup: 0,
                name: flowName,
                approvers: []
            };

            subGroups.push(subGroup);
        }

        renderSubGroups();

        if (subGroupModal) {
            subGroupModal.hide();
        }
    }

    function renderSubGroups() {
        const container = document.getElementById('approvalFlowsContainer');

        if (subGroups.length === 0) {
            container.innerHTML = '<p class="text-muted">No sub groups added yet. Click "Add New Approval Flow" to get started.</p>';
            return;
        }

        const html = subGroups.map((subGroup, index) => {
            const levelMap = {};
            subGroup.approvers.forEach(approver => {
                if (!levelMap[approver.level]) {
                    levelMap[approver.level] = [];
                }
                levelMap[approver.level].push(approver);
            });

            const levels = Object.keys(levelMap).map(Number).sort((a, b) => a - b);
            const maxLevel = Math.max(...levels, 0);

            return `
                <div class="approval-flow-card">
                    <div class="approval-flow-header">
                        <div class="approval-flow-name">${escapeHtml(subGroup.name)}</div>
                        <div>
                            <button type="button" class="btn btn-sm btn-info me-1" onclick="editSubGroup(${index})" title="Edit Sub Group Name">
                                <i class="ti ti-edit"></i>
                                Edit
                            </button>
                            <button type="button" class="btn btn-sm btn-danger" onclick="deleteSubGroup(${index})" title="Delete Sub Group">
                                <i class="ti ti-trash"></i>
                                Delete
                            </button>
                        </div>
                    </div>
                    <div class="approver-section">
                        <div class="mb-2">
                            <strong>Approver <span class="required">*</span></strong>
                        </div>
                        <button type="button" class="btn btn-sm btn-primary mb-2" onclick="addNewApprover(${index})">
                            <i class="ti ti-plus me-1"></i>
                            Add New Approver
                        </button>
                        <div>
                            ${levels.length === 0
                                ? '<p class="text-muted small mb-0">→ Add a new approver first</p>'
                                : `
                                    <table class="table table-sm approver-table">
                                        <thead>
                                            <tr>
                                                <th>Name</th>
                                                <th width="100">Level</th>
                                                <th width="150">Action</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            ${levels.map(level => {
                                                const approversInLevel = levelMap[level];
                                                const priorityApprovers = approversInLevel.filter(a => a.isPriority);
                                                const nonPriorityApprovers = approversInLevel.filter(a => !a.isPriority);

                                                const nameDisplay = [
                                                    ...priorityApprovers.map(a =>
                                                        `<span class="clickable-approver" onclick="editLevel(${index}, ${level})" style="cursor: pointer; color: #5a8dee; text-decoration: underline;">${escapeHtml(a.name)}</span>`
                                                    ),
                                                    ...nonPriorityApprovers.map(a =>
                                                        `<span class="clickable-approver" onclick="editLevel(${index}, ${level})" style="cursor: pointer; color: #6c757d;">${escapeHtml(a.name)}</span>`
                                                    )
                                                ].join(', ');

                                                return `
                                                <tr class="approver-row">
                                                    <td>${nameDisplay}</td>
                                                    <td>
                                                        <span class="authority-level">${level}</span>
                                                    </td>
                                                    <td>
                                                        <div class="btn-group" role="group">
                                                            <button type="button" class="btn btn-sm btn-primary"
                                                                    onclick="moveLevelUp(${index}, ${level})"
                                                                    ${level === 1 ? 'disabled' : ''}
                                                                    title="Move Level Up">
                                                                <i class="ti ti-arrow-up"></i>
                                                            </button>
                                                            <button type="button" class="btn btn-sm btn-primary"
                                                                    onclick="moveLevelDown(${index}, ${level})"
                                                                    ${level === maxLevel ? 'disabled' : ''}
                                                                    title="Move Level Down">
                                                                <i class="ti ti-arrow-down"></i>
                                                            </button>
                                                            <button type="button" class="btn btn-sm btn-danger"
                                                                    onclick="deleteLevel(${index}, ${level})"
                                                                    title="Delete Level">
                                                                <i class="ti ti-trash"></i>
                                                            </button>
                                                        </div>
                                                    </td>
                                                </tr>
                                            `}).join('')}
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

    window.editSubGroup = function(index) {
        const subGroup = subGroups[index];
        if (!subGroup) return;

        currentSubGroupIndex = index;
        document.getElementById('approvalFlowName').value = subGroup.name;
        if (subGroupModal) {
            subGroupModal.show();
        }
    };

    window.deleteSubGroup = function(index) {
        if (confirm('Are you sure you want to delete this sub group?')) {
            subGroups.splice(index, 1);
            renderSubGroups();
        }
    };

    window.addNewApprover = function(subGroupIndex) {
        currentSubGroupIndex = subGroupIndex;
        const subGroup = subGroups[subGroupIndex];

        const maxLevel = subGroup.approvers.length > 0
            ? Math.max(...subGroup.approvers.map(a => a.level || 1))
            : 0;

        currentEditingLevel = maxLevel + 1;

        document.getElementById('approverSearch').value = '';
        hideSearchResults();
        renderApproverTable([]);

        const modalTitle = document.querySelector('#approverModal .modal-title');
        if (modalTitle) {
            modalTitle.textContent = `Add Level ${currentEditingLevel} Approvers`;
        }

        const saveBtn = document.getElementById('saveApproversBtn');
        if (saveBtn) {
            saveBtn.innerHTML = '<i class="ti ti-device-floppy me-1"></i>Save New Approver';
        }

        if (approverModal) {
            approverModal.show();
        }
    };

    window.editLevel = function(subGroupIndex, level) {
        currentSubGroupIndex = subGroupIndex;
        currentEditingLevel = level;

        const subGroup = subGroups[subGroupIndex];
        const approversInLevel = subGroup.approvers.filter(a => a.level === level);

        document.getElementById('approverSearch').value = '';
        hideSearchResults();
        renderApproverTable(approversInLevel);

        const modalTitle = document.querySelector('#approverModal .modal-title');
        if (modalTitle) {
            modalTitle.textContent = `Edit Level ${level} Approvers`;
        }

        const saveBtn = document.getElementById('saveApproversBtn');
        if (saveBtn) {
            saveBtn.innerHTML = '<i class="ti ti-device-floppy me-1"></i>Save Approvers';
        }

        if (approverModal) {
            approverModal.show();
        }
    };

    function handleSearchFocus() {
        const searchInput = document.getElementById('approverSearch');
        if (searchInput.value.trim()) {
            performSearch(searchInput.value);
        }
    }

    function handleApproverSearch(e) {
        const searchTerm = e.target.value.trim();

        if (searchTimeout) {
            clearTimeout(searchTimeout);
        }

        if (!searchTerm) {
            hideSearchResults();
            return;
        }

        showSearchSpinner();

        searchTimeout = setTimeout(() => {
            performSearch(searchTerm);
        }, 300);
    }

    function performSearch(searchTerm) {
        const lowerSearch = searchTerm.toLowerCase();

        filteredEmployees = allEmployees.filter(emp =>
            (emp.name || '').toLowerCase().includes(lowerSearch) ||
            (emp.department || '').toLowerCase().includes(lowerSearch) ||
            (emp.title || '').toLowerCase().includes(lowerSearch)
        );

        renderSearchResults(filteredEmployees, searchTerm);
        hideSearchSpinner();
    }

    function renderSearchResults(results, searchTerm) {
        const resultsList = document.getElementById('approverResultsList');
        const resultsDropdown = document.getElementById('approverSearchResults');

        if (results.length === 0) {
            resultsList.innerHTML = `
                <div class="search-result-item no-results">
                    <i class="ti ti-search-off me-2"></i>
                    No approvers found for "${escapeHtml(searchTerm)}"
                </div>
            `;
        } else {
            const html = results.slice(0, 10).map(emp => `
                <div class="search-result-item" data-employee-id="${emp.id}" onclick="selectApproverFromSearch(${emp.id})">
                    <div class="result-main">
                        <i class="ti ti-user me-2"></i>
                        <div class="result-info">
                            <div class="result-name">${highlightMatch(escapeHtml(emp.name), searchTerm)}</div>
                            <div class="result-details">
                                ${emp.title ? `<span class="result-title">${escapeHtml(emp.title)}</span>` : ''}
                                ${emp.department ? `<span class="result-department">${escapeHtml(emp.department)}</span>` : ''}
                            </div>
                        </div>
                    </div>
                    <i class="ti ti-plus result-action"></i>
                </div>
            `).join('');

            resultsList.innerHTML = html;

            if (results.length > 10) {
                resultsList.innerHTML += `
                    <div class="search-result-footer">
                        Showing 10 of ${results.length} results. Refine your search to see more.
                    </div>
                `;
            }
        }

        resultsDropdown.style.display = 'block';
    }

    function highlightMatch(text, search) {
        if (!search) return text;
        const regex = new RegExp(`(${search})`, 'gi');
        return text.replace(regex, '<mark>$1</mark>');
    }

    window.selectApproverFromSearch = function(employeeId) {
        const employee = allEmployees.find(emp => emp.id === employeeId);
        if (!employee) return;

        const subGroup = subGroups[currentSubGroupIndex];
        if (!subGroup) return;

        if (subGroup.approvers.some(a => a.id === employeeId)) {
            showError('This approver is already added to this sub group');
            return;
        }

        const approversInCurrentLevel = subGroup.approvers.filter(a => a.level === currentEditingLevel);
        const isPriorityByDefault = approversInCurrentLevel.length === 0;

        const approver = {
            id: employee.id,
            name: employee.name,
            isPriority: isPriorityByDefault,
            level: currentEditingLevel
        };

        subGroup.approvers.push(approver);

        const updatedApproversInLevel = subGroup.approvers.filter(a => a.level === currentEditingLevel);
        renderApproverTable(updatedApproversInLevel);

        document.getElementById('approverSearch').value = '';
        hideSearchResults();

        showSuccess(`${employee.name} added to level ${currentEditingLevel}${isPriorityByDefault ? ' as priority approver' : ''}`);
    };

    function showSearchSpinner() {
        document.getElementById('searchSpinner').style.display = 'block';
    }

    function hideSearchSpinner() {
        document.getElementById('searchSpinner').style.display = 'none';
    }

    function hideSearchResults() {
        const resultsDropdown = document.getElementById('approverSearchResults');
        if (resultsDropdown) {
            resultsDropdown.style.display = 'none';
        }
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

    window.togglePriorityApprover = function(approverIndex) {
        const subGroup = subGroups[currentSubGroupIndex];
        if (!subGroup) return;

        const approversInCurrentLevel = subGroup.approvers.filter(a => a.level === currentEditingLevel);
        const approver = approversInCurrentLevel[approverIndex];
        if (!approver) return;

        const actualApprover = subGroup.approvers.find(a => a.id === approver.id && a.level === currentEditingLevel);
        if (!actualApprover) return;

        if (actualApprover.isPriority) {
            if (approversInCurrentLevel.length > 1) {
                showError(`Cannot uncheck priority status. Level ${currentEditingLevel} must have exactly one priority approver. Please assign another approver in this level as priority first.`);
                return;
            }
        } else {
            const currentPriority = approversInCurrentLevel.find(a => a.isPriority);
            if (currentPriority) {
                const actualPriority = subGroup.approvers.find(a => a.id === currentPriority.id && a.level === currentEditingLevel);
                if (actualPriority) {
                    actualPriority.isPriority = false;
                }
            }
        }

        actualApprover.isPriority = !actualApprover.isPriority;

        const updatedApproversInLevel = subGroup.approvers.filter(a => a.level === currentEditingLevel);
        renderApproverTable(updatedApproversInLevel);
    };

    window.removeApproverFromModal = function(approverIndex) {
        const subGroup = subGroups[currentSubGroupIndex];
        if (!subGroup) return;

        const approversInCurrentLevel = subGroup.approvers.filter(a => a.level === currentEditingLevel);
        const approver = approversInCurrentLevel[approverIndex];
        if (!approver) return;

        if (approver.isPriority && approversInCurrentLevel.length > 1) {
            showError(`Cannot remove priority approver when there are other approvers in level ${currentEditingLevel}. Please assign another priority approver first or remove other approvers in this level.`);
            return;
        }

        const actualIndex = subGroup.approvers.findIndex(a => a.id === approver.id && a.level === currentEditingLevel);
        if (actualIndex !== -1) {
            subGroup.approvers.splice(actualIndex, 1);
        }

        const updatedApproversInLevel = subGroup.approvers.filter(a => a.level === currentEditingLevel);
        renderApproverTable(updatedApproversInLevel);
    };

    window.moveLevelUp = function(subGroupIndex, level) {
        if (level <= 1) return;

        const subGroup = subGroups[subGroupIndex];
        if (!subGroup) return;

        const targetLevel = level - 1;

        subGroup.approvers.forEach(approver => {
            if (approver.level === level) {
                approver.level = targetLevel;
            } else if (approver.level === targetLevel) {
                approver.level = level;
            }
        });

        renderSubGroups();
    };

    window.moveLevelDown = function(subGroupIndex, level) {
        const subGroup = subGroups[subGroupIndex];
        if (!subGroup) return;

        const maxLevel = Math.max(...subGroup.approvers.map(a => a.level || 1));
        if (level >= maxLevel) return;

        const targetLevel = level + 1;

        subGroup.approvers.forEach(approver => {
            if (approver.level === level) {
                approver.level = targetLevel;
            } else if (approver.level === targetLevel) {
                approver.level = level;
            }
        });

        renderSubGroups();
    };

    window.deleteLevel = function(subGroupIndex, level) {
        const subGroup = subGroups[subGroupIndex];
        if (!subGroup) return;

        const approversInLevel = subGroup.approvers.filter(a => a.level === level);
        const approverNames = approversInLevel.map(a => a.name).join(', ');

        if (!confirm(`Delete level ${level} (${approverNames})? This will remove all approvers in this level.`)) {
            return;
        }

        subGroup.approvers = subGroup.approvers.filter(a => a.level !== level);

        subGroup.approvers.forEach(approver => {
            if (approver.level > level) {
                approver.level--;
            }
        });

        renderSubGroups();
    };

    function saveApprovers() {
        const subGroup = subGroups[currentSubGroupIndex];
        if (!subGroup) return;

        const approversInCurrentLevel = subGroup.approvers.filter(a => a.level === currentEditingLevel);

        if (approversInCurrentLevel.length === 0) {
            showError(`Please add at least one approver to level ${currentEditingLevel}`);
            return;
        }

        const priorityCount = approversInCurrentLevel.filter(a => a.isPriority).length;

        if (priorityCount === 0) {
            showError(`Level ${currentEditingLevel} must have exactly one priority approver`);
            return;
        } else if (priorityCount > 1) {
            showError(`Level ${currentEditingLevel} has ${priorityCount} priority approvers, but must have exactly one`);
            return;
        }

        renderSubGroups();

        if (approverModal) {
            approverModal.hide();
        }

        showSuccess(`Level ${currentEditingLevel} approvers saved successfully`);
    }

    async function handleFormSubmit(e) {
        e.preventDefault();

        const groupName = document.getElementById('groupName').value.trim();
        const description = document.getElementById('description').value.trim();
        const currencyIdEnum = parseInt(document.getElementById('currency').value);
        const rangeValueStart = parseFloat(document.getElementById('minValue').value) || 0;
        const rangeValueEnd = parseFloat(document.getElementById('maxValue').value) || 0;

        const selectedProperties = Array.from(document.querySelectorAll('.property-checkbox:checked'))
            .map(cb => parseInt(cb.value));

        const selectedCategories = Array.from(document.querySelectorAll('.category-checkbox:checked'))
            .map(cb => parseInt(cb.value));

        if (!groupName) {
            showError('Please enter cost approver group name');
            return;
        }

        if (!currencyIdEnum) {
            showError('Please select currency');
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

        if (subGroups.length === 0) {
            showError('Please add at least one sub group');
            return;
        }

        for (const subGroup of subGroups) {
            if (subGroup.approvers.length === 0) {
                showError(`Sub group "${subGroup.name}" must have at least one approver`);
                return;
            }

            const levels = [...new Set(subGroup.approvers.map(a => a.level))];

            for (const level of levels) {
                const approversInLevel = subGroup.approvers.filter(a => a.level === level);
                const priorityCount = approversInLevel.filter(a => a.isPriority).length;

                if (priorityCount !== 1) {
                    showError(`Sub group "${subGroup.name}" level ${level} must have exactly one priority approver`);
                    return;
                }
            }
        }

        const payload = {
            idCostApproverGroup: clientContext.idCostApproverGroup,
            idClient: clientContext.idClient,
            name: groupName,
            description: description,
            currencyIdEnum: currencyIdEnum,
            rangeValueStart: rangeValueStart,
            rangeValueEnd: rangeValueEnd,
            properties: selectedProperties,
            workCategories: selectedCategories,
            subGroups: subGroups.map(subGroup => ({
                idCostApproverSubGroup: subGroup.idCostApproverSubGroup || 0,
                name: subGroup.name,
                approvers: subGroup.approvers.map(a => ({
                    idEmployee: a.id,
                    level: a.level,
                    isPriorityApprover: a.isPriority
                }))
            }))
        };

        try {
            const endpoint = isEditMode ? CONFIG.apiEndpoints.updateCostApproverGroup : CONFIG.apiEndpoints.createCostApproverGroup;
            const method = isEditMode ? 'PUT' : 'POST';
            const token = $('input[name="__RequestVerificationToken"]').val();

            const response = await fetch(endpoint, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            // Check HTTP status first
            if (!response.ok) {
                // Handle BadRequest (400) and other error status codes
                handleValidationErrors(result);
                return;
            }

            if (result.success) {
                showSuccess(isEditMode ? 'Cost approver group updated successfully' : 'Cost approver group created successfully');
                setTimeout(() => {
                    window.location.href = '/Helpdesk/CostApprover';
                }, 1500);
            } else {
                handleValidationErrors(result);
            }
        } catch (error) {
            console.error('Error saving cost approver group:', error);
            showError('An error occurred while saving cost approver group');
        }
    }

    function handleValidationErrors(result) {
        let errorMessage = '';

        if (result.errors && Array.isArray(result.errors) && result.errors.length > 0) {
            if (result.errors.length === 1) {
                errorMessage = result.errors[0];
            } else {
                errorMessage = '<ul style="text-align: left; margin: 0; padding-left: 1.5rem;">';
                result.errors.forEach(err => {
                    errorMessage += `<li>${escapeHtml(err)}</li>`;
                });
                errorMessage += '</ul>';
            }
        } else if (result.message) {
            errorMessage = result.message;
        } else {
            errorMessage = 'Failed to save cost approver group';
        }

        showNotification(errorMessage, 'error', 'Validation Error', {
            timeOut: 0,
            extendedTimeOut: 0,
            closeButton: true,
            escapeHtml: false
        });
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
