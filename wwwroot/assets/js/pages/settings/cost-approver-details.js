// Cost Approver Details Page JavaScript (Read-Only)
(function () {
    'use strict';

    const CONFIG = {
        apiEndpoints: {
            locations: MvcEndpoints.Helpdesk.Location.GetByClient,
            workCategories: MvcEndpoints.Helpdesk.Settings.CostApproverGroup.GetWorkCategoriesForSettings,
            currencies: MvcEndpoints.Helpdesk.Extended.GetCurrencies,
            getCostApproverGroupById: MvcEndpoints.Helpdesk.Settings.CostApproverGroup.GetById
        }
    };

    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; },
        get idCostApproverGroup() { return window.PageContext?.idCostApproverGroup || 0; }
    };

    let subGroups = [];

    document.addEventListener('DOMContentLoaded', function () {
        loadInitialData();
    });

    async function loadInitialData() {
        await Promise.all([
            loadLocations(),
            loadWorkCategories(),
            loadCurrencies()
        ]);

        if (clientContext.idCostApproverGroup > 0) {
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
        document.getElementById('groupName').value = data.name || '';
        document.getElementById('description').value = data.description || '';

        const currencySelect = document.getElementById('currency');
        const currencyValue = data.rangeValue?.currency_IdEnum || data.rangeValue?.Currency_IdEnum || '';

        if (currencySelect && currencyValue) {
            setTimeout(() => {
                currencySelect.value = currencyValue;
            }, 50);
        }

        document.getElementById('minValue').value = data.rangeValue?.amountStart || data.rangeValue?.AmountStart || 0;
        document.getElementById('maxValue').value = data.rangeValue?.amountEnd || data.rangeValue?.AmountEnd || 0;

        if (data.properties && data.properties.length > 0) {
            data.properties.forEach(prop => {
                const checkbox = document.getElementById(`prop_${prop.idProperty}`);
                if (checkbox) {
                    checkbox.checked = true;
                    checkbox.disabled = true;
                }
            });
        }

        if (data.workCategories && data.workCategories.length > 0) {
            data.workCategories.forEach(cat => {
                const checkbox = document.getElementById(`cat_${cat.workCategory_IdType}`);
                if (checkbox) {
                    checkbox.checked = true;
                    checkbox.disabled = true;
                }
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
                <input class="form-check-input property-checkbox" type="checkbox" value="${loc.idProperty}" id="prop_${loc.idProperty}" disabled>
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
                <input class="form-check-input category-checkbox" type="checkbox" value="${cat.idType}" id="cat_${cat.idType}" disabled>
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
    }

    function renderSubGroups() {
        const container = document.getElementById('approvalFlowsContainer');

        if (subGroups.length === 0) {
            container.innerHTML = '<p class="text-muted">No approval flows configured.</p>';
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

            return `
                <div class="approval-flow-card">
                    <div class="approval-flow-header">
                        <div class="approval-flow-name">${escapeHtml(subGroup.name)}</div>
                    </div>
                    <div class="approver-section">
                        <div class="mb-2">
                            <strong>Approvers</strong>
                        </div>
                        <div>
                            ${levels.length === 0
                                ? '<p class="text-muted small mb-0">No approvers configured</p>'
                                : `
                                    <table class="table table-sm approver-table">
                                        <thead>
                                            <tr>
                                                <th>Name</th>
                                                <th width="100">Level</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            ${levels.map(level => {
                                                const approversInLevel = levelMap[level];
                                                const priorityApprovers = approversInLevel.filter(a => a.isPriority);
                                                const nonPriorityApprovers = approversInLevel.filter(a => !a.isPriority);

                                                const nameDisplay = [
                                                    ...priorityApprovers.map(a =>
                                                        `<span style="color: #5a8dee; font-weight: 600;">${escapeHtml(a.name)} <span class="priority-badge">Priority</span></span>`
                                                    ),
                                                    ...nonPriorityApprovers.map(a =>
                                                        `<span style="color: #6c757d;">${escapeHtml(a.name)}</span>`
                                                    )
                                                ].join(', ');

                                                return `
                                                <tr class="approver-row">
                                                    <td>${nameDisplay}</td>
                                                    <td>
                                                        <span class="authority-level">${level}</span>
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

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function showError(message) {
        showNotification(message, 'error', 'Error');
    }
})();
