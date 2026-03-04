# CLAUDE.md

Project guidance for Claude Code when working with this ASP.NET Core 8.0 MVC frontend application.

## Documentation Structure

This repository contains three reference documents:

### 1. CLAUDE.md (This File)
**Purpose:** Frontend-specific guidance for the ASP.NET Core 8.0 MVC application.
**When to Use:** Always loaded by default for all frontend development tasks.
**Contains:**
- Frontend architecture (MVC, Controllers, Views, ViewModels)
- Common frontend patterns (API calls, pagination, dropdowns)
- Session management and authentication flow
- JavaScript patterns and components
- UI/UX guidelines

### 2. BACKEND_REFERENCE.md
**Purpose:** Comprehensive backend API reference for integration work.
**When to Use:** Only load when you need to:
- Understand backend API endpoints and their contracts
- Debug backend integration issues
- Implement new features that call backend APIs
- Understand master data structure (Type vs Enum)
- Reference API request/response formats

**When NOT to Use:**
- Pure frontend work (Views, JavaScript, CSS)
- Existing integrations that are working correctly
- Client-side debugging

**Location:** `BACKEND_REFERENCE.md` in the same directory as this file.

**Important:** The backend repository is located at `C:\Users\kenri\OneDrive\Documents\Work\Colliers\Repo\CFM Backend\CFM Backend` and should NOT be modified. The BACKEND_REFERENCE.md is for reference only.

### 3. PLAYWRIGHT_TESTING_GUIDE.md
**Purpose:** Comprehensive guide for writing E2E tests using Playwright.
**When to Use:** Load when you need to:
- Write new Playwright tests for a page
- Understand testing patterns (CRUD, validation, modals)
- Debug failing tests
- Set up test authentication
- Create Page Objects for new pages
- Reference test examples and recipes

**When NOT to Use:**
- General frontend development without testing
- Backend API testing (use backend test docs if available)

**Location:** `PLAYWRIGHT_TESTING_GUIDE.md` in the same directory as this file.
**Test Project:** `c:\Repos\CfmFrontend.Tests`

---

## Project Overview

**ASP.NET Core 8.0 MVC** frontend acting as BFF (Backend for Frontend) layer. All business logic in backend API.
- Communication: JSON over HTTP with automatic bearer token injection
- Auth: Cookie-based with bearer token handling
- Primary feature: Work Request Management (Helpdesk)

**Critical Rule:** Never break existing functionality without confirmation.

## Development

```bash
dotnet build          # Build
dotnet run            # Run (http://localhost:5099, https://localhost:7035)
dotnet restore        # Restore packages
```

**Required Config** (`appsettings.json`):
```json
{ "BackendBaseURL": "https://your-backend-api-url" }
```

## Architecture

### MVC Structure
- **Controllers**: Coordinate requests, aggregate API data, manage session, return Views. NO business logic.
- **Models**: Domain models (`UserInfo`, `PagingInfo`, `Models/WorkRequest/`)
- **DTOs**: API request/response structures (`LoginResponse`, `WorkRequestCreateRequest`)
- **ViewModels**: Aggregate data from multiple APIs for complex views
- **Views**: Server-rendered Razor templates (`Views/Shared/`)

### BackendModels Folder (Reference Only)

**⚠️ IMPORTANT:** The `BackendModels/` folder contains model classes copied from the backend repository for **reference purposes only**. DO NOT use these models directly in the frontend code.

**Purpose:** To understand backend data structures when creating frontend DTOs/Models that need to match backend APIs.

**Key Backend Models for Reference:**
- `BackendModels/Models/DataModels/Type.cs` - Master data for client-specific choice sets (Work Categories, Other Categories, etc.)
  - Properties: `IdType`, `ClientIdClient`, `ParentTypeIdType`, `Category`, `Text`, `DisplayOrder`, `IsActiveData`
- `BackendModels/Models/DataModels/Enum.cs` - System-wide fixed choice sets (non-client-specific)

**Usage Rule:** When creating frontend DTOs that need to match backend structures:
1. Check `BackendModels/` to understand the backend model structure
2. Create a **new DTO** in the `DTOs/` folder with only the properties needed
3. Never reference `BackendModels` namespace directly in frontend code

### Backend API Integration

Use named `HttpClient` "BackendAPI":
```csharp
var client = _httpClientFactory.CreateClient("BackendAPI");
var response = await client.GetAsync($"{backendUrl}/api/endpoint");
```

**AuthTokenHandler** (automatic):
1. Injects bearer token
2. Handles 401 by refreshing tokens
3. Retries with new token
4. Signs out if refresh fails

**⚠️ CRITICAL:** DO NOT use "BackendAPI" during login (before `HttpContext.SignInAsync()`). Use plain `HttpClient` + manual Authorization header. AuthTokenHandler requires authenticated session cookie.

### Authentication Flow
1. POST `/api/auth/login` → tokens
2. GET `/api/auth/userinfo` → UserInfo
3. Store in `Session["UserSession"]`
4. GET `/api/WebUser/GetUserPrivileges` → UserPrivileges (use plain HttpClient + explicit token)
5. Store in `Session["UserPrivileges"]`
6. Create auth cookie with tokens
7. Redirect to Dashboard

**Session Data:**
```csharp
var userInfo = JsonSerializer.Deserialize<UserInfo>(HttpContext.Session.GetString("UserSession"));
var privileges = HttpContext.Session.GetPrivileges();
```

**Token Storage:** In auth cookie, retrieved via `await HttpContext.GetTokenAsync("access_token")`

### Remember Me Feature

**Files:** `LoginController.cs`, `AuthTokenHandler.cs`, `Program.cs`

When user checks "Remember me?" during login:
- Auth cookie is set with `IsPersistent = true` and 30-day expiration
- Cookie survives browser close/reopen
- Session data is auto-restored via `SessionRestoreMiddleware`

**Key Implementation Points:**

1. **LoginController.cs** - Sets persistence on login:
```csharp
var authProperties = new AuthenticationProperties
{
    IsPersistent = model.RememberMe,
    ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(12)
};
```

2. **AuthTokenHandler.cs** - Preserves persistence during token refresh:
```csharp
// When refreshing tokens, preserve Remember Me setting
if (properties.IsPersistent == true)
{
    properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
}
```

3. **SessionRestoreMiddleware** - Auto-restores session when cookie valid but session expired

**Landing Page Auth Detection:**
`HomeController.Index()` checks `User.Identity.IsAuthenticated` and passes to view:
- If authenticated: Shows username linking to Dashboard instead of LOGIN button
- If not authenticated: Shows LOGIN button

### Data Protection (Cookie Encryption Keys)

**File:** `Program.cs`

Auth cookies are encrypted using Data Protection keys. Configuration ensures keys persist across app restarts.

```csharp
// Persist Data Protection keys so auth cookies survive app restarts
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "Keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("CFM-Frontend");
```

**Key Storage by Environment:**

| Environment | Key Storage | Notes |
|-------------|-------------|-------|
| Development (F5) | `Keys/` folder | Required for cookies to survive app restart |
| IIS on VM | `Keys/` folder | Works fine, keys persist on disk |
| Load Balanced | ⚠️ Shared storage needed | Use network share, Redis, or database |
| Containers/K8s | ⚠️ Persistent volume needed | Or use Azure Blob/Redis |

**Current Deployment:** IIS on VM - file-based keys work correctly.

**Future Migration Notes:**
- For **load balancer**: Change to shared network path or Redis
- For **Azure**: Use `PersistKeysToAzureBlobStorage()`
- For **containers**: Use persistent volume or external key store

**⚠️ IMPORTANT:** The `Keys/` folder is in `.gitignore` - do not commit encryption keys to source control.

### DI Configuration (`Program.cs`)
```csharp
builder.Services.AddHttpClient("BackendAPI", c => c.BaseAddress = new Uri(config["BackendBaseUrl"]))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddScoped<IPrivilegeService, PrivilegeService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...);
builder.Services.AddSession(...);
```

## File Locations Quick Reference

When creating new files, follow these conventions:

| Type | Location | Naming | Example |
|------|----------|--------|---------|
| Controller | `Controllers/` | `{Feature}Controller.cs` | `HelpdeskController.cs` |
| ViewModel | `ViewModels/` | `{Feature}ViewModel.cs` | `WorkCategoryViewModel.cs` |
| DTO | `DTOs/{Feature}/` | `{Feature}{Operation}Dto.cs` | `WorkCategoryPayloadDto.cs` |
| Model | `Models/{Feature}/` | `{Feature}Model.cs` | `WorkRequestFilterModel.cs` |
| View | `Views/{Controller}/{Action}/` | `{Action}.cshtml` | `Settings/WorkCategory.cshtml` |
| Page JS | `wwwroot/assets/js/pages/{area}/` | `{page-name}.js` | `settings/work-category.js` |
| Component JS | `wwwroot/assets/js/components/` | `{component}.js` | `searchable-dropdown.js` |
| Helper JS | `wwwroot/assets/js/helpers/` | `{helper}.js` | `client-session-monitor.js` |
| CSS | `wwwroot/assets/css/components/` | `{component}.css` | `searchable-dropdown.css` |

## Common Patterns

### User Session
```csharp
var userInfo = JsonSerializer.Deserialize<UserInfo>(
    HttpContext.Session.GetString("UserSession"),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
var idClient = userInfo.PreferredClientId;
var idEmployee = userInfo.UserId;
```

### API Response Format

All backend API responses use the unified `ApiResponseDto<T>` wrapper (`DTOs/BaseResponse.cs`):

```csharp
public class ApiResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

**Usage Pattern:**
```csharp
var response = await client.GetAsync($"{backendUrl}/api/endpoint");
var responseStream = await response.Content.ReadAsStreamAsync();
var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<MyDataType>>(
    responseStream,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
);

if (apiResponse?.Success == true && apiResponse.Data != null)
{
    // Use apiResponse.Data
}
else
{
    // Handle error: apiResponse?.Message, apiResponse?.Errors
}
```

**Specialized Response Types:**
- `LoginResponse : ApiResponseDto<TokenData>` - Authentication responses

### API Calls
```csharp
var client = _httpClientFactory.CreateClient("BackendAPI");
var backendUrl = _configuration["BackendBaseUrl"];

// GET
var response = await client.GetAsync($"{backendUrl}/api/endpoint?param={value}");

// POST
var payload = new { field1 = value1 };
var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync($"{backendUrl}/api/endpoint", content);

// Deserialize with ApiResponseDto
var responseStream = await response.Content.ReadAsStreamAsync();
var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<ResponseType>>(
    responseStream,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

if (apiResponse?.Success == true && apiResponse.Data != null)
{
    var result = apiResponse.Data;
}
```

### Parallel API Calls
```csharp
await Task.WhenAll(GetLocationsAsync(...), GetCategoriesAsync(...), GetProvidersAsync(...));
```

### Multi-Tab Session Safety

**Problem:** User opens form in Tab A with Client X, switches to Client Y in Tab B, returns to Tab A and submits. Without safeguards, the form submits with wrong client context.

**Solution Pattern:**
1. **Capture client context at page load** (ViewModel + JS)
2. **Pass captured context in all AJAX calls** (optional params)
3. **Monitor for client changes** (ClientSessionMonitor)
4. **Validate on form submission** (backend)

**Implementation:**

**1. ViewModel - Capture at page load:**
```csharp
// In GET action
viewmodel.IdClient = userInfo.PreferredClientId;
viewmodel.IdCompany = userInfo.IdCompany;
```

**2. View - Expose to JavaScript:**
```html
<script>
    window.PageContext = {
        idClient: @Model.IdClient,
        idCompany: @Model.IdCompany
    };
</script>
```

**3. JavaScript - Client context helpers:**
```javascript
const clientContext = {
    get idClient() { return window.PageContext?.idClient || null; },
    get idCompany() { return window.PageContext?.idCompany || null; }
};

function getClientParams() {
    const params = {};
    if (clientContext.idClient) params.idClient = clientContext.idClient;
    if (clientContext.idCompany) params.idCompany = clientContext.idCompany;
    return params;
}

function withClientParams(data) {
    return { ...getClientParams(), ...data };
}

// Use in AJAX calls
$.ajax({
    url: '/api/endpoint',
    data: getClientParams()  // or withClientParams({ otherParam: value })
});
```

**4. Controller - Optional parameters with fallback:**
```csharp
[HttpGet]
public async Task<IActionResult> GetData(int? idClient = null)
{
    var userSessionJson = HttpContext.Session.GetString("UserSession");
    var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, ...);

    // Use passed idClient if provided, otherwise fall back to session
    var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

    // Use effectiveIdClient in API calls...
}
```

**5. Form Submission - Validate client match:**
```javascript
// Set captured client in form payload
const payload = {
    // ... form fields ...
    Client_idClient: clientContext.idClient  // Page-load client
};
```

```csharp
// Backend validation
[HttpPost]
public async Task<IActionResult> SubmitForm([FromBody] RequestDto model)
{
    var userInfo = // ... get from session
    var sessionClient = userInfo.PreferredClientId;

    // Reject if submitted client doesn't match current session
    if (model.Client_idClient != sessionClient)
    {
        return Json(new {
            success = false,
            message = "Client context has changed. Please refresh the page.",
            clientMismatch = true
        });
    }

    // Proceed with submission using model.Client_idClient
}
```

**6. Tab Focus Monitoring:**
```javascript
// Initialize ClientSessionMonitor (see Client Session Monitor section)
const monitor = new ClientSessionMonitor({
    pageLoadClientId: clientContext.idClient
});
monitor.start();
```

**When to Use:**
- ✅ Always use for forms that submit client-specific data
- ✅ Use for pages with client-specific AJAX calls
- ⚠️ Optional for read-only pages
- ❌ Not needed for client-agnostic pages

**⚠️ MANDATORY FOR NEW IMPLEMENTATIONS:**
When creating new pages or features that involve client-specific CRUD operations (Settings pages, Work Request forms, etc.), you MUST follow this pattern:
1. Add `IdClient` property to the ViewModel
2. Set `viewmodel.IdClient = userInfo.PreferredClientId` in the controller GET action
3. Expose via `window.PageContext = { idClient: @Model.IdClient }` in the View
4. Use `clientContext.idClient` in JavaScript AJAX payloads instead of hardcoding `0`

**Reference Implementation:** See `Views/Helpdesk/Settings/WorkCategory.cshtml` and `wwwroot/assets/js/pages/settings/work-category.js`

### Pagination
**Component:** `Views/Shared/_Pagination.cshtml` (auto-preserves query params)
**Model:** `Models/PagingInfo.cs`

```csharp
// Controller
public async Task<IActionResult> Index(int page = 1, string search = "") {
    var response = await GetDataAsync(page, search);
    viewmodel.Paging = new PagingInfo {
        CurrentPage = response.CurrentPage,
        TotalPages = response.TotalPages,
        PageSize = response.PageSize,
        TotalRecords = response.TotalCount
    };
    return View(viewmodel);
}
```

```razor
@* View *@
@await Html.PartialAsync("_Pagination", Model.Paging)
```

### Searchable Dropdown
**Files:** `wwwroot/assets/css/components/searchable-dropdown.css`, `wwwroot/assets/js/components/searchable-dropdown.js`

```html
<select data-searchable="true" data-placeholder="Select..." data-search-placeholder="Search...">
    <option value="">Select Option</option>
</select>
```

**JS API:**
```javascript
const dd = new SearchableDropdown('#mySelect', { placeholder: '...', onChange: (val, label) => {} });
dd.enable(); dd.disable(); dd.clear(); dd.setValue('2', 'Label', true);
```

**Cascade Pattern:** Use `onChange` to enable/populate dependent dropdowns. Call `clear()`, `loadFromSelect()`, `disable()` on children when parent changes.

### Loading Spinners for AJAX Operations
**Universal Pattern:** Always show loading indicators when fetching data via AJAX to provide visual feedback to users.

**Implementation Patterns:**

#### 1. Table/List Loading (Full Container)
For paginated lists or tables where the entire content area should show a spinner.

**HTML:**
```html
<div id="dataContainer">
    <div class="loading-spinner text-center py-5" id="loadingSpinner">
        <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2 text-muted">Loading data...</p>
    </div>
    <div id="dataContent" style="display: none;">
        <!-- Table or list content here -->
    </div>
    <div id="emptyState" style="display: none;">
        <!-- Empty state here -->
    </div>
</div>
```

**JavaScript:**
```javascript
async function loadData() {
    const spinner = document.getElementById('loadingSpinner');
    const content = document.getElementById('dataContent');
    const emptyState = document.getElementById('emptyState');

    // Show spinner
    spinner.style.display = 'block';
    content.style.display = 'none';
    emptyState.style.display = 'none';

    try {
        const response = await fetch('/api/data');
        const result = await response.json();

        // Hide spinner
        spinner.style.display = 'none';

        if (result.success && result.data && result.data.length > 0) {
            renderData(result.data);
            content.style.display = 'block';
        } else {
            emptyState.style.display = 'block';
        }
    } catch (error) {
        spinner.style.display = 'none';
        showError('Failed to load data');
    }
}
```

#### 2. Form Field Loading (Inline Spinner)
For dropdowns, checkboxes, or input fields being populated via AJAX.

**HTML:**
```html
<div class="mb-3">
    <label class="form-label">Properties</label>
    <div class="field-loading-container" id="propertiesContainer">
        <div class="field-spinner" id="propertiesSpinner">
            <div class="spinner-border spinner-border-sm text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <span class="ms-2 text-muted">Loading properties...</span>
        </div>
        <div id="propertiesGrid" style="display: none;">
            <!-- Checkboxes or options here -->
        </div>
    </div>
</div>
```

**JavaScript:**
```javascript
async function loadProperties() {
    const spinner = document.getElementById('propertiesSpinner');
    const grid = document.getElementById('propertiesGrid');

    spinner.style.display = 'flex';
    grid.style.display = 'none';

    try {
        const response = await fetch('/api/properties');
        const result = await response.json();

        spinner.style.display = 'none';

        if (result.success && result.data) {
            renderProperties(result.data);
            grid.style.display = 'block';
        }
    } catch (error) {
        spinner.style.display = 'none';
        showError('Failed to load properties');
    }
}
```

#### 3. Parallel Loading with Multiple Spinners
When loading multiple data sources simultaneously.

**JavaScript:**
```javascript
async function loadInitialData() {
    // Show all spinners
    showSpinner('propertiesSpinner');
    showSpinner('categoriesSpinner');
    showSpinner('currenciesSpinner');

    // Load in parallel
    await Promise.all([
        loadProperties(),
        loadCategories(),
        loadCurrencies()
    ]);
}

async function loadProperties() {
    try {
        const response = await fetch('/api/properties');
        const result = await response.json();

        if (result.success && result.data) {
            renderProperties(result.data);
        }
    } finally {
        hideSpinner('propertiesSpinner');
        showContent('propertiesGrid');
    }
}

function showSpinner(id) {
    const el = document.getElementById(id);
    if (el) el.style.display = 'flex';
}

function hideSpinner(id) {
    const el = document.getElementById(id);
    if (el) el.style.display = 'none';
}

function showContent(id) {
    const el = document.getElementById(id);
    if (el) el.style.display = 'block';
}
```

#### 4. Button Loading State
For submit buttons or action buttons during AJAX operations.

**HTML:**
```html
<button type="submit" class="btn btn-primary" id="saveBtn">
    <span class="btn-text">Save</span>
    <span class="btn-spinner" style="display: none;">
        <span class="spinner-border spinner-border-sm" role="status"></span>
        Saving...
    </span>
</button>
```

**JavaScript:**
```javascript
async function handleSubmit(e) {
    e.preventDefault();

    const btn = document.getElementById('saveBtn');
    const btnText = btn.querySelector('.btn-text');
    const btnSpinner = btn.querySelector('.btn-spinner');

    // Show loading state
    btn.disabled = true;
    btnText.style.display = 'none';
    btnSpinner.style.display = 'inline-block';

    try {
        const response = await fetch('/api/save', {
            method: 'POST',
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (result.success) {
            showSuccess('Saved successfully');
        }
    } finally {
        // Restore button state
        btn.disabled = false;
        btnText.style.display = 'inline';
        btnSpinner.style.display = 'none';
    }
}
```

**CSS (add to page or global styles):**
```css
.loading-spinner {
    min-height: 200px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
}

.field-loading-container {
    position: relative;
    min-height: 50px;
}

.field-spinner {
    display: flex;
    align-items: center;
    padding: 1rem;
    color: #6c757d;
}

[data-pc-theme="dark"] .field-spinner {
    color: #9ca3af;
}
```

**Best Practices:**
1. Always show spinner before AJAX call starts
2. Always hide spinner in `finally` block to ensure cleanup
3. Use `try/finally` or `try/catch/finally` to guarantee spinner removal
4. Disable interactive elements (buttons, inputs) during loading
5. Use descriptive loading messages ("Loading properties...", "Saving...")
6. For fast operations (<200ms), spinners may flash - consider minimum display time
7. For parallel loading, show individual spinners per section
8. Use `visually-hidden` class for screen reader support

### Client Session Monitor
**File:** `wwwroot/assets/js/helpers/client-session-monitor.js`

Monitors for client context changes across browser tabs and alerts users when the session client differs from the page-load client. Essential for multi-tab session safety.

**Use Case:** When a user opens a form in Tab A, switches clients in Tab B, then returns to Tab A, the monitor detects the mismatch and warns the user.

**Basic Usage:**
```javascript
// Include the script in your view
<script src="~/assets/js/helpers/client-session-monitor.js"></script>

// Initialize with page-load client context
const monitor = new ClientSessionMonitor({
    pageLoadClientId: clientContext.idClient,
    pageLoadCompanyId: clientContext.idCompany
});
monitor.start();
```

**Configuration Options:**
```javascript
{
    pageLoadClientId: null,              // Required - Client ID at page load
    pageLoadCompanyId: null,             // Optional - Company ID at page load
    checkEndpoint: '/Helpdesk/CheckSessionClient',  // API endpoint
    onMismatch: null,                    // Callback(sessionClient, pageLoadClient)
    onSessionExpired: null,              // Callback(response)
    onCheckError: null,                  // Callback(error)
    enableBanner: true,                  // Show default warning banner
    checkOnFocus: true,                  // Check when window gains focus
    checkOnVisibility: true              // Check when tab becomes visible
}
```

**Methods:**
```javascript
monitor.start();           // Start monitoring
monitor.stop();            // Stop monitoring
monitor.checkSession();    // Manually trigger check
```

**Custom Callbacks:**
```javascript
const monitor = new ClientSessionMonitor({
    pageLoadClientId: clientContext.idClient,
    onMismatch: (sessionClient, pageLoadClient) => {
        console.warn('Client changed!', sessionClient);
        // Custom handling
    },
    enableBanner: false  // Disable default banner when using custom callback
});
```

**Backend Requirement:** The `checkEndpoint` must return:
```json
{
  "success": true,
  "idClient": 123,
  "idCompany": 456,
  "sessionExpired": false
}
```

**Example Implementation:**
```csharp
// Controller action for CheckSessionClient
[HttpGet]
public IActionResult CheckSessionClient()
{
    var userSessionJson = HttpContext.Session.GetString("UserSession");
    if (string.IsNullOrEmpty(userSessionJson))
    {
        return Json(new { success = false, sessionExpired = true });
    }
    var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    return Json(new
    {
        success = true,
        idClient = userInfo.PreferredClientId,
        idCompany = userInfo.IdCompany
    });
}
```

**Best Practices:**
- Always use for forms that submit client-specific data
- Initialize after `$(document).ready()` or page context is set
- Store the monitor instance if you need to stop it later
- For read-only pages, monitoring is optional but recommended

### Breadcrumbs
Set in controllers via ViewBag:
```csharp
ViewBag.Title = "Current Page";           // Required
ViewBag.pTitle = "Parent";                // Optional
ViewBag.pTitleUrl = Url.Action("...");    // Optional
```
Renders: `Home > Parent > Current Page`

## Implementation Recipes

### Recipe: New Settings CRUD Page (Inline Editing)

For list-based settings pages like Work Category, Other Category, Priority Level.

**Files to Create/Modify:**
1. `ViewModels/{Feature}ViewModel.cs` - ViewModel with Items, Paging, IdClient
2. `Controllers/HelpdeskController.cs` - GET + POST/PUT/DELETE actions
3. `Constants/ApiEndpoints.cs` - Add endpoint constants
4. `wwwroot/assets/js/mvc-endpoints.js` - Add JS endpoint definitions
5. `Views/Helpdesk/Settings/{Feature}.cshtml` - View with PageContext
6. `wwwroot/assets/js/pages/settings/{feature}.js` - CRUD JavaScript

**ViewModel Pattern:**
```csharp
public class {Feature}ViewModel
{
    public List<TypeFormDetailResponse>? Items { get; set; }
    public PagingInfo? Paging { get; set; }
    public string? SearchKeyword { get; set; }
    public int IdClient { get; set; }  // REQUIRED for multi-tab safety
}
```

**Controller GET Action Pattern:**
```csharp
[Authorize]
public async Task<IActionResult> {Feature}(int page = 1, string search = "")
{
    var check = this.CheckViewAccess("Helpdesk", "{Page Name}");
    if (check != null) return check;

    var viewmodel = new {Feature}ViewModel();
    var client = _httpClientFactory.CreateClient("BackendAPI");
    var userInfo = GetUserInfoFromSession();

    var (success, data, message) = await SafeExecuteApiAsync<PaginatedResponse<ItemType>>(
        () => client.GetAsync($"{backendUrl}{ApiEndpoints.{Feature}.List}?cid={userInfo.PreferredClientId}&keyword={search}&page={page}"),
        "Failed to load items");

    if (success && data != null)
    {
        viewmodel.Items = data.Data;
        viewmodel.Paging = new PagingInfo { /* ... */ };
    }
    viewmodel.IdClient = userInfo.PreferredClientId;
    return View("~/Views/Helpdesk/Settings/{Feature}.cshtml", viewmodel);
}
```

**View PageContext Pattern:**
```html
@section scripts {
    <script>
        window.PageContext = {
            idClient: @(Model?.IdClient ?? 0)
        };
    </script>
    <script src="~/assets/js/pages/settings/{feature}.js"></script>
}
```

**JavaScript CRUD Pattern:**
```javascript
const CONFIG = {
    apiEndpoints: {
        create: MvcEndpoints.Helpdesk.Settings.{Feature}.Create,
        update: MvcEndpoints.Helpdesk.Settings.{Feature}.Update,
        delete: MvcEndpoints.Helpdesk.Settings.{Feature}.Delete
    }
};

const clientContext = {
    get idClient() { return window.PageContext?.idClient || 0; }
};

function createItem(data) {
    const token = $('input[name="__RequestVerificationToken"]').val();
    $.ajax({
        url: CONFIG.apiEndpoints.create,
        method: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify({ ...data, idClient: clientContext.idClient }),
        success: function(response) {
            if (response.success) {
                showNotification('Created', 'success');
                window.location.reload();
            }
        }
    });
}
```

**Reference Implementation:** `Views/Helpdesk/Settings/WorkCategory.cshtml` and `wwwroot/assets/js/pages/settings/work-category.js`

### Recipe: New Form Page with Cascading Dropdowns

For pages like Work Request Add with dependent dropdowns.

**Key Components:**
1. ViewModel with all dropdown lists + IdClient
2. Controller loading dropdowns in parallel (`Task.WhenAll`)
3. View with PageContext + SearchableDropdown markup
4. JavaScript initializing dropdowns with `onChange` cascade handlers

**Cascade Pattern:**
```javascript
const locationDropdown = new SearchableDropdown('#locationSelect', {
    onChange: (value) => {
        floorsDropdown.clear();
        floorsDropdown.disable();
        if (value) {
            loadFloors(value).then(floors => {
                floorsDropdown.loadOptions(floors);
                floorsDropdown.enable();
            });
        }
    }
});
```

**Reference:** Work Request Add/Edit pages

## Privilege Management

**Session-based.** Loaded at login, auto-refreshed in background if >30min old.

### Controller Authorization
```csharp
public class MyController : BaseController {
    public async Task<IActionResult> Index() {
        var check = this.CheckViewAccess("Module", "Page");
        if (check != null) return check;
        // ...
    }
}
```
Methods: `CheckViewAccess`, `CheckAddAccess`, `CheckEditAccess`, `CheckDeleteAccess`

### View Authorization
```razor
@using cfm_frontend.Extensions

@if (Context.Session.CanAdd("Helpdesk", "Work Request Management")) {
    <a href="...">Add</a>
}
```
Methods: `CanView`, `CanAdd`, `CanEdit`, `CanDelete`, `HasModuleAccess`

### Privilege Loading (Login)
**CRITICAL:** Use plain `HttpClient` + explicit token during login (before cookie creation):
```csharp
var privileges = await _privilegeService.LoadUserPrivilegesAsync(authResponse.Token);
```
After login, use parameterless overload (reads token from session):
```csharp
var privileges = await _privilegeService.LoadUserPrivilegesAsync();
```

## Important Model Notes

### WorkRequestFilterModel
**File:** `Models/WorkRequest/WorkRequestFilterModel.cs`

This model contains nested classes (`LocationModel`, `ServiceProviderModel`, `WorkCategoryModel`, `OtherCategoryModel`, `PriorityLevelModel`) that are **strictly used only for the filter options** in the Work Request Index page (`Views/Helpdesk/WorkRequest/Index.cshtml`). These classes populate the Advanced Filters modal checkboxes and dropdowns. Do not use these models for other purposes.

## Key Controllers

### BaseController
All controllers inherit from this. Provides:
- Auto privilege refresh (background, >30min)
- Authorization helpers: `CheckViewAccess()`, `CheckAddAccess()`, etc.
- `SafeExecuteApiAsync<T>()` for safe API calls
- Requires `IPrivilegeService`, `ILogger`

#### SafeExecuteApiAsync
**File:** `Controllers/BaseController.cs` (lines 85-123, 135+)

Wraps API calls to catch exceptions and return structured responses. Never throws exceptions to the browser.

```csharp
var (success, data, message) = await SafeExecuteApiAsync<List<FloorDto>>(
    () => client.GetAsync($"{backendUrl}/api/floors?locationId={id}"),
    "Failed to load floors"
);
return Json(new { success, data, message });
```

**Behavior:**
| Scenario | Returns |
|----------|---------|
| HTTP 2xx + `Success: true` | `(true, Data, Message)` |
| HTTP 2xx + `Success: false` | `(false, default, apiResponse.Message)` |
| HTTP 4xx/5xx | `(false, default, errorMessage)` |
| Exception | `(false, default, errorMessage)` |

**Overloads:**
1. Basic: `SafeExecuteApiAsync<T>(Func<Task<HttpResponseMessage>>, string errorMessage)`
2. With cancellation: `SafeExecuteApiAsync<T>(Func<CancellationToken, Task<HttpResponseMessage>>, string, CancellationToken, int timeoutMs)` - for parallel calls

**Key insight:** All failures become `{ success: false, message: "..." }` with HTTP 200 status. The global AJAX handler (`global-ajax-handler.js`) catches these and shows toastr notifications.

### LoginController
- `SignIn(SignInViewModel)`: Auth user, load privileges (explicit token), create session

### HelpdeskController
- 1000+ lines, Work Request management
- All actions have authorization checks
- API endpoints: `GetFloorsByLocation`, `GetRoomsByFloor`, `SearchEmployees`, etc.

## Error Handling & Notifications

**Use `toastr` globally.** Function: `showNotification(message, type, title, options)`

Types: `'success'`, `'error'`, `'warning'`, `'info'`

```javascript
// Success
showNotification('Saved!', 'success', 'Success');

// Error
showNotification('Failed', 'error', 'Error');

// AJAX pattern
$.ajax({
    success: function(res) {
        if (res.success) showNotification('OK', 'success');
        else showNotification(res.message || 'Failed', 'error');
    },
    error: function(xhr, status, err) {
        console.error('Error:', err);
        showNotification('Network error', 'error');
    }
});
```

**Best Practices:**
1. Always use `showNotification()` (never `alert()`)
2. Log errors to console + show user-friendly message
3. Use `response.message` from server when available

### Backend Error Response Parsing

**MANDATORY PATTERN:** All controller actions that call backend APIs **MUST** parse and return backend error messages to the user. This ensures business validation errors from the backend are properly displayed to the user.

#### Backend Error Response Structure

The backend returns errors in this format:
```json
{
  "success": false,
  "message": "BadRequest",
  "errors": [
    "Job Code with this name already exists",
    "Group is required"
  ],
  "timestamp": "2026-03-03T..."
}
```

#### Frontend Error Handling Pattern

**ALWAYS use this pattern** when calling backend APIs:

```csharp
var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.SomeEndpoint}", content);

if (response.IsSuccessStatusCode)
{
    return Json(new { success = true, message = "Operation successful." });
}
else
{
    var errorContent = await response.Content.ReadAsStringAsync();
    _controllerLogger.LogWarning("Operation failed. Status: {StatusCode}, Response: {Response}",
        response.StatusCode, errorContent);

    // Parse backend error response
    try
    {
        var errorResponse = JsonSerializer.Deserialize<ApiResponseDto<object>>(
            errorContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (errorResponse?.Errors != null && errorResponse.Errors.Any())
        {
            // Return all error messages from backend
            var errorMessages = string.Join(", ", errorResponse.Errors);
            return Json(new { success = false, message = errorMessages });
        }
        else if (!string.IsNullOrEmpty(errorResponse?.Message))
        {
            return Json(new { success = false, message = errorResponse.Message });
        }
    }
    catch (Exception ex)
    {
        _controllerLogger.LogError(ex, "Error parsing backend error response");
    }

    return Json(new { success = false, message = "Operation failed." });
}
```

#### Key Points:

1. **Parse the error content**: Deserialize the error response to `ApiResponseDto<object>`
2. **Check Errors array first**: If `errorResponse.Errors` exists and has items, join them with commas
3. **Fallback to Message**: If no Errors array, use `errorResponse.Message`
4. **Generic fallback**: If parsing fails, return a generic error message
5. **Log everything**: Always log the raw error content for debugging

#### Error Types from Backend:

| Exception Type | HTTP Status | Response Structure |
|----------------|-------------|-------------------|
| `BusinessException` | 400 Bad Request | `{ success: false, message: "BadRequest", errors: [...] }` |
| `KeyNotFoundException` | 404 Not Found | `{ success: false, message: "NotFound", errors: [...] }` |
| General Exception | 500 Internal Server Error | `{ success: false, message: "InternalServerError", errors: [...] }` |

#### Example Error Messages:

**Business Validation:**
- "Job Code with this name already exists"
- "Group is required"
- "Unit price must be greater than zero"

**Not Found:**
- "Job Code not found"
- "Client does not have access to this resource"

**Internal Server Error:**
- "An unexpected error occurred"

### Global AJAX Error Handler
**File:** `wwwroot/assets/js/global-ajax-handler.js`

Automatically intercepts all jQuery AJAX responses and displays `toastr` error notifications for "soft errors" (HTTP 200 with `success: false`).

```javascript
// Automatically triggers on ANY $.ajax call that returns { success: false, message: "..." }
// No manual error handling needed for SafeExecuteApiAsync failures
```

**How it works:**
- Uses `$(document).ajaxSuccess()` to intercept all AJAX responses
- Checks if `xhr.responseJSON.success === false`
- Displays error via `showNotification(message, 'error')`
- Logs to console: `API Logic Error: <message>`

**When to rely on it:**
- API data fetching via `SafeExecuteApiAsync` (floors, rooms, employees, etc.)
- Any endpoint returning the standard `{ success, data, message }` format

**When to handle manually:**
- Form submissions where you need custom success handling
- Cases where you want to suppress the notification (set `success: true` or don't include `message`)

## File Logger Service

**File:** `Services/FileLoggerService.cs`

Thread-safe file logging service for API timing and diagnostics. Writes to rotating daily log files.

### Configuration (`appsettings.json`)
```json
{
  "Logging": {
    "FileLogger": {
      "Path": "Logs"
    }
  },
  "ApplicationName": "CFM-Frontend"
}
```

Log files are created at: `{Path}/{ApplicationName}_{yyyy-MM-dd}.log`

### DI Registration
```csharp
builder.Services.AddSingleton<IFileLoggerService, FileLoggerService>();
```

### Basic Logging
```csharp
_fileLogger.LogInfo("Operation completed", "CATEGORY");
_fileLogger.LogWarning("Something unexpected", "CATEGORY");
_fileLogger.LogError("Failed to process", exception, "CATEGORY");
```

### Timed API Calls (Batch)
Use when tracking multiple parallel API calls with timing:

```csharp
var totalStopwatch = Stopwatch.StartNew();
var apiTimingResults = new List<ApiTimingResult>();

// Execute timed API calls in parallel
var task1 = _fileLogger.ExecuteTimedAsync(
    "Locations",
    "/api/locations",
    () => GetLocationsAsync(),
    apiTimingResults);

var task2 = _fileLogger.ExecuteTimedAsync(
    "Categories",
    "/api/categories",
    () => GetCategoriesAsync(),
    apiTimingResults);

await Task.WhenAll(task1, task2);

// Update record counts after completion
_fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "Locations", locations?.Count);
_fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "Categories", categories?.Count);

// Log batch summary
totalStopwatch.Stop();
_fileLogger.LogApiTimingBatch("Page Load", apiTimingResults, totalStopwatch.Elapsed);
```

### Timed API Calls (Standalone)
Use for individual API calls where you just want timing logged:

```csharp
var data = await _fileLogger.ExecuteTimedAsync(
    "GetUserDetails",
    "/api/users/123",
    () => GetUserDetailsAsync(123),
    "USER-API");  // optional category
```

### Log Output Example
```
=== Page Load API Timing Summary ===
Total Duration: 1234.56ms
Timestamp: 2026-01-21 10:30:45.123 UTC
--------------------------------------------------------------------------------
Results: 10 succeeded, 1 failed out of 11 total
--------------------------------------------------------------------------------
  [✗] ServiceProviders                       523.45ms - Connection timeout
  [✓] Locations                              412.32ms (15 records)
  [✓] Categories                             234.56ms (12 records)
================================================================================
```

### Features
- Thread-safe concurrent queue with background flushing (1 second interval)
- Auto-rotates files at 10MB
- Logs failures separately at WARNING level for easy filtering
- Categories: `API-TIMING`, `API-TIMING-BATCH`, `API-FAILURE`, `API-ERROR`, `PAGE-LOAD`, `AUTH`, `SESSION`

## Constants & Endpoints

- **C# API Endpoints:** `Constants/ApiEndpoints.cs`
- **JS Endpoints:** `assets/js/mvc-endpoints.js`

## UI/UX

- **Theme:** Light Able Bootstrap 5
- **Design:** Follow industry standard best practices
- **Comments:** Only for documentation/warnings, not overly verbose

## Common Mistakes to Avoid

### DO NOT:

1. **Use "BackendAPI" HttpClient during login**
   - WRONG: `_httpClientFactory.CreateClient("BackendAPI")` before `SignInAsync`
   - RIGHT: Use plain `HttpClient` + manual Authorization header

2. **Hardcode client ID as 0**
   - WRONG: `idClient: 0` in AJAX payload
   - RIGHT: `idClient: clientContext.idClient` from PageContext

3. **Forget multi-tab session safety**
   - WRONG: Forms without IdClient in ViewModel
   - RIGHT: Capture IdClient at page load, validate on submit

4. **Use alert() for notifications**
   - WRONG: `alert('Error!')`
   - RIGHT: `showNotification('Error message', 'error')`

5. **Skip privilege checks**
   - WRONG: Controller action without `CheckViewAccess()`
   - RIGHT: Always check permissions first in every action

6. **Return raw exceptions to browser**
   - WRONG: `throw new Exception("Error")`
   - RIGHT: Use `SafeExecuteApiAsync` pattern

7. **Forget CSRF tokens in AJAX**
   - WRONG: POST without RequestVerificationToken header
   - RIGHT: `headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }`

8. **Use BackendModels directly**
   - WRONG: `using cfm_frontend.BackendModels;` in frontend code
   - RIGHT: Create new DTOs in `DTOs/` folder referencing BackendModels structure

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| 401 Unauthorized | Using BackendAPI before auth | Use plain HttpClient during login |
| success:false in AJAX | Backend returned error | Check `response.message` for details |
| Session lost after redirect | Middleware order wrong | Ensure Session before Auth in Program.cs |
| Privilege denied unexpectedly | Case-sensitive name mismatch | Check exact module/page name spelling |
| JS not loading | Script path incorrect | Check `@section scripts` in View |
| AJAX not sending CSRF | Missing token header | Add RequestVerificationToken to headers |
| Token refresh loop | Both tokens expired | User needs to re-login |
| Empty dropdown data | IdClient not passed | Add `?cid={idClient}` to API endpoint |

### Debug Checklist

**For API Errors:**
1. Check browser Network tab for response body
2. Look in `Logs/` folder for server-side errors
3. Verify client ID is passed in request
4. Check if endpoint exists in `ApiEndpoints.cs`

**For JavaScript Errors:**
1. Check browser Console for errors
2. Verify `window.PageContext` is set before JS runs
3. Check if MvcEndpoints has the endpoint defined
4. Ensure jQuery is loaded before page scripts

**For Authentication Issues:**
1. Check if session cookie exists
2. Verify token not expired via JWT debugger
3. Check AuthTokenHandler logs for refresh attempts
4. Ensure correct middleware order in Program.cs

## Important Notes

### Namespace
Use `cfm_frontend` for consistency (some legacy use `Mvc.Controllers`)

### Session vs Claims
- **Session** (`HttpContext.Session["UserSession"]`): Full `UserInfo` (PreferredClientId, Department, etc.)
- **Claims** (`User.Claims`): Basic identity (Name, Email, Role, UserId)

### Middleware Order (`Program.cs`)
1. `UseHttpsRedirection()`
2. `UseStaticFiles()`
3. `UseRouting()`
4. `UseSession()` (before auth!)
5. `UseAuthentication()`
6. `UseTokenExpiration()` - Validates tokens, forces logout if both expired
7. `UseAuthorization()`
8. `UseSessionRestore()` - Auto-restores session for Remember Me users
9. `MapControllerRoute()`

### Static Assets
- Bootstrap 5, jQuery, "pcoded" theme
- Location: `wwwroot/`
- Loaded via `Views/Shared/VendorScripts.cshtml`

### Code Comments & Logging Style
- **No emojis** in code, comments, or log messages
- **No AI-like commentary** in debug logs or summary comments (e.g., avoid phrases like "Great job!", "Successfully completed!", "Everything looks good!")
- Keep log messages factual and technical (e.g., "Loaded 15 records" not "Successfully loaded 15 records!")
- Comments should be informative, not conversational
