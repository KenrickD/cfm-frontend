# Bug Fix Prompt Template

Use this template when asking Claude Code to diagnose and fix a bug.

## Prompt to Use

```
Bug in [PAGE_NAME]: [ONE-LINE SYMPTOM]

Steps to reproduce:
1. [Step 1]
2. [Step 2]
3. [Step 3]

Expected behavior: [EXPECTED]
Actual behavior: [ACTUAL]

Error message (if any): [ERROR]
Browser console errors (if any): [CONSOLE]
Network response (if any): [RESPONSE]
```

## Example Prompt

```
Bug in Work Category page: Delete returns "Failed to delete" error

Steps to reproduce:
1. Go to Settings > Work Category
2. Click delete icon on any category
3. Confirm deletion in modal

Expected behavior: Category is deleted, page refreshes
Actual behavior: Error toast shows "Failed to delete category"

Error message: None in console
Network response: { success: false, message: "Client ID is required" }
```

## Files Claude Should Check

**For frontend/UI bugs:**
- View: `Views/Helpdesk/{Page}.cshtml`
- JavaScript: `wwwroot/assets/js/pages/{page}.js`
- MVC Endpoints: `wwwroot/assets/js/mvc-endpoints.js`

**For backend/API bugs:**
- Controller: `Controllers/HelpdeskController.cs`
- API Endpoints: `Constants/ApiEndpoints.cs`
- SafeExecuteApiAsync usage in controller

**For authentication bugs:**
- `Handlers/AuthTokenHandler.cs`
- `Middleware/TokenExpirationMiddleware.cs`
- `Controllers/LoginController.cs`

## Bug Diagnosis Workflow

1. **REPRODUCE**: Follow exact steps to see the bug
2. **IDENTIFY LAYER**:
   - 401 error → Authentication (AuthTokenHandler)
   - success:false → Check response.message, trace API call
   - JS error → Page JavaScript file
   - Wrong data displayed → Controller/ViewModel logic
3. **TRACE**: Controller → API endpoint → Backend response
4. **FIX**: Make minimal change to fix root cause
5. **VERIFY**: Test original bug scenario + related functionality
