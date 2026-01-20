/**
 * BusinessDateCalculator
 * Calculates target dates based on priority level configurations, office hours, and public holidays
 *
 * Ported from legacy ASP.NET 4.5 SharedFunctions.addDateBasedOnOfficeHour method
 *
 * @class
 */
class BusinessDateCalculator {
    /**
     * Creates a new BusinessDateCalculator instance
     * @param {Array} officeHours - Array of office hour objects from backend API
     * @param {Array} publicHolidays - Array of public holiday objects from backend API
     */
    constructor(officeHours, publicHolidays) {
        this.officeHours = officeHours || [];
        this.publicHolidays = publicHolidays || [];
        this.prepareData();
    }

    /**
     * Prepares and indexes data for efficient lookup
     * Sorts office hours and creates public holiday lookup map
     */
    prepareData() {
        // Sort office hours by day and then by time
        this.officeHours.sort((a, b) => {
            if (a.officeDay !== b.officeDay) {
                return a.officeDay - b.officeDay;
            }
            return this.parseTimeToMinutes(a.fromHour) - this.parseTimeToMinutes(b.fromHour);
        });

        // Create public holiday lookup map for O(1) checking
        this.holidayMap = new Map();
        this.publicHolidays.forEach(holiday => {
            const key = this.formatDateKey(new Date(holiday.date));
            this.holidayMap.set(key, true);
        });
    }

    /**
     * Main calculation method - calculates target date based on duration and office hours
     * Port of C# SharedFunctions.addDateBasedOnOfficeHour
     *
     * @param {Date} startDate - Starting date/time (Request Date)
     * @param {number} days - Number of days from priority level configuration
     * @param {number} hours - Number of hours from priority level configuration
     * @param {number} minutes - Number of minutes from priority level configuration
     * @param {boolean} isWithinOfficeHours - Whether to respect office hours and holidays
     * @returns {Date} Calculated target date/time
     */
    calculateTargetDate(startDate, days, hours, minutes, isWithinOfficeHours) {
        let resultDate = new Date(startDate);
        let remainingMinutes = this.getTotalMinutes(days, hours, minutes);

        if (!isWithinOfficeHours) {
            // 24/7 Mode - Simple date addition
            resultDate.setDate(resultDate.getDate() + days);
            resultDate.setHours(resultDate.getHours() + hours);
            resultDate.setMinutes(resultDate.getMinutes() + minutes);
            return resultDate;
        }

        // Office Hours Mode - Complex calculation
        // Convert days to office hours (1 day = 8 office hours)
        if (days > 0) {
            const officeHoursFromDays = days * 8;
            remainingMinutes = (officeHoursFromDays * 60) + (hours * 60) + minutes;
        }

        // Add 1 second trick from legacy code to ensure proper boundary handling
        remainingMinutes += (1 / 60);

        // Loop until all time is consumed
        let iterations = 0;
        const maxIterations = 10000; // Safety limit to prevent infinite loops

        while (remainingMinutes > 0 && iterations < maxIterations) {
            iterations++;

            // Case 1: Public Holiday - Skip to next day
            if (this.isPublicHoliday(resultDate)) {
                resultDate = this.advanceToNextDay(resultDate);
                continue;
            }

            // Case 2: Non-Office Hours - Jump to next office period
            const currentPeriod = this.findCurrentOfficePeriod(resultDate);
            if (!currentPeriod || !currentPeriod.isWorkingHour) {
                resultDate = this.advanceToNextOfficePeriod(resultDate);
                continue;
            }

            // Case 3: Office Hours - Consume time until end of current period
            const periodEndTime = this.getPeriodEndTime(resultDate, currentPeriod);
            const minutesUntilBoundary = this.getMinutesDifference(resultDate, periodEndTime);

            if (remainingMinutes <= minutesUntilBoundary) {
                // Remaining time fits within current period
                resultDate.setMinutes(resultDate.getMinutes() + remainingMinutes);
                remainingMinutes = 0;
            } else {
                // Consume time until end of current period, then advance to next office period
                resultDate = new Date(periodEndTime);
                remainingMinutes -= minutesUntilBoundary;
            }
        }

        if (iterations >= maxIterations) {
            console.error('BusinessDateCalculator: Maximum iterations reached. Possible infinite loop.');
        }

        return resultDate;
    }

    /**
     * Converts days, hours, and minutes to total minutes
     * @param {number} days - Number of days
     * @param {number} hours - Number of hours
     * @param {number} minutes - Number of minutes
     * @returns {number} Total minutes
     */
    getTotalMinutes(days, hours, minutes) {
        return (days * 24 * 60) + (hours * 60) + minutes;
    }

    /**
     * Checks if a date is a public holiday
     * @param {Date} date - Date to check
     * @returns {boolean} True if date is a public holiday
     */
    isPublicHoliday(date) {
        const key = this.formatDateKey(date);
        return this.holidayMap.has(key);
    }

    /**
     * Formats date as "YYYY-MM-DD" for map key
     * @param {Date} date - Date to format
     * @returns {string} Formatted date key
     */
    formatDateKey(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    /**
     * Finds the office hour period that contains the given date/time
     * @param {Date} date - Date to find period for
     * @returns {Object|null} Office hour period object or null if not found
     */
    findCurrentOfficePeriod(date) {
        const dayOfWeek = date.getDay();
        const timeMinutes = this.parseTimeToMinutes(date.toTimeString().substring(0, 8));

        return this.officeHours.find(period => {
            if (period.officeDay !== dayOfWeek) return false;

            const fromMinutes = this.parseTimeToMinutes(period.fromHour);
            const toMinutes = this.parseTimeToMinutes(period.toHour);

            return timeMinutes >= fromMinutes && timeMinutes < toMinutes;
        }) || null;
    }

    /**
     * Advances date to the next working office period start time
     * @param {Date} date - Current date
     * @returns {Date} New date at next working office period start
     */
    advanceToNextOfficePeriod(date) {
        const dayOfWeek = date.getDay();
        const timeMinutes = this.parseTimeToMinutes(date.toTimeString().substring(0, 8));

        // Filter to only working hour periods
        const workingPeriods = this.officeHours.filter(p => p.isWorkingHour);

        if (workingPeriods.length === 0) {
            // No working periods defined, fallback to next day
            return this.advanceToNextDay(date);
        }

        // Find the next working period on the same day or future days
        // Search up to 2 weeks to handle all scenarios
        for (let dayOffset = 0; dayOffset <= 14; dayOffset++) {
            const checkDay = (dayOfWeek + dayOffset) % 7;

            // Get working periods for this day, sorted by start time
            const periodsForDay = workingPeriods
                .filter(p => p.officeDay === checkDay)
                .sort((a, b) => this.parseTimeToMinutes(a.fromHour) - this.parseTimeToMinutes(b.fromHour));

            for (const period of periodsForDay) {
                const periodFromMinutes = this.parseTimeToMinutes(period.fromHour);

                // If same day (offset=0), only consider periods that haven't started yet
                if (dayOffset === 0 && timeMinutes >= periodFromMinutes) {
                    continue;
                }

                // Found a valid future working period
                const newDate = new Date(date);
                newDate.setDate(newDate.getDate() + dayOffset);

                const timeParts = this.parseTimeSpanToObject(period.fromHour);
                newDate.setHours(timeParts.hours, timeParts.minutes, timeParts.seconds, 0);

                return newDate;
            }
        }

        // Fallback: advance to next day at 00:00
        return this.advanceToNextDay(date);
    }

    /**
     * Advances date to the next day at 00:00
     * @param {Date} date - Current date
     * @returns {Date} Next day at midnight
     */
    advanceToNextDay(date) {
        const newDate = new Date(date);
        newDate.setDate(newDate.getDate() + 1);
        newDate.setHours(0, 0, 0, 0);
        return newDate;
    }

    /**
     * Gets the end time of the current office period on the same day
     * @param {Date} currentDate - Current date/time
     * @param {Object} currentPeriod - Current office hour period
     * @returns {Date} End time of the current period
     */
    getPeriodEndTime(currentDate, currentPeriod) {
        const endTime = new Date(currentDate);
        const timeParts = this.parseTimeSpanToObject(currentPeriod.toHour);
        endTime.setHours(timeParts.hours, timeParts.minutes, timeParts.seconds, 0);
        return endTime;
    }

    /**
     * Calculates minutes difference between two dates
     * @param {Date} startDate - Start date
     * @param {Date} endDate - End date
     * @returns {number} Minutes difference
     */
    getMinutesDifference(startDate, endDate) {
        return Math.floor((endDate - startDate) / (1000 * 60));
    }

    /**
     * Parses time string (HH:mm:ss) or TimeSpan object to minutes since midnight
     * @param {string|Object} timeSpan - Time string or TimeSpan object
     * @returns {number} Minutes since midnight
     */
    parseTimeToMinutes(timeSpan) {
        if (typeof timeSpan === 'string') {
            const parts = timeSpan.split(':');
            const hours = parseInt(parts[0], 10) || 0;
            const minutes = parseInt(parts[1], 10) || 0;
            return (hours * 60) + minutes;
        } else if (typeof timeSpan === 'object' && timeSpan.hours !== undefined) {
            return (timeSpan.hours * 60) + (timeSpan.minutes || 0);
        }
        return 0;
    }

    /**
     * Parses time string (HH:mm:ss) or TimeSpan object to object
     * @param {string|Object} timeSpan - Time string or TimeSpan object
     * @returns {Object} Object with hours, minutes, seconds
     */
    parseTimeSpanToObject(timeSpan) {
        if (typeof timeSpan === 'string') {
            const parts = timeSpan.split(':');
            return {
                hours: parseInt(parts[0], 10) || 0,
                minutes: parseInt(parts[1], 10) || 0,
                seconds: parseInt(parts[2], 10) || 0
            };
        } else if (typeof timeSpan === 'object') {
            return {
                hours: timeSpan.hours || 0,
                minutes: timeSpan.minutes || 0,
                seconds: timeSpan.seconds || 0
            };
        }
        return { hours: 0, minutes: 0, seconds: 0 };
    }
}

// Make globally available
if (typeof window !== 'undefined') {
    window.BusinessDateCalculator = BusinessDateCalculator;
}
