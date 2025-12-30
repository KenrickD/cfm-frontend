// Priority Level Detail Page JavaScript
(function () {
    'use strict';

    let priorityLevelId = null;

    // Initialize on DOM load
    document.addEventListener('DOMContentLoaded', function () {
        getPriorityLevelIdFromUrl();
        initializeButtons();
        loadPriorityLevel();
    });

    function getPriorityLevelIdFromUrl() {
        const urlParams = new URLSearchParams(window.location.search);
        priorityLevelId = urlParams.get('id');
    }

    function initializeButtons() {
        const editButton = document.getElementById('editButton');
        if (editButton && priorityLevelId) {
            editButton.href = `/Helpdesk/PriorityLevelEdit?id=${priorityLevelId}`;
        }
    }

    async function loadPriorityLevel() {
        if (!priorityLevelId) {
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
        // Name and Visual Color
        setText('name', data.name);

        const visualColorBadge = document.getElementById('visualColorBadge');
        if (visualColorBadge && data.visualColor) {
            visualColorBadge.textContent = data.visualColor;
            visualColorBadge.style.backgroundColor = getColorStyle(data.visualColor);
            visualColorBadge.style.color = getTextColorForBackground(data.visualColor);
        }

        // Populate each section
        populateTargetSection('helpdeskResponse', data, 'helpdeskResponseTarget');
        populateTargetSection('quotationSubmission', data, 'quotationSubmissionTarget');
        populateTargetSection('workCompletion', data, 'workCompletionTarget');
        populateTargetSection('initialFollowUp', data, 'initialFollowUpTarget');
        populateTargetSection('costApproval', data, 'costApprovalTarget');
        populateTargetSection('afterWorkFollowUp', data, 'afterWorkFollowUpTarget');
    }

    function populateTargetSection(prefix, data, targetKey) {
        const camelPrefix = toCamelCase(targetKey);

        // Set target duration
        const days = data[`${camelPrefix}Days`] || 0;
        const hours = data[`${camelPrefix}Hours`] || 0;
        const minutes = data[`${camelPrefix}Minutes`] || 0;
        const duration = formatDuration(days, hours, minutes);
        const withinOfficeHours = data[`${camelPrefix}WithinOfficeHours`];

        setText(`${prefix}Target`, duration + (withinOfficeHours ? ' Within Office Hours' : ''));

        // Set reference
        setText(`${prefix}Reference`, formatReference(data[`${camelPrefix}Reference`]));

        // Set checkboxes
        const checkboxContainer = document.getElementById(`${prefix}Checkboxes`);
        if (checkboxContainer) {
            const checkboxes = [];

            if (data[`${camelPrefix}RequiredToFill`]) {
                checkboxes.push(createCheckboxItem('Required to Fill on Work Request Completion'));
            }
            if (data[`${camelPrefix}ActivateCompliance`]) {
                checkboxes.push(createCheckboxItem('Activate Compliance Duration'));
            }
            if (data[`${camelPrefix}AcknowledgeActual`]) {
                checkboxes.push(createCheckboxItem('Acknowledge Requestor when The Actual already filled'));
            }
            if (data[`${camelPrefix}AcknowledgeTargetChanged`]) {
                checkboxes.push(createCheckboxItem('Acknowledge Requestor when The Target Changed'));
            }
            if (data[`${camelPrefix}ReminderBeforeTarget`]) {
                checkboxes.push(createCheckboxItem('Reminder Before Target'));
            }

            // Special checkbox for After Work Follow Up
            if (prefix === 'afterWorkFollowUp' && data.afterWorkFollowUpTargetActivateAutoFill) {
                checkboxes.push(createCheckboxItem('Activate Auto Fill After Work Follow Up'));
            }

            checkboxContainer.innerHTML = checkboxes.join('');
        }

        // Special handling for Helpdesk Response compliance duration
        if (prefix === 'helpdeskResponse' && data.helpdeskResponseTargetActivateCompliance) {
            const complianceDiv = document.getElementById('helpdeskComplianceDurationDisplay');
            if (complianceDiv) {
                complianceDiv.style.display = 'block';
                const compDuration = formatDuration(
                    data.helpdeskResponseTargetComplianceDurationDays,
                    data.helpdeskResponseTargetComplianceDurationHours,
                    data.helpdeskResponseTargetComplianceDurationMinutes
                );
                setText('helpdeskComplianceDuration', compDuration);
            }
        }
    }

    function createCheckboxItem(text) {
        return `<div class="checkbox-item"><i class="ti ti-check"></i><span>${text}</span></div>`;
    }

    function toCamelCase(str) {
        return str.charAt(0).toLowerCase() + str.slice(1);
    }

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value || '-';
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
        // Convert camelCase to Title Case (e.g., "afterRequestDate" -> "After Request Date")
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

    function getTextColorForBackground(colorName) {
        const darkColors = ['Red', 'Purple', 'Blue', 'Brown', 'Gray'];
        return darkColors.includes(colorName) ? '#ffffff' : '#000000';
    }

    function showError(message) {
        showNotification(message, 'error', 'Error');
    }
})();
