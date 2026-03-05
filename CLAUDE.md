# CLAUDE.md

Project guidance for Claude Code when working with this ASP.NET Core 8.0 MVC frontend application.

## Documentation Structure

This repository contains three reference documents:

### 1. CLAUDE.md (This File)
**Purpose:** Frontend-specific guidance for the ASP.NET Core 8.0 MVC application.
**When to Use:** Always loaded by default for all frontend development tasks.

### 2. BACKEND_REFERENCE.md
**Purpose:** Comprehensive backend API reference for integration work.
**When to Use:** Only load when you need to understand backend API endpoints, debug backend integration issues, or implement new features that call backend APIs.
**Location:** `BACKEND_REFERENCE.md` in the same directory as this file.
**Important:** The backend repository is located at `C:\Users\kenri\OneDrive\Documents\Work\Colliers\Repo\CFM Backend\CFM Backend` and should NOT be modified.

### 3. PLAYWRIGHT_TESTING_GUIDE.md
**Purpose:** Comprehensive guide for writing E2E tests using Playwright.
**When to Use:** Load when you need to write tests, understand testing patterns, or debug failing tests.
**Location:** `PLAYWRIGHT_TESTING_GUIDE.md` in the same directory as this file.
**Test Project:** `c:\Repos\CfmFrontend.Tests`

---

## Project Overview

**ASP.NET Core 8.0 MVC** frontend acting as BFF (Backend for Frontend) layer. All business logic in backend API.
- Communication: JSON over HTTP with automatic bearer token injection
- Auth: Cookie-based with bearer token handling
- Primary feature: Work Request Management (Helpdesk)
- Runs on: http://localhost:5099, https://localhost:7035

**Critical Rule:** Never break existing functionality without confirmation.

## Development Commands

```bash
dotnet build          # Build
dotnet run            # Run
dotnet restore        # Restore packages
```

**Required Config:** `appsettings.json` must have `BackendBaseURL` set.

## Architecture Overview

### MVC Structure
- **Controllers**: Coordinate requests, aggregate API data, manage session, return Views. NO business logic.
- **Models**: Domain models in `Models/` (e.g., `UserInfo`, `PagingInfo`, `Models/WorkRequest/`)
- **DTOs**: API request/response structures in `DTOs/` (e.g., `LoginResponse`, `WorkRequestCreateRequest`)
- **ViewModels**: Aggregate data from multiple APIs for complex views in `ViewModels/`
- **Views**: Server-rendered Razor templates in `Views/`

### BackendModels Folder (Reference Only)
**⚠️ IMPORTANT:** The `BackendModels/` folder contains model classes copied from the backend repository for **reference purposes only**. DO NOT use these models directly in the frontend code.

**Usage Rule:**
1. Check `BackendModels/` to understand the backend model structure
2. Create a **new DTO** in the `DTOs/` folder with only the properties needed
3. Never reference `BackendModels` namespace directly in frontend code

**Key Backend Models:**
- `BackendModels/Models/DataModels/Type.cs` - Master data for client-specific choice sets
- `BackendModels/Models/DataModels/Enum.cs` - System-wide fixed choice sets

### Backend API Integration

**HttpClient Pattern:** Use named `HttpClient` "BackendAPI" configured in `Program.cs`
- **Reference:** See `Program.cs` for DI configuration
- **AuthTokenHandler** automatically: injects bearer token, handles 401 by refreshing tokens, retries with new token, signs out if refresh fails

**⚠️ CRITICAL:** DO NOT use "BackendAPI" during login (before `HttpContext.SignInAsync()`). Use plain `HttpClient` + manual Authorization header. See `LoginController.cs` for implementation.

### Authentication Flow
1. POST `/api/auth/login` → tokens
2. GET `/api/auth/userinfo` → UserInfo
3. Store in `Session["UserSession"]`
4. GET `/api/WebUser/GetUserPrivileges` → UserPrivileges (use plain HttpClient + explicit token)
5. Store in `Session["UserPrivileges"]`
6. Create auth cookie with tokens
7. Redirect to Dashboard

**Token Storage:** In auth cookie, retrieved via `await HttpContext.GetTokenAsync("access_token")`

**Reference Implementation:** See `Controllers/LoginController.cs` for complete login flow

### Remember Me Feature
**Files:** `LoginController.cs`, `AuthTokenHandler.cs`, `Program.cs`

When user checks "Remember me?" during login:
- Auth cookie is set with `IsPersistent = true` and 30-day expiration
- Cookie survives browser close/reopen
- Session data is auto-restored via `SessionRestoreMiddleware`

**Landing Page Auth Detection:** `HomeController.Index()` checks `User.Identity.IsAuthenticated` to show either username (linking to Dashboard) or LOGIN button.

### Data Protection (Cookie Encryption Keys)
**File:** `Program.cs`

Auth cookies are encrypted using Data Protection keys that persist across app restarts in `Keys/` folder.

**Key Storage by Environment:**
- Development (F5) / IIS on VM: `Keys/` folder works fine
- Load Balanced: ⚠️ Shared storage needed (network share, Redis, or database)
- Containers/K8s: ⚠️ Persistent volume needed (or Azure Blob/Redis)

**Current Deployment:** IIS on VM - file-based keys work correctly.

**⚠️ IMPORTANT:** The `Keys/` folder is in `.gitignore` - do not commit encryption keys to source control.

### DI Configuration
**File:** `Program.cs`
- HttpClient "BackendAPI" with AuthTokenHandler
- Scoped: IPrivilegeService
- Cookie Authentication
- Session middleware

## File Locations Quick Reference

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
Access user info from session: `HttpContext.Session.GetString("UserSession")` → deserialize to `UserInfo`
- `PreferredClientId` - Current client context
- `UserId` - Employee ID
- `IdCompany` - Company ID

### API Response Format
All backend API responses use `ApiResponseDto<T>` wrapper defined in `DTOs/BaseResponse.cs`
- Properties: `Success`, `Message`, `Data`, `Errors`, `Timestamp`
- Specialized: `LoginResponse : ApiResponseDto<TokenData>`

### API Calls
**Reference:** See `Controllers/HelpdeskController.cs` for examples of GET/POST patterns with `BackendAPI` HttpClient and `ApiResponseDto<T>` deserialization.

### Parallel API Calls
Use `Task.WhenAll()` for parallel API calls.
**Reference:** See `Controllers/HelpdeskController.cs` methods that load multiple dropdowns.

### Multi-Tab Session Safety

**Problem:** User opens form in Tab A with Client X, switches to Client Y in Tab B, returns to Tab A and submits. Without safeguards, the form submits with wrong client context.

**Solution Pattern (5 Steps):**
1. **ViewModel** - Add `IdClient` property, set to `userInfo.PreferredClientId` in GET action
2. **View** - Expose via `window.PageContext = { idClient: @Model.IdClient }`
3. **JavaScript** - Use `clientContext.idClient` helper to get page-load client
4. **Controller** - Accept optional `idClient` parameter with fallback to session
5. **Form Submission** - Validate submitted client matches current session client

**When to Use:**
- ✅ Always use for forms that submit client-specific data
- ✅ Use for pages with client-specific AJAX calls
- ⚠️ Optional for read-only pages
- ❌ Not needed for client-agnostic pages

**⚠️ MANDATORY FOR NEW IMPLEMENTATIONS:** When creating new pages with client-specific CRUD operations, you MUST follow this pattern.

**Reference Implementation:** See `Views/Helpdesk/Settings/WorkCategory.cshtml` and `wwwroot/assets/js/pages/settings/work-category.js`

### Pagination
**Component:** `Views/Shared/_Pagination.cshtml` (auto-preserves query params)
**Model:** `Models/PagingInfo.cs`
**Reference:** See any Settings page controller action for usage pattern.

### Searchable Dropdown
**Files:** `wwwroot/assets/css/components/searchable-dropdown.css`, `wwwroot/assets/js/components/searchable-dropdown.js`

**Usage:** Add `data-searchable="true"` attribute to `<select>` elements.

**JS API Methods:** `new SearchableDropdown()`, `enable()`, `disable()`, `clear()`, `setValue()`, `loadOptions()`

**Cascade Pattern:** Use `onChange` callback to enable/populate dependent dropdowns.
**Reference:** See Work Request Add/Edit pages for cascading location → floor → room pattern.

### Loading Spinners for AJAX Operations
**Pattern:** Always show loading indicators when fetching data via AJAX.

**MANDATORY:** All AJAX calls must display appropriate loading indicators to provide user feedback.

**Implementation Types:**

1. **Table/List Loading** - Full container spinner
```javascript
$('#tableLoadingSpinner').show();
$.ajax({
    url: endpoint,
    success: function(response) {
        // Render data
    },
    error: function(xhr, status, error) {
        // Handle error
    },
    complete: function() {
        $('#tableLoadingSpinner').hide();
    }
});
```

2. **Dropdown/Form Field Loading** - Inline spinner with disabled state
```javascript
const $select = $('#mySelect');
const selectElement = document.getElementById('mySelect');

// Show loading
$select.prop('disabled', true);
$select.empty().append('<option value="">Loading...</option>');

if (selectElement && selectElement._searchableDropdown) {
    const wrapper = selectElement.closest('.searchable-dropdown');
    if (wrapper) wrapper.classList.add('loading');
}

$.ajax({
    url: endpoint,
    success: function(response) {
        // Populate options
    },
    complete: function() {
        $select.prop('disabled', false);
        if (selectElement && selectElement._searchableDropdown) {
            const wrapper = selectElement.closest('.searchable-dropdown');
            if (wrapper) wrapper.classList.remove('loading');
        }
    }
});
```

3. **Typeahead/Search Loading** - Inline spinner in search input
```javascript
$searchResults.html('<div class="dropdown-item text-muted"><i class="ti ti-loader spinning me-2"></i>Searching...</div>');
```

4. **Full Page Loading Overlay** - For form submissions
```javascript
function showLoadingOverlay() {
    if ($('#loading-overlay').length > 0) return;
    const $overlay = $(`
        <div id="loading-overlay" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%;
             background: rgba(0, 0, 0, 0.5); display: flex; justify-content: center; align-items: center; z-index: 9999;">
            <div class="spinner-border text-light" role="status" style="width: 3rem; height: 3rem;">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    `);
    $('body').append($overlay);
}

function hideLoadingOverlay() {
    $('#loading-overlay').fadeOut(function() { $(this).remove(); });
}
```

**Best Practices:**
- ✅ Show spinner BEFORE AJAX call starts
- ✅ Hide spinner in `complete` callback (runs after success/error)
- ✅ Disable interactive elements during loading
- ✅ Use descriptive loading messages ("Loading floors...", "Searching...")
- ✅ Remove loading state even if AJAX fails
- ❌ Never leave spinners running indefinitely
- ❌ Don't use `success` callback to hide spinners (won't run on error)

**When to Use Each Type:**
- **Table/Container:** Loading lists, grids, or large data sets
- **Dropdown/Field:** Cascading dropdowns, dynamic form fields
- **Search/Typeahead:** Real-time search results
- **Full Page Overlay:** Form submissions, file uploads, critical operations

**Reference Implementations:**
- Table loading: `Views/Helpdesk/Inventory/Index.cshtml`
- Dropdown loading: `wwwroot/assets/js/pages/workrequest/send-work-request.js` (loadFloors, loadRooms)
- Full page overlay: `wwwroot/assets/js/pages/workrequest/send-work-request.js` (form submission)

### Client Session Monitor
**File:** `wwwroot/assets/js/helpers/client-session-monitor.js`

Monitors for client context changes across browser tabs. Essential for multi-tab session safety.

**Basic Usage:** Initialize with `new ClientSessionMonitor({ pageLoadClientId: clientContext.idClient })` and call `monitor.start()`.

**Configuration Options:** `pageLoadClientId`, `pageLoadCompanyId`, `checkEndpoint`, `onMismatch`, `onSessionExpired`, `onCheckError`, `enableBanner`, `checkOnFocus`, `checkOnVisibility`

**Backend Requirement:** Controller action must return `{ success, idClient, idCompany, sessionExpired }`
**Reference:** See `Controllers/HelpdeskController.CheckSessionClient()` for implementation.

### Breadcrumbs
Set in controllers via ViewBag:
- `ViewBag.Title` - Current page (required)
- `ViewBag.pTitle` - Parent page (optional)
- `ViewBag.pTitleUrl` - Parent page URL (optional)

## Implementation Recipes

### Recipe: New Settings CRUD Page (Inline Editing)
For list-based settings pages like Work Category, Other Category, Priority Level.

**Files to Create/Modify:**
1. `ViewModels/{Feature}ViewModel.cs` - Must include `IdClient` property
2. `Controllers/HelpdeskController.cs` - GET + POST/PUT/DELETE actions
3. `Constants/ApiEndpoints.cs` - Add endpoint constants
4. `wwwroot/assets/js/mvc-endpoints.js` - Add JS endpoint definitions
5. `Views/Helpdesk/Settings/{Feature}.cshtml` - Must include `window.PageContext`
6. `wwwroot/assets/js/pages/settings/{feature}.js` - CRUD JavaScript using `clientContext.idClient`

**Reference Implementation:**
- `Views/Helpdesk/Settings/WorkCategory.cshtml`
- `wwwroot/assets/js/pages/settings/work-category.js`
- `ViewModels/WorkCategoryViewModel.cs`
- `Controllers/HelpdeskController.cs` (WorkCategory actions)

### Recipe: New Form Page with Cascading Dropdowns
For pages like Work Request Add with dependent dropdowns.

**Key Components:**
1. ViewModel with all dropdown lists + IdClient
2. Controller loading dropdowns in parallel (`Task.WhenAll`)
3. View with PageContext + SearchableDropdown markup
4. JavaScript initializing dropdowns with `onChange` cascade handlers

**Reference:** See Work Request Add/Edit pages for complete implementation.

## Privilege Management

**Session-based.** Loaded at login, auto-refreshed in background if >30min old.

### Controller Authorization
**Reference:** See `Controllers/BaseController.cs` for methods:
- `CheckViewAccess("Module", "Page")`
- `CheckAddAccess("Module", "Page")`
- `CheckEditAccess("Module", "Page")`
- `CheckDeleteAccess("Module", "Page")`

### View Authorization
**Reference:** See `Extensions/SessionExtensions.cs` for methods:
- `Context.Session.CanView("Module", "Page")`
- `Context.Session.CanAdd("Module", "Page")`
- `Context.Session.CanEdit("Module", "Page")`
- `Context.Session.CanDelete("Module", "Page")`
- `Context.Session.HasModuleAccess("Module")`

### Privilege Loading (Login)
**CRITICAL:** Use plain `HttpClient` + explicit token during login (before cookie creation).
**Reference:** See `Services/PrivilegeService.cs` and `Controllers/LoginController.cs`

## Important Model Notes

### WorkRequestFilterModel
**File:** `Models/WorkRequest/WorkRequestFilterModel.cs`

Contains nested classes (`LocationModel`, `ServiceProviderModel`, etc.) that are **strictly used only for the filter options** in the Work Request Index page. Do not use these models for other purposes.

## Key Controllers

### BaseController
**File:** `Controllers/BaseController.cs`

All controllers inherit from this. Provides:
- Auto privilege refresh (background, >30min)
- Authorization helpers: `CheckViewAccess()`, `CheckAddAccess()`, etc.
- `SafeExecuteApiAsync<T>()` for safe API calls

#### SafeExecuteApiAsync
Wraps API calls to catch exceptions and return structured responses. Never throws exceptions to the browser.

**Behavior:**
- HTTP 2xx + `Success: true` → `(true, Data, Message)`
- HTTP 2xx + `Success: false` → `(false, default, apiResponse.Message)`
- HTTP 4xx/5xx → `(false, default, errorMessage)`
- Exception → `(false, default, errorMessage)`

**Overloads:**
1. Basic: `SafeExecuteApiAsync<T>(Func<Task<HttpResponseMessage>>, string errorMessage)`
2. With cancellation: for parallel calls

**Key insight:** All failures become `{ success: false, message: "..." }` with HTTP 200 status. The global AJAX handler automatically shows toastr notifications.

**Reference:** See `Controllers/BaseController.cs` (lines 85-123, 135+)

### LoginController
**File:** `Controllers/LoginController.cs`
- Handles authentication, privilege loading, session creation

### HelpdeskController
**File:** `Controllers/HelpdeskController.cs`
- 1000+ lines, Work Request management
- All actions have authorization checks
- API endpoints for dynamic data (floors, rooms, employees, etc.)

## Error Handling & Notifications

### Client-Side Notifications
**Function:** `showNotification(message, type, title, options)` (available globally)
**Types:** `'success'`, `'error'`, `'warning'`, `'info'`

**Best Practices:**
1. Always use `showNotification()` (never `alert()`)
2. Log errors to console + show user-friendly message
3. Use `response.message` from server when available

**Reference:** See `wwwroot/assets/js/layout.js` for implementation and any page JS for usage examples.

### Backend Error Response Parsing

**MANDATORY PATTERN:** All controller actions that call backend APIs **MUST** parse and return backend error messages.

**Backend Error Structure:**
- `success: false`
- `message: "BadRequest"`
- `errors: ["Error 1", "Error 2"]`

**Frontend Handling Steps:**
1. Parse error content to `ApiResponseDto<object>`
2. Check `Errors` array first, join with commas if exists
3. Fallback to `Message` property
4. Generic fallback if parsing fails
5. Always log raw error content

**Error Types from Backend:**
- `BusinessException` → 400 Bad Request
- `KeyNotFoundException` → 404 Not Found
- General Exception → 500 Internal Server Error

**Reference:** See `Controllers/HelpdeskController.cs` POST/PUT/DELETE actions for error handling pattern.

### Global AJAX Error Handler
**File:** `wwwroot/assets/js/global-ajax-handler.js`

Automatically intercepts all jQuery AJAX responses and displays `toastr` error notifications for "soft errors" (HTTP 200 with `success: false`).

**When to rely on it:**
- API data fetching via `SafeExecuteApiAsync`
- Any endpoint returning `{ success, data, message }` format

**When to handle manually:**
- Form submissions with custom success handling
- Cases where you want to suppress notifications

## File Logger Service

**File:** `Services/FileLoggerService.cs`

Thread-safe file logging service for API timing and diagnostics. Writes to rotating daily log files.

**Configuration:** `appsettings.json` - Set `Logging.FileLogger.Path`

**DI Registration:** `builder.Services.AddSingleton<IFileLoggerService, FileLoggerService>()`

**Methods:**
- `LogInfo()`, `LogWarning()`, `LogError()`
- `ExecuteTimedAsync()` - For individual or batch API timing
- `LogApiTimingBatch()` - For parallel API call summaries

**Reference:** See `Controllers/HelpdeskController.cs` for usage examples with parallel API calls.

## Constants & Endpoints

- **C# API Endpoints:** `Constants/ApiEndpoints.cs`
- **JS Endpoints:** `wwwroot/assets/js/mvc-endpoints.js`

## UI/UX

- **Theme:** Light Able Bootstrap 5
- **Design:** Follow industry standard best practices
- **Comments:** Only for documentation/warnings, not overly verbose

## Common Mistakes to Avoid

### DO NOT:

1. **Use "BackendAPI" HttpClient during login** - Use plain `HttpClient` + manual Authorization header before `SignInAsync`
2. **Hardcode client ID as 0** - Use `clientContext.idClient` from PageContext
3. **Forget multi-tab session safety** - Capture IdClient at page load, validate on submit
4. **Use alert() for notifications** - Use `showNotification('message', 'error')`
5. **Skip privilege checks** - Always call `CheckViewAccess()` in controller actions
6. **Return raw exceptions to browser** - Use `SafeExecuteApiAsync` pattern
7. **Forget CSRF tokens in AJAX** - Add `RequestVerificationToken` header to POST/PUT/DELETE
8. **Use BackendModels directly** - Create new DTOs in `DTOs/` folder

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

### Middleware Order (Program.cs)
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
- **No AI-like commentary** in debug logs or summary comments
- Keep log messages factual and technical
- Comments should be informative, not conversational
