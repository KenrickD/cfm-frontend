# CLAUDE.md

Project guidance for Claude Code when working with this ASP.NET Core 8.0 MVC frontend application.

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

### Breadcrumbs
Set in controllers via ViewBag:
```csharp
ViewBag.Title = "Current Page";           // Required
ViewBag.pTitle = "Parent";                // Optional
ViewBag.pTitleUrl = Url.Action("...");    // Optional
```
Renders: `Home > Parent > Current Page`

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
