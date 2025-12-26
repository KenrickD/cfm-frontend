// Priority Level List Page JavaScript
(function () {
    'use strict';

    let priorityLevels = [];
    let deleteModal;
    let currentDeleteId = null;

    // Initialize on DOM load
    document.addEventListener('DOMContentLoaded', function () {
        initializeComponents();
        loadPriorityLevels();
    });

    function initializeComponents() {
        // Initialize delete modal
        const modalElement = document.getElementById('deleteConfirmModal');
        if (modalElement) {
            deleteModal = new bootstrap.Modal(modalElement);
        }

        // Confirm delete button
        const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
        if (confirmDeleteBtn) {
            confirmDeleteBtn.addEventListener('click', handleDelete);
        }
    }

    async function loadPriorityLevels() {
        try {
            const response = await fetch('/Helpdesk/GetPriorityLevels');
            const result = await response.json();

            if (result.success) {
                priorityLevels = result.data || [];
                renderPriorityLevels();
                updateTotalCount();
            } else {
                showError('Failed to load priority levels: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error loading priority levels:', error);
            showError('An error occurred while loading priority levels');
        }
    }

    function renderPriorityLevels() {
        const tbody = document.getElementById('priorityTableBody');
        const emptyState = document.getElementById('emptyState');

        if (priorityLevels.length === 0) {
            emptyState.style.display = 'block';
            return;
        }

        emptyState.style.display = 'none';

        const html = priorityLevels.map((level, index) => {
            const colorStyle = getColorStyle(level.visualColor);
            return `
                <div class="priority-row">
                    <div class="row align-items-start">
                        <div class="col-md-1 text-center">
                            <div style="padding-top: 0.5rem;">
                                <span class="priority-badge" style="background-color: ${colorStyle}"></span>
                                ${index + 1}
                            </div>
                        </div>
                        <div class="col-md-2">
                            <a href="/Helpdesk/PriorityLevelDetail?id=${level.id}" class="priority-name">
                                ${escapeHtml(level.name)}
                            </a>
                        </div>
                        <div class="col-md-1">
                            <div class="target-info">
                                ${formatDuration(level.helpdeskResponseTargetDays, level.helpdeskResponseTargetHours, level.helpdeskResponseTargetMinutes)}
                                ${level.helpdeskResponseTargetWithinOfficeHours ? '<span class="within-office-hours">Within Office Hours</span>' : ''}
                            </div>
                            <div class="target-info">After Request Date</div>
                            ${level.helpdeskResponseTargetRequiredToFill ? '<div class="checkbox-info"><i class="ti ti-check"></i>Required to Fill on Work Request Completion</div>' : ''}
                            ${level.helpdeskResponseTargetActivateCompliance ? '<div class="checkbox-info"><i class="ti ti-check"></i>Activate Compliance Duration</div>' : ''}
                        </div>
                        <div class="col-md-2">
                            <div class="target-info">
                                ${formatDuration(level.initialFollowUpTargetDays, level.initialFollowUpTargetHours, level.initialFollowUpTargetMinutes)}
                                ${level.initialFollowUpTargetWithinOfficeHours ? '<span class="within-office-hours">Within Office Hours</span>' : ''}
                            </div>
                            <div class="target-info">${formatReference(level.initialFollowUpTargetReference)}</div>
                            ${level.initialFollowUpTargetRequiredToFill ? '<div class="checkbox-info"><i class="ti ti-check"></i>Required to Fill on Work Request Completion</div>' : ''}
                        </div>
                        <div class="col-md-2">
                            <div class="target-info">
                                ${formatDuration(level.quotationSubmissionTargetDays, level.quotationSubmissionTargetHours, level.quotationSubmissionTargetMinutes) || '-'}
                                ${level.quotationSubmissionTargetWithinOfficeHours ? '<span class="within-office-hours">Within Office Hours</span>' : ''}
                            </div>
                            <div class="target-info">${formatReference(level.quotationSubmissionTargetReference)}</div>
                            ${level.quotationSubmissionTargetAcknowledgeActual ? '<div class="checkbox-info"><i class="ti ti-check"></i>Acknowledge Requestor when The Actual already filled</div>' : ''}
                        </div>
                        <div class="col-md-1">
                            <div class="target-info">
                                ${formatDuration(level.costApprovalTargetDays, level.costApprovalTargetHours, level.costApprovalTargetMinutes) || '-'}
                                ${level.costApprovalTargetWithinOfficeHours ? '<span class="within-office-hours">Within Office Hours</span>' : ''}
                            </div>
                            <div class="target-info">${formatReference(level.costApprovalTargetReference)}</div>
                        </div>
                        <div class="col-md-2">
                            <div class="target-info">
                                ${formatDuration(level.workCompletionTargetDays, level.workCompletionTargetHours, level.workCompletionTargetMinutes)}
                                ${level.workCompletionTargetWithinOfficeHours ? '<span class="within-office-hours">Within Office Hours</span>' : ''}
                            </div>
                            <div class="target-info">${formatReference(level.workCompletionTargetReference)}</div>
                        </div>
                        <div class="col-md-1">
                            <div class="target-info">
                                ${formatDuration(level.afterWorkFollowUpTargetDays, level.afterWorkFollowUpTargetHours, level.afterWorkFollowUpTargetMinutes)}
                                ${level.afterWorkFollowUpTargetWithinOfficeHours ? '<span class="within-office-hours">Within Office Hours</span>' : ''}
                            </div>
                            <div class="target-info">${formatReference(level.afterWorkFollowUpTargetReference)}</div>
                            ${level.afterWorkFollowUpTargetActivateAutoFill ? '<div class="checkbox-info"><i class="ti ti-check"></i>Activate Auto Fill After Work Follow Up</div>' : ''}
                        </div>
                        <div class="col-md-1">
                            <div class="action-buttons">
                                <button type="button" class="btn btn-view btn-action" onclick="viewPriorityLevel(${level.id})" title="View">
                                    <i class="ti ti-eye"></i>
                                </button>
                                <button type="button" class="btn btn-edit btn-action" onclick="editPriorityLevel(${level.id})" title="Edit">
                                    <i class="ti ti-edit"></i>
                                </button>
                                <button type="button" class="btn btn-delete btn-action" onclick="showDeleteModal(${level.id}, '${escapeHtml(level.name)}')" title="Delete">
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

    function formatDuration(days, hours, minutes) {
        const parts = [];
        if (days > 0) parts.push(`${days} day${days > 1 ? 's' : ''}`);
        if (hours > 0) parts.push(`${hours} hour${hours > 1 ? 's' : ''}`);
        if (minutes > 0) parts.push(`${minutes} minute${minutes > 1 ? 's' : ''}`);

        if (parts.length === 0) return '-';
        return parts.join(' ');
    }

    function formatReference(reference) {
        if (!reference) return '-';
        return reference.replace(/([A-Z])/g, ' $1').trim();
    }

    function getColorStyle(colorName) {
        const colorMap = {
            'Red': '#dc3545',
            'Orange': '#fd7e14',
            'Yellow': '#ffc107',
            'Green': '#28a745',
            'Blue': '#007bff',
            'Purple': '#6f42c1',
            'Pink': '#e83e8c',
            'Cyan': '#17a2b8',
            'Gray': '#6c757d',
            'Brown': '#8b4513'
        };
        return colorMap[colorName] || '#6c757d';
    }

    function updateTotalCount() {
        const totalCountElement = document.getElementById('totalCount');
        if (totalCountElement) {
            totalCountElement.textContent = priorityLevels.length;
        }
    }

    window.viewPriorityLevel = function (id) {
        window.location.href = `/Helpdesk/PriorityLevelDetail?id=${id}`;
    };

    window.editPriorityLevel = function (id) {
        window.location.href = `/Helpdesk/PriorityLevelEdit?id=${id}`;
    };

    window.showDeleteModal = function (id, name) {
        currentDeleteId = id;
        const deleteNameElement = document.getElementById('deletePriorityName');
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
            const response = await fetch('/Helpdesk/DeletePriorityLevel', {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ id: currentDeleteId })
            });

            const result = await response.json();

            if (result.success) {
                showSuccess('Priority level deleted successfully');
                if (deleteModal) {
                    deleteModal.hide();
                }
                await loadPriorityLevels();
            } else {
                showError('Failed to delete priority level: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error deleting priority level:', error);
            showError('An error occurred while deleting the priority level');
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
