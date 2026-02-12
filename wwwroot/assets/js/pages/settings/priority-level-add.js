// Priority Level Add Page JavaScript
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

    async function loadDropdownData() {
        // Hardcode Helpdesk Response Target Reference (id: 172, "After Request Date")
        const helpdeskSelect = document.getElementById('helpdeskResponseTargetReference');
        if (helpdeskSelect) {
            const opt = document.createElement('option');
            opt.value = '172';
            opt.textContent = 'After Request Date';
            helpdeskSelect.appendChild(opt);

            // Auto-select the only option
            helpdeskSelect.value = '172';

            // Update searchable dropdown if present
            if (helpdeskSelect._searchableDropdown) {
                helpdeskSelect._searchableDropdown.loadFromSelect();
                helpdeskSelect._searchableDropdown.setValue('172', 'After Request Date', false);
            }
        }

        // Load visual colors from GetEnums
        await loadEnumDropdown('visualColor', 'visualColor');

        // Load reference dropdowns from GetEnums
        for (const dropdown of enumDropdowns) {
            await loadEnumDropdown(dropdown.id, dropdown.category);
        }
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

                    // Auto-select first option if data exists
                    if (result.data.length > 0 && select.value === '') {
                        const firstOption = result.data[0];
                        select.value = firstOption.idEnum;

                        // Update searchable dropdown if present
                        if (select._searchableDropdown) {
                            select._searchableDropdown.setValue(firstOption.idEnum, firstOption.enumName, false);
                        }
                    }
                }
            }
        } catch (error) {
            console.error(`Error loading enum dropdown for ${selectId}:`, error);
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

        // Get CSRF token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        try {
            const response = await fetch(MvcEndpoints.Helpdesk.Settings.PriorityLevel.Create, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
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

    function showSuccess(message) {
        showNotification(message, 'success', 'Success');
    }

    function showError(message) {
        showNotification(message, 'error', 'Error');
    }
})();
