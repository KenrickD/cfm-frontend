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
- Requires `IPrivilegeService`, `ILogger`

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
6. `UseAuthorization()`
7. `MapControllerRoute()`

### Static Assets
- Bootstrap 5, jQuery, "pcoded" theme
- Location: `wwwroot/`
- Loaded via `Views/Shared/VendorScripts.cshtml`
