// Priority Level Edit Page JavaScript
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

    let currentData = null;

    // Initialize on DOM load
    document.addEventListener('DOMContentLoaded', function () {
        initializeForm();
        loadDropdownDataThenPriorityLevel();
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

    async function loadDropdownDataThenPriorityLevel() {
        // Load visual colors
        await loadVisualColors();

        // Load reference dropdowns
        for (const dropdown of dropdownTypes) {
            await loadDropdownOptions(dropdown.id, dropdown.type);
        }

        // After dropdowns are loaded, load priority level data
        await loadPriorityLevel();
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

    async function loadPriorityLevel() {
        if (typeof priorityLevelId === 'undefined') {
            showError('Priority level ID not found');
            return;
        }

        try {
            const response = await fetch(`/Helpdesk/GetPriorityLevelById?id=${priorityLevelId}`);
            const result = await response.json();

            if (result.success && result.data) {
                currentData = result.data;
                populateForm(result.data);
            } else {
                showError('Failed to load priority level: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error loading priority level:', error);
            showError('An error occurred while loading the priority level');
        }
    }

    function populateForm(data) {
        // Basic Information
        setValue('name', data.name);
        setValue('visualColor', data.visualColor);
        updateColorPreview();

        // Helpdesk Response Target
        setValue('helpdeskResponseTargetDays', data.helpdeskResponseTargetDays);
        setValue('helpdeskResponseTargetHours', data.helpdeskResponseTargetHours);
        setValue('helpdeskResponseTargetMinutes', data.helpdeskResponseTargetMinutes);
        setCheckboxValue('helpdeskResponseTargetWithinOfficeHours', data.helpdeskResponseTargetWithinOfficeHours);
        setValue('helpdeskResponseTargetReference', data.helpdeskResponseTargetReference);
        setCheckboxValue('helpdeskResponseTargetRequiredToFill', data.helpdeskResponseTargetRequiredToFill);
        setCheckboxValue('helpdeskResponseTargetActivateCompliance', data.helpdeskResponseTargetActivateCompliance);

        // Show/hide compliance duration
        const complianceDiv = document.getElementById('helpdeskComplianceDuration');
        if (complianceDiv) {
            complianceDiv.style.display = data.helpdeskResponseTargetActivateCompliance ? 'block' : 'none';
        }

        setValue('helpdeskResponseTargetComplianceDurationDays', data.helpdeskResponseTargetComplianceDurationDays);
        setValue('helpdeskResponseTargetComplianceDurationHours', data.helpdeskResponseTargetComplianceDurationHours);
        setValue('helpdeskResponseTargetComplianceDurationMinutes', data.helpdeskResponseTargetComplianceDurationMinutes);
        setCheckboxValue('helpdeskResponseTargetAcknowledgeActual', data.helpdeskResponseTargetAcknowledgeActual);
        setCheckboxValue('helpdeskResponseTargetAcknowledgeTargetChanged', data.helpdeskResponseTargetAcknowledgeTargetChanged);
        setCheckboxValue('helpdeskResponseTargetReminderBeforeTarget', data.helpdeskResponseTargetReminderBeforeTarget);

        // Initial Follow Up Target
        setValue('initialFollowUpTargetDays', data.initialFollowUpTargetDays);
        setValue('initialFollowUpTargetHours', data.initialFollowUpTargetHours);
        setValue('initialFollowUpTargetMinutes', data.initialFollowUpTargetMinutes);
        setCheckboxValue('initialFollowUpTargetWithinOfficeHours', data.initialFollowUpTargetWithinOfficeHours);
        setValue('initialFollowUpTargetReference', data.initialFollowUpTargetReference);
        setCheckboxValue('initialFollowUpTargetRequiredToFill', data.initialFollowUpTargetRequiredToFill);
        setCheckboxValue('initialFollowUpTargetActivateCompliance', data.initialFollowUpTargetActivateCompliance);
        setCheckboxValue('initialFollowUpTargetAcknowledgeActual', data.initialFollowUpTargetAcknowledgeActual);
        setCheckboxValue('initialFollowUpTargetAcknowledgeTargetChanged', data.initialFollowUpTargetAcknowledgeTargetChanged);
        setCheckboxValue('initialFollowUpTargetReminderBeforeTarget', data.initialFollowUpTargetReminderBeforeTarget);

        // Quotation Submission Target
        setValue('quotationSubmissionTargetDays', data.quotationSubmissionTargetDays);
        setValue('quotationSubmissionTargetHours', data.quotationSubmissionTargetHours);
        setValue('quotationSubmissionTargetMinutes', data.quotationSubmissionTargetMinutes);
        setCheckboxValue('quotationSubmissionTargetWithinOfficeHours', data.quotationSubmissionTargetWithinOfficeHours);
        setValue('quotationSubmissionTargetReference', data.quotationSubmissionTargetReference);
        setCheckboxValue('quotationSubmissionTargetRequiredToFill', data.quotationSubmissionTargetRequiredToFill);
        setCheckboxValue('quotationSubmissionTargetActivateCompliance', data.quotationSubmissionTargetActivateCompliance);
        setCheckboxValue('quotationSubmissionTargetAcknowledgeActual', data.quotationSubmissionTargetAcknowledgeActual);
        setCheckboxValue('quotationSubmissionTargetAcknowledgeTargetChanged', data.quotationSubmissionTargetAcknowledgeTargetChanged);
        setCheckboxValue('quotationSubmissionTargetReminderBeforeTarget', data.quotationSubmissionTargetReminderBeforeTarget);

        // Cost Approval Target
        setValue('costApprovalTargetDays', data.costApprovalTargetDays);
        setValue('costApprovalTargetHours', data.costApprovalTargetHours);
        setValue('costApprovalTargetMinutes', data.costApprovalTargetMinutes);
        setCheckboxValue('costApprovalTargetWithinOfficeHours', data.costApprovalTargetWithinOfficeHours);
        setValue('costApprovalTargetReference', data.costApprovalTargetReference);
        setCheckboxValue('costApprovalTargetRequiredToFill', data.costApprovalTargetRequiredToFill);
        setCheckboxValue('costApprovalTargetActivateCompliance', data.costApprovalTargetActivateCompliance);
        setCheckboxValue('costApprovalTargetAcknowledgeActual', data.costApprovalTargetAcknowledgeActual);
        setCheckboxValue('costApprovalTargetAcknowledgeTargetChanged', data.costApprovalTargetAcknowledgeTargetChanged);
        setCheckboxValue('costApprovalTargetReminderBeforeTarget', data.costApprovalTargetReminderBeforeTarget);

        // Work Completion Target
        setValue('workCompletionTargetDays', data.workCompletionTargetDays);
        setValue('workCompletionTargetHours', data.workCompletionTargetHours);
        setValue('workCompletionTargetMinutes', data.workCompletionTargetMinutes);
        setCheckboxValue('workCompletionTargetWithinOfficeHours', data.workCompletionTargetWithinOfficeHours);
        setValue('workCompletionTargetReference', data.workCompletionTargetReference);
        setCheckboxValue('workCompletionTargetRequiredToFill', data.workCompletionTargetRequiredToFill);
        setCheckboxValue('workCompletionTargetActivateCompliance', data.workCompletionTargetActivateCompliance);
        setCheckboxValue('workCompletionTargetAcknowledgeActual', data.workCompletionTargetAcknowledgeActual);
        setCheckboxValue('workCompletionTargetAcknowledgeTargetChanged', data.workCompletionTargetAcknowledgeTargetChanged);
        setCheckboxValue('workCompletionTargetReminderBeforeTarget', data.workCompletionTargetReminderBeforeTarget);

        // After Work Follow Up Target
        setValue('afterWorkFollowUpTargetDays', data.afterWorkFollowUpTargetDays);
        setValue('afterWorkFollowUpTargetHours', data.afterWorkFollowUpTargetHours);
        setValue('afterWorkFollowUpTargetMinutes', data.afterWorkFollowUpTargetMinutes);
        setCheckboxValue('afterWorkFollowUpTargetWithinOfficeHours', data.afterWorkFollowUpTargetWithinOfficeHours);
        setValue('afterWorkFollowUpTargetReference', data.afterWorkFollowUpTargetReference);
        setCheckboxValue('afterWorkFollowUpTargetRequiredToFill', data.afterWorkFollowUpTargetRequiredToFill);
        setCheckboxValue('afterWorkFollowUpTargetActivateCompliance', data.afterWorkFollowUpTargetActivateCompliance);
        setCheckboxValue('afterWorkFollowUpTargetAcknowledgeActual', data.afterWorkFollowUpTargetAcknowledgeActual);
        setCheckboxValue('afterWorkFollowUpTargetAcknowledgeTargetChanged', data.afterWorkFollowUpTargetAcknowledgeTargetChanged);
        setCheckboxValue('afterWorkFollowUpTargetReminderBeforeTarget', data.afterWorkFollowUpTargetReminderBeforeTarget);
        setCheckboxValue('afterWorkFollowUpTargetActivateAutoFill', data.afterWorkFollowUpTargetActivateAutoFill);
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

        // Add ID from loaded data
        formData.id = currentData.id;
        formData.idClient = currentData.idClient;

        try {
            const response = await fetch('/Helpdesk/UpdatePriorityLevel', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {
                showSuccess('Priority level updated successfully');
                setTimeout(() => {
                    window.location.href = '/Helpdesk/PriorityLevel';
                }, 1000);
            } else {
                showError('Failed to update priority level: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error updating priority level:', error);
            showError('An error occurred while updating the priority level');
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

    function setValue(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.value = value || '';
        }
    }

    function setCheckboxValue(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.checked = value || false;
        }
    }

    function showSuccess(message) {
        alert(message);
    }

    function showError(message) {
        alert(message);
    }
})();
