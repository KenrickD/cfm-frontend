(function () {
    'use strict';

    const CONFIG = {
        apiEndpoints: {
            list: MvcEndpoints.Helpdesk.Settings.EmailDistribution.List,
            delete: MvcEndpoints.Helpdesk.Settings.EmailDistribution.Delete
        }
    };

    let distributionList = [];
    let deleteModal;
    let currentDeleteId = null;
    let currentDeleteName = '';

    document.addEventListener('DOMContentLoaded', function () {
        initializeComponents();
        loadDistributionList();
    });

    function initializeComponents() {
        deleteModal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
        document.getElementById('confirmDeleteBtn').addEventListener('click', confirmDelete);
    }

    async function loadDistributionList() {
        try {
            const response = await fetch(CONFIG.apiEndpoints.list);
            const result = await response.json();

            if (result.success) {
                distributionList = result.data || [];
                renderDistributionList();
                updateTotalCount();
            } else {
                showNotification(result.message || 'Failed to load email distribution list', 'error');
            }
        } catch (error) {
            console.error('Error loading email distribution list:', error);
            showNotification('An error occurred while loading the data', 'error');
        }
    }

    function renderDistributionList() {
        const tbody = document.getElementById('distributionListBody');
        const emptyState = document.getElementById('emptyState');

        if (distributionList.length === 0) {
            tbody.innerHTML = '';
            emptyState.classList.remove('d-none');
            return;
        }

        emptyState.classList.add('d-none');

        tbody.innerHTML = distributionList.map(item => {
            const hasDistribution = item.hasDistributionList;
            const statusBadge = hasDistribution
                ? '<span class="status-badge status-configured">Configured</span>'
                : '<span class="status-badge status-not-configured">Not Configured</span>';

            const actionButton = hasDistribution
                ? `<button type="button" class="btn btn-edit btn-action" onclick="editDistribution(${item.distributionListId})">
                       <i class="ti ti-edit me-1"></i>Edit
                   </button>`
                : `<button type="button" class="btn btn-setup btn-action" onclick="setupDistribution('${escapeHtml(item.value)}')">
                       <i class="ti ti-plus me-1"></i>Set Up
                   </button>`;

            return `
                <div class="distribution-row">
                    <div class="col-md-6">
                        <div class="distribution-name">${escapeHtml(item.text)}</div>
                    </div>
                    <div class="col-md-3">
                        <div class="distribution-status">${statusBadge}</div>
                    </div>
                    <div class="col-md-3">
                        <div class="distribution-actions justify-content-end">
                            ${actionButton}
                            ${hasDistribution ? `
                                <button type="button" class="btn btn-danger btn-action" onclick="showDeleteModal(${item.distributionListId}, '${escapeHtml(item.text)}')">
                                    <i class="ti ti-trash"></i>
                                </button>
                            ` : ''}
                        </div>
                    </div>
                </div>
            `;
        }).join('');
    }

    function updateTotalCount() {
        const totalCount = distributionList.length;
        const configuredCount = distributionList.filter(x => x.hasDistributionList).length;

        document.getElementById('totalCount').innerHTML =
            `Showing total of <strong>${totalCount}</strong> Email Distribution List` +
            ` (<strong>${configuredCount}</strong> configured, <strong>${totalCount - configuredCount}</strong> not configured)`;
    }

    window.setupDistribution = function (pageReference) {
        window.location.href = `/Helpdesk/EmailDistributionListSetup?pageReference=${encodeURIComponent(pageReference)}`;
    };

    window.editDistribution = function (id) {
        window.location.href = `/Helpdesk/EmailDistributionListEdit?id=${id}`;
    };

    window.showDeleteModal = function (id, name) {
        currentDeleteId = id;
        currentDeleteName = name;
        document.getElementById('deleteItemName').textContent = name;
        deleteModal.show();
    };

    async function confirmDelete() {
        if (!currentDeleteId) return;

        const deleteBtn = document.getElementById('confirmDeleteBtn');
        deleteBtn.disabled = true;
        deleteBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Deleting...';

        try {
            const response = await fetch(CONFIG.apiEndpoints.delete, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ id: currentDeleteId })
            });

            const result = await response.json();

            if (result.success) {
                showNotification('Email distribution deleted successfully', 'success');
                deleteModal.hide();
                loadDistributionList();
            } else {
                showNotification(result.message || 'Failed to delete email distribution', 'error');
            }
        } catch (error) {
            console.error('Error deleting email distribution:', error);
            showNotification('An error occurred while deleting', 'error');
        } finally {
            deleteBtn.disabled = false;
            deleteBtn.innerHTML = 'Delete';
        }
    }

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
})();
