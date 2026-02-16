# New Settings Page Prompt Template

Use this template when asking Claude Code to create a new settings page with inline editing.

## Prompt to Use

```
Create a new settings page for [FEATURE_NAME] with inline editing.

Backend API endpoints:
- List: [GET /api/...]
- Create: [POST /api/...]
- Update: [PUT /api/...]
- Delete: [DELETE /api/...]

Fields to display: [field1, field2, ...]

Reference implementation: Work Category (see CLAUDE.md Implementation Recipes)
```

## Example Prompt

```
Create a new settings page for "Material Type" with inline editing.

Backend API endpoints:
- List: GET /api/v1/material-type/list
- Create: POST /api/v1/material-type
- Update: PUT /api/v1/material-type
- Delete: DELETE /api/v1/material-type

Fields to display: typeName, description

Reference implementation: Work Category
```

## Files Claude Should Create/Modify

1. [ ] `ViewModels/MaterialTypeViewModel.cs` - ViewModel
2. [ ] `Controllers/HelpdeskController.cs` - Add actions
3. [ ] `Constants/ApiEndpoints.cs` - Add endpoint constants
4. [ ] `wwwroot/assets/js/mvc-endpoints.js` - Add JS endpoints
5. [ ] `Views/Helpdesk/Settings/MaterialType.cshtml` - View
6. [ ] `wwwroot/assets/js/pages/settings/material-type.js` - JavaScript

## Checklist After Implementation

- [ ] `dotnet build` succeeds
- [ ] Page loads without JS errors
- [ ] Create new item works
- [ ] Edit item works (inline)
- [ ] Delete item works (with confirmation modal)
- [ ] Search/filter works
- [ ] Pagination works
- [ ] Multi-tab safety: IdClient captured in PageContext
- [ ] Privilege check added to controller action
