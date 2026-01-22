# Person In Charge (PIC) Dropdown Implementation Plan

## Goal
Implement the logic to load the "Person In Charge" (PIC) dropdown on the `WorkRequestAdd` page. This involves adding a new API endpoint in a dedicated region, securely handling client context via session, and implementing a cascading auto-selection logic for Location, Floor, and Room Zone dropdowns upon page load.

## Proposed Changes

### Backend - API Constants
#### [MODIFY] [ApiEndpoints.cs](file:///c:/Repos/CFM%20Frontend/Constants/ApiEndpoints.cs)
- Create a **NEW REGION** `Person In Charge` (separate from `Settings - Person In Charge`).
- Add a new class `PersonInCharge` with the `GetPIC` constant.
```csharp
#region Person In Charge
public static class PersonInCharge
{
    private const string Base = ApiBase + "/person-in-charge"; // Check actual controller route
    // OR if it's under Employee controller in backend but we want a separate helper class here:
    // public const string GetPIC = ApiBase + "/employee/pic"; or "/api/v1/pic"; - Verify exact backend route
    
    // Assuming the user wants: /api/v{version}/pic
    public const string GetPIC = ApiBase + "/pic"; 
}
#endregion
```

### Backend - Controller
#### [MODIFY] [HelpdeskController.cs](file:///c:/Repos/CFM%20Frontend/Controllers/Helpdesk/HelpdeskController.cs)
- Implement `GetPersonInCharge` action method.
    - **Route**: `GetPersonInCharge(int idProperty)`
    - **Logic**:
        1.  Retrieve `idClient` from `HttpContext.Session` (UserInfo).
        2.  Call backend API (`ApiEndpoints.PersonInCharge.GetPIC`) passing `idClient` (from session) and `idProperty` (from parameter).
        3.  Return JSON response.
- **Note**: Ensure strictly checking session existence and handling 401 if missing.

### Frontend - JavaScript
#### [MODIFY] [work-request-add.js](file:///c:/Repos/CFM%20Frontend/wwwroot/assets/js/pages/workrequest/work-request-add.js)
- **Update Configuration**: Add the new endpoint to `CONFIG.apiEndpoints`.
```javascript
apiEndpoints: {
    // ...
    personInChargeList: '/Helpdesk/GetPersonInCharge',
    // ...
}
```
- **Implement Cascade Auto-Select**:
    - Modify `loadLocations`, `loadFloors`, and `loadRooms` (or their success callbacks) to support an `autoSelect` parameter or logic.
    - **Logic**:
        1.  **On Init**: `autoSelectFirstLocation()` triggers change.
        2.  **Location Change**: `loadFloors` is called.
            - *Update*: In `loadFloors` success callback, if options exist, auto-select the first valid option.
            - Trigger `change` on Floor select.
        3.  **Floor Change**: `loadRooms` is called.
            - *Update*: In `loadRooms` success callback, if options exist, auto-select the first valid option.
            - Trigger `change` on Room select.
    - **Modify `initializeLocationCascade`** to wire purely event-based triggering but ensure the *first load* cascades. NOTE: We must be careful not to auto-select if the user is manually changing selections later, OR clarify if this auto-select is *always* desired when only one option exists, or just on first load. 
    - *Refinement*: The user said "since location dropdown is selected on load... make sure floor and room zone also does the same". This implies the cascade should happen initially.
    - **Strategy**:
        - In `loadFloors` success: Check if value is not set. If not set and options > 0, select first and trigger change.
        - In `loadRooms` success: Check if value is not set. If not set and options > 0, select first and trigger change.
        - This behavior is generally acceptable for a "quick fill" form.

- **Update PIC Loading**:
    - Ensure `loadPersonsInCharge` is called when Location changes (already planned).
    - ensure it uses the new endpoint `/Helpdesk/GetPersonInCharge` with `idProperty`.

## Verification Plan

### Manual Verification
1.  **Prerequisites**: Login and go to "Add New Work Request".
2.  **Observation 1 (Full Cascade)**:
    *   Verify **Location** auto-selects first option.
    *   Verify **Floor** loads and auto-selects first option.
    *   Verify **Room** loads and auto-selects first option.
    *   Verify **PIC** loads for the selected location (check Network tab for `GetPersonInCharge` call).
3.  **Network Check**:
    *   Verify `GetPersonInCharge` call uses `idProperty` from the auto-selected location.
    *   Verify logic uses server-side session for `idClient` (no `idClient` in request params).
4.  **Interaction**:
    *   Manually change Location. Verify Floor resets and auto-selects new first option (if desired) or just loads. *Decision*: We will implement auto-select on load. For manual changes, usually standard behavior is to reset to "Select...", but getting the first one is also fine if consistent.
