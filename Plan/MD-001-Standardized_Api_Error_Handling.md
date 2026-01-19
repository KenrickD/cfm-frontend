# MD-001-Standardized_Api_Error_Handling

## Context
The current codebase handles API responses individually in each controller action. This leads to code duplication, inconsistent error logging, and potential for missing error checks.
Common pattern observed:
```csharp
var response = await client.PostAsync(...);
if (response.IsSuccessStatusCode) {
    var result = JsonSerializer.Deserialize...
    if (result.success) { ... }
    else { ModelState.AddModelError... }
} else {
    // Log error, add model error
}
```

## Goal
Standardize API response handling to:
1.  Reduce code duplication.
2.  Ensure consistent logging of all API failures (non-success status codes and `success: false` responses).
3.  Simplify Controller actions.

## Proposed Solution
Implement a protected generic helper method in `BaseController` that handles the plumbing of calling APIs and processing `ApiResponseDto<T>`.

### 1. Define `ServiceResult<T>` (Optional, or reuse `ApiResponseDto`)
Since we already have `ApiResponseDto<T>`, we can reuse it or return a tuple/struct that is easier to consume in the controller.

### 2. Add Helper Method to `BaseController`

Safe, robust helper to handle GET/POST execution and deserialization.

```csharp
// In BaseController.cs

protected async Task<(bool Success, T? Data, string Message)> SafeExecuteApiAsync<T>(
    Func<Task<HttpResponseMessage>> apiCall, 
    string errorMessage = "API call failed")
{
    try 
    {
        var response = await apiCall();
        
        if (response.IsSuccessStatusCode)
        {
            using var responseStream = await response.Content.ReadAsStreamAsync();
            var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<T>>(
                responseStream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse != null && apiResponse.Success)
            {
                return (true, apiResponse.Data, apiResponse.Message);
            }
            
            // Handle logical error from API (Success = false)
            var msg = !string.IsNullOrEmpty(apiResponse?.Message) ? apiResponse.Message : errorMessage;
            _logger.LogWarning("API Logic Error: {Message}", msg);
             // Optionally add to ModelState here if passed in, or return failure
            return (false, default, msg);
        }
        else 
        {
            // Handle HTTP error
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("API HTTP Error {StatusCode}: {Content}", response.StatusCode, errorContent);
            return (false, default, errorMessage);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "API Exception during: {ErrorMessage}", errorMessage);
        return (false, default, errorMessage);
    }
}
```

### 3. Usage Example
Refactor `HelpdeskController.Index`:

```csharp
// Old
// Huge try-catch block with repeated client creation etc.

// New
public async Task<IActionResult> Index(...) 
{
    // ... setup requestBody ...
    
    var client = _httpClientFactory.CreateClient("BackendAPI");
    // ...
    
    // Execute
    var (success, workRequests, msg) = await SafeExecuteApiAsync<List<WorkRequestResponseModel>>(
        async () => await client.PostAsJsonAsync($"{backendUrl}{ApiEndpoints.WorkRequest.List}", requestBody),
        "Error loading work requests");
        
    if (success) {
        viewModel.WorkRequest = workRequests;
    } else {
        showNotification(msg, "error"); // via TempData or similar
    }
    
    return View(viewModel);
}
```

## Refactoring Plan
1.  Modify `BaseController.cs` to include `SafeExecuteApiAsync`.
2.  Refactor `HelpdeskController.cs` methods to use this new helper.
3.  Verify that logging works as expected.
