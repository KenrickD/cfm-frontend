# Pre-Implementation Checklist

Run through this checklist before implementing any feature.

## Before Any Feature

### Information Gathering
- [ ] Identified module (Helpdesk, Dashboard, Admin, etc.)
- [ ] Confirmed privilege name to protect this feature
- [ ] Found reference implementation (similar existing feature)
- [ ] Listed backend API endpoints needed
- [ ] Listed all files to create/modify

### Technical Requirements
- [ ] Need new DTO? (Check BackendModels/ for structure)
- [ ] Need new ViewModel? (List all data needed by view)
- [ ] Need new API endpoints in constants?
- [ ] Need new MVC endpoints for JS?

## For Pages with Forms

### Multi-Tab Session Safety (MANDATORY)
- [ ] Added `IdClient` property to ViewModel
- [ ] Set `viewmodel.IdClient = userInfo.PreferredClientId` in controller
- [ ] Added `window.PageContext = { idClient: @Model.IdClient }` in View
- [ ] Using `clientContext.idClient` in JavaScript AJAX payloads
- [ ] Form submission validates client context on backend

### AJAX Operations
- [ ] CSRF token included in headers
- [ ] Using SafeExecuteApiAsync in controller
- [ ] Success/error notifications using showNotification()
- [ ] Loading indicators during async operations

## For New Controller Actions

- [ ] Added privilege check: `this.CheckViewAccess("Module", "Page")`
- [ ] Using SafeExecuteApiAsync for API calls
- [ ] Returning structured response: `Json(new { success, data, message })`
- [ ] Added to appropriate controller (HelpdeskController for Helpdesk features)

## After Implementation

### Build & Test
- [ ] `dotnet build` succeeds with no errors
- [ ] Page loads without browser console errors
- [ ] All CRUD operations work correctly
- [ ] Pagination works (if applicable)
- [ ] Search/filter works (if applicable)

### Security & Quality
- [ ] Privilege checks are in place
- [ ] No hardcoded client IDs (use clientContext)
- [ ] Error messages are user-friendly
- [ ] No sensitive data exposed in JS console

## Quick Reference: Common Patterns

**Get User Info:**
```csharp
var userSessionJson = HttpContext.Session.GetString("UserSession");
var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
```

**Safe API Call:**
```csharp
var (success, data, message) = await SafeExecuteApiAsync<T>(
    () => client.GetAsync(url),
    "Error message for user");
```

**PageContext Setup:**
```html
<script>
    window.PageContext = { idClient: @(Model?.IdClient ?? 0) };
</script>
```

**AJAX with CSRF:**
```javascript
$.ajax({
    url: endpoint,
    method: 'POST',
    headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
    data: JSON.stringify({ ...data, idClient: clientContext.idClient })
});
```
