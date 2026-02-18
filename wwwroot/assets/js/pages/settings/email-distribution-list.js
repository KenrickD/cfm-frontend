(function () {
    'use strict';

    const CONFIG = {
        apiEndpoints: {
            list: MvcEndpoints.Helpdesk.Settings.EmailDistribution.List
        }
    };

    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; }
    };

    let distributionList = [];

    document.addEventListener('DOMContentLoaded', function () {
        loadDistributionList();
    });

    async function loadDistributionList() {
        try {
            const url = `${CONFIG.apiEndpoints.list}?cid=${clientContext.idClient}`;
            const response = await fetch(url);
            const result = await response.json();

            if (result.success && result.data) {
                distributionList = result.data.data || [];
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
            const statusBadge = item.canEdit
                ? '<span class="status-badge status-configured">Configured</span>'
                : '<span class="status-badge status-not-configured">Not Configured</span>';

            const actionButton = item.canEdit
                ? `<button type="button" class="btn btn-edit btn-action" onclick="editDistribution(${item.idEnum}, '${escapeHtml(item.pageReference)}')">
                       <i class="ti ti-edit me-1"></i>Edit
                   </button>`
                : `<button type="button" class="btn btn-setup btn-action" onclick="setupDistribution(${item.idEnum}, '${escapeHtml(item.pageReference)}')">
                       <i class="ti ti-plus me-1"></i>Set Up
                   </button>`;

            return `
                <div class="distribution-row">
                    <div class="col-md-6">
                        <div class="distribution-name">${escapeHtml(item.pageReference)}</div>
                    </div>
                    <div class="col-md-3">
                        <div class="distribution-status">${statusBadge}</div>
                    </div>
                    <div class="col-md-3">
                        <div class="distribution-actions justify-content-end">
                            ${actionButton}
                        </div>
                    </div>
                </div>
            `;
        }).join('');
    }

    function updateTotalCount() {
        const totalCount = distributionList.length;
        const configuredCount = distributionList.filter(x => x.canEdit).length;

        document.getElementById('totalCount').innerHTML =
            `Showing total of <strong>${totalCount}</strong> Email Distribution List` +
            ` (<strong>${configuredCount}</strong> configured, <strong>${totalCount - configuredCount}</strong> not configured)`;
    }

    window.setupDistribution = function (idEnum, pageReference) {
        const params = new URLSearchParams({ idEnum, pageReference });
        window.location.href = `/Helpdesk/EmailDistributionListSetup?${params.toString()}`;
    };

    window.editDistribution = function (idEnum, pageReference) {
        const params = new URLSearchParams({ idEnum, pageReference });
        window.location.href = `/Helpdesk/EmailDistributionListEdit?${params.toString()}`;
    };

    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return String(text).replace(/[&<>"']/g, m => map[m]);
    }
})();
