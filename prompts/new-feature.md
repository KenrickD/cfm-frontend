# New Feature Prompt Template

Use this template when asking Claude Code to implement a new feature.

## Prompt to Use

```
Add [FEATURE] to the [MODULE] module.

User story: As a [user type], I want [action] so that [benefit].

Similar to: [EXISTING_FEATURE] (if any)
Backend API available: [YES/NO]
API endpoints: [LIST IF KNOWN]

Requirements:
- [Requirement 1]
- [Requirement 2]
- [Requirement 3]
```

## Example Prompt

```
Add "Export to Excel" button to the Work Request list page.

User story: As an admin, I want to export work requests to Excel so that I can analyze data offline.

Similar to: None (new feature)
Backend API available: Yes
API endpoints: GET /api/v1/work-request/export?cid={clientId}&filters={filters}

Requirements:
- Button appears in page header
- Respects current filters
- Downloads .xlsx file
- Shows loading indicator during export
- Shows error toast if export fails
```

## Pre-Implementation Checklist

Before starting, confirm:
- [ ] Identified which module this belongs to (e.g., Helpdesk)
- [ ] Confirmed privilege name needed (or if new privilege required)
- [ ] Identified backend API endpoints available
- [ ] Found similar existing feature to reference
- [ ] Listed all files that will need changes

## Questions to Consider

1. **Privileges**: Does this need view/add/edit/delete permission checks?
2. **Multi-client**: Does this feature involve client-specific data?
3. **UI location**: Where does this feature appear (page, modal, sidebar)?
4. **Error handling**: What happens if the API fails?
5. **Loading states**: Does the UI need loading indicators?

## Files to Potentially Modify

- `ViewModels/` - If new data needed in view
- `Controllers/HelpdeskController.cs` - New controller actions
- `Constants/ApiEndpoints.cs` - New API endpoint constants
- `wwwroot/assets/js/mvc-endpoints.js` - New JS endpoints
- `Views/Helpdesk/` - View modifications
- `wwwroot/assets/js/pages/` - JavaScript changes
