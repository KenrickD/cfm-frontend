# MD-005-DocumentRetrievalStrategy

## Goal Description
Establish a secure, efficient architecture for retrieving and viewing documents (Images, PDFs, Word/Excel) stored in Azure Blob Storage within the Helpdesk module.

## Critical Security Constraint
**User Concern**: Standard SAS URLs can be shared with unauthorized users, allowing access until the token expires.
**Requirement**: The system must ensure that **only** the authenticated user specifically authorized to view requirements can access the file. If a link is shared, it should **not work** for others.

## Strategic Recommendation: Authenticated Proxy Stream

To strictly satisfy the security requirement and prevent unauthorized link sharing, we must use the **Authenticated Proxy Pattern** instead of direct Blob access.

### The "Proxy Stream" Architecture

**Concept**:
The file is **never** exposed via a direct public URL (even a temporary one). Instead, the data is streamed through your Backend API.

**Workflow**:
1.  **Frontend Request**: Browser requests `GET /api/Helpdesk/WorkRequest/Document/{docId}`.
    -   *Crucial*: This request effectively includes the user's Auth Token (Bearer) or Cookie.
2.  **Backend Validation**:
    -   API validates the Auth Token.
    -   API checks specific permissions (e.g., `CanViewWorkRequest(docId)`).
3.  **Stream Retrieval**:
    -   If authorized, Backend opens a stream to Azure Blob Storage (using its private server-side credentials).
4.  **Response**:
    -   Backend pipes the stream directly to the HTTP Response `Body` (returning `FileStreamResult`).
    -   The file data flows: `Blob Storage` -> `Backend Server` -> `User`.

**Security Pros**:
-   **No Shared Links**: The URL `.../Document/123` behaves exactly like any other API endpoint. If a user copies this link and sends it to someone else, that person must be logged in *and* authorized to see anything. Otherwise, they get `401 Unauthorized` or `403 Forbidden`.
-   **Audit Logging**: Every single *read* of the file is logged by the application.

**Performance Cons**:
-   **Server Load**: The file traffic passes through your API server, consuming memory and bandwidth.
    -   *Mitigation*: Use `.NET 8`'s high-performance streaming (avoid loading the whole file into RAM).
    -   *Constraint*: Valid concern for massive files (e.g., videos > 100MB), but negligible for standard Helpdesk docs (PDFs/Images < 10MB).

---

## Implementation Plan

### 1. Backend API Requirements (For Backend Developer)

**Endpoint**: `GET /api/Documents/{id}`

**Implementation Logic (C# Example)**:
```csharp
[HttpGet("{id}")]
[Authorize]
public async Task<IActionResult> GetDocument(int id)
{
    // 1. Validate DB Access
    var docMetadata = _repo.GetDocument(id);
    if (!_authService.CanView(User, docMetadata)) 
        return Forbid();

    // 2. Fetch from Blob (Stream, NOT Byte Array)
    var blobClient = _blobContainerClient.GetBlobClient(docMetadata.StoredFileName);
    var downloadInfo = await blobClient.DownloadStreamingAsync();

    // 3. Return File Stream
    // Important: Set correct Content-Type (image/jpeg, application/pdf)
    return File(downloadInfo.Value.Content, docMetadata.ContentType, docMetadata.OriginalFileName);
}
```

### 2. Frontend (MVC) Implementation

#### ViewModel Updates (`WorkRequestViewModel`)
The DTO no longer needs a `DownloadUrl`. It only needs the `Id`.

```csharp
public class DocumentDto {
    public int Id { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; } 
    // Computed property for the view
    public string ViewUrl => $"/Helpdesk/GetDocument/{Id}";
}
```

#### Controller Proxy (Optional but Recommended for MVC)
To keep the "BFF" pattern consistent, the MVC Controller can also proxy the request if the pure Backend API is not directly accessible by the browser (e.g., if Backend provided only JSON and MVC handles the cookies). 

**However**, if your Backend API (`BackendBaseUrl`) is accessible, you can point specific `<img>` tags to an MVC action that calls the API.

**Recommended MVC Action**:
```csharp
// In HelpdeskController.cs
[HttpGet]
public async Task<IActionResult> GetDocument(int id)
{
    // Check View Perms
    if (CheckViewAccess(...) != null) return Forbid();

    var client = _httpClientFactory.CreateClient("BackendAPI");
    // GetStreamAsync ensures we don't buffer the whole file
    var stream = await client.GetStreamAsync($"{_config["BackendBaseUrl"]}/api/documents/{id}");
    
    // You'd need to fetch content type separately or assume/detect it
    return File(stream, "application/octet-stream", "filename.ext"); 
}
```
*Note: The above double-proxy (Blob->API->MVC->User) is heavy. Ideally, the frontend calls the API directly if auth permits.*

### 3. Viewing Strategy by File Type

#### A. Images (JPG, PNG)
- **Code**: `<img src="/Helpdesk/GetDocument/123" />`
- **Result**: Browser requests image. MVC checks session. Returns image data. Secure.

#### B. PDFs
- **Code**: `<a href="/Helpdesk/GetDocument/123" target="_blank">View PDF</a>`
- **Result**: Browser opens new tab. Request hits MVC. Auth check passes. PDF renders natively in browser.
- **Sharing Risk**: If user copies URL from address bar and sends to friend -> Friend clicks -> Not logged in -> Redirect to Login -> Access Denied. **Secure.**

#### C. Word/Excel
- **Code**: `<a href="/Helpdesk/GetDocument/123">Download</a>`
- **Result**: Browser downloads the file. User opens locally.

---

## Comparison Summary

| Feature | Strategy A: SAS Token | Strategy B: Proxy Stream (Selected) |
| :--- | :--- | :--- |
| **Link Sharing** | **Risk:** Valid until token expires (e.g., 15m). | **Secure:** Valid only for authorized session. |
| **Server Load** | Low (Direct to Azure). | Moderate (Traffic flows through Server). |
| **Revocation** | Difficult (wait for expiry). | Immediate (remove user rights). |
| **Complexity** | Medium (Token generation). | Medium (Stream handling). |

## Final Recommendation
**Implement Strategy B (Proxy Stream)**.
Given the stated concern about unauthorized link sharing, the Proxy Stream is the industry standard for securing private enterprise data. The performance overhead is acceptable for standard document sizes.
