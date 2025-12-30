// Cost Approver Group List Page JavaScript
(function () {
    'use strict';

    const CONFIG = {
        apiEndpoints: {
            list: MvcEndpoints.Helpdesk.Settings.CostApproverGroup.List,
            delete: MvcEndpoints.Helpdesk.Settings.CostApproverGroup.Delete
        }
    };

    let costApproverGroups = [];
    let deleteModal;
    let currentDeleteId = null;

    document.addEventListener('DOMContentLoaded', function () {
        initializeComponents();
        loadCostApproverGroups();
    });

    function initializeComponents() {
        const modalElement = document.getElementById('deleteConfirmModal');
        if (modalElement) {
            deleteModal = new bootstrap.Modal(modalElement);
        }

        const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
        if (confirmDeleteBtn) {
            confirmDeleteBtn.addEventListener('click', handleDelete);
        }
    }

    async function loadCostApproverGroups() {
        try {
            const response = await fetch(CONFIG.apiEndpoints.list);
            const result = await response.json();

            if (result.success) {
                costApproverGroups = result.data || [];
                renderCostApproverGroups();
                updateTotalCount();
            } else {
                showError('Failed to load cost approver groups: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error loading cost approver groups:', error);
            showError('An error occurred while loading cost approver groups');
        }
    }

    function renderCostApproverGroups() {
        const tbody = document.getElementById('approverTableBody');
        const emptyState = document.getElementById('emptyState');

        if (costApproverGroups.length === 0) {
            emptyState.style.display = 'block';
            return;
        }

        emptyState.style.display = 'none';

        const html = costApproverGroups.map((group) => {
            const categoryBadges = renderCategoryBadges(group.workCategories);
            const flowBadges = renderFlowBadges(group.approvalFlows);

            return `
                <div class="approver-row">
                    <div class="row align-items-start">
                        <div class="col-md-3">
                            <a href="/Helpdesk/CostApproverDetail?id=${group.id}" class="approver-name">
                                ${escapeHtml(group.name)}
                            </a>
                        </div>
                        <div class="col-md-2">
                            <div class="property-info">
                                ${escapeHtml(group.propertyName || '-')}
                            </div>
                        </div>
                        <div class="col-md-2">
                            <div class="scrollable-cell">
                                ${categoryBadges || '<span class="text-muted">-</span>'}
                            </div>
                        </div>
                        <div class="col-md-2">
                            <div class="scrollable-cell">
                                ${flowBadges || '<span class="text-muted">-</span>'}
                            </div>
                        </div>
                        <div class="col-md-2">
                            <div class="range-value">
                                ${formatRangeValue(group.minValue, group.maxValue)}
                            </div>
                        </div>
                        <div class="col-md-1">
                            <div class="action-buttons">
                                <button type="button" class="btn btn-edit btn-action" onclick="editCostApproverGroup(${group.id})" title="Edit">
                                    <i class="ti ti-edit"></i>
                                </button>
                                <button type="button" class="btn btn-delete btn-action" onclick="showDeleteModal(${group.id}, '${escapeHtml(group.name)}')" title="Delete">
                                    <i class="ti ti-trash"></i>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        }).join('');

        tbody.innerHTML = html;
    }

    function renderCategoryBadges(categories) {
        if (!categories || categories.length === 0) {
            return '';
        }

        return categories.map(category =>
            `<span class="category-badge">${escapeHtml(category.name || category)}</span>`
        ).join('');
    }

    function renderFlowBadges(flows) {
        if (!flows || flows.length === 0) {
            return '';
        }

        return flows.map(flow =>
            `<span class="flow-badge">${escapeHtml(flow.name || flow)}</span>`
        ).join('');
    }

    function formatRangeValue(minValue, maxValue) {
        if (minValue == null && maxValue == null) {
            return '-';
        }

        const formatter = new Intl.NumberFormat('id-ID', {
            style: 'currency',
            currency: 'IDR',
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });

        const min = minValue != null ? formatter.format(minValue) : '0';
        const max = maxValue != null ? formatter.format(maxValue) : '∞';

        return `${min} - ${max}`;
    }

    function updateTotalCount() {
        const totalCountElement = document.getElementById('totalCount');
        if (totalCountElement) {
            totalCountElement.textContent = costApproverGroups.length;
        }
    }

    window.editCostApproverGroup = function (id) {
        window.location.href = `/Helpdesk/CostApproverEdit?id=${id}`;
    };

    window.showDeleteModal = function (id, name) {
        currentDeleteId = id;
        const deleteNameElement = document.getElementById('deleteApproverName');
        if (deleteNameElement) {
            deleteNameElement.textContent = name;
        }
        if (deleteModal) {
            deleteModal.show();
        }
    };

    async function handleDelete() {
        if (!currentDeleteId) return;

        try {
            const response = await fetch(CONFIG.apiEndpoints.delete, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ id: currentDeleteId })
            });

            const result = await response.json();

            if (result.success) {
                showSuccess('Cost approver group deleted successfully');
                if (deleteModal) {
                    deleteModal.hide();
                }
                await loadCostApproverGroups();
            } else {
                showError('Failed to delete cost approver group: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error deleting cost approver group:', error);
            showError('An error occurred while deleting the cost approver group');
        }

        currentDeleteId = null;
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
