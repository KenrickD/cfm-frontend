# Implementation Plan: Labor/Material Job Code Search

Implement Job Code search and Ad Hoc material addition in the Work Request Add modal.

## User Review Required

> [!IMPORTANT]
> The API endpoint for Job Code is `/api/v{version}/jobcode`. I will assume `v1` for now.
> I will use the existing `ApiResponseDto` structure for the response.

## Proposed Changes

### 1. Update `work-request-add.js`

#### [MODIFY] [work-request-add.js](file:///c:/Repos/CFM%20Frontend/wwwroot/assets/js/pages/workrequest/work-request-add.js)

*   **Update `CONFIG`**:
    *   Add `jobCodes: '/api/v1/jobcode'` (or via `MvcEndpoints`).
    *   Add `measurementUnits` and `currencies` endpoints if not present.
*   **Update `state`**:
    *   Add `selectedJobCode`: Store selected job code object.
    *   Add `laborMaterialMode`: 'jobCode' or 'adHoc'.
*   **Enhance `initializeLaborMaterial`**:
    *   Add event listeners for Radio Buttons (`#modeJobCode`, `#modeAdHoc`) to toggle visibility of `#jobCodeMode` and `#adHocMode`.
    *   Call `initializeJobCodeSearch`.
    *   Call `initializeAdHocFields`.
*   **Implement `initializeJobCodeSearch`**:
    *   Use `debounce`.
    *   Call API on keyup in `#jobCodeSearch`.
    *   Render dropdown with `Name`.
    *   On Select:
        *   Store `selectedJobCode`.
        *   Render Card (`#jobCodeSelected`) with Name and Stock (Colorized).
        *   Hide Search.
        *   Populate `LaborMaterialMeasurementUnit` as label.
*   **Implement `initializeAdHocFields`**:
    *   **Loading Strategy**: Fetch these enums via AJAX when the page initializes (in `init()`). This ensures they are ready by the time the user clicks "Add" (no spinner delay) without blocking the initial page server-render.
    *   Load Currencies (Category: "currency").
    *   Load Measurement Units (Category: "measurementUnit").
    *   Cache the results in `state` to avoid re-fetching.
*   **Update `addLaborMaterialRow` / Save Logic**:
    *   **State Management**: Maintain a single list in UI for display, but ensure each item has a `type` ('jobCode' or 'adHoc').
    *   **Form Submission (`initializeFormSubmission`)**:
        *   Filter the items into two separate arrays: `jobCodeItems` and `adHocItems`.
        *   Serialize `jobCodeItems` into input `JobCodeItemsJson`.
        *   Serialize `adHocItems` into input `AdHocItemsJson`.
        *   **Crucial**: This ensures that on the backend, we can bind these to two different properties/classes as requested.

### 2. View Changes (Already implemented, will verify/tweak)

#### [MODIFY] [WorkRequestAdd.cshtml](file:///c:/Repos/CFM%20Frontend/Views/Helpdesk/WorkRequest/WorkRequestAdd.cshtml)

*   Verify Modal IDs match JS selectors.
*   Ensure `typeahead-dropdown` structure is correct for Job Code.

## Verification Plan

### Manual Verification
1.  **Job Code Search**:
    *   Open "Labor/Material" modal.
    *   Ensure "Search from Job Code" is selected.
    *   Type a known job code prefix.
    *   Verify dropdown appears.
    *   Select item.
    *   Verify Card appears with correct Stock color (Green > Min, Red < Min).
    *   Verify "Delete" button resets view.
2.  **Ad Hoc Mode**:
    *   Select "Add new Ad hoc".
    *   Verify form fields appear (Name, Price, etc.).
    *   Verify Currency and Unit dropdowns populate (requires Enum API).
3.  **Adding to Table**:
    *   Add a Job Code item.
    *   Add an Ad Hoc item.
    *   Verify both appear in the table.
    *   Verify Cost Estimation updates.
