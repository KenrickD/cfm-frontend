# Implementation Plan - Module Related Asset

## Goal Description
Implement "Related Asset" feature in the Work Request Add page (`WorkRequestAdd.cshtml`). This feature allows users to search and attach assets to a work request using two modes: Individual Asset and Asset Group.

## Proposed Changes

### Backend (C#)

#### [NEW] [DTOs/Asset/RelatedAssetFormDetailResponse.cs](file:///c:/Repos/CFM%20Frontend/DTOs/Asset/RelatedAssetFormDetailResponse.cs)
Create a new DTO for individual asset search results.
```csharp
namespace cfm_frontend.DTOs.Asset
{
    public class RelatedAssetFormDetailResponse
    {
        public int IdAsset { get; set; }
        public string Label { get; set; }
        public string Name { get; set; }
        public string OtherCode { get; set; }
    }
}
```

#### [NEW] [DTOs/Asset/RelatedAssetGroupFormDetailResponse.cs](file:///c:/Repos/CFM%20Frontend/DTOs/Asset/RelatedAssetGroupFormDetailResponse.cs)
Create a new DTO for asset group search results.
```csharp
namespace cfm_frontend.DTOs.Asset
{
    public class RelatedAssetGroupFormDetailResponse
    {
        public string AssetGroupName { get; set; }
        public List<RelatedAssetFormDetailResponse> Asset { get; set; } = new();
    }
}
```

#### [MODIFY] [Constants/ApiEndpoints.cs](file:///c:/Repos/CFM%20Frontend/Constants/ApiEndpoints.cs)
Add a new region for Asset endpoints.

```csharp
// ... existing code ...
        #region Asset

        /// <summary>
        /// Asset management endpoints
        /// </summary>
        public static class Asset
        {
            private const string Base = ApiBase + "/asset";

            /// <summary>
            /// GET: Search asset by property and prefix
            /// Path params: {idProperty}
            /// Query params: term (prefix), idClient
            /// </summary>
            public static string Search(int idProperty) => $"{Base}/{idProperty}";

            /// <summary>
            /// GET: Search asset group by property and prefix
            /// Path params: {idProperty}
            /// Query params: term (prefix), idClient
            /// </summary>
            public static string SearchGroup(int idProperty) => $"{Base}/asset-group/{idProperty}";
        }

        #endregion
// ... existing code ...
```

#### [MODIFY] [Controllers/Helpdesk/HelpdeskController.cs](file:///c:/Repos/CFM%20Frontend/Controllers/Helpdesk/HelpdeskController.cs)
Add proxy actions for Asset search.

```csharp
// In API Endpoints for Dynamic Data Loading region

        /// <summary>
        /// API: Search assets by property and term
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchAsset(int propertyId, string term)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            // Get client ID from session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson)) return Json(new { success = false, message = "Session expired" });
            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            var url = $"{backendUrl}{ApiEndpoints.Asset.Search(propertyId)}?idClient={userInfo.PreferredClientId}&term={Uri.EscapeDataString(term)}";

            var (success, data, message) = await SafeExecuteApiAsync<List<RelatedAssetFormDetailResponse>>(
                () => client.GetAsync(url),
                "Failed to search assets");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Search asset groups by property and term
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchAssetGroup(int propertyId, string term)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson)) return Json(new { success = false, message = "Session expired" });
            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var url = $"{backendUrl}{ApiEndpoints.Asset.SearchGroup(propertyId)}?idClient={userInfo.PreferredClientId}&term={Uri.EscapeDataString(term)}";

            var (success, data, message) = await SafeExecuteApiAsync<List<RelatedAssetGroupFormDetailResponse>>(
                () => client.GetAsync(url),
                "Failed to search asset groups");

            return Json(new { success, data, message });
        }
```

### Frontend (Razor/JS)

#### [MODIFY] [wwwroot/assets/js/mvc-endpoints.js](file:///c:/Repos/CFM%20Frontend/wwwroot/assets/js/mvc-endpoints.js)
Register the new MVC endpoints.

```javascript
// Inside Helpdesk.Search object
            Search: {
                // ... existing endpoints
                Asset: '/Helpdesk/SearchAsset',
                AssetGroup: '/Helpdesk/SearchAssetGroup',
                // ...
            },
```

#### [MODIFY] [Views/Helpdesk/WorkRequest/WorkRequestAdd.cshtml](file:///c:/Repos/CFM%20Frontend/Views/Helpdesk/WorkRequest/WorkRequestAdd.cshtml)
1.  **Add "Related Assets" Section**: Add a card for Related Assets, likely below "Important Checklist" and above "Labor/Material".
    *   It should contain a Table with columns: Name, Other Code, Label, Action.
    *   It should have an "Add Asset" button that opens the modal.
2.  **Add Modal**: Add a Bootstrap Modal at the bottom of the file (outside the form/cards).
    *   **Modal Header**: "Add Related Asset".
    *   **Modal Body**:
        *   **Tabs (Nav Pills)**: "Individual Asset" and "Asset Group".
        *   **Content Area**:
            *   **Individual Tab**:
                *   Search input (`#assetSearchInput`).
                *   Results container (dropdown or list).
            *   **Group Tab**:
                *   Search input (`#assetGroupSearchInput`).
                *   Results container.
    *   **Modal Footer**: Close button.

#### [MODIFY] [wwwroot/assets/js/pages/workrequest/work-request-add.js](file:///c:/Repos/CFM%20Frontend/wwwroot/assets/js/pages/workrequest/work-request-add.js)
1.  **Initialize Module**: Call `initializeAssetModule()` in `init()`.
2.  **State Management**: Add `assets: []` to `state` object.
3.  **Implement `initializeAssetModule()`**:
    *   Event listener for "Add Asset" button to open modal.
    *   Event listener for Tab switching (clear search inputs/results).
    *   **Individual Search**:
        *   Debounced keyup on search input.
        *   Call `MvcEndpoints.Helpdesk.Search.Asset`.
        *   Render suggestions.
        *   On click: Add asset to table, update state, clear search.
    *   **Group Search**:
        *   Debounced keyup on search input.
        *   Call `MvcEndpoints.Helpdesk.Search.AssetGroup`.
        *   Render group suggestions.
        *   On click: Iterate through group assets and add each to table (check existing to avoid duplicates), update state.
    *   **Table Management**:
        *   Render table rows based on `state.assets`.
        *   Handle "Remove" button click.
    *   **Submit Preparation**:
        *   In `initializeFormSubmission`, serialize `state.assets` to `AssetsJson` hidden input (or append to form data).
        *   Format: `[{ "idAsset": 123 }, ...]` matching `AssetDto`.

## Verification Plan

### Manual Verification
1.  **Setup**:
    *   Run the application (`dotnet run`).
    *   Navigate to Work Request Add page.
    *   Select a Location (required for asset search).
2.  **Test Individual Search**:
    *   Click "Add Asset".
    *   Ensure "Individual" tab is active.
    *   Type a known asset name.
    *   Verify API call to `/Helpdesk/SearchAsset`.
    *   Select an asset.
    *   Verify it appears in the table.
3.  **Test Group Search**:
    *   Click "Add Asset".
    *   Switch to "Asset Group" tab.
    *   Type a known group name.
    *   Verify API call to `/Helpdesk/SearchAssetGroup`.
    *   Select a group.
    *   Verify all assets in the group appear in the table.
4.  **Test Save**:
    *   Fill all required fields.
    *   Save the Work Request.
    *   Verify success message.
    *   (Optional) Check database or detailed view to confirm assets were linked.
