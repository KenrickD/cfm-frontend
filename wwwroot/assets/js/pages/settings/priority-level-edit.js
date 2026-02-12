// Priority Level Edit Page JavaScript
(function () {
    'use strict';

    // Reference dropdowns to load from GetEnums endpoint
    const enumDropdowns = [
        { id: 'initialFollowUpTargetReference', category: 'priorityLevelInitialFollowUp' },
        { id: 'quotationSubmissionTargetReference', category: 'priorityLevelQuotationSubmission' },
        { id: 'costApprovalTargetReference', category: 'priorityLevelCostApproval' },
        { id: 'workCompletionTargetReference', category: 'priorityLevelWorkCompletion' },
        { id: 'afterWorkFollowUpTargetReference', category: 'priorityLevelAfterWorkFollowUp' }
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

        // Handle compliance duration toggles for all target sections
        const complianceToggles = [
            { checkboxId: 'helpdeskResponseTargetActivateCompliance', divId: 'helpdeskComplianceDuration' },
            { checkboxId: 'initialFollowUpTargetActivateCompliance', divId: 'initialFollowUpComplianceDuration' },
            { checkboxId: 'quotationSubmissionTargetActivateCompliance', divId: 'quotationSubmissionComplianceDuration' },
            { checkboxId: 'costApprovalTargetActivateCompliance', divId: 'costApprovalComplianceDuration' },
            { checkboxId: 'workCompletionTargetActivateCompliance', divId: 'workCompletionComplianceDuration' },
            { checkboxId: 'afterWorkFollowUpTargetActivateCompliance', divId: 'afterWorkFollowUpComplianceDuration' }
        ];

        complianceToggles.forEach(toggle => {
            const checkbox = document.getElementById(toggle.checkboxId);
            if (checkbox) {
                checkbox.addEventListener('change', function () {
                    const complianceDiv = document.getElementById(toggle.divId);
                    if (complianceDiv) {
                        complianceDiv.style.display = this.checked ? 'block' : 'none';
                    }
                });
            }
        });

        // Handle reminder duration toggles for all target sections
        const reminderToggles = [
            { checkboxId: 'helpdeskResponseTargetReminderBeforeTarget', divId: 'helpdeskReminderDuration' },
            { checkboxId: 'initialFollowUpTargetReminderBeforeTarget', divId: 'initialFollowUpReminderDuration' },
            { checkboxId: 'quotationSubmissionTargetReminderBeforeTarget', divId: 'quotationSubmissionReminderDuration' },
            { checkboxId: 'costApprovalTargetReminderBeforeTarget', divId: 'costApprovalReminderDuration' },
            { checkboxId: 'workCompletionTargetReminderBeforeTarget', divId: 'workCompletionReminderDuration' },
            { checkboxId: 'afterWorkFollowUpTargetReminderBeforeTarget', divId: 'afterWorkFollowUpReminderDuration' }
        ];

        reminderToggles.forEach(toggle => {
            const checkbox = document.getElementById(toggle.checkboxId);
            if (checkbox) {
                checkbox.addEventListener('change', function () {
                    const reminderDiv = document.getElementById(toggle.divId);
                    if (reminderDiv) {
                        reminderDiv.style.display = this.checked ? 'block' : 'none';
                    }
                });
            }
        });

        // Handle visual color preview
        const colorSelect = document.getElementById('visualColor');
        if (colorSelect) {
            colorSelect.addEventListener('change', updateColorPreview);
        }
    }

    async function loadDropdownDataThenPriorityLevel() {
        // Hardcode Helpdesk Response Target Reference (id: 172, "After Request Date")
        const helpdeskSelect = document.getElementById('helpdeskResponseTargetReference');
        if (helpdeskSelect) {
            const opt = document.createElement('option');
            opt.value = '172';
            opt.textContent = 'After Request Date';
            helpdeskSelect.appendChild(opt);

            // Reload the searchable dropdown to recognize the new option
            if (helpdeskSelect._searchableDropdown) {
                helpdeskSelect._searchableDropdown.loadFromSelect();
            }
        }

        // Load visual colors from GetEnums
        await loadEnumDropdown('visualColor', 'visualColor');

        // Load reference dropdowns from GetEnums
        for (const dropdown of enumDropdowns) {
            await loadEnumDropdown(dropdown.id, dropdown.category);
        }

        // After dropdowns are loaded, load priority level data
        await loadPriorityLevel();
    }

    async function loadEnumDropdown(selectId, category) {
        try {
            const response = await fetch(`${MvcEndpoints.Helpdesk.GetEnumsByCategory}?category=${category}`);
            const result = await response.json();

            if (result.success && result.data) {
                const select = document.getElementById(selectId);
                if (select) {
                    result.data.forEach(option => {
                        const opt = document.createElement('option');
                        opt.value = option.idEnum;        // EnumFormDetailResponse.IdEnum
                        opt.textContent = option.enumName; // EnumFormDetailResponse.EnumName
                        select.appendChild(opt);
                    });

                    // Reload the searchable dropdown to recognize new options
                    if (select._searchableDropdown) {
                        select._searchableDropdown.loadFromSelect();
                    }
                }
            }
        } catch (error) {
            console.error(`Error loading enum dropdown for ${selectId}:`, error);
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
        setValue('visualColor', data.visualColorId);
        updateColorPreview();

        // Helpdesk Response Target
        setValue('helpdeskResponseTargetDays', data.helpdeskResponseTargetDays);
        setValue('helpdeskResponseTargetHours', data.helpdeskResponseTargetHours);
        setValue('helpdeskResponseTargetMinutes', data.helpdeskResponseTargetMinutes);
        setCheckboxValue('helpdeskResponseTargetWithinOfficeHours', data.helpdeskResponseTargetWithinOfficeHours);
        setValue('helpdeskResponseTargetReference', '172'); // Always "After Request Date"
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

        // Show/hide reminder duration
        const helpdeskReminderDiv = document.getElementById('helpdeskReminderDuration');
        if (helpdeskReminderDiv) {
            helpdeskReminderDiv.style.display = data.helpdeskResponseTargetReminderBeforeTarget ? 'block' : 'none';
        }

        setValue('helpdeskResponseTargetReminderBeforeTargetDurationDays', data.helpdeskResponseTargetReminderBeforeTargetDurationDays);
        setValue('helpdeskResponseTargetReminderBeforeTargetDurationHours', data.helpdeskResponseTargetReminderBeforeTargetDurationHours);
        setValue('helpdeskResponseTargetReminderBeforeTargetDurationMinutes', data.helpdeskResponseTargetReminderBeforeTargetDurationMinutes);

        // Initial Follow Up Target
        setValue('initialFollowUpTargetDays', data.initialFollowUpTargetDays);
        setValue('initialFollowUpTargetHours', data.initialFollowUpTargetHours);
        setValue('initialFollowUpTargetMinutes', data.initialFollowUpTargetMinutes);
        setCheckboxValue('initialFollowUpTargetWithinOfficeHours', data.initialFollowUpTargetWithinOfficeHours);
        setValue('initialFollowUpTargetReference', data.initialFollowUpTargetReference);
        setCheckboxValue('initialFollowUpTargetRequiredToFill', data.initialFollowUpTargetRequiredToFill);
        setCheckboxValue('initialFollowUpTargetActivateCompliance', data.initialFollowUpTargetActivateCompliance);

        // Show/hide compliance duration
        const initialFollowUpComplianceDiv = document.getElementById('initialFollowUpComplianceDuration');
        if (initialFollowUpComplianceDiv) {
            initialFollowUpComplianceDiv.style.display = data.initialFollowUpTargetActivateCompliance ? 'block' : 'none';
        }

        setValue('initialFollowUpTargetComplianceDurationDays', data.initialFollowUpTargetComplianceDurationDays);
        setValue('initialFollowUpTargetComplianceDurationHours', data.initialFollowUpTargetComplianceDurationHours);
        setValue('initialFollowUpTargetComplianceDurationMinutes', data.initialFollowUpTargetComplianceDurationMinutes);
        setCheckboxValue('initialFollowUpTargetAcknowledgeActual', data.initialFollowUpTargetAcknowledgeActual);
        setCheckboxValue('initialFollowUpTargetAcknowledgeTargetChanged', data.initialFollowUpTargetAcknowledgeTargetChanged);
        setCheckboxValue('initialFollowUpTargetReminderBeforeTarget', data.initialFollowUpTargetReminderBeforeTarget);

        // Show/hide reminder duration
        const initialFollowUpReminderDiv = document.getElementById('initialFollowUpReminderDuration');
        if (initialFollowUpReminderDiv) {
            initialFollowUpReminderDiv.style.display = data.initialFollowUpTargetReminderBeforeTarget ? 'block' : 'none';
        }

        setValue('initialFollowUpTargetReminderBeforeTargetDurationDays', data.initialFollowUpTargetReminderBeforeTargetDurationDays);
        setValue('initialFollowUpTargetReminderBeforeTargetDurationHours', data.initialFollowUpTargetReminderBeforeTargetDurationHours);
        setValue('initialFollowUpTargetReminderBeforeTargetDurationMinutes', data.initialFollowUpTargetReminderBeforeTargetDurationMinutes);

        // Quotation Submission Target
        setValue('quotationSubmissionTargetDays', data.quotationSubmissionTargetDays);
        setValue('quotationSubmissionTargetHours', data.quotationSubmissionTargetHours);
        setValue('quotationSubmissionTargetMinutes', data.quotationSubmissionTargetMinutes);
        setCheckboxValue('quotationSubmissionTargetWithinOfficeHours', data.quotationSubmissionTargetWithinOfficeHours);
        setValue('quotationSubmissionTargetReference', data.quotationSubmissionTargetReference);
        setCheckboxValue('quotationSubmissionTargetRequiredToFill', data.quotationSubmissionTargetRequiredToFill);
        setCheckboxValue('quotationSubmissionTargetActivateCompliance', data.quotationSubmissionTargetActivateCompliance);

        // Show/hide compliance duration
        const quotationSubmissionComplianceDiv = document.getElementById('quotationSubmissionComplianceDuration');
        if (quotationSubmissionComplianceDiv) {
            quotationSubmissionComplianceDiv.style.display = data.quotationSubmissionTargetActivateCompliance ? 'block' : 'none';
        }

        setValue('quotationSubmissionTargetComplianceDurationDays', data.quotationSubmissionTargetComplianceDurationDays);
        setValue('quotationSubmissionTargetComplianceDurationHours', data.quotationSubmissionTargetComplianceDurationHours);
        setValue('quotationSubmissionTargetComplianceDurationMinutes', data.quotationSubmissionTargetComplianceDurationMinutes);
        setCheckboxValue('quotationSubmissionTargetAcknowledgeActual', data.quotationSubmissionTargetAcknowledgeActual);
        setCheckboxValue('quotationSubmissionTargetAcknowledgeTargetChanged', data.quotationSubmissionTargetAcknowledgeTargetChanged);
        setCheckboxValue('quotationSubmissionTargetReminderBeforeTarget', data.quotationSubmissionTargetReminderBeforeTarget);

        // Show/hide reminder duration
        const quotationSubmissionReminderDiv = document.getElementById('quotationSubmissionReminderDuration');
        if (quotationSubmissionReminderDiv) {
            quotationSubmissionReminderDiv.style.display = data.quotationSubmissionTargetReminderBeforeTarget ? 'block' : 'none';
        }

        setValue('quotationSubmissionTargetReminderBeforeTargetDurationDays', data.quotationSubmissionTargetReminderBeforeTargetDurationDays);
        setValue('quotationSubmissionTargetReminderBeforeTargetDurationHours', data.quotationSubmissionTargetReminderBeforeTargetDurationHours);
        setValue('quotationSubmissionTargetReminderBeforeTargetDurationMinutes', data.quotationSubmissionTargetReminderBeforeTargetDurationMinutes);

        // Cost Approval Target
        setValue('costApprovalTargetDays', data.costApprovalTargetDays);
        setValue('costApprovalTargetHours', data.costApprovalTargetHours);
        setValue('costApprovalTargetMinutes', data.costApprovalTargetMinutes);
        setCheckboxValue('costApprovalTargetWithinOfficeHours', data.costApprovalTargetWithinOfficeHours);
        setValue('costApprovalTargetReference', data.costApprovalTargetReference);
        setCheckboxValue('costApprovalTargetRequiredToFill', data.costApprovalTargetRequiredToFill);
        setCheckboxValue('costApprovalTargetActivateCompliance', data.costApprovalTargetActivateCompliance);

        // Show/hide compliance duration
        const costApprovalComplianceDiv = document.getElementById('costApprovalComplianceDuration');
        if (costApprovalComplianceDiv) {
            costApprovalComplianceDiv.style.display = data.costApprovalTargetActivateCompliance ? 'block' : 'none';
        }

        setValue('costApprovalTargetComplianceDurationDays', data.costApprovalTargetComplianceDurationDays);
        setValue('costApprovalTargetComplianceDurationHours', data.costApprovalTargetComplianceDurationHours);
        setValue('costApprovalTargetComplianceDurationMinutes', data.costApprovalTargetComplianceDurationMinutes);
        setCheckboxValue('costApprovalTargetAcknowledgeActual', data.costApprovalTargetAcknowledgeActual);
        setCheckboxValue('costApprovalTargetAcknowledgeTargetChanged', data.costApprovalTargetAcknowledgeTargetChanged);
        setCheckboxValue('costApprovalTargetReminderBeforeTarget', data.costApprovalTargetReminderBeforeTarget);

        // Show/hide reminder duration
        const costApprovalReminderDiv = document.getElementById('costApprovalReminderDuration');
        if (costApprovalReminderDiv) {
            costApprovalReminderDiv.style.display = data.costApprovalTargetReminderBeforeTarget ? 'block' : 'none';
        }

        setValue('costApprovalTargetReminderBeforeTargetDurationDays', data.costApprovalTargetReminderBeforeTargetDurationDays);
        setValue('costApprovalTargetReminderBeforeTargetDurationHours', data.costApprovalTargetReminderBeforeTargetDurationHours);
        setValue('costApprovalTargetReminderBeforeTargetDurationMinutes', data.costApprovalTargetReminderBeforeTargetDurationMinutes);

        // Work Completion Target
        setValue('workCompletionTargetDays', data.workCompletionTargetDays);
        setValue('workCompletionTargetHours', data.workCompletionTargetHours);
        setValue('workCompletionTargetMinutes', data.workCompletionTargetMinutes);
        setCheckboxValue('workCompletionTargetWithinOfficeHours', data.workCompletionTargetWithinOfficeHours);
        setValue('workCompletionTargetReference', data.workCompletionTargetReference);
        setCheckboxValue('workCompletionTargetRequiredToFill', data.workCompletionTargetRequiredToFill);
        setCheckboxValue('workCompletionTargetActivateCompliance', data.workCompletionTargetActivateCompliance);

        // Show/hide compliance duration
        const workCompletionComplianceDiv = document.getElementById('workCompletionComplianceDuration');
        if (workCompletionComplianceDiv) {
            workCompletionComplianceDiv.style.display = data.workCompletionTargetActivateCompliance ? 'block' : 'none';
        }

        setValue('workCompletionTargetComplianceDurationDays', data.workCompletionTargetComplianceDurationDays);
        setValue('workCompletionTargetComplianceDurationHours', data.workCompletionTargetComplianceDurationHours);
        setValue('workCompletionTargetComplianceDurationMinutes', data.workCompletionTargetComplianceDurationMinutes);
        setCheckboxValue('workCompletionTargetAcknowledgeActual', data.workCompletionTargetAcknowledgeActual);
        setCheckboxValue('workCompletionTargetAcknowledgeTargetChanged', data.workCompletionTargetAcknowledgeTargetChanged);
        setCheckboxValue('workCompletionTargetReminderBeforeTarget', data.workCompletionTargetReminderBeforeTarget);

        // Show/hide reminder duration
        const workCompletionReminderDiv = document.getElementById('workCompletionReminderDuration');
        if (workCompletionReminderDiv) {
            workCompletionReminderDiv.style.display = data.workCompletionTargetReminderBeforeTarget ? 'block' : 'none';
        }

        setValue('workCompletionTargetReminderBeforeTargetDurationDays', data.workCompletionTargetReminderBeforeTargetDurationDays);
        setValue('workCompletionTargetReminderBeforeTargetDurationHours', data.workCompletionTargetReminderBeforeTargetDurationHours);
        setValue('workCompletionTargetReminderBeforeTargetDurationMinutes', data.workCompletionTargetReminderBeforeTargetDurationMinutes);

        // After Work Follow Up Target
        setValue('afterWorkFollowUpTargetDays', data.afterWorkFollowUpTargetDays);
        setValue('afterWorkFollowUpTargetHours', data.afterWorkFollowUpTargetHours);
        setValue('afterWorkFollowUpTargetMinutes', data.afterWorkFollowUpTargetMinutes);
        setCheckboxValue('afterWorkFollowUpTargetWithinOfficeHours', data.afterWorkFollowUpTargetWithinOfficeHours);
        setValue('afterWorkFollowUpTargetReference', data.afterWorkFollowUpTargetReference);
        setCheckboxValue('afterWorkFollowUpTargetRequiredToFill', data.afterWorkFollowUpTargetRequiredToFill);
        setCheckboxValue('afterWorkFollowUpTargetActivateCompliance', data.afterWorkFollowUpTargetActivateCompliance);

        // Show/hide compliance duration
        const afterWorkFollowUpComplianceDiv = document.getElementById('afterWorkFollowUpComplianceDuration');
        if (afterWorkFollowUpComplianceDiv) {
            afterWorkFollowUpComplianceDiv.style.display = data.afterWorkFollowUpTargetActivateCompliance ? 'block' : 'none';
        }

        setValue('afterWorkFollowUpTargetComplianceDurationDays', data.afterWorkFollowUpTargetComplianceDurationDays);
        setValue('afterWorkFollowUpTargetComplianceDurationHours', data.afterWorkFollowUpTargetComplianceDurationHours);
        setValue('afterWorkFollowUpTargetComplianceDurationMinutes', data.afterWorkFollowUpTargetComplianceDurationMinutes);
        setCheckboxValue('afterWorkFollowUpTargetAcknowledgeActual', data.afterWorkFollowUpTargetAcknowledgeActual);
        setCheckboxValue('afterWorkFollowUpTargetAcknowledgeTargetChanged', data.afterWorkFollowUpTargetAcknowledgeTargetChanged);
        setCheckboxValue('afterWorkFollowUpTargetReminderBeforeTarget', data.afterWorkFollowUpTargetReminderBeforeTarget);

        // Show/hide reminder duration
        const afterWorkFollowUpReminderDiv = document.getElementById('afterWorkFollowUpReminderDuration');
        if (afterWorkFollowUpReminderDiv) {
            afterWorkFollowUpReminderDiv.style.display = data.afterWorkFollowUpTargetReminderBeforeTarget ? 'block' : 'none';
        }

        setValue('afterWorkFollowUpTargetReminderBeforeTargetDurationDays', data.afterWorkFollowUpTargetReminderBeforeTargetDurationDays);
        setValue('afterWorkFollowUpTargetReminderBeforeTargetDurationHours', data.afterWorkFollowUpTargetReminderBeforeTargetDurationHours);
        setValue('afterWorkFollowUpTargetReminderBeforeTargetDurationMinutes', data.afterWorkFollowUpTargetReminderBeforeTargetDurationMinutes);
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

        // Get CSRF token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        try {
            const response = await fetch(MvcEndpoints.Helpdesk.Settings.PriorityLevel.Update, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {
                showNotification('Priority level updated successfully', 'success');
                setTimeout(() => {
                    window.location.href = '/Helpdesk/PriorityLevel';
                }, 1000);
            } else {
                showNotification('Failed to update priority level: ' + (result.message || 'Unknown error'), 'error');
            }
        } catch (error) {
            console.error('Error updating priority level:', error);
            showNotification('An error occurred while updating the priority level', 'error');
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
            helpdeskResponseTargetReminderBeforeTargetDurationDays: getNumericValue('helpdeskResponseTargetReminderBeforeTargetDurationDays'),
            helpdeskResponseTargetReminderBeforeTargetDurationHours: getNumericValue('helpdeskResponseTargetReminderBeforeTargetDurationHours'),
            helpdeskResponseTargetReminderBeforeTargetDurationMinutes: getNumericValue('helpdeskResponseTargetReminderBeforeTargetDurationMinutes'),

            // Initial Follow Up Target
            initialFollowUpTargetDays: getNumericValue('initialFollowUpTargetDays'),
            initialFollowUpTargetHours: getNumericValue('initialFollowUpTargetHours'),
            initialFollowUpTargetMinutes: getNumericValue('initialFollowUpTargetMinutes'),
            initialFollowUpTargetWithinOfficeHours: getCheckboxValue('initialFollowUpTargetWithinOfficeHours'),
            initialFollowUpTargetReference: getValue('initialFollowUpTargetReference'),
            initialFollowUpTargetRequiredToFill: getCheckboxValue('initialFollowUpTargetRequiredToFill'),
            initialFollowUpTargetActivateCompliance: getCheckboxValue('initialFollowUpTargetActivateCompliance'),
            initialFollowUpTargetComplianceDurationDays: getNumericValue('initialFollowUpTargetComplianceDurationDays'),
            initialFollowUpTargetComplianceDurationHours: getNumericValue('initialFollowUpTargetComplianceDurationHours'),
            initialFollowUpTargetComplianceDurationMinutes: getNumericValue('initialFollowUpTargetComplianceDurationMinutes'),
            initialFollowUpTargetAcknowledgeActual: getCheckboxValue('initialFollowUpTargetAcknowledgeActual'),
            initialFollowUpTargetAcknowledgeTargetChanged: getCheckboxValue('initialFollowUpTargetAcknowledgeTargetChanged'),
            initialFollowUpTargetReminderBeforeTarget: getCheckboxValue('initialFollowUpTargetReminderBeforeTarget'),
            initialFollowUpTargetReminderBeforeTargetDurationDays: getNumericValue('initialFollowUpTargetReminderBeforeTargetDurationDays'),
            initialFollowUpTargetReminderBeforeTargetDurationHours: getNumericValue('initialFollowUpTargetReminderBeforeTargetDurationHours'),
            initialFollowUpTargetReminderBeforeTargetDurationMinutes: getNumericValue('initialFollowUpTargetReminderBeforeTargetDurationMinutes'),

            // Quotation Submission Target
            quotationSubmissionTargetDays: getNumericValue('quotationSubmissionTargetDays'),
            quotationSubmissionTargetHours: getNumericValue('quotationSubmissionTargetHours'),
            quotationSubmissionTargetMinutes: getNumericValue('quotationSubmissionTargetMinutes'),
            quotationSubmissionTargetWithinOfficeHours: getCheckboxValue('quotationSubmissionTargetWithinOfficeHours'),
            quotationSubmissionTargetReference: getValue('quotationSubmissionTargetReference'),
            quotationSubmissionTargetRequiredToFill: getCheckboxValue('quotationSubmissionTargetRequiredToFill'),
            quotationSubmissionTargetActivateCompliance: getCheckboxValue('quotationSubmissionTargetActivateCompliance'),
            quotationSubmissionTargetComplianceDurationDays: getNumericValue('quotationSubmissionTargetComplianceDurationDays'),
            quotationSubmissionTargetComplianceDurationHours: getNumericValue('quotationSubmissionTargetComplianceDurationHours'),
            quotationSubmissionTargetComplianceDurationMinutes: getNumericValue('quotationSubmissionTargetComplianceDurationMinutes'),
            quotationSubmissionTargetAcknowledgeActual: getCheckboxValue('quotationSubmissionTargetAcknowledgeActual'),
            quotationSubmissionTargetAcknowledgeTargetChanged: getCheckboxValue('quotationSubmissionTargetAcknowledgeTargetChanged'),
            quotationSubmissionTargetReminderBeforeTarget: getCheckboxValue('quotationSubmissionTargetReminderBeforeTarget'),
            quotationSubmissionTargetReminderBeforeTargetDurationDays: getNumericValue('quotationSubmissionTargetReminderBeforeTargetDurationDays'),
            quotationSubmissionTargetReminderBeforeTargetDurationHours: getNumericValue('quotationSubmissionTargetReminderBeforeTargetDurationHours'),
            quotationSubmissionTargetReminderBeforeTargetDurationMinutes: getNumericValue('quotationSubmissionTargetReminderBeforeTargetDurationMinutes'),

            // Cost Approval Target
            costApprovalTargetDays: getNumericValue('costApprovalTargetDays'),
            costApprovalTargetHours: getNumericValue('costApprovalTargetHours'),
            costApprovalTargetMinutes: getNumericValue('costApprovalTargetMinutes'),
            costApprovalTargetWithinOfficeHours: getCheckboxValue('costApprovalTargetWithinOfficeHours'),
            costApprovalTargetReference: getValue('costApprovalTargetReference'),
            costApprovalTargetRequiredToFill: getCheckboxValue('costApprovalTargetRequiredToFill'),
            costApprovalTargetActivateCompliance: getCheckboxValue('costApprovalTargetActivateCompliance'),
            costApprovalTargetComplianceDurationDays: getNumericValue('costApprovalTargetComplianceDurationDays'),
            costApprovalTargetComplianceDurationHours: getNumericValue('costApprovalTargetComplianceDurationHours'),
            costApprovalTargetComplianceDurationMinutes: getNumericValue('costApprovalTargetComplianceDurationMinutes'),
            costApprovalTargetAcknowledgeActual: getCheckboxValue('costApprovalTargetAcknowledgeActual'),
            costApprovalTargetAcknowledgeTargetChanged: getCheckboxValue('costApprovalTargetAcknowledgeTargetChanged'),
            costApprovalTargetReminderBeforeTarget: getCheckboxValue('costApprovalTargetReminderBeforeTarget'),
            costApprovalTargetReminderBeforeTargetDurationDays: getNumericValue('costApprovalTargetReminderBeforeTargetDurationDays'),
            costApprovalTargetReminderBeforeTargetDurationHours: getNumericValue('costApprovalTargetReminderBeforeTargetDurationHours'),
            costApprovalTargetReminderBeforeTargetDurationMinutes: getNumericValue('costApprovalTargetReminderBeforeTargetDurationMinutes'),

            // Work Completion Target
            workCompletionTargetDays: getNumericValue('workCompletionTargetDays'),
            workCompletionTargetHours: getNumericValue('workCompletionTargetHours'),
            workCompletionTargetMinutes: getNumericValue('workCompletionTargetMinutes'),
            workCompletionTargetWithinOfficeHours: getCheckboxValue('workCompletionTargetWithinOfficeHours'),
            workCompletionTargetReference: getValue('workCompletionTargetReference'),
            workCompletionTargetRequiredToFill: getCheckboxValue('workCompletionTargetRequiredToFill'),
            workCompletionTargetActivateCompliance: getCheckboxValue('workCompletionTargetActivateCompliance'),
            workCompletionTargetComplianceDurationDays: getNumericValue('workCompletionTargetComplianceDurationDays'),
            workCompletionTargetComplianceDurationHours: getNumericValue('workCompletionTargetComplianceDurationHours'),
            workCompletionTargetComplianceDurationMinutes: getNumericValue('workCompletionTargetComplianceDurationMinutes'),
            workCompletionTargetAcknowledgeActual: getCheckboxValue('workCompletionTargetAcknowledgeActual'),
            workCompletionTargetAcknowledgeTargetChanged: getCheckboxValue('workCompletionTargetAcknowledgeTargetChanged'),
            workCompletionTargetReminderBeforeTarget: getCheckboxValue('workCompletionTargetReminderBeforeTarget'),
            workCompletionTargetReminderBeforeTargetDurationDays: getNumericValue('workCompletionTargetReminderBeforeTargetDurationDays'),
            workCompletionTargetReminderBeforeTargetDurationHours: getNumericValue('workCompletionTargetReminderBeforeTargetDurationHours'),
            workCompletionTargetReminderBeforeTargetDurationMinutes: getNumericValue('workCompletionTargetReminderBeforeTargetDurationMinutes'),

            // After Work Follow Up Target
            afterWorkFollowUpTargetDays: getNumericValue('afterWorkFollowUpTargetDays'),
            afterWorkFollowUpTargetHours: getNumericValue('afterWorkFollowUpTargetHours'),
            afterWorkFollowUpTargetMinutes: getNumericValue('afterWorkFollowUpTargetMinutes'),
            afterWorkFollowUpTargetWithinOfficeHours: getCheckboxValue('afterWorkFollowUpTargetWithinOfficeHours'),
            afterWorkFollowUpTargetReference: getValue('afterWorkFollowUpTargetReference'),
            afterWorkFollowUpTargetRequiredToFill: getCheckboxValue('afterWorkFollowUpTargetRequiredToFill'),
            afterWorkFollowUpTargetActivateCompliance: getCheckboxValue('afterWorkFollowUpTargetActivateCompliance'),
            afterWorkFollowUpTargetComplianceDurationDays: getNumericValue('afterWorkFollowUpTargetComplianceDurationDays'),
            afterWorkFollowUpTargetComplianceDurationHours: getNumericValue('afterWorkFollowUpTargetComplianceDurationHours'),
            afterWorkFollowUpTargetComplianceDurationMinutes: getNumericValue('afterWorkFollowUpTargetComplianceDurationMinutes'),
            afterWorkFollowUpTargetAcknowledgeActual: getCheckboxValue('afterWorkFollowUpTargetAcknowledgeActual'),
            afterWorkFollowUpTargetAcknowledgeTargetChanged: getCheckboxValue('afterWorkFollowUpTargetAcknowledgeTargetChanged'),
            afterWorkFollowUpTargetReminderBeforeTarget: getCheckboxValue('afterWorkFollowUpTargetReminderBeforeTarget'),
            afterWorkFollowUpTargetReminderBeforeTargetDurationDays: getNumericValue('afterWorkFollowUpTargetReminderBeforeTargetDurationDays'),
            afterWorkFollowUpTargetReminderBeforeTargetDurationHours: getNumericValue('afterWorkFollowUpTargetReminderBeforeTargetDurationHours'),
            afterWorkFollowUpTargetReminderBeforeTargetDurationMinutes: getNumericValue('afterWorkFollowUpTargetReminderBeforeTargetDurationMinutes'),
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
            // Check if this is a searchable dropdown
            if (element._searchableDropdown) {
                // Find the option with this value to get the label text
                const option = Array.from(element.options).find(opt => opt.value == value);
                if (option) {
                    element._searchableDropdown.setValue(value, option.textContent, false);
                }
            } else {
                element.value = value || '';
            }
        }
    }

    function setCheckboxValue(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.checked = value || false;
        }
    }

    function showSuccess(message) {
        showNotification(message, 'success', 'Success');
    }

    function showError(message) {
        showNotification(message, 'error', 'Error');
    }
})();
