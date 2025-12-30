# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an **ASP.NET Core 8.0 MVC frontend application** that serves as a web-based client interface for a backend REST API. It follows a traditional server-rendered MVC pattern with Razor views and uses cookie-based authentication with bearer token handling.

**Key characteristics:**
- Frontend-only application - all business logic lives in the backend API
- Acts as a "Backend for Frontend" (BFF) layer
- Communication: JSON over HTTP with automatic bearer token injection
- Primary feature: Work Request Management (Helpdesk module)

## Most Important Points
- do not in any way break the previous functionality without first confirming.

## Development Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run the application (development mode)
dotnet run

# Restore packages
dotnet restore
```

**Default URLs:**
- HTTP: http://localhost:5099
- HTTPS: https://localhost:7035

### Configuration

**Required configuration** in `appsettings.json` or `appsettings.Development.json`:
```json
{
  "BackendBaseURL": "https://your-backend-api-url"
}
```

The application will not function without a valid `BackendBaseURL` pointing to the backend API.

## Architecture

### MVC Pattern Implementation

**Controllers** (`Controllers/`)
- Coordinate requests and aggregate data from backend API
- Handle session management and user context
- Return Views with ViewModels
- **Never** contain business logic - delegate to backend API

**Models** (`Models/`)
- Domain models like `UserInfo`, `PagingInfo`
- Work Request models in `Models/WorkRequest/`

**DTOs** (`DTOs/`)
- Data Transfer Objects for API communication
- Request/Response structures matching backend API contracts
- Examples: `LoginResponse`, `WorkRequestCreateRequest`

**ViewModels** (`ViewModels/`)
- Aggregate data from multiple API calls for complex views
- Example: `WorkRequestViewModel` combines work requests, locations, service providers, categories, etc.

**Views** (`Views/`)
- Server-rendered Razor templates
- Shared layouts in `Views/Shared/`

### Backend API Integration

All HTTP requests to the backend API use the named `HttpClient` called `"BackendAPI"`:

```csharp
var client = _httpClientFactory.CreateClient("BackendAPI");
var backendUrl = _configuration["BackendBaseUrl"];
var response = await client.GetAsync($"{backendUrl}/api/endpoint");
```

**Important:** The `"BackendAPI"` client automatically includes the `AuthTokenHandler` which:
1. Injects the bearer token into every request
2. Handles 401 responses by refreshing tokens
3. Retries the original request with the new token
4. Signs out the user if token refresh fails

**⚠️ CRITICAL WARNING:** Do NOT use the `"BackendAPI"` client during the login flow (before `HttpContext.SignInAsync()` is called). The `AuthTokenHandler` requires an authenticated session cookie to retrieve the access token. Before the cookie exists, use a plain `HttpClient` and manually add the Authorization header. See [Privilege Loading Implementation Details](#privilege-loading-implementation-details) for detailed explanation.

### Authentication Flow

**Login process** (see `Controllers/LoginController.cs`):
1. User submits credentials via `SignIn` action
2. POST to `/api/auth/login` returns access token + refresh token
3. Fetch user info via `/api/auth/userinfo` with tokens
4. Store `UserInfo` in `HttpContext.Session["UserSession"]` as JSON
5. **Load user privileges** from `GET api/WebUser/GetUserPrivileges`
6. Store `UserPrivileges` in `HttpContext.Session["UserPrivileges"]` as JSON
7. Create authentication cookie with claims and tokens
8. Redirect to Dashboard

**Session data structure:**
```csharp
// User session data
var userSessionJson = HttpContext.Session.GetString("UserSession");
var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson);
// Access: userInfo.UserId, userInfo.PreferredClientId, etc.

// User privileges (loaded at login)
var privileges = HttpContext.Session.GetPrivileges();
// Access: privileges.CanViewPage("Helpdesk", "Work Request Management")
```

**Token storage:**
- Access token and refresh token stored in authentication cookie properties
- Retrieved via: `await HttpContext.GetTokenAsync("access_token")`
- Automatically refreshed by `AuthTokenHandler` on 401 responses

**Important files:**
- `Controllers/LoginController.cs` - Authentication logic
- `Handlers/AuthTokenHandler.cs` - Token injection and refresh logic
- `Models/UserInfo.cs` - Session data model
- `Services/PrivilegeService.cs` - Privilege loading from API

### Dependency Injection Configuration

In `Program.cs`:
```csharp
// Named HTTP client with automatic token handling
builder.Services.AddHttpClient("BackendAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BackendBaseUrl"]);
})
.AddHttpMessageHandler<AuthTokenHandler>();

// Privilege service for loading user privileges
builder.Services.AddScoped<IPrivilegeService, PrivilegeService>();

// Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// Session configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

## Common Patterns

### Retrieving User Session Data

User information (including `IdClient`, `IdEmployee`) is stored in session during login:

```csharp
// Get session data
var userSessionJson = HttpContext.Session.GetString("UserSession");
if (string.IsNullOrEmpty(userSessionJson))
{
    return RedirectToAction("Index", "Login");
}

var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

// Use session data
var idClient = userInfo.PreferredClientId;
var idEmployee = userInfo.UserId;
```

### Making API Calls

```csharp
var client = _httpClientFactory.CreateClient("BackendAPI");
var backendUrl = _configuration["BackendBaseUrl"];

// GET request
var response = await client.GetAsync($"{backendUrl}/api/endpoint?param={value}");

// POST request
var payload = new { field1 = value1, field2 = value2 };
var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});
var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
var response = await client.PostAsync($"{backendUrl}/api/endpoint", content);

// Deserialize response
if (response.IsSuccessStatusCode)
{
    var responseStream = await response.Content.ReadAsStreamAsync();
    var result = await JsonSerializer.DeserializeAsync<ResponseType>(
        responseStream,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
}
```

### Parallel API Calls

For loading multiple data sources (common in `HelpdeskController`):

```csharp
var locationsTask = GetLocationsAsync(client, backendUrl, idClient);
var categoriesTask = GetCategoriesAsync(client, backendUrl);
var providersTask = GetProvidersAsync(client, backendUrl, idClient);

await Task.WhenAll(locationsTask, categoriesTask, providersTask);

viewmodel.Locations = await locationsTask;
viewmodel.Categories = await categoriesTask;
viewmodel.Providers = await providersTask;
```

### Pagination Pattern

The application uses a **standardized pagination system** for all list views. Pagination is handled through a shared partial view component that automatically renders pagination controls with query string parameter preservation.

#### Pagination Components

**Shared Partial View**: `Views/Shared/_Pagination.cshtml`
- Reusable pagination component used across all list pages
- Automatically preserves all query string parameters (search, filters, etc.)
- Renders "Showing X to Y of Z results" info text
- Displays Previous/Next buttons and page numbers
- Smart page number display with ellipsis for large page counts

**PagingInfo Model**: `Models/PagingInfo.cs`
```csharp
public class PagingInfo
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public List<int> GetPageNumbers()  // Smart page number generation with ellipsis
}
```

#### Controller Implementation Pattern

**1. Accept page parameter in action:**
```csharp
public async Task<IActionResult> Index(int page = 1, string search = "")
{
    var accessCheck = this.CheckViewAccess("Helpdesk", "Work Request Management");
    if (accessCheck != null) return accessCheck;

    var viewmodel = new YourViewModel();

    // Call backend API with page parameter
    var response = await GetDataAsync(client, backendUrl, page, search);

    // Map response to PagingInfo
    if (response != null)
    {
        viewmodel.Data = response.data;
        viewmodel.Paging = new PagingInfo
        {
            CurrentPage = response.CurrentPage,
            TotalPages = response.TotalPages,
            PageSize = response.PageSize,
            TotalRecords = response.TotalCount
        };
    }

    return View(viewmodel);
}
```

**2. Backend API Response DTO:**
```csharp
public class ListApiResponse<T>
{
    public List<T> data { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
```

#### View Implementation Pattern

**Add PagingInfo to ViewModel:**
```csharp
public class YourViewModel
{
    public List<YourDataModel>? Data { get; set; }
    public PagingInfo? Paging { get; set; }
    // Other properties...
}
```

**Render pagination in view:**
```razor
@model YourViewModel

<div class="table-responsive">
    <table class="table">
        <!-- Table content -->
    </table>
</div>

@* Pagination Component *@
@await Html.PartialAsync("_Pagination", Model.Paging)
```

#### How It Works

1. **Controller receives page number** from query string (defaults to 1)
2. **Passes page to backend API** along with other filter parameters
3. **Backend returns paginated data** plus metadata (CurrentPage, TotalPages, PageSize, TotalCount)
4. **Controller maps to PagingInfo** model
5. **View renders pagination component** via `@await Html.PartialAsync("_Pagination", Model.Paging)`
6. **Component automatically preserves query parameters** when building pagination links

#### Example: Work Request List

**Controller** ([HelpdeskController.cs:41-127](Controllers/HelpdeskController.cs#L41-L127)):
```csharp
public async Task<IActionResult> Index(int page = 1, string search = "")
{
    var workRequestResponse = await GetWorkRequestsAsync(client, backendUrl, page, idClient, idActor, idEmployee, search);

    if (workRequestResponse != null)
    {
        viewmodel.WorkRequest = workRequestResponse.data;
        viewmodel.Paging = new PagingInfo
        {
            CurrentPage = workRequestResponse.CurrentPage,
            TotalPages = workRequestResponse.TotalPages,
            PageSize = workRequestResponse.PageSize,
            TotalRecords = workRequestResponse.TotalCount
        };
    }

    return View(viewmodel);
}
```

**View** ([Views/Helpdesk/WorkRequest/Index.cshtml:296-297](Views/Helpdesk/WorkRequest/Index.cshtml#L296-L297)):
```razor
@* Pagination Component *@
@await Html.PartialAsync("_Pagination", Model.Paging)
```

#### Best Practices

1. **Always use the shared `_Pagination.cshtml` component** - Never create custom pagination markup
2. **Include `PagingInfo` in all list ViewModels** - Standardize the property name as `Paging`
3. **Default page to 1** in controller actions - Use `int page = 1` parameter
4. **Map all pagination fields** from API response to `PagingInfo` model
5. **Backend API must return pagination metadata** - CurrentPage, TotalPages, PageSize, TotalCount
6. **The component handles query string preservation** - No need to manually build URLs in views
7. **Use consistent page size** - Backend typically uses 10, 20, or 50 items per page

#### Styling

The pagination component includes built-in Bootstrap-compatible styles:
- Primary color (`#04a9f5`) for active page
- Rounded page buttons with hover effects
- Disabled state for Previous/Next when not applicable
- Ellipsis (`...`) for large page ranges
- Responsive layout with centered alignment

All pagination styling is self-contained in `_Pagination.cshtml` to avoid conflicts.

## Searchable Dropdown Component

The application uses a **standardized searchable dropdown component** for all `<select>` elements that need search functionality. This provides a modern, accessible alternative to native dropdowns with built-in search, keyboard navigation, and consistent styling.

### Overview

The searchable dropdown component transforms standard HTML `<select>` elements into modern search selection dropdowns, inspired by Semantic UI but optimized for Bootstrap 5. It provides:
- **Live search filtering** with instant results
- **Keyboard navigation** (Arrow keys, Enter, Escape)
- **Clear button** for easy reset
- **Custom templates** for option rendering
- **AJAX support** for dynamic data loading
- **Accessibility** with ARIA attributes
- **Lightweight** - No external dependencies (no jQuery required)

### Component Files

**CSS**: `wwwroot/assets/css/components/searchable-dropdown.css`
- Complete styling for dropdown states, search input, options list
- Includes hover, active, disabled, and validation states
- Smooth transitions and animations
- Custom scrollbar styling
- Bootstrap-compatible validation classes

**JavaScript**: `wwwroot/assets/js/components/searchable-dropdown.js`
- Core `SearchableDropdown` class
- Automatic initialization via `data-searchable` attribute
- Event handling for keyboard, mouse, and touch interactions
- AJAX loading support
- Extensible through options and callbacks

### Basic Usage

#### Automatic Initialization (Recommended)

Simply add the `data-searchable="true"` attribute to any `<select>` element:

```html
<select class="form-select form-select-sm"
        id="locationSelect"
        name="IdLocation"
        data-searchable="true"
        data-placeholder="Select Location"
        data-search-placeholder="Search location..."
        required>
    <option value="">Select Location</option>
    <option value="1">Building A</option>
    <option value="2">Building B</option>
    <option value="3">Building C</option>
</select>
```

**Data Attributes:**
- `data-searchable="true"` - Enables searchable dropdown (required)
- `data-placeholder` - Text shown when no option is selected
- `data-search-placeholder` - Placeholder text in search input
- `data-allow-clear="false"` - Disable the clear button (default: true)

The component will automatically initialize on `DOMContentLoaded`.

#### Manual Initialization (JavaScript)

For dynamic dropdowns or custom configurations:

```javascript
const dropdown = new SearchableDropdown('#mySelect', {
    placeholder: 'Select an option',
    searchPlaceholder: 'Search...',
    allowClear: true,
    size: 'small',  // 'default' or 'small'
    data: [
        { value: '1', label: 'Option 1', description: 'Description text' },
        { value: '2', label: 'Option 2' }
    ],
    onChange: function(value, label) {
        console.log('Selected:', value, label);
    }
});
```

### Advanced Features

#### AJAX Data Loading

Load options dynamically from an API endpoint:

```javascript
const dropdown = new SearchableDropdown('#serviceProviderSelect', {
    placeholder: 'Select Service Provider',
    searchPlaceholder: 'Type to search providers...',
    minimumInputLength: 2,  // Only search after 2 characters
    ajax: async function(searchTerm) {
        const response = await fetch(`/api/providers/search?term=${encodeURIComponent(searchTerm)}`);
        const data = await response.json();

        return data.map(item => ({
            value: item.id,
            label: item.name,
            description: item.companyName
        }));
    }
});
```

#### Custom Option Templates

Customize how options are displayed:

```javascript
const dropdown = new SearchableDropdown('#priorityLevelSelect', {
    placeholder: 'Select Priority',
    templateResult: function(item) {
        return `
            <span style="display: flex; align-items: center; gap: 0.5rem;">
                <span style="width: 12px; height: 12px; border-radius: 50%; background-color: ${item.color};"></span>
                <span>${item.label}</span>
            </span>
        `;
    }
});
```

#### Cascading Dropdowns

Enable/disable dependent dropdowns based on selection:

```javascript
// Location dropdown
const locationDropdown = new SearchableDropdown('#locationSelect', {
    onChange: async function(value, label) {
        if (value) {
            // Load floors for selected location
            const floors = await loadFloorsForLocation(value);

            // Enable and populate floor dropdown
            floorDropdown.enable();
            floorDropdown.renderOptions(floors);
        } else {
            // Disable dependent dropdowns
            floorDropdown.disable();
            roomDropdown.disable();
        }
    }
});

const floorDropdown = new SearchableDropdown('#floorSelect', {
    disabled: true,
    onChange: async function(value, label) {
        if (value) {
            const rooms = await loadRoomsForFloor(value);
            roomDropdown.enable();
            roomDropdown.renderOptions(rooms);
        } else {
            roomDropdown.disable();
        }
    }
});
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `placeholder` | string | "Select an option" | Text shown when no option selected |
| `searchPlaceholder` | string | "Search..." | Placeholder in search input |
| `allowClear` | boolean | true | Show clear button to reset selection |
| `data` | array | null | Array of option objects: `[{ value, label, description }]` |
| `ajax` | function | null | Async function for loading data: `async (searchTerm) => []` |
| `minimumInputLength` | number | 0 | Minimum characters before AJAX search triggers |
| `disabled` | boolean | false | Initial disabled state |
| `size` | string | "default" | Size variant: "default" or "small" |
| `templateResult` | function | null | Custom HTML template for options |
| `templateSelection` | function | null | Custom HTML template for selected value |
| `onChange` | function | null | Callback when value changes: `(value, label) => {}` |
| `onSelect` | function | null | Callback when option selected: `(value, label) => {}` |

### API Methods

```javascript
const dropdown = new SearchableDropdown('#mySelect', options);

// Get current value
const value = dropdown.getValue();          // Returns selected value
const label = dropdown.getLabel();          // Returns selected label text

// Set value programmatically
dropdown.setValue('2', 'Building B', true); // (value, label, triggerChange)

// Clear selection
dropdown.clear();

// Enable/disable
dropdown.enable();
dropdown.disable();

// Update options
dropdown.renderOptions([
    { value: '1', label: 'New Option 1' },
    { value: '2', label: 'New Option 2' }
]);

// Destroy component
dropdown.destroy();  // Removes component, restores original <select>
```

### Keyboard Navigation

The searchable dropdown supports full keyboard navigation:

- **Tab** - Focus next element
- **Enter / Space** - Open dropdown menu
- **Arrow Down** - Highlight next option
- **Arrow Up** - Highlight previous option
- **Enter** (in menu) - Select highlighted option
- **Escape** - Close menu and return focus to toggle
- **Type to search** - Filters options in real-time

### Styling and Customization

#### Size Variants

```html
<!-- Default size -->
<select class="form-select" data-searchable="true">...</select>

<!-- Small size -->
<select class="form-select form-select-sm" data-searchable="true">...</select>
```

#### Validation States

The component automatically applies Bootstrap validation classes:

```html
<!-- Invalid state -->
<select class="form-select is-invalid" data-searchable="true">...</select>
<div class="invalid-feedback">This field is required.</div>

<!-- Valid state -->
<select class="form-select is-valid" data-searchable="true">...</select>
<div class="valid-feedback">Looks good!</div>
```

#### Custom Styling

Override default styles using CSS:

```css
/* Change primary color */
.searchable-dropdown-toggle:focus,
.searchable-dropdown.active .searchable-dropdown-toggle {
    border-color: #28a745;
    box-shadow: 0 0 0 0.25rem rgba(40, 167, 69, 0.25);
}

.searchable-dropdown-option.active {
    background-color: #28a745;
}

/* Custom option height */
.searchable-dropdown-option {
    padding: 0.75rem 1rem;
}
```

### Common Patterns

#### Work Request Form Pattern

```html
<!-- Location, Floor, Room cascade -->
<div class="row">
    <div class="col-md-4 mb-3">
        <label class="form-label fw-semibold required-field">Location</label>
        <select class="form-select form-select-sm"
                id="locationSelect"
                name="IdLocation"
                data-searchable="true"
                data-placeholder="Select Location"
                data-search-placeholder="Search location..."
                required>
            <option value="">Select Location</option>
        </select>
    </div>

    <div class="col-md-4 mb-3">
        <label class="form-label fw-semibold required-field">Floor</label>
        <select class="form-select form-select-sm"
                id="floorSelect"
                name="IdFloor"
                data-searchable="true"
                data-placeholder="Select Floor"
                data-search-placeholder="Search floor..."
                required
                disabled>
            <option value="">Select Floor</option>
        </select>
    </div>

    <div class="col-md-4 mb-3">
        <label class="form-label fw-semibold required-field">Room/Area/Zone</label>
        <select class="form-select form-select-sm"
                id="roomSelect"
                name="IdRoom"
                data-searchable="true"
                data-placeholder="Select Room/Area/Zone"
                data-search-placeholder="Search room..."
                required
                disabled>
            <option value="">Select Room/Area/Zone</option>
        </select>
    </div>
</div>
```

### Including Component in Views

**In the `@section styles` block:**
```razor
@section styles {
    <link rel="stylesheet" href="~/assets/css/components/searchable-dropdown.css">
}
```

**In the `@section scripts` block:**
```razor
@section scripts {
    <script src="~/assets/js/components/searchable-dropdown.js"></script>
    <script src="~/assets/js/pages/your-page.js"></script>
}
```

### Migration from Select2

If migrating from Select2, the searchable dropdown component provides similar functionality with these benefits:

**Before (Select2):**
```html
<link href="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css" rel="stylesheet" />
<script src="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js"></script>
<script>
$('#mySelect').select2({
    placeholder: 'Select an option',
    allowClear: true
});
</script>
```

**After (Searchable Dropdown):**
```html
<link rel="stylesheet" href="~/assets/css/components/searchable-dropdown.css">
<script src="~/assets/js/components/searchable-dropdown.js"></script>

<select id="mySelect"
        data-searchable="true"
        data-placeholder="Select an option">
    ...
</select>
```

**Benefits:**
- No jQuery dependency (30KB+ saved)
- No CDN dependencies (faster, more reliable)
- Better keyboard navigation
- Consistent styling with Bootstrap 5
- Smaller footprint (~15KB vs 70KB+)
- Modern ES6+ code

### Best Practices

1. **Always use `data-searchable="true"` for automatic initialization** - Simplest and most consistent approach
2. **Set meaningful placeholders** - Use `data-placeholder` and `data-search-placeholder`
3. **Use small size for forms** - Add `form-select-sm` class for compact forms
4. **Enable validation** - Add `required`, `is-invalid`, or `is-valid` classes
5. **Implement cascading logic** - Use `onChange` callback to update dependent dropdowns
6. **Use AJAX for large datasets** - Load options dynamically when >100 items
7. **Add descriptions for clarity** - Use `description` property in data objects
8. **Test keyboard navigation** - Ensure all dropdowns are keyboard accessible
9. **Handle loading states** - Show loading indicator during AJAX calls
10. **Clean up on page unload** - Call `destroy()` if dynamically removing dropdowns

### Accessibility

The searchable dropdown component follows WCAG 2.1 AA guidelines:

- ✅ **Keyboard navigable** - All functions accessible via keyboard
- ✅ **ARIA attributes** - Proper `role`, `aria-expanded`, `aria-label`
- ✅ **Focus management** - Logical focus order and visible indicators
- ✅ **Screen reader support** - Meaningful labels and state announcements
- ✅ **High contrast** - Sufficient color contrast ratios
- ✅ **Touch friendly** - Minimum 44x44px touch targets

### Browser Support

- ✅ Chrome 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Edge 90+
- ✅ Mobile browsers (iOS Safari, Chrome Mobile)

### Troubleshooting

**Dropdown not initializing:**
- Ensure `data-searchable="true"` is present
- Check that `searchable-dropdown.js` is loaded
- Verify `<select>` element exists in DOM when script runs

**Options not showing:**
- Check browser console for errors
- Verify `<option>` elements have `value` attributes
- For AJAX: ensure the function returns properly formatted array

**Keyboard navigation not working:**
- Ensure dropdown is focused (click or Tab to it)
- Check for JavaScript errors preventing event handlers

**Styling conflicts:**
- Load `searchable-dropdown.css` after Bootstrap
- Check for CSS specificity issues
- Use browser DevTools to inspect applied styles

## Breadcrumb Navigation System

The application uses a **centralized breadcrumb component** that automatically renders navigation breadcrumbs on every page. Breadcrumbs show the user's current location in the application hierarchy and provide clickable navigation back to parent pages.

### Breadcrumb Component

**Location**: `Views/Shared/Breadcrumb.cshtml`

The shared breadcrumb component is automatically included in the main layout at line 22 of `_Layout.cshtml`. It renders a three-level breadcrumb structure:

```
Home > Parent Page > Current Page
```

### Setting Breadcrumb Data in Controllers

Controllers use `ViewBag` properties to configure the breadcrumb:

```csharp
public IActionResult WorkCategory()
{
    ViewBag.Title = "Work Category";              // Current page name (required)
    ViewBag.pTitle = "Settings";                  // Parent page name (optional)
    ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");  // Parent page URL (optional)
    return View("~/Views/Helpdesk/Settings/WorkCategory.cshtml");
}
```

**ViewBag Properties:**
- `ViewBag.Title` - **Required** - Current page name, shown in breadcrumb and page header
- `ViewBag.pTitle` - *Optional* - Parent page name (e.g., "Settings")
- `ViewBag.pTitleUrl` - *Optional* - Parent page clickable URL. If not provided, parent will be non-clickable text

### Breadcrumb Rendering Logic

The breadcrumb component (`Views/Shared/Breadcrumb.cshtml`) intelligently renders based on provided data:

1. **Home link** - Always shown, links to `Dashboard/Index`
2. **Parent link** - Shown if `ViewBag.pTitle` is set
   - Clickable if `ViewBag.pTitleUrl` is provided
   - Plain text if `ViewBag.pTitleUrl` is not provided
3. **Current page** - Shown if `ViewBag.Title` is set, marked with `active` class

**Example outputs:**

```html
<!-- With parent URL -->
Home > Settings > Work Category
 ↑       ↑            ↑
Link    Link    Active (current)

<!-- Without parent URL -->
Home > Settings > Work Category
 ↑         ↑            ↑
Link     Text    Active (current)

<!-- Minimal (no parent) -->
Home > Dashboard
 ↑          ↑
Link    Active
```

### Common Breadcrumb Patterns

**Settings Subsection Pages:**
```csharp
ViewBag.Title = "Work Category";
ViewBag.pTitle = "Settings";
ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
// Renders: Home > Settings > Work Category
```

**Work Request Pages:**
```csharp
ViewBag.Title = "Work Request Details";
ViewBag.pTitle = "Work Requests";
ViewBag.pTitleUrl = Url.Action("Index", "Helpdesk");
// Renders: Home > Work Requests > Work Request Details
```

**Top-Level Pages:**
```csharp
ViewBag.Title = "Dashboard";
// Renders: Home > Dashboard
```

### Breadcrumb Styling

The breadcrumb uses Bootstrap's breadcrumb component with the following structure:

```html
<div class="page-header">
    <div class="page-block">
        <div class="row align-items-center">
            <div class="col-md-12">
                <ul class="breadcrumb">
                    <!-- Breadcrumb items here -->
                </ul>
            </div>
            <div class="col-md-12">
                <div class="page-header-title">
                    <h2 class="mb-0">@ViewBag.Title</h2>
                </div>
            </div>
        </div>
    </div>
</div>
```

**CSS Classes:**
- `.page-header` - Outer container
- `.breadcrumb` - Bootstrap breadcrumb list
- `.breadcrumb-item` - Individual breadcrumb item
- `.breadcrumb-item.active` - Current page (non-clickable)

### Migration from Custom Breadcrumbs

**Previously**, many Settings pages had their own custom breadcrumb implementations with:
- `<nav aria-label="breadcrumb">` sections
- `.breadcrumb-custom` CSS styling
- Hardcoded URLs like `/Helpdesk/Settings`

**All custom breadcrumbs have been removed** from these pages:
- WorkCategory.cshtml
- ImportantChecklist.cshtml
- JobCodeGroup.cshtml
- MaterialType.cshtml
- OtherCategory.cshtml
- OtherCategory2.cshtml
- PersonInCharge.cshtml
- RelatedDocument.cshtml
- PriorityLevel.cshtml
- PriorityLevelAdd.cshtml
- PriorityLevelEdit.cshtml
- PriorityLevelDetail.cshtml

These pages now rely solely on the shared breadcrumb component with proper `ViewBag` configuration in their controller actions.

### Best Practices

1. **Always set `ViewBag.Title`** - Required for both breadcrumb and page header
2. **Use `Url.Action()` for `pTitleUrl`** - Never hardcode URLs
3. **Keep breadcrumb depth to 3 levels max** - Home > Parent > Current
4. **Use consistent parent names** - E.g., all Settings subsections use "Settings" as parent
5. **Don't create custom breadcrumbs** - Use the shared component

### Example Controller Updates

**HelpdeskController Settings Actions** (lines 1572-2402):

```csharp
// Work Category
public IActionResult WorkCategory()
{
    ViewBag.Title = "Work Category";
    ViewBag.pTitle = "Settings";
    ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
    return View("~/Views/Helpdesk/Settings/WorkCategory.cshtml");
}

// Important Checklist
public IActionResult ImportantChecklist()
{
    ViewBag.Title = "Important Checklist";
    ViewBag.pTitle = "Settings";
    ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
    return View("~/Views/Helpdesk/Settings/ImportantChecklist.cshtml");
}

// Person in Charge
public IActionResult PersonInCharge()
{
    ViewBag.Title = "Person in Charge";
    ViewBag.pTitle = "Settings";
    ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
    return View("~/Views/Helpdesk/Settings/PersonInCharge.cshtml");
}
```

## Privilege Management System

This application implements a **session-based privilege management system** that controls user access to modules, pages, and CRUD operations. Privileges are loaded once at login and cached in session for the duration of the user's session.

### Architecture Overview

**Two-Layer Authorization:**
1. **Frontend (this app)**: UI/UX optimization + first-line security
   - Hides unauthorized buttons/links in views
   - Blocks unauthorized actions in controllers
   - Improves user experience
2. **Backend API**: Authoritative enforcement (defense in depth)
   - Every API endpoint validates permissions
   - Backend is the ultimate security boundary

### Privilege Data Structure

Privileges follow a hierarchical structure: **Module → Page → CRUD Permissions**

```csharp
// Models/Privilege/UserPrivileges.cs
public class UserPrivileges
{
    public List<ModulePrivilege> Modules { get; set; }
    public DateTime LoadedAt { get; set; }  // For staleness checking
}

public class ModulePrivilege
{
    public string ModuleName { get; set; }  // e.g., "Helpdesk"
    public List<PagePrivilege> Pages { get; set; }
}

public class PagePrivilege
{
    public string PageName { get; set; }  // e.g., "Work Request Management"
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
```

### Privilege Lifecycle

1. **Login**: Privileges loaded from `GET api/WebUser/GetUserPrivileges` and stored in session
2. **Use**: Every request checks privilege age via `BaseController.OnActionExecuting()`
3. **Auto-Refresh**: If privileges > 30 minutes old, triggers background refresh (non-blocking)
4. **Manual Refresh**: User can click "Refresh Permissions" button in profile dropdown
5. **Logout**: Privileges cleared from session

### Controller Authorization Pattern

**All controllers inherit from `BaseController`** which provides:
- Automatic smart lazy privilege refresh (background, non-blocking)
- Consistent authorization check methods

```csharp
public class HelpdeskController : BaseController
{
    public HelpdeskController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HelpdeskController> logger,
        IPrivilegeService privilegeService)  // Required dependency
        : base(privilegeService, logger)
    {
        // Controller-specific dependencies
    }

    public async Task<IActionResult> Index()
    {
        // Check if user has permission to view this page
        var accessCheck = this.CheckViewAccess("Helpdesk", "Work Request Management");
        if (accessCheck != null) return accessCheck;  // Returns AccessDenied page

        // ... rest of action
    }

    [HttpPost]
    public async Task<IActionResult> WorkRequestAdd(WorkRequestCreateRequest model)
    {
        // Check if user has permission to add
        var accessCheck = this.CheckAddAccess("Helpdesk", "Work Request Management");
        if (accessCheck != null) return accessCheck;

        // ... rest of action
    }
}
```

**Available authorization check methods:**
- `this.CheckViewAccess(moduleName, pageName)` - Check canView
- `this.CheckAddAccess(moduleName, pageName)` - Check canAdd
- `this.CheckEditAccess(moduleName, pageName)` - Check canEdit
- `this.CheckDeleteAccess(moduleName, pageName)` - Check canDelete

### View Authorization Pattern

**Add `@using cfm_frontend.Extensions` at the top of Razor views:**

```razor
@using cfm_frontend.Extensions
@model YourViewModel

@* Only show Add button if user has canAdd privilege *@
@if (Context.Session.CanAdd("Helpdesk", "Work Request Management"))
{
    <a href="@Url.Action("WorkRequestAdd", "Helpdesk")" class="btn btn-primary">
        <i class="ti ti-plus"></i> Add Work Request
    </a>
}

@* Only show Edit button if user has canEdit privilege *@
@if (Context.Session.CanEdit("Helpdesk", "Work Request Management"))
{
    <a href="@Url.Action("Edit", "Helpdesk", new { id = item.Id })" class="btn btn-info">
        <i class="ti ti-edit"></i> Edit
    </a>
}

@* Only show Delete button if user has canDelete privilege *@
@if (Context.Session.CanDelete("Helpdesk", "Work Request Management"))
{
    <button class="btn btn-danger" onclick="deleteItem(@item.Id)">
        <i class="ti ti-trash"></i> Delete
    </button>
}
```

**Available view helper methods:**
- `Context.Session.CanView(moduleName, pageName)` - Returns bool
- `Context.Session.CanAdd(moduleName, pageName)` - Returns bool
- `Context.Session.CanEdit(moduleName, pageName)` - Returns bool
- `Context.Session.CanDelete(moduleName, pageName)` - Returns bool
- `Context.Session.HasModuleAccess(moduleName)` - Returns true if user has ANY accessible page in module

### Menu Visibility Pattern

**In `Views/Shared/MenuList.cshtml`:**

```razor
@using cfm_frontend.Extensions

@* Only show module if user has access to at least one page *@
@if (Context.Session.HasModuleAccess("Helpdesk"))
{
    <li class="pc-item pc-hasmenu">
        <a href="#!" class="pc-link">
            <span class="pc-mtext">Helpdesk</span>
        </a>
        <ul class="pc-submenu">
            @if (Context.Session.CanView("Helpdesk", "Send Work Request"))
            {
                <li class="pc-item">
                    <a class="pc-link" href="/helpdesk/sendnewworkrequest">Send Work Request</a>
                </li>
            }
            @if (Context.Session.CanView("Helpdesk", "Work Request Management"))
            {
                <li class="pc-item">
                    <a class="pc-link" href="/helpdesk/index">Work Request Management</a>
                </li>
            }
        </ul>
    </li>
}
```

### Smart Lazy Privilege Refresh

**Automatic background refresh** when privileges become stale:

```csharp
// BaseController.OnActionExecuting() - runs on every request
var privileges = HttpContext.Session.GetPrivileges();
if (privileges != null)
{
    var age = DateTime.UtcNow - privileges.LoadedAt;

    // If privileges older than 30 minutes, refresh in background
    if (age.TotalMinutes > 30)
    {
        _ = Task.Run(async () =>
        {
            var newPrivileges = await _privilegeService.LoadUserPrivilegesAsync();
            if (newPrivileges != null)
                HttpContext.Session.SetPrivileges(newPrivileges);
        });
    }
}
```

**Benefits:**
- No blocking - current request continues immediately
- Minimal API load - only refreshes when > 30 min old
- Automatic - no user action required
- Fresh privileges - max 30 min stale

### Manual Privilege Refresh

**User can manually refresh via profile dropdown:**

Location: `Views/Shared/HeaderContent.cshtml` (user profile dropdown)

JavaScript function in `Views/Shared/_Layout.cshtml`:
```javascript
function refreshPrivileges() {
    $.ajax({
        url: '@Url.Action("RefreshPrivileges", "Account")',
        type: 'POST',
        success: function(response) {
            if (response.success) {
                toastr.success('Permissions refreshed. Reloading page...', 'Success');
                setTimeout(function() { location.reload(); }, 1500);
            }
        }
    });
}
```

**When to use:**
- Admin just updated user's privileges
- User suspects their permissions have changed
- Debugging privilege issues

### Access Denied Page

When a user attempts to access a page/action without proper privileges:

1. Controller calls `this.CheckViewAccess()` or similar
2. Returns `RedirectToAction("AccessDenied", "Error")` with error message in TempData
3. User sees [Views/Error/AccessDenied.cshtml](Views/Error/AccessDenied.cshtml) with:
   - Clear error message
   - "Back to Dashboard" button
   - "Refresh Permissions" button (in case permissions just updated)

### Key Files

**Models & DTOs:**
- `Models/Privilege/UserPrivileges.cs` - Main privilege model with helper methods
- `Models/Privilege/ModulePrivilege.cs` - Module-level privilege
- `Models/Privilege/PagePrivilege.cs` - Page-level CRUD permissions
- `DTOs/Privilege/UserPrivilegesResponse.cs` - API response DTO

**Services:**
- `Services/PrivilegeService.cs` - Loads privileges from API
- `Services/IPrivilegeService.cs` - Service interface

**Controllers:**
- `Controllers/BaseController.cs` - Smart lazy refresh + authorization helpers
- `Controllers/AccountController.cs` - Manual refresh endpoint
- `Controllers/ErrorController.cs` - Access denied page

**Extensions:**
- `Extensions/SessionExtensions.cs` - Type-safe session get/set for privileges
- `Extensions/ControllerExtensions.cs` - Authorization check methods for controllers
- `Extensions/ViewPrivilegeExtensions.cs` - Privilege check methods for Razor views

**Views:**
- `Views/Error/AccessDenied.cshtml` - 403 error page
- `Views/Shared/MenuList.cshtml` - Navigation menu with privilege checks
- `Views/Shared/HeaderContent.cshtml` - Profile dropdown with "Refresh Permissions" button
- `Views/Shared/_Layout.cshtml` - Contains `refreshPrivileges()` JavaScript function

### Privilege Loading Implementation Details

**IMPORTANT: Two Loading Modes**

The `PrivilegeService` supports two distinct modes for loading privileges, designed to handle different lifecycle stages of the authentication process:

#### 1. Explicit Token Mode (Login Flow)

Used during the login process **before** the authentication cookie is created:

```csharp
// In LoginController.SignIn() - Line 99
var privileges = await _privilegeService.LoadUserPrivilegesAsync(authResponse.Token);
```

**Why this is necessary:**
- At login time (line 99), privileges are loaded **before** the authentication cookie is created (line 136)
- The `AuthTokenHandler` retrieves tokens from `HttpContext.GetTokenAsync("access_token")`, which requires an authenticated session
- Since the cookie doesn't exist yet, `AuthTokenHandler` cannot inject the bearer token
- Solution: Pass the access token explicitly to the service, bypassing the need for the cookie

**Implementation:**
```csharp
// Services/PrivilegeService.cs - LoadUserPrivilegesAsync(string accessToken)
public async Task<UserPrivileges?> LoadUserPrivilegesAsync(string accessToken)
{
    // Use plain HttpClient (NOT "BackendAPI") to avoid AuthTokenHandler dependency
    var client = _httpClientFactory.CreateClient();
    var backendUrl = _configuration["BackendBaseUrl"];

    // Manually add bearer token to request headers
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.UserInfo.GetUserPrivileges}");
    // ... rest of implementation
}
```

#### 2. Session Token Mode (Background Refresh)

Used for automatic privilege refresh after user is authenticated:

```csharp
// In BaseController.OnActionExecuting() - Background refresh
var privileges = await _privilegeService.LoadUserPrivilegesAsync();
```

**Why this is necessary:**
- After login, the user has an active authenticated session
- The authentication cookie contains the access token and refresh token
- The parameterless overload can retrieve the token from the session and delegate to the explicit token method

**Implementation:**
```csharp
// Services/PrivilegeService.cs - LoadUserPrivilegesAsync()
public async Task<UserPrivileges?> LoadUserPrivilegesAsync()
{
    var context = _httpContextAccessor.HttpContext;
    if (context != null)
    {
        var accessToken = await context.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(accessToken))
        {
            // Delegate to the explicit token method
            return await LoadUserPrivilegesAsync(accessToken);
        }
    }

    _logger.LogWarning("Cannot load privileges: No access token available in context");
    return null;
}
```

#### Why NOT Use "BackendAPI" HttpClient for Privileges?

**The "BackendAPI" named HttpClient has `AuthTokenHandler` configured**, which automatically:
1. Retrieves access token from `HttpContext.GetTokenAsync("access_token")`
2. Injects it as a Bearer token in request headers
3. Handles 401 responses by refreshing tokens

**This creates a chicken-and-egg problem during login:**
- Privileges need to be loaded at **line 99** of `LoginController.SignIn()`
- Authentication cookie is created at **line 136** (37 lines later)
- `AuthTokenHandler` expects the cookie to exist to retrieve the token
- Without the cookie, the request fails with "response ended prematurely"

**Solution:** Use a plain `HttpClient` and manually add the Authorization header, completely bypassing `AuthTokenHandler` for privilege loading.

#### Login Flow Timeline

```
1. POST /api/auth/login → Returns { token, refreshToken }
2. GET /api/auth/userinfo (with Bearer token) → Returns UserInfo
3. Store UserInfo in Session["UserSession"]
4. ✅ GET /api/WebUser/GetUserPrivileges (with explicit token) → Returns UserPrivileges
5. Store UserPrivileges in Session["UserPrivileges"]
6. Create authentication cookie with tokens
7. Redirect to Dashboard
```

**Key Point:** Step 4 happens **before** step 6, so we must pass the token explicitly.

#### Dependencies for PrivilegeService

```csharp
public PrivilegeService(
    IHttpClientFactory httpClientFactory,      // For creating HttpClient instances
    IConfiguration configuration,              // For BackendBaseUrl
    IHttpContextAccessor httpContextAccessor,  // For session-based token retrieval
    ILogger<PrivilegeService> logger)          // For logging
{
    // ...
}
```

**Why `IHttpContextAccessor` is needed:**
- Required for the parameterless `LoadUserPrivilegesAsync()` method
- Allows access to `HttpContext.GetTokenAsync()` for session-based token retrieval
- Registered in `Program.cs` via `builder.Services.AddHttpContextAccessor()`

### Best Practices

1. **Always inherit from BaseController** for automatic privilege refresh
2. **Check privileges in controllers** before executing actions (security)
3. **Hide unauthorized UI elements** in views (UX)
4. **Use exact module/page names** from the backend API privilege structure
5. **Handle missing privileges gracefully** - extension methods return false if privileges not loaded
6. **Test both scenarios**: user with privileges AND user without privileges
7. **CRITICAL: Never use "BackendAPI" HttpClient during login flow** - The named "BackendAPI" client has `AuthTokenHandler` configured, which requires an authenticated session cookie to retrieve the access token. During login (before the cookie is created), always use a plain `HttpClient` instance and manually add the Authorization header. Any API calls made before `HttpContext.SignInAsync()` MUST NOT use the "BackendAPI" client.

### Common Privilege Names (Helpdesk Module)

Based on the backend API response structure:

```
Module: "Helpdesk"
├── Page: "Send Work Request"
│   ├── canView: bool
│   ├── canAdd: bool
│   ├── canEdit: bool
│   └── canDelete: bool
├── Page: "Work Request Management"
│   ├── canView: bool
│   ├── canAdd: bool
│   ├── canEdit: bool
│   └── canDelete: bool
└── Page: "Settings"
    ├── canView: bool
    ├── canAdd: bool
    ├── canEdit: bool
    └── canDelete: bool
```

## Key Controllers

### BaseController (`Controllers/BaseController.cs`)
**All controllers should inherit from this base class** which provides:
- Smart lazy privilege refresh (automatic background refresh when > 30 min old)
- Authorization helper methods: `CheckViewAccess()`, `CheckAddAccess()`, `CheckEditAccess()`, `CheckDeleteAccess()`
- Requires `IPrivilegeService` and `ILogger` dependencies

### LoginController (`Controllers/LoginController.cs`)
Inherits from `BaseController`
- `Index()` - Login page
- `SignIn(SignInViewModel)` - Authenticate user, fetch user info, **load privileges**, create session
- `Logout()` - Clear session (including privileges) and sign out

### AccountController (`Controllers/AccountController.cs`)
Inherits from `BaseController`
- `RefreshPrivileges()` - POST endpoint for manual privilege refresh
- Returns JSON: `{ success: bool, message: string, timestamp: DateTime }`

### ErrorController (`Controllers/ErrorController.cs`)
Inherits from `BaseController`
- `AccessDenied()` - Displays 403 error page when user lacks required privileges
- Reads error message from `TempData["AccessDeniedMessage"]`

### HelpdeskController (`Controllers/HelpdeskController.cs`)
Inherits from `BaseController`
- Largest controller (1000+ lines) handling Work Request management
- **All actions have authorization checks** using `CheckViewAccess()`, `CheckAddAccess()`, etc.
- `Index()` - Work Request list page with filters (requires canView)
- `WorkRequestAdd()` GET - Display add form (requires canView)
- `WorkRequestAdd(WorkRequestCreateRequest)` POST - Create new work request (requires canAdd)
- `SendNewWorkRequest()` GET - End-user work request form (requires canView)
- `SendNewWorkRequest(...)` POST - Submit end-user work request (requires canAdd)
- `WorkRequestDetail(int id)` - View work request details (requires canView)
- API endpoints for dynamic data: `GetFloorsByLocation`, `GetRoomsByFloor`, `SearchEmployees`, etc.

### ClientController (`Controllers/ClientController.cs`)
Inherits from `BaseController`
- Client switching functionality for users with multiple client access
- **Note**: Does NOT refresh privileges on client switch (privileges are user-specific, not client-specific)

## Important Notes

### Namespace Inconsistency
- Most files use `cfm_frontend` namespace
- Some controllers use `Mvc.Controllers` namespace
- When creating new files, use `cfm_frontend` for consistency

### Session vs Claims
- **Session** (`HttpContext.Session["UserSession"]`) contains full `UserInfo` object
- **Claims** (`User.Claims`) contain basic identity info (Name, Email, Role, UserId)
- Both are set during login, but session has more detailed information (e.g., `PreferredClientId`, `Department`)

### Error Handling and Notifications

This application uses **toastr** for all user-facing notifications and errors. The notification system is globally available and standardized across all views and JavaScript modules.

#### Toastr Configuration

**Library**: [Toastr](https://github.com/CodeSeven/toastr) (loaded via CDN)
- **CSS**: `https://cdnjs.cloudflare.com/ajax/libs/toastr.js/latest/toastr.min.css`
- **JS**: `https://cdnjs.cloudflare.com/ajax/libs/toastr.js/latest/toastr.min.js`
- **Location**: Loaded in [Views/Shared/HeadCSS.cshtml](Views/Shared/HeadCSS.cshtml) and [Views/Shared/VendorScripts.cshtml](Views/Shared/VendorScripts.cshtml)

**Global Configuration** (in [Views/Shared/_Layout.cshtml](Views/Shared/_Layout.cshtml)):
```javascript
toastr.options = {
    "closeButton": true,
    "newestOnTop": true,
    "progressBar": true,
    "positionClass": "toast-top-right",
    "timeOut": "5000",
    "extendedTimeOut": "1000",
    "showMethod": "fadeIn",
    "hideMethod": "fadeOut"
};
```

#### Global Notification Helper Function

A global `showNotification()` function is available on all pages (defined in [Views/Shared/_Layout.cshtml](Views/Shared/_Layout.cshtml)):

```javascript
/**
 * Global notification helper function using toastr
 * Parameters:
 *   - message (string): The message to display
 *   - type (string): Notification type: 'success', 'error', 'warning', 'info'
 *   - title (string): Optional title for the notification
 *   - options (object): Optional toastr options to override defaults
 */
function showNotification(message, type = 'info', title = '', options = {})
```

**Note:** JSDoc `@param` syntax cannot be used in Razor views as the `@` symbol is interpreted as Razor syntax.

**Usage examples:**
```javascript
// Success notification
showNotification('Work request created successfully', 'success', 'Success');

// Error notification
showNotification('Failed to load data', 'error', 'Error');

// Warning notification
showNotification('This action cannot be undone', 'warning', 'Warning');

// Info notification
showNotification('Loading data...', 'info', 'Please wait');

// With custom options
showNotification('Saved!', 'success', '', { timeOut: 2000 });
```

#### Notification Types

Four notification types are available:
1. **success** - Green toast for successful operations
2. **error** - Red toast for errors and failures
3. **warning** - Orange/yellow toast for warnings
4. **info** - Blue toast for informational messages

#### Server-Side Error Handling Pattern

All controllers follow this pattern:
```csharp
try
{
    // API calls
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error message");
    ModelState.AddModelError(string.Empty, "User-friendly error message");
}
```

For AJAX endpoints, return JSON with error information:
```csharp
try
{
    // API call logic
    return Json(new { success = true, data = result });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error creating work request");
    return Json(new { success = false, message = "Failed to create work request" });
}
```

#### Client-Side Error Handling Pattern

**In Razor Views** - Display TempData messages as toastr:
```razor
@section Scripts {
    <script>
        @if (TempData["ErrorMessage"] != null)
        {
            <text>
            showNotification('@Html.Raw(TempData["ErrorMessage"])', 'error', 'Error');
            </text>
        }

        @if (TempData["SuccessMessage"] != null)
        {
            <text>
            showNotification('@Html.Raw(TempData["SuccessMessage"])', 'success', 'Success');
            </text>
        }
    </script>
}
```

**In JavaScript/AJAX** - Standard error handling:
```javascript
$.ajax({
    url: '/api/endpoint',
    method: 'POST',
    data: JSON.stringify(payload),
    contentType: 'application/json',
    success: function(response) {
        if (response.success) {
            showNotification('Operation completed successfully', 'success', 'Success');
            // Additional success logic
        } else {
            showNotification(response.message || 'Operation failed', 'error', 'Error');
        }
    },
    error: function(xhr, status, error) {
        console.error('AJAX error:', error);
        showNotification('Network error. Please try again.', 'error', 'Error');
    }
});
```

**Fetch API Pattern:**
```javascript
fetch('/api/endpoint', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
})
.then(response => response.json())
.then(data => {
    if (data.success) {
        showNotification('Operation successful', 'success', 'Success');
    } else {
        showNotification(data.message || 'Operation failed', 'error', 'Error');
    }
})
.catch(error => {
    console.error('Error:', error);
    showNotification('Network error. Please try again.', 'error', 'Error');
});
```

#### Validation Error Display

**Server-side validation** (Login page example):
```razor
<div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>
<span asp-validation-for="Username" class="text-danger small"></span>
```

**Client-side validation** (JavaScript):
```javascript
// Show validation error
$('#inputField').addClass('is-invalid');
$('#validationError').text('Field is required').addClass('d-block');

// Clear validation error
$('#inputField').removeClass('is-invalid');
$('#validationError').text('').removeClass('d-block');
```

#### Modal Confirmations

For destructive actions (delete, etc.), use Bootstrap modals for confirmation:
```html
<div class="modal fade" id="deleteConfirmModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Confirm Delete</h5>
            </div>
            <div class="modal-body">
                <p>Are you sure you want to delete this item?</p>
                <p class="text-danger small mt-2 mb-0">
                    <i class="ti ti-alert-triangle me-1"></i>
                    This action cannot be undone.
                </p>
            </div>
            <div class="modal-footer">
                <button class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <button class="btn btn-danger" id="confirmDeleteBtn">Delete</button>
            </div>
        </div>
    </div>
</div>
```

#### Best Practices

1. **Always use `showNotification()`** - Never use `alert()`, custom toast implementations, or inline Bootstrap alerts for dynamic messages
2. **Console logging** - Always log errors to console in addition to showing user notifications:
   ```javascript
   .catch(error => {
       console.error('Error details:', error);
       showNotification('User-friendly message', 'error', 'Error');
   });
   ```
3. **User-friendly messages** - Show technical details in console, user-friendly messages in toastr
4. **Success feedback** - Always provide visual feedback for successful operations
5. **Error specificity** - Use `response.message` from server when available, fall back to generic message
6. **Timeout for success** - Success notifications can have shorter timeout if followed by redirect:
   ```javascript
   showNotification('Saved! Redirecting...', 'success', 'Success', { timeOut: 1500 });
   setTimeout(() => location.reload(), 1500);
   ```
7. **C# API Endpoints constants are centralized on Constants/APIEndpoint.cs, if there are any changes or addition on endpoints refer to this class.
8. **Javascript Endpoints constant are centralized on assets/js/mvc-endpoints.js, if there are any changes or addition on endpoints refer to this class.
9. **Do not add comment that is too AI, add comment only for documentation and for warning the next developer.

#### VIews Design
Use the light able bootstrap as the basis for any design or html related things. make sure the design adhere to industry standard best practice regarding UI/UX of Modern application.

#### Migration Notes

**Legacy patterns removed:**
- Custom `showNotification()` functions in individual JavaScript modules ✅ Removed
- Bootstrap Toast implementations ✅ Replaced with toastr
- Bootstrap Alert notifications for dynamic content ✅ Replaced with toastr
- `alert()` fallbacks ✅ Removed (toastr now always available)

**Files updated:**
- [Views/Shared/_Layout.cshtml](Views/Shared/_Layout.cshtml) - Global `showNotification()` function and toastr config
- [Views/Shared/HeadCSS.cshtml](Views/Shared/HeadCSS.cshtml) - Toastr CSS
- [Views/Shared/VendorScripts.cshtml](Views/Shared/VendorScripts.cshtml) - Toastr JS
- [Views/Error/AccessDenied.cshtml](Views/Error/AccessDenied.cshtml) - TempData → toastr
- [Views/Shared/Sidebar.cshtml](Views/Shared/Sidebar.cshtml) - Client switching errors → toastr
- [wwwroot/assets/js/pages/settings/*.js](wwwroot/assets/js/pages/settings/) - Removed local implementations

### Static Assets
- Bootstrap 5, jQuery, and custom "pcoded" theme
- Located in `wwwroot/` directory
- Vendor scripts loaded via `Views/Shared/VendorScripts.cshtml`

## Middleware Order (Program.cs)

The middleware pipeline order is critical:
1. `app.UseHttpsRedirection()`
2. `app.UseStaticFiles()`
3. `app.UseRouting()`
4. `app.UseSession()` - Must come before authentication
5. `app.UseAuthentication()`
6. `app.UseAuthorization()`
7. `app.MapControllerRoute()`
