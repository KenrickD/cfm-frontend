# CFM Frontend Troubleshooting Guide

This document contains solutions to common issues encountered during development of the CFM Frontend application.

---

## Table of Contents
1. [Searchable Dropdown Issues](#searchable-dropdown-issues)
2. [Calendar/Timeline Issues](#calendartimeline-issues)
3. [Session Management Issues](#session-management-issues)
4. [General Best Practices](#general-best-practices)

---

## Searchable Dropdown Issues

### Problem: Dropdown Initializes Twice (Doubling)

**Symptoms:**
- Dropdown appears duplicated in the UI
- Two wrapper divs created around the same select element
- Select element has multiple `.searchable-dropdown` parent wrappers
- Console may show initialization warnings

**Screenshot Example:**
```
Property Group [Select an option ▼] [Select an option ▼]  ← Doubled
```

**Root Cause:**

The SearchableDropdown component can be initialized in two ways:

1. **Automatically via data attribute:**
   ```html
   <select id="mySelect" data-searchable="true">
   ```
   - Automatically initialized by a global script (usually in layout.js or similar)
   - Runs on `$(document).ready()` or `DOMContentLoaded`

2. **Manually via JavaScript:**
   ```javascript
   new SearchableDropdown('#mySelect', options);
   ```

When **BOTH** methods are used on the same element, the component initializes twice, creating duplicate UI elements.

**Solution:**

**Choose ONE initialization method per dropdown.**

#### Option A: Use Data Attribute Only (Simple Dropdowns)

```html
<!-- In your .cshtml view -->
<select id="mySelect" class="form-select" data-searchable="true">
    <option>...</option>
</select>
```

**Remove any manual JavaScript initialization** for this element.

**Best for:**
- Simple dropdowns without custom logic
- Static dropdowns that don't need callbacks
- When you don't need to configure special options

#### Option B: Use JavaScript Only (Recommended for Complex Cases)

```html
<!-- In your .cshtml view - NO data-searchable attribute -->
<select id="mySelect" class="form-select">
    <option>...</option>
</select>
```

```javascript
// In your page-specific JavaScript file
function initializeDropdowns() {
    const element = document.querySelector('#mySelect');

    // Guard against double initialization
    if (element && !element._searchableDropdown) {
        new SearchableDropdown(element, {
            placeholder: 'Select an option',
            searchPlaceholder: 'Search...',
            allowClear: true,
            onChange: function(value) {
                // Custom logic here
            }
        });
    }
}
```

**Best for:**
- Cascading dropdowns (location → floor → room)
- Dropdowns with AJAX data loading
- Dropdowns that need custom event handlers
- Dynamic dropdowns that are created/destroyed

---

### Prevention in Code

**Add Guard in Component:**

If you maintain the SearchableDropdown component, add this guard in the `init()` method:

```javascript
// searchable-dropdown.js
init() {
    // Check if already initialized
    if (this.select._searchableDropdown) {
        console.warn('SearchableDropdown already initialized on element:', this.select);
        return; // Exit early
    }

    // Store reference
    this.select._searchableDropdown = this;

    // Continue with initialization...
}
```

**Check Before Manual Initialization:**

```javascript
function initializeDropdown(selector) {
    const element = document.querySelector(selector);

    if (!element) {
        console.error('Element not found:', selector);
        return;
    }

    if (element._searchableDropdown) {
        console.warn('SearchableDropdown already initialized:', selector);
        return; // Already initialized
    }

    new SearchableDropdown(element, options);
}
```

---

### When to Use Each Method

| Scenario | Recommended Method | Rationale |
|----------|-------------------|-----------|
| Simple static dropdown, no custom logic | `data-searchable="true"` | Easiest, no JS needed |
| Dropdown with custom onChange callback | JavaScript initialization | Need to pass options |
| Cascading dropdowns (A → B → C) | JavaScript initialization | Need programmatic control |
| AJAX-loaded options | JavaScript initialization | Need to call `loadOptions()` |
| Multiple similar dropdowns on page | JavaScript initialization | Centralized config |
| Form with validation logic | JavaScript initialization | Need event handlers |

---

### Example: Fixed Maintenance Management Dropdowns

**Before (Caused Doubling):**
```html
<select id="propertyGroupFilter" data-searchable="true">
```
```javascript
$('[data-searchable="true"]').each(function() {
    new SearchableDropdown(this); // ← Initializes again!
});
```

**After (Fixed):**
```html
<!-- Removed data-searchable attribute -->
<select id="propertyGroupFilter" class="form-select">
```
```javascript
function initializeDropdowns() {
    const dropdownConfigs = [
        {
            selector: '#propertyGroupFilter',
            options: {
                placeholder: 'All property groups',
                searchPlaceholder: 'Search property groups...',
                allowClear: true
            }
        }
    ];

    dropdownConfigs.forEach(config => {
        const element = document.querySelector(config.selector);

        if (element && !element._searchableDropdown) {
            new SearchableDropdown(element, config.options);
        }
    });
}
```

---

## Calendar/Timeline Issues

### Problem: Schedule Tiles Positioned Incorrectly

**Symptoms:**
- Schedule tiles appear in wrong weeks
- Tile for January 1-5 shows up in February
- Tiles are offset by several weeks

**Root Cause:**

Incorrect week calculation that doesn't account for:
1. The actual start day of the year (e.g., Jan 1, 2026 is Thursday)
2. Week boundaries (weeks start on different days)
3. Date normalization (time components causing off-by-one errors)

**Incorrect Implementation:**
```javascript
// BAD: Simple division doesn't work
function calculateTilePosition(date) {
    const diffTime = date - state.calendar.startDate;
    const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
    const weekIndex = Math.floor(diffDays / 7); // ← Too simplistic
    return weekIndex;
}
```

**Why This Fails:**
- Doesn't account for partial weeks
- Doesn't match tiles to actual week boundaries
- Can cause off-by-one errors due to daylight saving time

**Correct Implementation:**

```javascript
// GOOD: Find which week contains the date
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
            return i; // Return the week index
        }
    }

    // Date not found in any week
    return -1;
}
```

**Key Points:**
- ✅ Iterates through actual week boundaries
- ✅ Normalizes dates to avoid time-of-day issues
- ✅ Handles partial weeks correctly
- ✅ Returns -1 for out-of-range dates

---

### Problem: Tooltip Shows at Wrong Position

**Symptoms:**
- Tooltip appears far from the hovered tile
- Tooltip position doesn't update when scrolling
- Tooltip gets cut off at viewport edges

**Root Cause:**

Using `event.pageX`/`event.pageY` for positioning:
- These are relative to the entire document, not viewport
- Don't account for scrolling
- Don't update if the calendar scrolls horizontally

**Incorrect Implementation:**
```javascript
// BAD: Uses cursor position
function positionTooltip(event) {
    let left = event.pageX + 10;
    let top = event.pageY + 10;
    $tooltip.css({ left: left + 'px', top: top + 'px' });
}
```

**Correct Implementation:**

```javascript
// GOOD: Uses tile element position
function positionTooltip(tileElement) {
    const $tooltip = $('#scheduleTooltip');

    // Get tile position relative to viewport
    const tileRect = tileElement.getBoundingClientRect();

    // Get tooltip dimensions
    $tooltip.css({ visibility: 'hidden', display: 'block' });
    const tooltipRect = $tooltip[0].getBoundingClientRect();
    $tooltip.css({ visibility: 'visible', display: 'none' });

    const tooltipWidth = tooltipRect.width || 300;
    const tooltipHeight = tooltipRect.height || 150;

    // Calculate position above tile (add window.scrollY for document position)
    let top = tileRect.top + window.scrollY - tooltipHeight - 8;
    let left = tileRect.left + window.scrollX + (tileRect.width / 2) - (tooltipWidth / 2);

    // Check if goes off edges and adjust
    if (top < window.scrollY) {
        top = tileRect.bottom + window.scrollY + 8; // Show below instead
    }

    if (left < 0) left = 8;
    if (left + tooltipWidth > window.innerWidth) {
        left = window.innerWidth - tooltipWidth - 8;
    }

    $tooltip.css({ left: left + 'px', top: top + 'px' });
}
```

**Key Changes:**
- ✅ Uses `getBoundingClientRect()` for accurate positioning
- ✅ Accounts for window scroll position
- ✅ Centers tooltip on tile, not cursor
- ✅ Smart edge detection and flipping

---

### Problem: Text Overflow in Activity Columns

**Symptoms:**
- Long location names overflow the column
- Text wraps to multiple lines, breaking layout
- No way to see full text

**Solution:**

**CSS Fix:**
```css
.activity-col-location,
.activity-col-name {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
```

**JavaScript Fix (Add title attribute):**
```javascript
$('<div>').addClass('activity-col-location')
    .text(activity.location)
    .attr('title', activity.location) // ← Shows tooltip on hover
```

**Result:**
- Text truncates with "..." when too long
- Hover shows full text in browser's native tooltip
- Layout stays clean

---

## Session Management Issues

### Problem: Multi-Tab Client Context Mismatch

**Symptoms:**
- User opens form in Tab A (Client X)
- Switches to Client Y in Tab B
- Returns to Tab A and submits form
- Form submits with wrong client ID (Client Y instead of X)

**Solution:**

**Capture Client ID at Page Load:**

```csharp
// In ViewModel
public class MyViewModel {
    public int IdClient { get; set; } // ← Captured from session
}
```

```csharp
// In Controller
var viewModel = new MyViewModel {
    IdClient = userInfo.PreferredClientId // ← From session at page load
};
```

**Expose to JavaScript:**

```html
<script>
    window.PageContext = {
        idClient: @Model.IdClient  // ← Page-load client
    };
</script>
```

**Use in AJAX Calls:**

```javascript
const clientContext = {
    get idClient() { return window.PageContext?.idClient || 0; }
};

// All AJAX calls use page-load client, not current session
$.ajax({
    data: {
        idClient: clientContext.idClient  // ← Correct client
    }
});
```

**Monitor for Changes:**

```javascript
const monitor = new ClientSessionMonitor({
    pageLoadClientId: clientContext.idClient,
    onMismatch: function(currentClientId) {
        // Warn user and reload page
        location.reload();
    }
});
monitor.start();
```

---

## General Best Practices

### 1. Always Initialize Components with Guards

```javascript
function initComponent(selector) {
    const element = document.querySelector(selector);

    if (!element) {
        console.error('Element not found:', selector);
        return;
    }

    if (element._componentInstance) {
        console.warn('Already initialized:', selector);
        return;
    }

    element._componentInstance = new Component(element);
}
```

### 2. Use Page-Specific JavaScript Files

Don't put all JavaScript in global files. Create page-specific files:

```
wwwroot/assets/js/pages/
    ├── preventivemaintenance/
    │   └── maintenance-management.js
    ├── workrequest/
    │   ├── work-request-add.js
    │   └── work-request-edit.js
```

### 3. Always Add Loading States

```javascript
// Show loading
$('#spinner').show();

$.ajax({
    url: endpoint,
    complete: function() {
        $('#spinner').hide(); // ← Always hide in complete()
    }
});
```

### 4. Normalize Dates for Comparisons

```javascript
// GOOD: Normalize before comparing
const date1 = new Date(dateString);
date1.setHours(0, 0, 0, 0);

const date2 = new Date(otherDateString);
date2.setHours(0, 0, 0, 0);

if (date1.getTime() === date2.getTime()) {
    // Dates are same day
}
```

### 5. Use Title Attributes for Truncated Text

```javascript
$('<div>')
    .text(longText)
    .attr('title', longText) // ← Shows on hover
    .css({
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        whiteSpace: 'nowrap'
    });
```

---

## Getting Help

If you encounter an issue not covered here:

1. Check the browser console for errors
2. Check the Network tab for failed API calls
3. Review the CLAUDE.md file for project patterns
4. Search this file for similar issues
5. Add new solutions to this file when resolved

---

**Document Version:** 1.0
**Last Updated:** 2026-03-12
**Maintainer:** Development Team
