# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an **ASP.NET Core 8.0 MVC frontend application** that serves as a web-based client interface for a backend REST API. It follows a traditional server-rendered MVC pattern with Razor views and uses cookie-based authentication with bearer token handling.

**Key characteristics:**
- Frontend-only application - all business logic lives in the backend API
- Acts as a "Backend for Frontend" (BFF) layer
- Communication: JSON over HTTP with automatic bearer token injection
- Primary feature: Work Request Management (Helpdesk module)

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

### Authentication Flow

**Login process** (see `Controllers/LoginController.cs`):
1. User submits credentials via `SignIn` action
2. POST to `/api/auth/login` returns access token + refresh token
3. Fetch user info via `/api/auth/userinfo` with tokens
4. Store `UserInfo` in `HttpContext.Session["UserSession"]` as JSON
5. Create authentication cookie with claims and tokens
6. Redirect to Dashboard

**Session data structure:**
```csharp
var userSessionJson = HttpContext.Session.GetString("UserSession");
var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson);
// Access: userInfo.UserId, userInfo.PreferredClientId, etc.
```

**Token storage:**
- Access token and refresh token stored in authentication cookie properties
- Retrieved via: `await HttpContext.GetTokenAsync("access_token")`
- Automatically refreshed by `AuthTokenHandler` on 401 responses

**Important files:**
- `Controllers/LoginController.cs` - Authentication logic
- `Handlers/AuthTokenHandler.cs` - Token injection and refresh logic
- `Models/UserInfo.cs` - Session data model

### Dependency Injection Configuration

In `Program.cs`:
```csharp
// Named HTTP client with automatic token handling
builder.Services.AddHttpClient("BackendAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BackendBaseUrl"]);
})
.AddHttpMessageHandler<AuthTokenHandler>();

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

## Key Controllers

### LoginController (`Controllers/LoginController.cs`)
- `Index()` - Login page
- `SignIn(SignInViewModel)` - Authenticate user, fetch user info, create session
- `Logout()` - Clear session and sign out

### HelpdeskController (`Controllers/HelpdeskController.cs`)
Largest controller (770+ lines) handling Work Request management:
- `Index()` - Work Request list page with filters
- `WorkRequestAdd()` - GET: Display add form
- `WorkRequestAdd(WorkRequestCreateRequest)` - POST: Create new work request
- API endpoints for dynamic data: `GetFloorsByLocation`, `GetRoomsByFloor`, `SearchEmployees`, etc.

### ClientController (`Controllers/ClientController.cs`)
- Client switching functionality for users with multiple client access

## Important Notes

### Namespace Inconsistency
- Most files use `cfm_frontend` namespace
- Some controllers use `Mvc.Controllers` namespace
- When creating new files, use `cfm_frontend` for consistency

### Session vs Claims
- **Session** (`HttpContext.Session["UserSession"]`) contains full `UserInfo` object
- **Claims** (`User.Claims`) contain basic identity info (Name, Email, Role, UserId)
- Both are set during login, but session has more detailed information (e.g., `PreferredClientId`, `Department`)

### Error Handling Pattern
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
