# CFM Backend API Reference

Comprehensive reference documentation for the CFM System Backend API (ASP.NET Core 8.0). This document provides quick lookup for API capabilities, architecture patterns, and integration guidelines.

## Table of Contents

1. [Overview](#overview)
2. [Technology Stack](#technology-stack)
3. [Project Structure](#project-structure)
4. [Configuration](#configuration)
5. [Database Architecture](#database-architecture)
6. [Authentication & Authorization](#authentication--authorization)
7. [API Response Format](#api-response-format)
8. [Controllers & Endpoints](#controllers--endpoints)
9. [Services Architecture](#services-architecture)
10. [Master Data System](#master-data-system)
11. [Helper Classes](#helper-classes)
12. [Security Features](#security-features)
13. [Common Patterns](#common-patterns)
14. [Best Practice Notes](#best-practice-notes)

---

## Overview

**Framework:** ASP.NET Core 8.0 Web API
**Architecture:** RESTful API with service-based architecture
**Database:** SQL Server via Entity Framework Core 9.0
**Authentication:** JWT Bearer Token + Refresh Token
**API Versioning:** URL Segment (v1.0)
**Documentation:** Swagger/OpenAPI (Development only)

**Primary Purpose:** Backend API for CFM (Colliers Facility Management) System providing CRUD operations for work requests, assets, properties, service providers, employees, and master data.

---

## Technology Stack

### Core Frameworks
- **ASP.NET Core:** 8.0
- **Entity Framework Core:** 9.0.11
- **Microsoft.EntityFrameworkCore.SqlServer:** 9.0.11

### Authentication & Security
- **Microsoft.AspNetCore.Authentication.JwtBearer:** 8.0.22
- **System.IdentityModel.Tokens.Jwt:** 8.15.0

### Data Access
- **Dapper:** 2.1.66 (for raw SQL/stored procedures)
- **Microsoft.Data.SqlClient:** (bundled with EF Core)

### Cloud Storage
- **Azure.Storage.Blobs:** 12.27.0
- **Azure.Identity:** 17.1

### API Tools
- **Microsoft.AspNetCore.Mvc.Versioning:** 5.1.0
- **Swashbuckle.AspNetCore:** 6.6.2 (Swagger)

---

## Project Structure

```
cfm-backend/
├── Controllers/
│   ├── AuthController.cs                  # Login, refresh token
│   ├── MastersController.cs              # Types, Enums, master data
│   ├── WebUser/
│   │   └── WebUserController.cs          # User info, privileges
│   ├── Helpdesk/
│   │   ├── WorkRequestController.cs      # Work Request CRUD
│   │   ├── JobCodeController.cs          # Job codes
│   │   ├── PriorityLevelController.cs    # Priority levels
│   │   ├── PICController.cs              # Person-in-charge
│   │   └── Settings/
│   │       ├── WorkRequestController.cs  # WR settings CRUD
│   │       └── JobCodeController.cs      # Job code settings
│   ├── Property/
│   │   └── PropertyController.cs         # Properties, floors, rooms
│   ├── ServiceProvider/
│   │   └── ServiceProviderController.cs  # Service provider management
│   ├── ContactLibrary/
│   │   └── EmployeeController.cs         # Employee management
│   ├── Asset/
│   │   └── AssetController.cs            # Asset management
│   └── DocumentController.cs             # Document operations
├── Services/
│   ├── TokenService.cs                    # JWT generation
│   ├── TokenValidationMiddleware.cs       # Token validation
│   ├── Users/
│   │   └── IUserServices.cs              # User operations
│   ├── Helpdesk/
│   │   ├── IWorkRequestServices.cs       # Work Request business logic
│   │   ├── IHelpdeskServices.cs
│   │   ├── IJobCodeServices.cs
│   │   ├── IPriorityLevelServices.cs
│   │   └── IPICServices.cs
│   ├── Property/
│   │   └── IPropertyService.cs
│   ├── ServiceProvider/
│   │   └── IServiceProviderServices.cs
│   ├── ContactLibrary/
│   │   └── IEmployeeServices.cs
│   ├── Asset/
│   │   └── IAssetServices.cs
│   ├── IMasterServices.cs                 # Master data operations
│   ├── IBlobServices.cs                   # Azure Blob operations
│   └── IEmailDistributionServices.cs      # Email distribution lists
├── Models/
│   ├── DataModels/
│   │   ├── CFMDbContext.cs               # EF Core DbContext (274 models)
│   │   ├── Type.cs                       # Client-specific choice sets
│   │   ├── Enum.cs                       # System-wide choice sets
│   │   ├── WebUser.cs                    # User accounts
│   │   ├── WorkRequest.cs                # Work request entity
│   │   ├── Property.cs                   # Property entity
│   │   ├── Employee.cs                   # Employee entity
│   │   └── ... (270+ other entities)
│   ├── ApiResponseDto.cs                  # Unified API response wrapper
│   ├── SharedFunctions.cs                 # Utility functions
│   ├── HelpdeskModel/                     # Work Request DTOs
│   ├── PropertyModel/                     # Property DTOs
│   ├── ServiceProviderModel/              # Service Provider DTOs
│   ├── ContactLibraryModel/               # Employee DTOs
│   ├── AssetModel/                        # Asset DTOs
│   ├── WebUserModel/                      # User DTOs
│   ├── Authentication/                    # Login DTOs
│   └── GeneralModel.cs                    # Shared DTOs
├── Helpers/
│   ├── DbHelper.cs                        # Dapper operations
│   ├── BlobHelper.cs                      # Azure Blob helper
│   └── DocumentValidator.cs               # File validation
├── Program.cs                             # DI, middleware configuration
└── appsettings.json                       # Configuration
```

---

## Configuration

### appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Jwt": {
    "Key": "this_is_a_very_long_secret_key_1234567890",
    "Issuer": "CFMSystemAPI",
    "Audience": "CFMSystemClient"
  },
  "DatabaseSettings": {
    "CommandTimeout": 150,
    "DapperCommandTimeout": 120,
    "EFCoreCommandTimeout": 120
  },
  "ConnectionStrings": {
    "conn_cfmsystem": "Server=...;Database=CFMSystem;...",
    "conn_cfmsystem_changehistory": "Server=...;Database=CFMSystem_ChangeHistory;...",
    "conn_cfmsystem_SeperateDB": "Server=...;Database=CFMSystem_SeperateDB;...",
    "BlobConnectionString1": "DefaultEndpointsProtocol=https;AccountName=..."
  },
  "BlobConnectionString1": "...",
  "BlobConnectionString2": "...",
  "AllowedHosts": "*"
}
```

### JWT Configuration

- **Token Expiry:** 15 minutes (hardcoded in TokenService)
- **Refresh Token Expiry:** 1 day (stored in WebUser table)
- **Algorithm:** HMAC-SHA256
- **Claims:** `ClaimTypes.Name` (username), `idWebUser` (user ID)

### Database Command Timeouts

- **Global Timeout:** 150 seconds
- **Dapper Timeout:** 120 seconds
- **EF Core Timeout:** 120 seconds (configured in Program.cs)

---

## Database Architecture

### Entity Framework Core DbContext

**File:** `Models/DataModels/CFMDbContext.cs`

**Total Entities:** 274 database tables mapped as `DbSet<T>` properties

**Key Entities:**
- WebUser, WebUserPrivilege, WebUserClient
- Client, Company, Department, Employee
- Property, PropertyFloor, PropertyRoomZone
- WorkRequest, WorkRequestUpdate, WorkRequestWorker
- ServiceProvider, ServiceProviderContract
- Asset, AssetGroup, AssetAssignment
- Type (client-specific master data)
- Enum (system-wide master data)
- PriorityLevel, JobCode
- CostApproval, CostApproverGroup
- Budget, BudgetCode
- Document, ContactInfo

### Database Connection

**Primary:** `conn_cfmsystem` (main operational database)
**Change History:** `conn_cfmsystem_changehistory` (audit trail)
**Separate DB:** `conn_cfmsystem_SeperateDB` (specialized data)

### Data Access Patterns

1. **Entity Framework Core:** Standard CRUD, LINQ queries
2. **Dapper:** Stored procedures, complex queries (via DbHelper)
3. **Raw SQL:** ADO.NET via DbHelper for DataSet operations

### Database Warmup

On application startup, Program.cs performs EF Core model compilation to reduce first-request latency:

```csharp
var dbContext = scope.ServiceProvider.GetRequiredService<CFMDbContext>();
_ = await dbContext.TimeZones.Take(1).ToListAsync();
```

---

## Authentication & Authorization

### Authentication Flow

1. **Login:** `POST /api/auth/login`
   - **Method:** Basic Authentication (Base64 encoded `username:password` in Authorization header)
   - **Password Hashing:** SHA512 with salt (via `SharedFunctions.ComputeHash`)
   - **Response:** JWT access token (15 min) + refresh token (1 day)
   - **Database Update:** Stores tokens in `WebUser` table
   - **Response Format:** `ApiResponseDto<LoginResponse>`

2. **Token Refresh:** `POST /api/auth/refresh_token?refreshToken={token}`
   - **Validation:** Checks refresh token + expiry in database
   - **Response:** New access token + new refresh token
   - **Database Update:** Updates tokens in `WebUser` table

### JWT Token Structure

**Token Service:** `Services/TokenService.cs`

**Token Generation:**
```csharp
public string GenerateJwtToken(WebUser user, out DateTime expires)
{
    expires = DateTime.UtcNow.AddMinutes(15);
    var claims = new[]
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim("idWebUser", user.IdWebUser.ToString())
    };
    // ... signing with HMAC-SHA256
}
```

**Accessing Claims in Controllers:**
```csharp
var claimValue = User.FindFirst("idWebUser")?.Value;
int idWebUser;
if (!int.TryParse(claimValue, out idWebUser))
    idWebUser = 0;
```

### Token Validation Middleware

**File:** `Services/TokenValidationMiddleware.cs`

**Functionality:**
- Intercepts all requests with `Authorization: Bearer {token}` header
- Validates JWT signature, issuer, audience, lifetime
- Returns 401 if token is invalid or expired
- Applied globally via `app.UseMiddleware<TokenValidationMiddleware>()`

### Authorization Attribute

All controllers (except AuthController login endpoint) require `[Authorize]` attribute:

```csharp
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/...")]
public class SomeController : ControllerBase { }
```

---

## API Response Format

### Unified Response Wrapper

**File:** `Models/ApiResponseDto.cs`

All API endpoints return responses wrapped in `ApiResponseDto<T>`:

```csharp
public class ApiResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    // Static helpers
    public static ApiResponseDto<T> SuccessResult(T data, string message = "Success");
    public static ApiResponseDto<T> ErrorResult(string message, List<string>? errors = null);
}
```

### Response Examples

**Success Response:**
```json
{
  "success": true,
  "message": "success",
  "timestamp": "2026-02-26T10:30:45.123Z",
  "data": { /* actual data */ },
  "errors": []
}
```

**Error Response:**
```json
{
  "success": false,
  "message": "Invalid idClient",
  "timestamp": "2026-02-26T10:30:45.123Z",
  "data": null,
  "errors": [
    "User does not have access to this client"
  ]
}
```

### HTTP Status Codes

| Status | Scenario | Usage |
|--------|----------|-------|
| 200 OK | Success | All successful responses (even with `success: false` in body) |
| 400 Bad Request | Invalid input | ModelState errors, validation failures |
| 401 Unauthorized | Auth failure | Invalid credentials, expired token |
| 500 Internal Server Error | Server error | Unhandled exceptions |
| 503 Service Unavailable | DB unavailable | Database connection issues |

---

## Controllers & Endpoints

### AuthController

**Base Route:** `/api/v1.0/auth`
**Authorization:** Not required for login, required for others

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| POST | `/login` | User login | Basic Auth header | `ApiResponseDto<LoginResponse>` |
| POST | `/refresh_token?refreshToken={token}` | Refresh access token | - | `ApiResponseDto<LoginResponse>` |

**LoginResponse:**
```csharp
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
  "refreshToken": "base64randomstring...",
  "idUser": 123
}
```

---

### WebUserController

**Base Route:** `/api/v1.0/web-user`
**Authorization:** Required

| Method | Endpoint | Description | Query Params | Response |
|--------|----------|-------------|--------------|----------|
| GET | `/info` | Get current user details | - | `UserInfoDto` |
| GET | `/privileges` | Get user privileges | - | `List<UserPrivilegeDto>` |

**Use Cases:**
- Frontend loads user info after login
- Frontend loads privileges for permission checks

---

### MastersController

**Base Route:** `/api/v1.0/masters`
**Authorization:** Required

| Method | Endpoint | Description | Query Params | Response |
|--------|----------|-------------|--------------|----------|
| GET | `/types/{category}` | Get client-specific types | `idClient`, `parentTypeId?` | `ApiResponseDto<IEnumerable<TypeFormDetailResponse>>` |
| GET | `/enums/{category}` | Get system-wide enums | - | `ApiResponseDto<IEnumerable<EnumFormDetailResponse>>` |
| GET | `/public-holidays/{year}` | Get public holidays | `idClient` | `ApiResponseDto<IEnumerable<PublicHolidayResponse>>` |
| GET | `/office-hours` | Get office hours | `idClient` | `ApiResponseDto<IEnumerable<OfficeHourResponse>>` |
| GET | `/company-contacts` | Get company contacts | `cid`, `prefix?` | `ApiResponseDto<IEnumerable<CompanyContactInfoDto>>` |

**Common Type Categories:**
- `workCategory` - Work Request categories
- `otherCategory` - Other categories for WR
- `priorityLevel` - Priority levels (also available as separate endpoint)
- `workRequestAdditionalInformation` - WR additional info checkboxes

**Common Enum Categories:**
- `workRequestStatus` - Work Request statuses (New, In Progress, Completed, Cancelled)
- `requestMethod` - Request methods (Email, Phone, Walk-in, etc.)
- `feedbackType` - Feedback types
- `documentFileType` - Allowed file extensions

---

### WorkRequestController (Helpdesk)

**Base Route:** `/api/v1.0/work-request`
**Authorization:** Required

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| POST | `/list` | Get paginated work request list | `WorkRequestListParam` | `ApiResponseDto<PagedResponse<WorkRequestListResponse>>` |
| POST | `/list-filter` | Get filter options for WR list | `WorkRequestListFilterRequest` | `ApiResponseDto<WorkRequestListFilter>` |
| GET | `/{idWorkRequest}` | Get work request details | Query: `cid` | `ApiResponseDto<WorkRequestFormDetailDto>` |
| POST | `` | Create work request | `WorkRequestDetailParam` | `ApiResponseDto<object>` with `idWorkRequest` |
| PUT | `` | Update work request | `WorkRequestDetailParam` | `ApiResponseDto<object>` with `idWorkRequest` |

**WorkRequestListParam:**
```csharp
{
  "Client_idClient": 1,
  "page": 1,
  "keywords": "search term",
  // ... filter fields
}
```

**WorkRequestListFilter Response:**
- Feedback types
- Important checklists
- Locations (grouped by property groups)
- Statuses
- Request methods
- Service providers
- Work categories
- Priority levels
- Other categories

**Client Validation:**
All endpoints validate that `idWebUser` has access to the specified `Client_idClient` via `SharedFunctions.ValidateClientAccessAsync()`.

---

### PropertyController

**Base Route:** `/api/v1.0/property`
**Authorization:** Required

| Method | Endpoint | Description | Query Params | Response |
|--------|----------|-------------|--------------|----------|
| GET | `` | Get properties list | `idClient`, `idPropertyType?` | `ApiResponseDto<IEnumerable<PropertyFormDetailResponse>>` |
| GET | `/{idProperty}/floors` | Get floors by property | - | `ApiResponseDto<IEnumerable<PropertyFloorFormDetailResponse>>` |
| GET | `/{idProperty}/floors/{Idfloor}/roomzones` | Get room zones by floor | - | `ApiResponseDto<IEnumerable<PropertyRoomZoneFormDetailResponse>>` |

**Typical Flow:**
1. Load properties dropdown → `GET /property?idClient=1`
2. User selects property → `GET /property/{id}/floors`
3. User selects floor → `GET /property/{id}/floors/{floorId}/roomzones`

---

### Helpdesk Settings Controllers

**Priority Level:** `/api/v1.0/helpdesk/settings/priority-level`
**Job Code:** `/api/v1.0/helpdesk/settings/job-code`
**Work Request Settings:** `/api/v1.0/helpdesk/settings/work-request/*`

Standard CRUD operations for helpdesk master data (client-specific).

---

### ServiceProviderController

**Base Route:** `/api/v1.0/service-provider`
**Authorization:** Required

Standard CRUD operations for service providers.

---

### EmployeeController (ContactLibrary)

**Base Route:** `/api/v1.0/contact-library/employee`
**Authorization:** Required

Standard CRUD + search operations for employees.

---

### AssetController

**Base Route:** `/api/v1.0/asset`
**Authorization:** Required

Standard CRUD operations for asset management.

---

## Services Architecture

### Service Layer Pattern

All business logic is encapsulated in service classes implementing interfaces:

```csharp
public interface IWorkRequestServices
{
    Task<WorkRequestListResult> GetWorkRequestList(WorkRequestListParam param, WorkRequestDTO context);
    Task<WorkRequestFormDetailDto> GetWorkRequestDetailsAsync(int idWorkRequest, int idWebUser, int cid);
    Task<int> CreateWorkRequestAsync(WorkRequestDetailParam dto, int idWebUser);
    Task<int> UpdateWorkRequestAsync(WorkRequestDetailParam dto, int idWebUser);
}
```

### Dependency Injection

**File:** `Program.cs`

```csharp
builder.Services.AddScoped<IUserServices, UserService>();
builder.Services.AddScoped<IWorkRequestServices, WorkRequestService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IMasterServices, MasterServices>();
builder.Services.AddScoped<IEmployeeServices, EmployeeService>();
builder.Services.AddScoped<IServiceProviderServices, ServiceProviderService>();
builder.Services.AddScoped<IPriorityLevelServices, PrioritylevelService>();
builder.Services.AddScoped<IJobCodeServices, JobCodeService>();
builder.Services.AddScoped<IPICServices, PICServices>();
builder.Services.AddScoped<IAssetServices, AssetService>();
builder.Services.AddScoped<IBlobServices, BlobService>();
builder.Services.AddScoped<IHelpdeskServices, HelpdeskServices>();
builder.Services.AddScoped<IEmailDistributionServices, EmailDistributionServices>();
```

### Service Implementation Example

**File:** `Services/IMasterServices.cs`

```csharp
public class MasterServices : IMasterServices
{
    private readonly CFMDbContext _dbContext;

    public MasterServices(CFMDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<TypeFormDetailResponse>> GetTypesAsync(int idClient, string category)
    {
        return await (from p in _dbContext.Types
                      where p.IsActiveData == true
                        && p.ClientIdClient == idClient
                        && p.Category == category
                      select new TypeFormDetailResponse
                      {
                          IdType = p.IdType,
                          Parent_Type_idType = p.ParentTypeIdType,
                          DisplayOrder = p.DisplayOrder,
                          TypeName = WebUtility.HtmlDecode(p.Text)
                      })
                     .ToListAsync();
    }

    // CRUD operations with transactions...
}
```

---

## Master Data System

### Type vs Enum

The backend has two parallel master data systems:

#### Type Table (Client-Specific)

**File:** `Models/DataModels/Type.cs`

**Purpose:** Client-specific choice sets that vary by client/company.

**Key Fields:**
- `IdType` - Primary key
- `ClientIdClient` - Ref to Client (owner)
- `ParentTypeIdType` - Self-reference for hierarchical data
- `Category` - Code matched in programming (e.g., `"workCategory"`)
- `Text` - Display text
- `DisplayOrder` - Custom sort order
- `IsActiveData` - Soft delete flag

**Common Categories:**
- `workCategory` - Work Request categories (Cleaning, Maintenance, etc.)
- `otherCategory` - Other categorization
- `priorityLevel` - Priority levels
- `workRequestAdditionalInformation` - Important checklists

**API Endpoint:** `GET /api/v1.0/masters/types/{category}?idClient={idClient}`

#### Enum Table (System-Wide)

**File:** `Models/DataModels/Enum.cs`

**Purpose:** Fixed system-wide choice sets (same for all clients).

**Key Fields:**
- `IdEnum` - Primary key
- `ParentEnumIdEnum` - Self-reference for hierarchical data
- `Category` - Code matched in programming (e.g., `"workRequestStatus"`)
- `Text` - Display text
- `DisplayOrder` - Custom sort order
- `IsActiveData` - Soft delete flag

**Common Categories:**
- `workRequestStatus` - Work Request statuses (New, In Progress, Completed, Cancelled)
- `requestMethod` - Request methods (Email, Phone, Walk-in)
- `feedbackType` - Feedback types
- `documentFileType` - Allowed file extensions

**API Endpoint:** `GET /api/v1.0/masters/enums/{category}`

### When to Use Which?

| Use Type When | Use Enum When |
|---------------|---------------|
| Different clients need different values | All clients use same values |
| Client wants custom categories | System-defined categories |
| Values change frequently per client | Values rarely change |
| Example: Work categories, priority levels | Example: Status codes, file types |

---

## Helper Classes

### DbHelper

**File:** `Helpers/DbHelper.cs`

**Purpose:** Dapper-based raw SQL and stored procedure execution.

**Key Method:**
```csharp
public DataSet ExecuteCommand(
    CommandType commandType,
    string commandText,
    string connectionKey,
    SqlParameter[] parameters = null)
{
    // ... executes SQL/stored proc and returns DataSet
}
```

**Usage:**
```csharp
var parameters = new List<SqlParameter>()
{
    new SqlParameter("@idWebUser", SqlDbType.Int) { Value = idWebUser },
    new SqlParameter("@idClient", SqlDbType.Int) { Value = idClient }
};

var ds = _dbHelper.ExecuteCommand(
    CommandType.StoredProcedure,
    "App_Web_Helpdesk_WorkRequest_GetListFilter",
    "conn_cfmsystem",
    parameters.ToArray()
);
```

**Configuration:** Command timeout defaults to 120 seconds (from appsettings.json).

---

### BlobHelper

**File:** `Helpers/BlobHelper.cs`

**Purpose:** Azure Blob Storage operations (currently minimal implementation).

**Dependency:** `IBlobServices` injected.

---

### SharedFunctions

**File:** `Models/SharedFunctions.cs`

**Purpose:** Utility functions used across the backend.

**Key Functions:**

#### Password Hashing
```csharp
public static string ComputeHash(string plainText, string hashAlgorithm, byte[] saltBytes)
// SHA512 hashing with salt
```

#### Client Access Validation
```csharp
public static async Task ValidateClientAccessAsync(
    CFMDbContext dbContext,
    int idWebUser,
    int idClient)
// Throws exception if user doesn't have access to client
```

#### Pagination
```csharp
public static int GetTotalPages(int totalData, int pageSize)
// Calculate total pages for pagination
```

#### Media URL Encryption
```csharp
public static string EncryptMediaUrl(string plainText)
public static string DecryptMediaUrl(string encryptedText)
// AES encryption for media URLs
```

#### Time Zone Conversion
```csharp
public static DateTime ConvertToUserPreferredTime(DateTime originalDateTime, UserPreferredTimeZone timeZone)
public static DateTime ConvertToUniversalTime(DateTime originalDateTime, UserPreferredTimeZone timeZone)
// Convert between UTC and user time zone
```

#### Office Hour Calculations
```csharp
public static async Task<DateTime> AddDateBasedOnOfficeHour(
    DateTime inputDate,
    TimeSpan duration,
    bool isWithinOfficeHours,
    int clientId)
// Calculate target dates accounting for office hours and holidays
```

#### Keyword Search Generation
```csharp
public static KeywordsGenerate(string keywords)
// Generate search tags and keyword tables for SQL
```

---

## Security Features

### Client Access Validation

**Function:** `SharedFunctions.ValidateClientAccessAsync()`

**Purpose:** Ensures users can only access data for clients they're authorized for.

**Implementation:**
```csharp
public static async Task ValidateClientAccessAsync(
    CFMDbContext dbContext,
    int idWebUser,
    int idClient)
{
    var userClient = await dbContext.WebUserClients
        .Where(x => x.WebUserIdWebUser == idWebUser && x.ClientIdClient == idClient)
        .FirstOrDefaultAsync();

    if (userClient == null)
        throw new UnauthorizedAccessException("User does not have access to this client");
}
```

**Usage Pattern:**
All controllers that accept `idClient` parameter must validate access:

```csharp
try
{
    await SharedFunctions.ValidateClientAccessAsync(_dbContext, idWebUser, dto.Client_idClient);
}
catch (Exception x)
{
    return StatusCode(400, ApiResponseDto<object>.ErrorResult("Invalid idClient", new List<string> { x.Message }));
}
```

### Password Security

**Algorithm:** SHA512 with random salt
**Salt Storage:** Stored in `WebUser.Salt` field
**Hash Storage:** Stored in `WebUser.Password` field

**Login Verification:**
```csharp
var user = await _context.WebUsers.SingleOrDefaultAsync(x => x.Username == username);
string userEnteredPasswordHash = SharedFunctions.ComputeHash(password, "SHA512", SharedFunctions.StringToByte(user.Salt));

if (user.Password == userEnteredPasswordHash)
{
    // Login successful
}
```

### CORS Configuration

**File:** `Program.cs`

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

**Note:** Currently allows all origins. Production should restrict to specific origins.

---

## Common Patterns

### Controller Standard Pattern

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/resource")]
[ApiController]
[Authorize]
public class ResourceController : ControllerBase
{
    private readonly IResourceService _service;
    private readonly CFMDbContext _dbContext;
    private readonly ILogger<ResourceController> _logger;

    public ResourceController(IResourceService service, CFMDbContext dbContext, ILogger<ResourceController> logger)
    {
        _service = service;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetResource([FromQuery] int idClient)
    {
        try
        {
            // 1. Extract user ID from claims
            var claimValue = User.FindFirst("idWebUser")?.Value;
            int idWebUser;
            if (!int.TryParse(claimValue, out idWebUser))
                idWebUser = 0;

            // 2. Validate client access
            try
            {
                await SharedFunctions.ValidateClientAccessAsync(_dbContext, idWebUser, idClient);
            }
            catch (Exception x)
            {
                return StatusCode(400, ApiResponseDto<object>.ErrorResult("Invalid idClient", new List<string> { x.Message }));
            }

            // 3. Execute business logic
            var result = await _service.GetResourceAsync(idClient);

            // 4. Return success response
            return Ok(ApiResponseDto<ResourceDto>.SuccessResult(result, "success"));
        }
        catch (Exception ex)
        {
            // 5. Return error response
            return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server Error", new List<string> { ex.Message }));
        }
    }
}
```

### CRUD Service Pattern

```csharp
public async Task<int> CreateAsync(int idWebUser, CreateDto dto)
{
    try
    {
        var entity = new Entity
        {
            ClientIdClient = dto.IdClient,
            // ... map properties
            IsActiveData = true,
            CreatedBy = idWebUser,
            CreatedDate = DateTime.UtcNow
        };

        _dbContext.Entities.Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity.IdEntity;
    }
    catch
    {
        throw;
    }
}

public async Task<int> UpdateAsync(int idWebUser, UpdateDto dto)
{
    try
    {
        var entity = await _dbContext.Entities
            .SingleOrDefaultAsync(x => x.IdEntity == dto.IdEntity && x.IsActiveData == true);

        if (entity == null)
            throw new KeyNotFoundException("Data not found");

        // Update properties
        entity.UpdatedBy = idWebUser;
        entity.UpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return entity.IdEntity;
    }
    catch
    {
        throw;
    }
}

public async Task<bool> DeleteAsync(int idEntity, int idWebUser)
{
    await using var transaction = await _dbContext.Database.BeginTransactionAsync();
    try
    {
        var entity = await _dbContext.Entities
            .SingleAsync(x => x.IdEntity == idEntity && x.IsActiveData == true);

        entity.IsActiveData = false;  // Soft delete
        entity.UpdatedBy = idWebUser;
        entity.UpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }
    catch
    {
        await transaction.RollbackAsync();
        return false;
    }
}
```

### Pagination Pattern

**Response Structure:**
```csharp
public class PagedResponse<T>
{
    public List<T> Data { get; set; }
    public Metadata Metadata { get; set; }
}

public class Metadata
{
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}
```

**Controller Implementation:**
```csharp
int totalPages = SharedFunctions.GetTotalPages(result.CountWorkRequest, 50);

var response = new PagedResponse<WorkRequestListResponse>
{
    Data = result.WorkRequests,
    Metadata = new Metadata
    {
        TotalCount = result.CountWorkRequest,
        PageSize = 50,
        CurrentPage = requestDto.page,
        TotalPages = totalPages
    }
};

return Ok(ApiResponseDto<PagedResponse<WorkRequestListResponse>>.SuccessResult(response, "success"));
```

### Stored Procedure Pattern

**Usage with DbHelper:**
```csharp
var parameters = new List<SqlParameter>()
{
    new SqlParameter("@idWebUser", SqlDbType.Int) { Value = idWebUser },
    new SqlParameter("@idClient", SqlDbType.Int) { Value = reqBody.idClient }
};

if (reqBody.keywords != null)
{
    var keywordResult = SharedFunctions.KeywordsGenerate(reqBody.keywords);
    if (keywordResult.searchTag != null)
        parameters.Add(new SqlParameter("@searchTag", SqlDbType.NVarChar) { Value = keywordResult.searchTag });

    if (keywordResult.keywords != null)
        parameters.Add(new SqlParameter("@Tb_Keywords", SqlDbType.Structured) { Value = keywordResult.keywords });
}

DataSet ds = _dbHelper.ExecuteCommand(
    CommandType.StoredProcedure,
    "App_Web_Helpdesk_WorkRequest_GetListFilter",
    "conn_cfmsystem",
    parameters.ToArray()
);

// Process DataSet
if (ds.Tables.Count == 0)
    return BadRequest("No data");

DataTable dt = ds.Tables[0];
// ... parse DataTable rows
```

### HTML Encoding/Decoding

**Always encode when saving to database:**
```csharp
newEntity.Text = WebUtility.HtmlEncode(dto.Text);
```

**Always decode when reading from database:**
```csharp
TypeName = WebUtility.HtmlDecode(p.Text)
```

This prevents XSS attacks and encoding issues.

---

## Best Practice Notes

### Issues and Recommendations

These are observations about the backend implementation that may not follow industry best practices. **DO NOT MODIFY THE BACKEND.** These notes are for awareness only.

#### 1. CORS Configuration - Allow All Origins

**Issue:** CORS policy allows any origin.

**File:** `Program.cs`
```csharp
policy.AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod();
```

**Recommendation:** In production, restrict to specific frontend origins:
```csharp
policy.WithOrigins("https://cfm-frontend.example.com")
    .AllowAnyHeader()
    .AllowAnyMethod();
```

#### 2. Database Credentials in appsettings.json

**Issue:** SQL Server credentials stored in plain text in appsettings.json.

**Recommendation:** Use:
- Azure Key Vault for cloud deployments
- User Secrets for development
- Integrated Security / Managed Identity for production

#### 3. Hardcoded Encryption Keys

**Issue:** AES encryption keys hardcoded in SharedFunctions.cs:
```csharp
DOC_AES_KEY = "CfmSyst3m%D0cK3y";
BLOB_AES_KEY = "CfmSyst%Bl0b%K3y";
```

**Recommendation:** Move to configuration/secrets management.

#### 4. Implicit Distributed Transactions

**Issue:** Global enabling of distributed transactions in Program.cs:
```csharp
TransactionManager.ImplicitDistributedTransactions = true;
```

**Recommendation:** Only enable when actually needed. Consider using explicit `TransactionScope` where required.

#### 5. Generic Exception Handling

**Issue:** Controllers catch all exceptions and return generic 500 errors:
```csharp
catch (Exception ex)
{
    return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server Error", new List<string> { ex.Message }));
}
```

**Recommendation:** Implement global exception handling middleware to avoid repetitive try-catch blocks.

#### 6. Password Hashing Algorithm

**Issue:** Using SHA512 for password hashing.

**Recommendation:** Modern best practice is to use:
- **BCrypt** (preferred)
- **Argon2** (most secure)
- **PBKDF2** (acceptable)

SHA512 is fast, which makes it vulnerable to brute-force attacks. BCrypt/Argon2 are intentionally slow.

#### 7. Token Storage in Database

**Issue:** JWT tokens stored in database (WebUser table).

**Observation:** This works but reduces JWT benefits (stateless auth). Tokens can't be invalidated without database check.

**Alternative Approach:** Store only refresh tokens in database, not access tokens. Use token blacklisting if needed.

#### 8. No Request/Response Logging

**Issue:** No centralized API request/response logging.

**Recommendation:** Implement middleware to log:
- Request method, path, body
- Response status, body
- Execution time
- User ID

Useful for debugging and audit trails.

#### 9. No Rate Limiting

**Issue:** No API rate limiting or throttling.

**Recommendation:** Implement rate limiting to prevent abuse:
```csharp
builder.Services.AddRateLimiter(options => { ... });
```

#### 10. Model Validation

**Issue:** Some endpoints check `ModelState.IsValid` manually, some don't.

**Recommendation:** Use `[ApiController]` attribute consistently (already present) which auto-validates and returns 400 for invalid models.

#### 11. Inconsistent Error Messages

**Issue:** Some errors return technical details (`ex.Message`), exposing internals.

**Recommendation:** Return user-friendly messages, log technical details server-side.

#### 12. Database Command Timeout

**Issue:** Long command timeouts (120-150 seconds) may mask slow queries.

**Recommendation:**
- Optimize slow queries
- Use shorter timeouts (30-60s)
- Monitor query performance

#### 13. No API Versioning in Response

**Issue:** API version defined but not returned in response headers.

**Recommendation:** Add version to response headers for client awareness:
```csharp
context.Response.Headers["X-API-Version"] = "1.0";
```

---

## API Endpoints Quick Reference

### Authentication
- `POST /api/v1.0/auth/login` - Login (Basic Auth)
- `POST /api/v1.0/auth/refresh_token` - Refresh token

### User Management
- `GET /api/v1.0/web-user/info` - Get user info
- `GET /api/v1.0/web-user/privileges` - Get user privileges

### Master Data
- `GET /api/v1.0/masters/types/{category}` - Get client-specific types
- `GET /api/v1.0/masters/enums/{category}` - Get system-wide enums
- `GET /api/v1.0/masters/public-holidays/{year}` - Get public holidays
- `GET /api/v1.0/masters/office-hours` - Get office hours
- `GET /api/v1.0/masters/company-contacts` - Get company contacts

### Work Requests
- `POST /api/v1.0/work-request/list` - Get work request list (paginated)
- `POST /api/v1.0/work-request/list-filter` - Get filter options
- `GET /api/v1.0/work-request/{idWorkRequest}` - Get work request details
- `POST /api/v1.0/work-request` - Create work request
- `PUT /api/v1.0/work-request` - Update work request

### Properties
- `GET /api/v1.0/property` - Get properties
- `GET /api/v1.0/property/{idProperty}/floors` - Get floors
- `GET /api/v1.0/property/{idProperty}/floors/{Idfloor}/roomzones` - Get room zones

### Service Providers
- `GET /api/v1.0/service-provider` - Get service providers
- CRUD endpoints for service provider management

### Employees (Contact Library)
- `GET /api/v1.0/contact-library/employee` - Get employees
- CRUD endpoints + search

### Assets
- `GET /api/v1.0/asset` - Get assets
- CRUD endpoints for asset management

### Helpdesk Settings
- `/api/v1.0/helpdesk/settings/priority-level` - Priority level CRUD
- `/api/v1.0/helpdesk/settings/job-code` - Job code CRUD
- `/api/v1.0/helpdesk/settings/work-request/*` - Work request settings

---

## Integration Guidelines for Frontend

### Authentication Flow

1. **Login:**
```javascript
// Encode credentials
const credentials = btoa(`${username}:${password}`);

// Call login API
const response = await fetch('https://backend/api/v1.0/auth/login', {
    method: 'POST',
    headers: {
        'Authorization': `Basic ${credentials}`
    }
});

const result = await response.json();
// result.data.token - Store in session/cookie
// result.data.refreshToken - Store in session/cookie
```

2. **Authenticated Requests:**
```javascript
const response = await fetch('https://backend/api/v1.0/resource', {
    headers: {
        'Authorization': `Bearer ${accessToken}`
    }
});
```

3. **Token Refresh (on 401):**
```javascript
const response = await fetch(`https://backend/api/v1.0/auth/refresh_token?refreshToken=${refreshToken}`, {
    method: 'POST'
});

const result = await response.json();
// Update stored tokens
```

### Error Handling

```javascript
const response = await fetch(url, options);
const result = await response.json();

if (result.success) {
    // Use result.data
} else {
    // Handle error
    console.error(result.message);
    console.error(result.errors);
}
```

### Client Context

Always pass `idClient` parameter when required:
```javascript
const url = `/api/v1.0/masters/types/workCategory?idClient=${clientId}`;
```

Backend validates user has access to the specified client.

### Pagination

Request with page parameter:
```javascript
const payload = {
    Client_idClient: clientId,
    page: currentPage,
    // ... other filters
};

const response = await fetch('/api/v1.0/work-request/list', {
    method: 'POST',
    headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
});

const result = await response.json();
// result.data.data - Array of items
// result.data.metadata.totalPages - Total pages
// result.data.metadata.currentPage - Current page
```

---

## Database Schema Notes

### Soft Deletes

Most entities use soft delete pattern:
- `IsActiveData` field (boolean)
- `true` = active, `false` = deleted/inactive
- Queries always filter by `IsActiveData == true`
- Deletions set `IsActiveData = false` and update `UpdatedBy`, `UpdatedDate`

### Audit Fields

Standard audit fields on most entities:
- `CreatedBy` - WebUser ID who created the record
- `CreatedDate` - UTC timestamp of creation
- `UpdatedBy` - WebUser ID who last updated
- `UpdatedDate` - UTC timestamp of last update

### Client Isolation

Multi-tenant architecture:
- Most entities have `ClientIdClient` foreign key
- Queries always filter by client ID
- Client access validation enforced at API level

### Time Zone Handling

- All datetime fields stored in UTC
- User-specific time zone stored in `WebUser` table
- Frontend responsible for displaying in user's time zone
- Backend provides conversion utilities in SharedFunctions

---

## Summary

The CFM Backend is a well-structured ASP.NET Core 8.0 Web API providing:

- ✅ RESTful API with versioning support
- ✅ JWT authentication with refresh tokens
- ✅ Comprehensive work request management system
- ✅ Multi-tenant architecture with client isolation
- ✅ Service-based architecture with clear separation of concerns
- ✅ Entity Framework Core + Dapper for flexible data access
- ✅ Unified API response format
- ✅ Soft delete pattern for data retention
- ✅ Audit trail with CreatedBy/UpdatedBy tracking
- ✅ Master data management (Types + Enums)

**Key Architectural Decisions:**
- **Entity Framework Core** for CRUD operations
- **Dapper** for complex queries and stored procedures
- **JWT tokens** stored in database for invalidation capability
- **Client-based data isolation** enforced at API level
- **Service layer** contains all business logic
- **ApiResponseDto<T>** wrapper for consistent responses

**Frontend Integration:**
- All endpoints require `Authorization: Bearer {token}` header (except login)
- All responses wrapped in `ApiResponseDto<T>` format
- Client ID (`idClient`) required for most operations
- Backend validates user access to specified client
- Pagination supported with standard metadata format

This documentation should be consulted when:
- Implementing new frontend features that call backend APIs
- Understanding available API endpoints and their contracts
- Debugging integration issues
- Planning new features that require backend changes
- Understanding master data structure (Type vs Enum)