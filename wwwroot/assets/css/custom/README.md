# CFM Frontend Design System

Custom CSS design system built on top of Light Able Bootstrap 5 theme.

## Overview

This design system standardizes CSS, styles, and design across all CFM Frontend pages using the Work Request Index and Add pages as the baseline. All styles are extracted into reusable component files following industry best practices.

## File Organization

```
wwwroot/assets/css/custom/
├── core/
│   ├── variables.css          # CSS custom properties (colors, spacing, shadows)
│   ├── typography.css         # Text styles and heading definitions
│   └── utilities.css          # Utility classes for common patterns
├── components/
│   ├── buttons.css            # Button variants and styles
│   ├── forms.css              # Form controls, inputs, labels, validation
│   ├── cards.css              # Card component styles
│   ├── tables.css             # Table layouts and styling
│   ├── modals.css             # Modal dialog styles
│   ├── badges.css             # Badge and label styles
│   ├── pagination.css         # Pagination component
│   └── search.css             # Search bars and typeahead
├── custom-main.css            # Main import file (imports all CSS files above)
└── README.md                  # This file
```

## Usage

The design system is automatically loaded on all pages via `HeadCSS.cshtml`:

```html
<link rel="stylesheet" href="~/assets/css/custom/custom-main.css">
```

All component classes and CSS variables are available globally across the application.

## CSS Variables

All custom colors and values are defined as CSS variables in `core/variables.css`:

### Colors
- `--cfm-primary`: #04a9f5 (Primary blue)
- `--cfm-primary-dark`: #0396de (Darker blue for hovers)
- `--cfm-primary-light`: #e7f3ff (Light blue backgrounds)
- `--cfm-success`: #2ed8b6 (Success green)
- `--cfm-danger`: #f5576c (Danger red)
- `--cfm-warning`: #ff9800 (Warning orange)
- `--cfm-info`: #04a9f5 (Info blue)

### Background Colors
- `--cfm-bg-light-primary`: rgba(4, 169, 245, 0.1)
- `--cfm-bg-light-success`: rgba(46, 216, 182, 0.1)
- `--cfm-bg-light-danger`: rgba(245, 87, 108, 0.1)
- `--cfm-bg-light-secondary`: rgba(108, 117, 125, 0.1)

### Grays
- `--cfm-gray-100` through `--cfm-gray-700`

### Usage Example
```css
.my-custom-element {
  color: var(--cfm-primary);
  background-color: var(--cfm-bg-light-primary);
  border-radius: var(--cfm-border-radius);
  transition: var(--cfm-transition-base);
}
```

## Component Classes

### Forms (`components/forms.css`)

**Required Field Indicator**
```html
<label class="required-field">Field Name</label>
<!-- Displays: Field Name * (red asterisk) -->
```

**DateTime Input Groups**
```html
<div class="datetime-group">
  <input type="date" class="form-control">
  <input type="time" class="form-control">
</div>
```

**Typeahead Dropdown**
```html
<div class="location-search-wrapper">
  <input type="text" class="form-control">
  <div class="typeahead-dropdown">
    <div class="typeahead-item">Option 1</div>
    <div class="typeahead-item">Option 2</div>
  </div>
</div>
```

**Target Time Display** (Work Request feature)
```html
<div class="target-time-display">
  <i class="ti ti-target"></i>
  <span>Target Date</span>
</div>
```

### Badges (`components/badges.css`)

**Status Badges**
```html
<span class="badge bg-light-primary">New</span>
<span class="badge bg-light-success">In Progress</span>
<span class="badge bg-light-secondary">Completed</span>
<span class="badge bg-light-danger">Cancelled</span>
<span class="badge bg-light-warning">Pending</span>
<span class="badge bg-light-info">Info</span>
```

### Modals (`components/modals.css`)

**Filter Modal**
```html
<div class="modal filter-modal">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header no-divider">
        <h5>Filters</h5>
      </div>
      <div class="modal-body">
        <div class="filter-section">
          <label class="filter-section-title">Category</label>
          <div class="filter-checkbox-group">
            <div class="filter-checkbox-item form-check">
              <input type="checkbox" id="opt1">
              <label for="opt1">Option 1</label>
            </div>
          </div>
        </div>
      </div>
      <div class="modal-footer no-divider">
        <button class="btn btn-clear-filter">Clear</button>
        <button class="btn btn-primary">Apply</button>
      </div>
    </div>
  </div>
</div>
```

### Buttons (`components/buttons.css`)

**Custom Button Variants**
```html
<button class="btn btn-add-labor">Add Item</button>
<button class="btn btn-clear-filter">Clear Filters</button>
```

### Cards (`components/cards.css`)

**Card with Header**
```html
<div class="card">
  <div class="card-header">
    <h5>Card Title</h5>
    <p class="text-muted small">Description text</p>
  </div>
  <div class="card-body">
    Content here
  </div>
</div>
```

### Tables (`components/tables.css`)

**Responsive Table with Hover**
```html
<div class="table-responsive">
  <table class="table table-hover">
    <thead>
      <tr>
        <th>Column 1</th>
        <th>Column 2</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>Data 1</td>
        <td>Data 2</td>
      </tr>
    </tbody>
  </table>
</div>
```

**Empty State**
```html
<table class="table">
  <tbody>
    <tr>
      <td colspan="3" class="text-center py-4">
        <i class="ti ti-inbox"></i>
        <p class="mt-2">No results found</p>
      </td>
    </tr>
  </tbody>
</table>
```

### Pagination (`components/pagination.css`)

Automatically styled when using `_Pagination.cshtml` partial:
```razor
@await Html.PartialAsync("_Pagination", Model.Paging)
```

### Search (`components/search.css`)

**Search Input Group**
```html
<div class="input-group">
  <input type="text" class="form-control" placeholder="Search...">
  <button class="btn btn-light"><i class="ti ti-search"></i></button>
  <button class="btn btn-light"><i class="ti ti-filter"></i></button>
</div>
```

## Baseline Pages

Design patterns were extracted from these pages:
- **`/Helpdesk/Index`** - List page with search, filters, data table
- **`/Helpdesk/WorkRequestAdd`** - Form page with cards, inputs, modals

These pages demonstrate the complete design system in action.

## Compatibility

### Works Alongside
- ✅ Light Able Bootstrap 5 theme
- ✅ Existing inline `@section styles` in views (43 pages)
- ✅ Bootstrap 5 utility classes
- ✅ Plugin CSS (DataTables, Select2, Flatpickr, etc.)
- ✅ Searchable dropdown component (`components/searchable-dropdown.css`)

### Browser Support
- Modern browsers (Chrome, Firefox, Safari, Edge)
- CSS custom properties (variables) support required

## Best Practices

### For New Pages
1. Use component classes instead of inline styles
2. Leverage CSS variables for colors and spacing
3. Follow the baseline page patterns
4. Keep page-specific styles to a minimum

### Example - Creating a New Filter Modal
```html
<!-- ✅ GOOD: Using design system classes -->
<div class="modal filter-modal">
  <div class="filter-section">
    <label class="filter-section-title">Status</label>
    <div class="filter-checkbox-group">
      <!-- checkboxes here -->
    </div>
  </div>
</div>

<!-- ❌ BAD: Inline styles -->
<div class="modal" style="max-width: 900px">
  <div style="margin-bottom: 25px">
    <label style="font-weight: 600; color: #04a9f5">Status</label>
    <div style="max-height: 300px; overflow-y: auto">
      <!-- checkboxes here -->
    </div>
  </div>
</div>
```

## Extending the Design System

To add new component styles:

1. Create new CSS file in `components/` folder
2. Follow existing naming conventions
3. Use CSS variables from `variables.css`
4. Import in `custom-main.css`
5. Document in this README

Example:
```css
/* components/alerts.css */
.alert-custom {
  background-color: var(--cfm-bg-light-primary);
  color: var(--cfm-primary);
  border-radius: var(--cfm-border-radius);
}
```

Then add to `custom-main.css`:
```css
@import url('./components/alerts.css');
```

## Maintenance

### Updating Colors
Edit `core/variables.css` to change colors across the entire application.

### Adding Utility Classes
Add to `core/utilities.css` for reusable helper classes.

### Component Updates
Edit individual component files as needed. Changes apply globally.

## Future Enhancements

- Dark mode support using CSS variables
- Additional component variants (tabs, accordions, etc.)
- Animation library
- Print styles
- Accessibility improvements

## Support

For questions or issues with the design system:
1. Review this documentation
2. Check baseline pages for examples
3. Examine component CSS files
4. Test changes in browser DevTools first

---

**Version**: 1.0
**Last Updated**: 2026-01-07
**Baseline Pages**: Work Request Index & Add
**Theme**: Light Able Bootstrap 5
