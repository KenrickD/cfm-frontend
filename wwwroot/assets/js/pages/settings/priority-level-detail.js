// Priority Level Detail Page JavaScript
(function () {
    'use strict';

    // Initialize on DOM load
    document.addEventListener('DOMContentLoaded', function () {
        initializeButtons();
        loadPriorityLevel();
    });

    function initializeButtons() {
        const editBtn = document.getElementById('editBtn');
        if (editBtn) {
            editBtn.addEventListener('click', function () {
                if (typeof priorityLevelId !== 'undefined') {
                    window.location.href = `/Helpdesk/PriorityLevelEdit?id=${priorityLevelId}`;
                }
            });
        }
    }

    async function loadPriorityLevel() {
        if (typeof priorityLevelId === 'undefined') {
            showError('Priority level ID not found');
            return;
        }

        try {
            const response = await fetch(`/Helpdesk/GetPriorityLevelById?id=${priorityLevelId}`);
            const result = await response.json();

            if (result.success && result.data) {
                populateDetails(result.data);
            } else {
                showError('Failed to load priority level: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error loading priority level:', error);
            showError('An error occurred while loading the priority level');
        }
    }

    function populateDetails(data) {
        // Basic Information
        setText('name', data.name);
        setText('visualColorName', data.visualColor);

        const colorPreview = document.getElementById('colorPreview');
        if (colorPreview) {
            colorPreview.style.backgroundColor = getColorStyle(data.visualColor);
        }

        // Helpdesk Response Target
        setTargetDisplay('helpdeskResponseTarget', data.helpdeskResponseTargetDays, data.helpdeskResponseTargetHours, data.helpdeskResponseTargetMinutes);
        setWithinOfficeHours('helpdeskResponseWithinOfficeHours', data.helpdeskResponseTargetWithinOfficeHours);
        setText('helpdeskResponseReference', formatReference(data.helpdeskResponseTargetReference));
        setCheckboxIndicator('helpdeskResponseRequiredToFill', data.helpdeskResponseTargetRequiredToFill);
        setCheckboxIndicator('helpdeskResponseActivateCompliance', data.helpdeskResponseTargetActivateCompliance);

        if (data.helpdeskResponseTargetActivateCompliance) {
            const complianceDiv = document.getElementById('helpdeskComplianceDurationDisplay');
            if (complianceDiv) {
                complianceDiv.style.display = 'block';
                setTargetDisplay('helpdeskComplianceDuration', data.helpdeskResponseTargetComplianceDurationDays, data.helpdeskResponseTargetComplianceDurationHours, data.helpdeskResponseTargetComplianceDurationMinutes);
            }
        }

        setCheckboxIndicator('helpdeskResponseAcknowledgeActual', data.helpdeskResponseTargetAcknowledgeActual);
        setCheckboxIndicator('helpdeskResponseAcknowledgeTargetChanged', data.helpdeskResponseTargetAcknowledgeTargetChanged);
        setCheckboxIndicator('helpdeskResponseReminderBeforeTarget', data.helpdeskResponseTargetReminderBeforeTarget);

        // Initial Follow Up Target
        setTargetDisplay('initialFollowUpTarget', data.initialFollowUpTargetDays, data.initialFollowUpTargetHours, data.initialFollowUpTargetMinutes);
        setWithinOfficeHours('initialFollowUpWithinOfficeHours', data.initialFollowUpTargetWithinOfficeHours);
        setText('initialFollowUpReference', formatReference(data.initialFollowUpTargetReference));
        setCheckboxIndicator('initialFollowUpRequiredToFill', data.initialFollowUpTargetRequiredToFill);
        setCheckboxIndicator('initialFollowUpActivateCompliance', data.initialFollowUpTargetActivateCompliance);
        setCheckboxIndicator('initialFollowUpAcknowledgeActual', data.initialFollowUpTargetAcknowledgeActual);
        setCheckboxIndicator('initialFollowUpAcknowledgeTargetChanged', data.initialFollowUpTargetAcknowledgeTargetChanged);
        setCheckboxIndicator('initialFollowUpReminderBeforeTarget', data.initialFollowUpTargetReminderBeforeTarget);

        // Quotation Submission Target
        setTargetDisplay('quotationSubmissionTarget', data.quotationSubmissionTargetDays, data.quotationSubmissionTargetHours, data.quotationSubmissionTargetMinutes);
        setWithinOfficeHours('quotationSubmissionWithinOfficeHours', data.quotationSubmissionTargetWithinOfficeHours);
        setText('quotationSubmissionReference', formatReference(data.quotationSubmissionTargetReference));
        setCheckboxIndicator('quotationSubmissionRequiredToFill', data.quotationSubmissionTargetRequiredToFill);
        setCheckboxIndicator('quotationSubmissionActivateCompliance', data.quotationSubmissionTargetActivateCompliance);
        setCheckboxIndicator('quotationSubmissionAcknowledgeActual', data.quotationSubmissionTargetAcknowledgeActual);
        setCheckboxIndicator('quotationSubmissionAcknowledgeTargetChanged', data.quotationSubmissionTargetAcknowledgeTargetChanged);
        setCheckboxIndicator('quotationSubmissionReminderBeforeTarget', data.quotationSubmissionTargetReminderBeforeTarget);

        // Cost Approval Target
        setTargetDisplay('costApprovalTarget', data.costApprovalTargetDays, data.costApprovalTargetHours, data.costApprovalTargetMinutes);
        setWithinOfficeHours('costApprovalWithinOfficeHours', data.costApprovalTargetWithinOfficeHours);
        setText('costApprovalReference', formatReference(data.costApprovalTargetReference));
        setCheckboxIndicator('costApprovalRequiredToFill', data.costApprovalTargetRequiredToFill);
        setCheckboxIndicator('costApprovalActivateCompliance', data.costApprovalTargetActivateCompliance);
        setCheckboxIndicator('costApprovalAcknowledgeActual', data.costApprovalTargetAcknowledgeActual);
        setCheckboxIndicator('costApprovalAcknowledgeTargetChanged', data.costApprovalTargetAcknowledgeTargetChanged);
        setCheckboxIndicator('costApprovalReminderBeforeTarget', data.costApprovalTargetReminderBeforeTarget);

        // Work Completion Target
        setTargetDisplay('workCompletionTarget', data.workCompletionTargetDays, data.workCompletionTargetHours, data.workCompletionTargetMinutes);
        setWithinOfficeHours('workCompletionWithinOfficeHours', data.workCompletionTargetWithinOfficeHours);
        setText('workCompletionReference', formatReference(data.workCompletionTargetReference));
        setCheckboxIndicator('workCompletionRequiredToFill', data.workCompletionTargetRequiredToFill);
        setCheckboxIndicator('workCompletionActivateCompliance', data.workCompletionTargetActivateCompliance);
        setCheckboxIndicator('workCompletionAcknowledgeActual', data.workCompletionTargetAcknowledgeActual);
        setCheckboxIndicator('workCompletionAcknowledgeTargetChanged', data.workCompletionTargetAcknowledgeTargetChanged);
        setCheckboxIndicator('workCompletionReminderBeforeTarget', data.workCompletionTargetReminderBeforeTarget);

        // After Work Follow Up Target
        setTargetDisplay('afterWorkFollowUpTarget', data.afterWorkFollowUpTargetDays, data.afterWorkFollowUpTargetHours, data.afterWorkFollowUpTargetMinutes);
        setWithinOfficeHours('afterWorkFollowUpWithinOfficeHours', data.afterWorkFollowUpTargetWithinOfficeHours);
        setText('afterWorkFollowUpReference', formatReference(data.afterWorkFollowUpTargetReference));
        setCheckboxIndicator('afterWorkFollowUpRequiredToFill', data.afterWorkFollowUpTargetRequiredToFill);
        setCheckboxIndicator('afterWorkFollowUpActivateCompliance', data.afterWorkFollowUpTargetActivateCompliance);
        setCheckboxIndicator('afterWorkFollowUpAcknowledgeActual', data.afterWorkFollowUpTargetAcknowledgeActual);
        setCheckboxIndicator('afterWorkFollowUpAcknowledgeTargetChanged', data.afterWorkFollowUpTargetAcknowledgeTargetChanged);
        setCheckboxIndicator('afterWorkFollowUpReminderBeforeTarget', data.afterWorkFollowUpTargetReminderBeforeTarget);
        setCheckboxIndicator('afterWorkFollowUpActivateAutoFill', data.afterWorkFollowUpTargetActivateAutoFill);
    }

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value || '-';
        }
    }

    function setTargetDisplay(id, days, hours, minutes) {
        const element = document.getElementById(id);
        if (element) {
            const formatted = formatDuration(days, hours, minutes);
            element.textContent = formatted;
        }
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

    function setWithinOfficeHours(id, value) {
        const element = document.getElementById(id);
        if (element) {
            if (value) {
                element.innerHTML = '<span class="within-office-hours-badge">Within Office Hours</span>';
            } else {
                element.innerHTML = '';
            }
        }
    }

    function setCheckboxIndicator(id, value) {
        const element = document.getElementById(id);
        if (element) {
            if (value) {
                element.classList.add('checked');
                element.classList.remove('unchecked');
                element.querySelector('i').className = 'ti ti-square-check';
            } else {
                element.classList.add('unchecked');
                element.classList.remove('checked');
                element.querySelector('i').className = 'ti ti-square';
            }
        }
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

    function showError(message) {
        alert(message);
    }
})();
