/**
 * Maintenance Management Calendar Page
 * Handles calendar visualization, activity list, and schedule interactions
 */

(function ($) {
    'use strict';

    // Configuration
    const CONFIG = {
        endpoints: {
            activities: MvcEndpoints.PreventiveMaintenance.GetActivities,
            schedules: MvcEndpoints.PreventiveMaintenance.GetSchedules,
            tooltip: MvcEndpoints.PreventiveMaintenance.GetScheduleTooltip,
            scheduleDetail: MvcEndpoints.PreventiveMaintenance.ScheduleDetail
        },
        calendar: {
            weeksPerYear: 52,
            daysPerWeek: 7
        }
    };

    // Client context for multi-tab session safety
    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; },
        get fromDate() { return window.PageContext?.fromDate || new Date().toISOString().split('T')[0]; },
        get viewMode() { return window.PageContext?.viewMode || '52-Week View'; }
    };

    // State management
    const state = {
        activities: [],
        schedules: [],
        filters: {
            propertyGroupId: null,
            buildingId: null,
            fromDate: clientContext.fromDate,
            viewMode: clientContext.viewMode
        },
        calendar: {
            weeks: [],
            startDate: null,
            endDate: null
        },
        ui: {
            tooltipTimeout: null,
            currentTooltipScheduleId: null
        }
    };

    /**
     * Initialize the module
     */
    function init() {
        console.log('Maintenance Management page initialized');
        console.log('Client ID:', clientContext.idClient);

        initializeDropdowns();
        bindEvents();
        loadInitialData();
        initializeSessionMonitor();
    }

    /**
     * Initialize searchable dropdowns
     */
    function initializeDropdowns() {
        // Initialize specific dropdowns manually (NOT using data-searchable attribute)
        const dropdownConfigs = [
            {
                selector: '#propertyGroupFilter',
                options: {
                    placeholder: 'All property groups',
                    searchPlaceholder: 'Search property groups...',
                    allowClear: true
                }
            },
            {
                selector: '#buildingFilter',
                options: {
                    placeholder: 'All buildings',
                    searchPlaceholder: 'Search buildings...',
                    allowClear: true
                }
            }
        ];

        dropdownConfigs.forEach(config => {
            const element = document.querySelector(config.selector);

            // Guard against double initialization
            if (element && !element._searchableDropdown) {
                new SearchableDropdown(element, config.options);
            } else if (element && element._searchableDropdown) {
                console.warn(`SearchableDropdown already initialized on ${config.selector}`);
            }
        });
    }

    /**
     * Bind event handlers
     */
    function bindEvents() {
        // Apply Filters button
        $('#applyFiltersBtn').on('click', applyFilters);

        // Add New Activity buttons
        $('#addNewActivityBtn, #addActivityFromEmpty').on('click', function () {
            // TODO: Navigate to add activity page or open modal
            showNotification('Add Activity functionality will be implemented when backend is ready', 'info', 'Coming Soon');
        });

        // Freeze Calendar button
        $('#freezeCalendarBtn').on('click', function () {
            showNotification('Freeze Calendar functionality will be implemented when backend is ready', 'info', 'Coming Soon');
        });

        // Property Group filter change
        $('#propertyGroupFilter').on('change', function () {
            const propertyGroupId = $(this).val();
            // TODO: Filter buildings by property group
            console.log('Property group changed:', propertyGroupId);
        });

        // Horizontal scroll synchronization
        $('.calendar-grid-column').on('scroll', function () {
            const scrollLeft = $(this).scrollLeft();
            $('.calendar-grid-header').scrollLeft(scrollLeft);
        });
    }

    /**
     * Apply filters and reload calendar
     */
    function applyFilters() {
        state.filters.propertyGroupId = $('#propertyGroupFilter').val() || null;
        state.filters.buildingId = $('#buildingFilter').val() || null;
        state.filters.fromDate = $('#fromDatePicker').val();
        state.filters.viewMode = $('#viewModeSelect').val();

        loadCalendarData();
    }

    /**
     * Load initial data
     */
    function loadInitialData() {
        state.filters.fromDate = $('#fromDatePicker').val();
        calculateCalendarRange();
        loadCalendarData();
    }

    /**
     * Calculate calendar date range based on view mode
     */
    function calculateCalendarRange() {
        const fromDate = new Date(state.filters.fromDate);
        state.calendar.startDate = fromDate;

        if (state.filters.viewMode === '52-Week View') {
            // Calculate 52 weeks from start date
            state.calendar.endDate = new Date(fromDate);
            state.calendar.endDate.setDate(fromDate.getDate() + (CONFIG.calendar.weeksPerYear * CONFIG.calendar.daysPerWeek));
        } else {
            // Monthly view - 12 months
            state.calendar.endDate = new Date(fromDate);
            state.calendar.endDate.setMonth(fromDate.getMonth() + 12);
        }

        generateWeeksArray();
    }

    /**
     * Generate weeks array for calendar headers
     */
    function generateWeeksArray() {
        state.calendar.weeks = [];
        const currentDate = new Date(state.calendar.startDate);
        let weekNumber = 1;

        while (currentDate < state.calendar.endDate) {
            const weekStart = new Date(currentDate);
            const weekEnd = new Date(currentDate);
            weekEnd.setDate(currentDate.getDate() + 6);

            state.calendar.weeks.push({
                number: weekNumber,
                start: new Date(weekStart),
                end: weekEnd > state.calendar.endDate ? state.calendar.endDate : new Date(weekEnd),
                month: weekStart.toLocaleString('default', { month: 'short' }),
                year: weekStart.getFullYear()
            });

            currentDate.setDate(currentDate.getDate() + 7);
            weekNumber++;
        }
    }

    /**
     * Load calendar data (activities and schedules)
     */
    async function loadCalendarData() {
        showLoading(true);

        try {
            // Load activities
            await loadActivities();

            // Load schedules
            await loadSchedules();

            // Render calendar
            renderCalendar();

            // Update summary
            updateSummary();

            showLoading(false);
        } catch (error) {
            console.error('Error loading calendar data:', error);
            showNotification('Failed to load calendar data', 'error', 'Error');
            showLoading(false);
            showEmptyState(true);
        }
    }

    /**
     * Load maintenance activities
     */
    function loadActivities() {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: CONFIG.endpoints.activities,
                method: 'GET',
                data: {
                    propertyGroupId: state.filters.propertyGroupId,
                    buildingId: state.filters.buildingId,
                    fromDate: state.filters.fromDate,
                    page: 1,
                    limit: 100
                },
                success: function (response) {
                    if (response.success && response.data) {
                        state.activities = response.data.items || [];
                        console.log('Loaded activities:', state.activities.length);
                        resolve();
                    } else {
                        reject(new Error(response.message || 'Failed to load activities'));
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Load activities error:', error);
                    reject(error);
                }
            });
        });
    }

    /**
     * Load maintenance schedules
     */
    function loadSchedules() {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: CONFIG.endpoints.schedules,
                method: 'GET',
                data: {
                    fromDate: state.calendar.startDate.toISOString().split('T')[0],
                    toDate: state.calendar.endDate.toISOString().split('T')[0],
                    propertyGroupId: state.filters.propertyGroupId,
                    buildingId: state.filters.buildingId
                },
                success: function (response) {
                    if (response.success && response.data) {
                        state.schedules = response.data || [];
                        console.log('Loaded schedules:', state.schedules.length);
                        resolve();
                    } else {
                        reject(new Error(response.message || 'Failed to load schedules'));
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Load schedules error:', error);
                    reject(error);
                }
            });
        });
    }

    /**
     * Render unified Gantt calendar
     */
    function renderCalendar() {
        if (state.activities.length === 0) {
            showEmptyState(true);
            return;
        }

        showEmptyState(false);
        renderGanttHeader();
        renderGanttRows();
    }

    /**
     * Render table header (weeks/months)
     */
    function renderGanttHeader() {
        const $thead = $('#ganttTableHead');
        $thead.empty();

        let currentMonth = '';
        let monthColspan = 0;
        const monthGroups = [];

        // Group weeks by month
        state.calendar.weeks.forEach((week, index) => {
            const monthYear = `${week.month} ${week.year}`;
            if (monthYear !== currentMonth) {
                if (monthColspan > 0) {
                    monthGroups.push({ label: currentMonth, colspan: monthColspan });
                }
                currentMonth = monthYear;
                monthColspan = 1;
            } else {
                monthColspan++;
            }
        });

        // Add last month
        if (monthColspan > 0) {
            monthGroups.push({ label: currentMonth, colspan: monthColspan });
        }

        // Month header row
        const $monthRow = $('<tr>').addClass('month-header-row');

        // Corner cell (sticky both top and left)
        $monthRow.append($('<th>').addClass('corner-cell').text(''));

        // Month cells
        monthGroups.forEach(group => {
            $monthRow.append(
                $('<th>').addClass('month-cell')
                    .attr('colspan', group.colspan)
                    .text(group.label)
            );
        });

        // Week header row
        const $weekRow = $('<tr>').addClass('week-header-row');

        // Activity info header (sticky left)
        const $activityHeader = $('<th>').addClass('activity-info-header').html(`
            <div style="display: flex; align-items: center; padding: 0 0.75rem;">
                <div style="width: 50px; text-align: center;">No.</div>
                <div style="flex: 2; padding: 0 0.5rem;">Activity</div>
                <div style="flex: 2; padding: 0 0.5rem;">Location</div>
                <div style="width: 100px; padding: 0 0.5rem;">Every</div>
                <div style="flex: 1.5; padding: 0 0.5rem;">Service Provider</div>
                <div style="width: 90px; text-align: center;">Actions</div>
            </div>
        `);
        $weekRow.append($activityHeader);

        // Week number headers
        state.calendar.weeks.forEach(week => {
            $weekRow.append(
                $('<th>').addClass('week-header')
                    .text(week.number)
                    .attr('data-week', week.number)
            );
        });

        $thead.append($monthRow, $weekRow);
    }

    /**
     * Render table body rows (activity info + calendar cells)
     */
    function renderGanttRows() {
        const $tbody = $('#ganttTableBody');
        $tbody.empty();

        state.activities.forEach((activity, index) => {
            // Create table row
            const $row = $('<tr>').attr('data-activity-id', activity.activityId);

            // Activity info cell (sticky left)
            const $activityCell = $('<td>').addClass('activity-info-cell').html(`
                <div style="display: flex; align-items: center; padding: 0.75rem;">
                    <div style="width: 50px; text-align: center;">${index + 1}</div>
                    <div style="flex: 2; padding: 0 0.5rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" title="${activity.activityName}">${activity.activityName}</div>
                    <div style="flex: 2; padding: 0 0.5rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" title="${activity.location}">${activity.location}</div>
                    <div style="width: 100px; padding: 0 0.5rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${activity.frequency}</div>
                    <div style="flex: 1.5; padding: 0 0.5rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" title="${activity.serviceProvider}">${activity.serviceProvider}</div>
                    <div style="width: 90px; text-align: center;">
                        <button class="btn btn-sm btn-primary btn-edit-activity" data-id="${activity.activityId}" title="Edit">
                            <i class="ti ti-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-danger btn-delete-activity" data-id="${activity.activityId}" title="Delete">
                            <i class="ti ti-trash"></i>
                        </button>
                    </div>
                </div>
            `);

            $row.append($activityCell);

            // Create week cells for this activity
            state.calendar.weeks.forEach(week => {
                const $weekCell = $('<td>')
                    .addClass('week-cell')
                    .attr('data-week', week.number);
                $row.append($weekCell);
            });

            $tbody.append($row);
        });

        // Render schedule tiles into the week cells
        renderTilesIntoWeekCells();

        // Bind action buttons
        $('.btn-edit-activity').on('click', function () {
            const activityId = $(this).data('id');
            showNotification('Edit activity functionality will be implemented when backend is ready', 'info', 'Coming Soon');
        });

        $('.btn-delete-activity').on('click', function () {
            const activityId = $(this).data('id');
            showNotification('Delete activity functionality will be implemented when backend is ready', 'info', 'Coming Soon');
        });
    }

    /**
     * Render schedule tiles into the correct week cells
     */
    function renderTilesIntoWeekCells() {
        state.schedules.forEach(schedule => {
            // Calculate which week this schedule belongs to
            const weekIndex = calculateTilePosition(schedule.plannedStartDate);

            if (weekIndex >= 0 && weekIndex < state.calendar.weeks.length) {
                // Find the row for this activity
                const $row = $(`tr[data-activity-id="${schedule.activityId}"]`);

                if ($row.length === 0) {
                    console.warn(`No row found for activity ${schedule.activityId}`);
                    return;
                }

                // Find the week cell (skip first cell which is activity info, so weekIndex + 1)
                const $targetCell = $row.find('td.week-cell').eq(weekIndex);

                if ($targetCell.length === 0) {
                    console.warn(`No week cell found at index ${weekIndex} for activity ${schedule.activityId}`);
                    return;
                }

                // Create and append the tile
                const $tile = createScheduleTile(schedule);
                $targetCell.append($tile);
            } else {
                console.warn(`Schedule ${schedule.scheduleId} date ${schedule.plannedStartDate} is outside calendar range (weekIndex: ${weekIndex})`);
            }
        });
    }

    /**
     * Calculate tile position (week index) - finds which week the date falls into
     */
    function calculateTilePosition(date) {
        const scheduleDate = new Date(date);
        scheduleDate.setHours(0, 0, 0, 0); // Normalize to start of day

        // Find the week that contains this date
        for (let i = 0; i < state.calendar.weeks.length; i++) {
            const week = state.calendar.weeks[i];
            const weekStart = new Date(week.start);
            const weekEnd = new Date(week.end);

            weekStart.setHours(0, 0, 0, 0);
            weekEnd.setHours(23, 59, 59, 999);

            // Check if schedule date falls within this week
            if (scheduleDate >= weekStart && scheduleDate <= weekEnd) {
                console.log(`Schedule ${scheduleDate.toDateString()} falls in week ${i + 1} (${weekStart.toDateString()} - ${weekEnd.toDateString()})`);
                return i;
            }
        }

        // Date not found in any week (outside calendar range)
        console.warn(`Schedule date ${scheduleDate.toDateString()} is outside calendar range`);
        return -1;
    }

    /**
     * Create schedule tile element
     */
    function createScheduleTile(schedule) {
        const statusClass = getStatusClass(schedule.status);
        const dayOfMonth = new Date(schedule.plannedStartDate).getDate();

        const $tile = $('<div>')
            .addClass('schedule-tile')
            .addClass(statusClass)
            .attr('data-schedule-id', schedule.scheduleId)
            .text(dayOfMonth);

        // Hover handler - pass the tile element for positioning
        $tile.on('mouseenter', function () {
            showScheduleTooltip(schedule.scheduleId, this);
        });

        $tile.on('mouseleave', function () {
            hideScheduleTooltip();
        });

        // Click handler - navigate to detail page
        $tile.on('click', function () {
            window.location.href = `${CONFIG.endpoints.scheduleDetail}/${schedule.scheduleId}`;
        });

        return $tile;
    }

    /**
     * Get status CSS class
     */
    function getStatusClass(status) {
        const statusLower = (status || 'new').toLowerCase();
        switch (statusLower) {
            case 'new':
                return 'status-new';
            case 'in progress':
                return 'status-in-progress';
            case 'completed':
                return 'status-completed';
            case 'overdue':
                return 'status-overdue';
            default:
                return 'status-new';
        }
    }

    /**
     * Show schedule tooltip on hover
     */
    function showScheduleTooltip(scheduleId, tileElement) {
        // Clear previous timeout
        if (state.ui.tooltipTimeout) {
            clearTimeout(state.ui.tooltipTimeout);
        }

        // Set new timeout for smooth UX
        state.ui.tooltipTimeout = setTimeout(() => {
            loadAndShowTooltip(scheduleId, tileElement);
        }, 300);
    }

    /**
     * Load and show tooltip data
     */
    function loadAndShowTooltip(scheduleId, tileElement) {
        state.ui.currentTooltipScheduleId = scheduleId;

        const $tooltip = $('#scheduleTooltip');
        const $loading = $('#tooltipLoading');
        const $data = $('#tooltipData');

        // Position tooltip relative to tile
        positionTooltip(tileElement);

        // Show loading state
        $loading.show();
        $data.hide();
        $tooltip.fadeIn(200);

        // Load tooltip data
        $.ajax({
            url: CONFIG.endpoints.tooltip,
            method: 'GET',
            data: { id: scheduleId },
            success: function (response) {
                if (response.success && response.data) {
                    // Only show if still hovering same tile
                    if (state.ui.currentTooltipScheduleId === scheduleId) {
                        displayTooltipData(response.data);
                    }
                } else {
                    console.error('Failed to load tooltip:', response.message);
                    hideScheduleTooltip();
                }
            },
            error: function (xhr, status, error) {
                console.error('Tooltip AJAX error:', error);
                hideScheduleTooltip();
            }
        });
    }

    /**
     * Display tooltip data
     */
    function displayTooltipData(data) {
        $('#tooltipCode').text(data.scheduleCode);
        $('#tooltipPlannedDate').text(data.plannedDate);
        $('#tooltipActualDate').text(data.actualDate);

        const statusClass = getStatusClass(data.status);
        $('#tooltipStatus')
            .removeClass('badge-primary badge-warning badge-success badge-danger')
            .addClass(statusClass.replace('status-', 'badge-'))
            .text(data.status);

        $('#tooltipLoading').hide();
        $('#tooltipData').fadeIn(200);
    }

    /**
     * Position tooltip relative to tile element
     */
    function positionTooltip(tileElement) {
        const $tooltip = $('#scheduleTooltip');
        const tooltip = $tooltip[0];

        // Get tile position relative to viewport
        const tileRect = tileElement.getBoundingClientRect();

        // Get tooltip dimensions (needs to be visible to measure)
        $tooltip.css({ visibility: 'hidden', display: 'block' });
        const tooltipRect = tooltip.getBoundingClientRect();
        $tooltip.css({ visibility: 'visible', display: 'none' });

        const tooltipWidth = tooltipRect.width || 300;
        const tooltipHeight = tooltipRect.height || 150;
        const offset = 8;
        const arrowSize = 8;

        let left, top;
        let arrowPosition = 'bottom'; // Default: arrow points down (tooltip above tile)

        // Calculate position - try to show above tile first
        top = tileRect.top + window.scrollY - tooltipHeight - arrowSize - offset;
        left = tileRect.left + window.scrollX + (tileRect.width / 2) - (tooltipWidth / 2);

        // Check if tooltip goes off top edge - show below tile instead
        if (top < window.scrollY) {
            top = tileRect.bottom + window.scrollY + arrowSize + offset;
            arrowPosition = 'top';
        }

        // Check if tooltip goes off left edge
        if (left < 0) {
            left = offset;
        }

        // Check if tooltip goes off right edge
        if (left + tooltipWidth > window.innerWidth) {
            left = window.innerWidth - tooltipWidth - offset;
        }

        // Adjust arrow position based on tile center
        const tileCenterX = tileRect.left + (tileRect.width / 2);
        const arrowLeft = tileCenterX - left;

        // Set tooltip position
        $tooltip.css({
            left: left + 'px',
            top: top + 'px'
        });

        // Position arrow
        const $arrow = $tooltip.find('.tooltip-arrow');
        $arrow.css({ left: arrowLeft + 'px' });

        // Rotate arrow based on position
        if (arrowPosition === 'top') {
            $arrow.css({ transform: 'rotate(180deg)', top: '-8px', bottom: 'auto' });
        } else {
            $arrow.css({ transform: 'rotate(0deg)', bottom: '-8px', top: 'auto' });
        }
    }

    /**
     * Hide schedule tooltip
     */
    function hideScheduleTooltip() {
        if (state.ui.tooltipTimeout) {
            clearTimeout(state.ui.tooltipTimeout);
        }

        state.ui.currentTooltipScheduleId = null;
        $('#scheduleTooltip').fadeOut(200);
    }

    /**
     * Update summary information and stats cards
     */
    function updateSummary() {
        const totalActivities = state.activities.length;
        const totalSchedules = state.schedules.length;

        // Calculate stats
        const schedulesThisWeek = calculateSchedulesThisWeek();
        const overdueCount = state.schedules.filter(s => s.status.toLowerCase() === 'overdue').length;
        const completedCount = state.schedules.filter(s => s.status.toLowerCase() === 'completed').length;
        const completionRate = totalSchedules > 0 ? Math.round((completedCount / totalSchedules) * 100) : 0;

        // Update stats cards
        $('#statTotalActivities').text(totalActivities);
        $('#statSchedulesThisWeek').text(schedulesThisWeek);
        $('#statOverdue').text(overdueCount);
        $('#statCompletionRate').text(completionRate);
    }

    /**
     * Calculate schedules for current week
     */
    function calculateSchedulesThisWeek() {
        const now = new Date();
        const weekStart = new Date(now);
        weekStart.setDate(now.getDate() - now.getDay()); // Start of week (Sunday)
        weekStart.setHours(0, 0, 0, 0);

        const weekEnd = new Date(weekStart);
        weekEnd.setDate(weekStart.getDate() + 6); // End of week (Saturday)
        weekEnd.setHours(23, 59, 59, 999);

        return state.schedules.filter(schedule => {
            const scheduleDate = new Date(schedule.plannedStartDate);
            return scheduleDate >= weekStart && scheduleDate <= weekEnd;
        }).length;
    }

    /**
     * Show/hide loading spinner
     */
    function showLoading(show) {
        if (show) {
            $('#calendarLoadingSpinner').show();
            $('#calendarWrapper').hide();
            $('#emptyState').hide();
        } else {
            $('#calendarLoadingSpinner').hide();
            $('#calendarWrapper').show();
        }
    }

    /**
     * Show/hide empty state
     */
    function showEmptyState(show) {
        if (show) {
            $('#calendarWrapper').hide();
            $('#emptyState').show();
        } else {
            $('#emptyState').hide();
            $('#calendarWrapper').show();
        }
    }

    /**
     * Initialize client session monitor
     */
    function initializeSessionMonitor() {
        // Initialize client session monitor for multi-tab safety
        const monitor = new ClientSessionMonitor({
            pageLoadClientId: clientContext.idClient,
            checkEndpoint: '/Helpdesk/CheckSessionClient',
            onMismatch: function (currentClientId) {
                showNotification(
                    'Your client context has changed. This page will reload to reflect the new context.',
                    'warning',
                    'Client Context Changed'
                );
                setTimeout(() => location.reload(), 2000);
            }
        });

        monitor.start();
    }

    // Initialize on document ready
    $(document).ready(function () {
        init();
    });

})(jQuery);
