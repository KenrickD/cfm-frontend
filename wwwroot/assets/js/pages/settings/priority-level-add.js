// Priority Level Add Page JavaScript
(function () {
    'use strict';

    const dropdownTypes = [
        { id: 'helpdeskResponseTargetReference', type: 'priorityLevelInitialFollowUp' },
        { id: 'initialFollowUpTargetReference', type: 'priorityLevelInitialFollowUp' },
        { id: 'quotationSubmissionTargetReference', type: 'priorityLevelQuotationSubmission' },
        { id: 'costApprovalTargetReference', type: 'priorityLevelCostApproval' },
        { id: 'workCompletionTargetReference', type: 'priorityLevelWorkCompletion' },
        { id: 'afterWorkFollowUpTargetReference', type: 'priorityLevelAfterWorkFollowUp' }
    ];

    // Initialize on DOM load
    document.addEventListener('DOMContentLoaded', function () {
        initializeForm();
        loadDropdownData();
    });

    function initializeForm() {
        const form = document.getElementById('priorityLevelForm');
        if (form) {
            form.addEventListener('submit', handleSubmit);
        }

        // Handle compliance duration toggles
        const helpdeskComplianceCheckbox = document.getElementById('helpdeskResponseTargetActivateCompliance');
        if (helpdeskComplianceCheckbox) {
            helpdeskComplianceCheckbox.addEventListener('change', function () {
                const complianceDiv = document.getElementById('helpdeskComplianceDuration');
                if (complianceDiv) {
                    complianceDiv.style.display = this.checked ? 'block' : 'none';
                }
            });
        }

        // Handle visual color preview
        const colorSelect = document.getElementById('visualColor');
        if (colorSelect) {
            colorSelect.addEventListener('change', updateColorPreview);
        }
    }

    async function loadDropdownData() {
        // Load visual colors
        await loadVisualColors();

        // Load reference dropdowns
        for (const dropdown of dropdownTypes) {
            await loadDropdownOptions(dropdown.id, dropdown.type);
        }
    }

    async function loadVisualColors() {
        try {
            const response = await fetch('/Helpdesk/GetPriorityLevelDropdownOptions?type=visualColor');
            const result = await response.json();

            if (result.success && result.data) {
                const select = document.getElementById('visualColor');
                if (select) {
                    result.data.forEach(option => {
                        const opt = document.createElement('option');
                        opt.value = option.value;
                        opt.textContent = option.label;
                        select.appendChild(opt);
                    });
                }
            }
        } catch (error) {
            console.error('Error loading visual colors:', error);
        }
    }

    async function loadDropdownOptions(selectId, type) {
        try {
            const response = await fetch(`/Helpdesk/GetPriorityLevelDropdownOptions?type=${type}`);
            const result = await response.json();

            if (result.success && result.data) {
                const select = document.getElementById(selectId);
                if (select) {
                    result.data.forEach(option => {
                        const opt = document.createElement('option');
                        opt.value = option.value;
                        opt.textContent = option.label;
                        select.appendChild(opt);
                    });
                }
            }
        } catch (error) {
            console.error(`Error loading dropdown options for ${selectId}:`, error);
        }
    }

    function updateColorPreview() {
        const colorSelect = document.getElementById('visualColor');
        const colorPreview = document.getElementById('colorPreview');

        if (colorSelect && colorPreview) {
            const selectedColor = colorSelect.value;
            const colorStyle = getColorStyle(selectedColor);
            colorPreview.style.backgroundColor = colorStyle;
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
        return colorMap[colorName] || '#ffffff';
    }

    async function handleSubmit(e) {
        e.preventDefault();

        const formData = collectFormData();

        // Validate
        if (!formData.name) {
            showError('Please enter a priority level name');
            return;
        }

        try {
            const response = await fetch('/Helpdesk/CreatePriorityLevel', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {
                showSuccess('Priority level created successfully');
                setTimeout(() => {
                    window.location.href = '/Helpdesk/PriorityLevel';
                }, 1000);
            } else {
                showError('Failed to create priority level: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error creating priority level:', error);
            showError('An error occurred while creating the priority level');
        }
    }

    function collectFormData() {
        return {
            name: getValue('name'),
            visualColor: getValue('visualColor'),

            // Helpdesk Response Target
            helpdeskResponseTargetDays: getNumericValue('helpdeskResponseTargetDays'),
            helpdeskResponseTargetHours: getNumericValue('helpdeskResponseTargetHours'),
            helpdeskResponseTargetMinutes: getNumericValue('helpdeskResponseTargetMinutes'),
            helpdeskResponseTargetWithinOfficeHours: getCheckboxValue('helpdeskResponseTargetWithinOfficeHours'),
            helpdeskResponseTargetReference: getValue('helpdeskResponseTargetReference'),
            helpdeskResponseTargetRequiredToFill: getCheckboxValue('helpdeskResponseTargetRequiredToFill'),
            helpdeskResponseTargetActivateCompliance: getCheckboxValue('helpdeskResponseTargetActivateCompliance'),
            helpdeskResponseTargetComplianceDurationDays: getNumericValue('helpdeskResponseTargetComplianceDurationDays'),
            helpdeskResponseTargetComplianceDurationHours: getNumericValue('helpdeskResponseTargetComplianceDurationHours'),
            helpdeskResponseTargetComplianceDurationMinutes: getNumericValue('helpdeskResponseTargetComplianceDurationMinutes'),
            helpdeskResponseTargetAcknowledgeActual: getCheckboxValue('helpdeskResponseTargetAcknowledgeActual'),
            helpdeskResponseTargetAcknowledgeTargetChanged: getCheckboxValue('helpdeskResponseTargetAcknowledgeTargetChanged'),
            helpdeskResponseTargetReminderBeforeTarget: getCheckboxValue('helpdeskResponseTargetReminderBeforeTarget'),

            // Initial Follow Up Target
            initialFollowUpTargetDays: getNumericValue('initialFollowUpTargetDays'),
            initialFollowUpTargetHours: getNumericValue('initialFollowUpTargetHours'),
            initialFollowUpTargetMinutes: getNumericValue('initialFollowUpTargetMinutes'),
            initialFollowUpTargetWithinOfficeHours: getCheckboxValue('initialFollowUpTargetWithinOfficeHours'),
            initialFollowUpTargetReference: getValue('initialFollowUpTargetReference'),
            initialFollowUpTargetRequiredToFill: getCheckboxValue('initialFollowUpTargetRequiredToFill'),
            initialFollowUpTargetActivateCompliance: getCheckboxValue('initialFollowUpTargetActivateCompliance'),
            initialFollowUpTargetAcknowledgeActual: getCheckboxValue('initialFollowUpTargetAcknowledgeActual'),
            initialFollowUpTargetAcknowledgeTargetChanged: getCheckboxValue('initialFollowUpTargetAcknowledgeTargetChanged'),
            initialFollowUpTargetReminderBeforeTarget: getCheckboxValue('initialFollowUpTargetReminderBeforeTarget'),

            // Quotation Submission Target
            quotationSubmissionTargetDays: getNumericValue('quotationSubmissionTargetDays'),
            quotationSubmissionTargetHours: getNumericValue('quotationSubmissionTargetHours'),
            quotationSubmissionTargetMinutes: getNumericValue('quotationSubmissionTargetMinutes'),
            quotationSubmissionTargetWithinOfficeHours: getCheckboxValue('quotationSubmissionTargetWithinOfficeHours'),
            quotationSubmissionTargetReference: getValue('quotationSubmissionTargetReference'),
            quotationSubmissionTargetRequiredToFill: getCheckboxValue('quotationSubmissionTargetRequiredToFill'),
            quotationSubmissionTargetActivateCompliance: getCheckboxValue('quotationSubmissionTargetActivateCompliance'),
            quotationSubmissionTargetAcknowledgeActual: getCheckboxValue('quotationSubmissionTargetAcknowledgeActual'),
            quotationSubmissionTargetAcknowledgeTargetChanged: getCheckboxValue('quotationSubmissionTargetAcknowledgeTargetChanged'),
            quotationSubmissionTargetReminderBeforeTarget: getCheckboxValue('quotationSubmissionTargetReminderBeforeTarget'),

            // Cost Approval Target
            costApprovalTargetDays: getNumericValue('costApprovalTargetDays'),
            costApprovalTargetHours: getNumericValue('costApprovalTargetHours'),
            costApprovalTargetMinutes: getNumericValue('costApprovalTargetMinutes'),
            costApprovalTargetWithinOfficeHours: getCheckboxValue('costApprovalTargetWithinOfficeHours'),
            costApprovalTargetReference: getValue('costApprovalTargetReference'),
            costApprovalTargetRequiredToFill: getCheckboxValue('costApprovalTargetRequiredToFill'),
            costApprovalTargetActivateCompliance: getCheckboxValue('costApprovalTargetActivateCompliance'),
            costApprovalTargetAcknowledgeActual: getCheckboxValue('costApprovalTargetAcknowledgeActual'),
            costApprovalTargetAcknowledgeTargetChanged: getCheckboxValue('costApprovalTargetAcknowledgeTargetChanged'),
            costApprovalTargetReminderBeforeTarget: getCheckboxValue('costApprovalTargetReminderBeforeTarget'),

            // Work Completion Target
            workCompletionTargetDays: getNumericValue('workCompletionTargetDays'),
            workCompletionTargetHours: getNumericValue('workCompletionTargetHours'),
            workCompletionTargetMinutes: getNumericValue('workCompletionTargetMinutes'),
            workCompletionTargetWithinOfficeHours: getCheckboxValue('workCompletionTargetWithinOfficeHours'),
            workCompletionTargetReference: getValue('workCompletionTargetReference'),
            workCompletionTargetRequiredToFill: getCheckboxValue('workCompletionTargetRequiredToFill'),
            workCompletionTargetActivateCompliance: getCheckboxValue('workCompletionTargetActivateCompliance'),
            workCompletionTargetAcknowledgeActual: getCheckboxValue('workCompletionTargetAcknowledgeActual'),
            workCompletionTargetAcknowledgeTargetChanged: getCheckboxValue('workCompletionTargetAcknowledgeTargetChanged'),
            workCompletionTargetReminderBeforeTarget: getCheckboxValue('workCompletionTargetReminderBeforeTarget'),

            // After Work Follow Up Target
            afterWorkFollowUpTargetDays: getNumericValue('afterWorkFollowUpTargetDays'),
            afterWorkFollowUpTargetHours: getNumericValue('afterWorkFollowUpTargetHours'),
            afterWorkFollowUpTargetMinutes: getNumericValue('afterWorkFollowUpTargetMinutes'),
            afterWorkFollowUpTargetWithinOfficeHours: getCheckboxValue('afterWorkFollowUpTargetWithinOfficeHours'),
            afterWorkFollowUpTargetReference: getValue('afterWorkFollowUpTargetReference'),
            afterWorkFollowUpTargetRequiredToFill: getCheckboxValue('afterWorkFollowUpTargetRequiredToFill'),
            afterWorkFollowUpTargetActivateCompliance: getCheckboxValue('afterWorkFollowUpTargetActivateCompliance'),
            afterWorkFollowUpTargetAcknowledgeActual: getCheckboxValue('afterWorkFollowUpTargetAcknowledgeActual'),
            afterWorkFollowUpTargetAcknowledgeTargetChanged: getCheckboxValue('afterWorkFollowUpTargetAcknowledgeTargetChanged'),
            afterWorkFollowUpTargetReminderBeforeTarget: getCheckboxValue('afterWorkFollowUpTargetReminderBeforeTarget'),
            afterWorkFollowUpTargetActivateAutoFill: getCheckboxValue('afterWorkFollowUpTargetActivateAutoFill')
        };
    }

    function getValue(id) {
        const element = document.getElementById(id);
        return element ? element.value : '';
    }

    function getNumericValue(id) {
        const element = document.getElementById(id);
        return element ? parseInt(element.value) || 0 : 0;
    }

    function getCheckboxValue(id) {
        const element = document.getElementById(id);
        return element ? element.checked : false;
    }

    function showSuccess(message) {
        alert(message);
    }

    function showError(message) {
        alert(message);
    }
})();
