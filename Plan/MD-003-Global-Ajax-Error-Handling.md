# Global AJAX Error Notification System

## Goal Description
Implement a global client-side mechanism to automatically display error notifications (using `toastr`) when any AJAX request returns a "soft error" response (HTTP 200 OK with `{ success: false }`).

This specifically addresses the issue where `SafeExecuteApiAsync` in `BaseController` catches exceptions and returns a structured failure response, but the frontend currently swallows these errors without notifying the user (as seen in `GetFloorsByLocation`).

The solution will standardize error feedback across the application without requiring changes to every individual AJAX call.

## User Review Required
> [!IMPORTANT]
> **Global Behavior Change**: This change will affect ALL AJAX calls that return `{ success: false, message: "..." }`. This is generally desired, but verify if there are any specific flows where silent failures are intentional.

## Analysis of Current State

### SafeExecuteApiAsync Reference
**Location:** [BaseController.cs:85-123](file:///c:/Repos/CFM%20Frontend/Controllers/BaseController.cs)

This is the core method that creates "soft errors". It has two overloads:

1. **Basic overload** (line 85-123): Simple API call wrapper
2. **Cancellation overload** (line 135+): Supports cancellation tokens and timeouts for parallel calls

**Method Signature:**
```csharp
protected async Task<(bool Success, T? Data, string Message)> SafeExecuteApiAsync<T>(
    Func<Task<HttpResponseMessage>> apiCall,
    string errorMessage = "API call failed")
```

**Behavior:**
| Scenario | Action | Return Value |
|----------|--------|--------------|
| HTTP 2xx + `apiResponse.Success == true` | Success path | `(true, apiResponse.Data, apiResponse.Message)` |
| HTTP 2xx + `apiResponse.Success == false` | API logic error | `(false, default, apiResponse.Message)` |
| HTTP 4xx/5xx | HTTP error | `(false, default, errorMessage)` |
| Exception thrown | Catches & logs | `(false, default, errorMessage)` |

**Key Insight:** The method never throws exceptions or returns HTTP errors to the browser. All failures become `{ success: false, message: "..." }` JSON responses with HTTP 200 OK status.

### Error Flow (Example: GetFloorsByLocation)
1.  **Backend API**: Returns a non-200 status (e.g., 500 Internal Server Error) or a connection failure occurs.
2.  **`SafeExecuteApiAsync`** ([BaseController.cs:85](file:///c:/Repos/CFM%20Frontend/Controllers/BaseController.cs)):
    *   Catches the `Exception` (lines 118-122) OR handles the non-success status code (lines 110-116).
    *   **Crucially**: It returns a valid tuple: `(Success: false, Data: null, Message: "Failed to load floors")`. It does *not* re-throw the exception.
3.  **Controller (`HelpdeskController`)**:
    *   Receives the failure tuple.
    *   Calls `return Json(new { success, data, message });`.
    *   **Result**: This generates an **HTTP 200 OK** response to the browser, with a JSON body containing `success: false`.
4.  **Frontend (`work-request-add.js`)**:
    *   The `$.ajax` call sees an **HTTP 200 OK** success.
    *   The `error:` callback is **NOT** triggered (because it's not a 4xx/5xx error).
    *   The `success:` callback runs, but often lacks an `else` block to handle `response.success === false`, or simply fails silently.

### The Fix
Since `SafeExecuteApiAsync` is designed to "fail safely" (hence the name) and not crash the controller, we must handle this "soft error" on the client side.

A **Global AJAX Interceptor** will listen for *every* successful AJAX request. If the JSON body contains `success: false`, it will automatically trigger the `toastr` error notification. This solves the issue globally for all endpoints using this pattern.

## Proposed Changes

### Frontend Core

#### [NEW] [global-ajax-handler.js](file:///c:/Repos/CFM%20Frontend/wwwroot/assets/js/global-ajax-handler.js)
Create a new JavaScript file to handle global AJAX events.

```javascript
/**
 * Global AJAX Event Handlers
 * Intercepts all AJAX responses to provide consistent error handling.
 */
(function ($) {
    'use strict';

    $(document).ajaxSuccess(function (event, xhr, settings) {
        try {
            // Check if response is JSON and has specific success: false indicator
            if (xhr.responseJSON && xhr.responseJSON.success === false) {
                // Ensure there is a message to show
                if (xhr.responseJSON.message) {
                    // Prevent duplicate notifications if one was already handled manually?
                    // For now, we assume the global handler is the primary notifier for system errors.
                    showNotification(xhr.responseJSON.message, 'error');
                    console.warn('API Logic Error:', xhr.responseJSON.message);
                }
            }
        } catch (e) {
            console.error('Global AJAX handler error:', e);
        }
    });

    // Optional: Also handle generic HTTP errors globally if not already handled
    $(document).ajaxError(function (event, xhr, settings, error) {
        // Only show if no manual error handler silenced it? 
        // jQuery doesn't easily expose "handled" status, but we can check specifically 
        // for 500 errors or unhandled types if needed.
        // For now, focus on the 'success: false' requirement.
    });

})(jQuery);
```

### Shared Layout

#### [MODIFY] [_Layout.cshtml](file:///c:/Repos/CFM%20Frontend/Views/Shared/_Layout.cshtml)
Include the new handler script so it applies to all pages.

```html
    <!-- ... existing scripts ... -->
    <script src="~/assets/js/mvc-endpoints.js"></script>
    
    <!-- Toastr configuration (existing) -->
    
    <!-- [NEW] Global AJAX Handler -->
    <script src="~/assets/js/global-ajax-handler.js"></script>

    <!-- Global Session Expiration Handler -->
    <script src="~/assets/js/global-session-handler.js"></script>
    <!-- ... -->
```

## Verification Plan

### Automated Tests
*   None feasible for client-side AJAX interception in this environment without a browser test suite.

### Manual Verification
1.  **Setup**:
    *   Temporarily modify `HelpdeskController.GetFloorsByLocation` (or use the user's current modified state) to force an error:
        ```csharp
        // In GetFloorsByLocation
        return Json(new { success = false, message = "Simulated API Error" });
        ```
2.  **Execution**:
    *   Run the application (`dotnet run`).
    *   Login and navigate to **Helpdesk > Add Work Request**.
    *   Select a Location from the dropdown to trigger `GetFloorsByLocation`.
3.  **Expected Result**:
    *   A red `toastr` notification should appear with the text "Simulated API Error".
    *   The console logs should show "API Logic Error: Simulated API Error".
4.  **Regression Test**:
    *   Test a normal successful flow (undo the temporary error). Ensure no error toastr appears.
    *   Test a form validation error (if applicable) to ensure double notifications don't occur in a confusing way (though `SafeExecuteApiAsync` is mostly for data fetching, so conflict is minimal).
