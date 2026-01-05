/**
 * Work Request Edit Page JavaScript
 * Handles target date calculations and form interactions for editing work requests
 */

(function () {
    'use strict';

    // Configuration
    const CONFIG = {
        apiEndpoints: {
            priorityLevels: MvcEndpoints.Helpdesk.WorkRequest.GetPriorityLevels,
            officeHours: MvcEndpoints.Helpdesk.Extended.GetOfficeHours,
            publicHolidays: MvcEndpoints.Helpdesk.Extended.GetPublicHolidays
        }
    };

    // State management
    const state = {
        priorityLevelsCache: {},
        targetCalculator: null
    };

    // Initialize page
    $(document).ready(function () {
        initializePage();
    });

    async function initializePage() {
        try {
            await loadPriorityLevels();
            setupEventListeners();
            setupDateTimePickers();
        } catch (error) {
            console.error('Error initializing page:', error);
            showNotification('Failed to initialize page', 'error', 'Error');
        }
    }

    /**
     * Setup event listeners for real-time target date calculation
     */
    function setupEventListeners() {
        // Real-time calculation triggers
        $('#requestDate, #requestTime').on('change', function () {
            triggerTargetDateCalculation();
        });

        $('#priorityLevelSelect').on('change', function () {
            triggerTargetDateCalculation();
        });

        // Target override buttons
        setupTargetOverrideListeners();
    }

    /**
     * Setup date and time pickers
     */
    function setupDateTimePickers() {
        // Set current date/time as default for request date if empty
        const now = new Date();
        const dateInput = $('#requestDate');
        const timeInput = $('#requestTime');

        if (!dateInput.val()) {
            dateInput.val(formatDateForInput(now));
        }

        if (!timeInput.val()) {
            timeInput.val(formatTimeForInput(now));
        }

        // Trigger initial calculation if priority is selected
        if ($('#priorityLevelSelect').val()) {
            triggerTargetDateCalculation();
        }
    }

    /**
     * Load priority levels from API and cache full details
     */
    async function loadPriorityLevels() {
        try {
            // Fetch priority levels list
            const listResponse = await fetch(CONFIG.apiEndpoints.priorityLevels);
            if (!listResponse.ok) {
                throw new Error(`HTTP error! status: ${listResponse.status}`);
            }

            const listData = await listResponse.json();
            if (!listData.success || !listData.data) {
                throw new Error('Failed to load priority levels');
            }

            // Fetch detailed data for each priority level in parallel
            const detailPromises = listData.data.map(async (priority) => {
                try {
                    const priorityId = priority.value || priority.id;
                    const detailResponse = await fetch(`/Helpdesk/GetPriorityLevelById?id=${priorityId}`);

                    if (!detailResponse.ok) {
                        console.error(`Failed to load details for priority ${priorityId}`);
                        return null;
                    }

                    const detailData = await detailResponse.json();
                    if (detailData.success && detailData.data) {
                        // Cache the full priority data
                        state.priorityLevelsCache[priorityId] = detailData.data;

                        // Add to dropdown
                        const option = new Option(
                            priority.text || priority.label,
                            priorityId,
                            false,
                            false
                        );
                        $('#priorityLevelSelect').append(option);

                        return detailData.data;
                    }

                    return null;
                } catch (error) {
                    console.error(`Error loading priority ${priority.value}:`, error);
                    return null;
                }
            });

            await Promise.all(detailPromises);
            console.log('Priority levels loaded and cached:', Object.keys(state.priorityLevelsCache).length);

        } catch (error) {
            console.error('Error loading priority levels:', error);
            showNotification('Failed to load priority levels', 'error', 'Error');
        }
    }

    /**
     * Trigger target date calculation
     * Fetches office hours and public holidays, then calculates all target dates
     */
    async function triggerTargetDateCalculation() {
        const requestDate = $('#requestDate').val();
        const requestTime = $('#requestTime').val();
        const priorityId = $('#priorityLevelSelect').val();

        // Validate inputs
        if (!requestDate || !requestTime || !priorityId) {
            clearAllTargets();
            return;
        }

        try {
            // Show loading state
            showCalculationLoading();

            // Fetch office hours and public holidays in parallel
            const [officeHoursResponse, publicHolidaysResponse] = await Promise.all([
                fetch(CONFIG.apiEndpoints.officeHours),
                fetch(CONFIG.apiEndpoints.publicHolidays)
            ]);

            if (!officeHoursResponse.ok || !publicHolidaysResponse.ok) {
                throw new Error('Failed to fetch calculation data');
            }

            const officeHoursData = await officeHoursResponse.json();
            const publicHolidaysData = await publicHolidaysResponse.json();

            if (!officeHoursData.success || !publicHolidaysData.success) {
                throw new Error('Invalid calculation data received');
            }

            // Get cached priority data
            const priorityData = state.priorityLevelsCache[priorityId];
            if (!priorityData) {
                throw new Error('Priority level data not found in cache');
            }

            // Initialize calculator
            state.targetCalculator = new BusinessDateCalculator(
                officeHoursData.data,
                publicHolidaysData.data
            );

            // Calculate all target dates
            calculateAndDisplayTargets(requestDate, requestTime, priorityData);

        } catch (error) {
            console.error('Error calculating target dates:', error);
            clearAllTargets();
            showNotification('Failed to calculate target dates. Please try again.', 'error', 'Calculation Error');
        }
    }

    /**
     * Calculate all target dates and display them
     */
    function calculateAndDisplayTargets(requestDate, requestTime, priorityData) {
        try {
            const startDateTime = new Date(`${requestDate}T${requestTime}`);

            // Define target types with their priority level configurations
            const targets = [
                {
                    type: 'helpdesk',
                    elementId: 'helpdeskTarget',
                    days: priorityData.helpdeskResponseDay,
                    hours: priorityData.helpdeskResponseHour,
                    minutes: priorityData.helpdeskResponseMinute,
                    withinOfficeHours: priorityData.helpdeskResponseWithinOfficeHour
                },
                {
                    type: 'initialFollowUp',
                    elementId: 'initialFollowUpTarget',
                    days: priorityData.initialFollowUpDay,
                    hours: priorityData.initialFollowUpHour,
                    minutes: priorityData.initialFollowUpMinute,
                    withinOfficeHours: priorityData.initialFollowUpWithinOfficeHour
                },
                {
                    type: 'quotation',
                    elementId: 'quotationTarget',
                    days: priorityData.quotationSubmissionDay,
                    hours: priorityData.quotationSubmissionHour,
                    minutes: priorityData.quotationSubmissionMinute,
                    withinOfficeHours: priorityData.quotationSubmissionWithinOfficeHour
                },
                {
                    type: 'costApproval',
                    elementId: 'costApprovalTarget',
                    days: priorityData.costApprovalDay,
                    hours: priorityData.costApprovalHour,
                    minutes: priorityData.costApprovalMinute,
                    withinOfficeHours: priorityData.costApprovalWithinOfficeHour
                },
                {
                    type: 'workCompletion',
                    elementId: 'workCompletionTarget',
                    days: priorityData.workCompletionDay,
                    hours: priorityData.workCompletionHour,
                    minutes: priorityData.workCompletionMinute,
                    withinOfficeHours: priorityData.workCompletionWithinOfficeHour
                },
                {
                    type: 'afterWork',
                    elementId: 'afterWorkTarget',
                    days: priorityData.afterWorkFollowUpDay,
                    hours: priorityData.afterWorkFollowUpHour,
                    minutes: priorityData.afterWorkFollowUpMinute,
                    withinOfficeHours: priorityData.afterWorkFollowUpWithinOfficeHour
                }
            ];

            // Calculate and display each target
            targets.forEach(target => {
                if (target.days > 0 || target.hours > 0 || target.minutes > 0) {
                    const targetDate = state.targetCalculator.calculateTargetDate(
                        new Date(startDateTime),
                        target.days,
                        target.hours,
                        target.minutes,
                        target.withinOfficeHours
                    );

                    const tooltipText = buildTooltipText(
                        target.days,
                        target.hours,
                        target.minutes,
                        target.withinOfficeHours
                    );

                    updateTargetDateDisplay(target.elementId, targetDate, tooltipText);
                } else {
                    updateTargetDateDisplay(target.elementId, null, 'No target configured');
                }
            });

        } catch (error) {
            console.error('Error in calculateAndDisplayTargets:', error);
            throw error;
        }
    }

    /**
     * Build tooltip text for target date (matching legacy format)
     */
    function buildTooltipText(days, hours, minutes, withinOfficeHours) {
        const parts = [];

        if (days > 0) {
            parts.push(`${days} day${days > 1 ? 's' : ''}`);
        }
        if (hours > 0) {
            parts.push(`${hours} hour${hours > 1 ? 's' : ''}`);
        }
        if (minutes > 0) {
            parts.push(`${minutes} minute${minutes > 1 ? 's' : ''}`);
        }

        let tooltip = parts.join(', ');

        if (withinOfficeHours) {
            tooltip += ' (within office hours)';
        } else {
            tooltip += ' (24/7)';
        }

        return tooltip;
    }

    /**
     * Update target date display
     */
    function updateTargetDateDisplay(elementId, targetDate, tooltip) {
        const element = $(`#${elementId}`);
        const dateSpan = element.find('.target-date');

        if (targetDate) {
            dateSpan.text(formatDisplayDate(targetDate));
            element.attr('title', tooltip);
            element.removeClass('text-muted');
        } else {
            dateSpan.text(tooltip || '-');
            element.attr('title', '');
            element.addClass('text-muted');
        }
    }

    /**
     * Format date for display: "dd MMM yyyy hh:mm tt"
     * Example: "05 Jan 2026 02:30 PM"
     */
    function formatDisplayDate(date) {
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

        const day = String(date.getDate()).padStart(2, '0');
        const month = months[date.getMonth()];
        const year = date.getFullYear();

        let hours = date.getHours();
        const minutes = String(date.getMinutes()).padStart(2, '0');
        const ampm = hours >= 12 ? 'PM' : 'AM';
        hours = hours % 12 || 12;

        return `${day} ${month} ${year} ${String(hours).padStart(2, '0')}:${minutes} ${ampm}`;
    }

    /**
     * Clear all target displays
     */
    function clearAllTargets() {
        const targets = [
            'helpdeskTarget',
            'initialFollowUpTarget',
            'quotationTarget',
            'costApprovalTarget',
            'workCompletionTarget',
            'afterWorkTarget'
        ];

        targets.forEach(targetId => {
            updateTargetDateDisplay(targetId, null, '-');
        });
    }

    /**
     * Show loading state during calculation
     */
    function showCalculationLoading() {
        const targets = [
            'helpdeskTarget',
            'initialFollowUpTarget',
            'quotationTarget',
            'costApprovalTarget',
            'workCompletionTarget',
            'afterWorkTarget'
        ];

        targets.forEach(targetId => {
            updateTargetDateDisplay(targetId, null, 'Calculating...');
        });
    }

    /**
     * Setup target override listeners (edit mode)
     */
    function setupTargetOverrideListeners() {
        // Click on target to show edit form
        $('.target-time-display').on('click', function () {
            const targetType = $(this).data('target-type');
            showTargetEditForm(targetType);
        });
    }

    /**
     * Show target edit form
     */
    function showTargetEditForm(targetType) {
        $(`#${targetType}Target`).hide();
        $(`#${targetType}TargetForm`).show();
    }

    /**
     * Save target override
     */
    window.saveTarget = function (targetType) {
        const newTarget = $(`#${targetType}NewTarget`).val();
        const remark = $(`#${targetType}Remark`).val();

        if (!newTarget) {
            showNotification('Please select a new target date', 'warning', 'Validation');
            return;
        }

        // Update display
        const targetDate = new Date(newTarget);
        updateTargetDateDisplay(`${targetType}Target`, targetDate, remark || 'Manual override');

        // Store remark in hidden field
        $(`#${targetType}RemarkDisplay`).val(remark);

        // Hide form
        hideTargetEditForm(targetType);

        showNotification('Target date updated', 'success', 'Success');
    };

    /**
     * Cancel target override
     */
    window.cancelTarget = function (targetType) {
        hideTargetEditForm(targetType);
    };

    /**
     * Hide target edit form
     */
    function hideTargetEditForm(targetType) {
        $(`#${targetType}TargetForm`).hide();
        $(`#${targetType}Target`).show();

        // Clear inputs
        $(`#${targetType}NewTarget`).val('');
        $(`#${targetType}Remark`).val('');
    }

    /**
     * Format date for input (YYYY-MM-DD)
     */
    function formatDateForInput(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    /**
     * Format time for input (HH:MM)
     */
    function formatTimeForInput(date) {
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        return `${hours}:${minutes}`;
    }

})();
