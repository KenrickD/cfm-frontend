# Theme Preference Implementation Guide

## Current Implementation (localStorage-based)

The dark mode theme preference is currently stored in the browser's localStorage and persists across page navigations within the same browser/device.

### How It Works

1. **User toggles theme** (sidebar switch or customizer buttons)
2. **Saved to localStorage** with key `'pc-theme'` and value `'light'` or `'dark'`
3. **On page load**, [_Layout.cshtml](Views/Shared/_Layout.cshtml) reads localStorage and applies theme immediately
4. **Logo and UI states** are updated via [pcoded.js](wwwroot/assets/js/pcoded.js) DOMContentLoaded handler

### Files Modified

- **Views/Shared/_Layout.cshtml** (lines 10-31, 39-45) - Theme loading and application
- **Views/Shared/VendorScripts.cshtml** - Removed OS dark mode auto-detection script
- **wwwroot/assets/js/pcoded.js**:
  - `layout_change()` (line 481) - Saves to localStorage
  - `layout_change_default()` (lines 334-338) - Simplified to just set light mode
  - DOMContentLoaded handler (lines 629-666) - Restores theme state (logos, buttons, checkbox)

---

## Future: Database-backed User Preferences

### Database Schema

Create a new table `UserPreferences` or add a field to existing `UserInfo`:

```sql
-- Option 1: Add to existing user table
ALTER TABLE Users ADD ThemePreference NVARCHAR(10) DEFAULT 'light';

-- Option 2: Create dedicated UserPreferences table
CREATE TABLE UserPreferences (
    UserId INT PRIMARY KEY,
    ThemePreference NVARCHAR(10) DEFAULT 'light' CHECK (ThemePreference IN ('light', 'dark')),
    -- Add other preferences here in the future
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
```

### Backend Implementation

#### 1. Update UserInfo Model

Add theme preference property to [Models/UserInfo.cs](Models/UserInfo.cs):

```csharp
public class UserInfo
{
    // ... existing properties ...

    public string? ThemePreference { get; set; } // "light" or "dark"
}
```

#### 2. Load Preference on Login

In [Controllers/LoginController.cs](Controllers/LoginController.cs), after loading UserInfo from API:

```csharp
// Existing code fetches userInfo from API
var userInfo = await GetUserInfoFromApi(authResponse.Token);

// NEW: Fetch theme preference from database or API
userInfo.ThemePreference = await GetUserThemePreferenceAsync(userInfo.UserId);
// OR if API already returns it:
// userInfo.ThemePreference is already populated

// Store in session as usual
HttpContext.Session.SetString("UserSession", JsonSerializer.Serialize(userInfo));
```

#### 3. Set ViewBag in BaseController

In [Controllers/BaseController.cs](Controllers/BaseController.cs), uncomment and implement lines 58-65:

```csharp
public override void OnActionExecuting(ActionExecutingContext context)
{
    // ... existing privilege refresh code ...

    // Load user theme preference from session
    var userSessionJson = HttpContext.Session.GetString("UserSession");
    if (!string.IsNullOrEmpty(userSessionJson))
    {
        var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (userInfo?.ThemePreference != null)
        {
            ViewBag.UserThemePreference = userInfo.ThemePreference; // "light" or "dark"
        }
    }

    base.OnActionExecuting(context);
}
```

#### 4. Create API Endpoint to Save Preference

Create a new endpoint to save theme preference when user changes it:

```csharp
// In a new AccountController or PreferencesController
[HttpPost]
public async Task<IActionResult> SaveThemePreference([FromBody] SaveThemeRequest request)
{
    var userInfo = JsonSerializer.Deserialize<UserInfo>(
        HttpContext.Session.GetString("UserSession"),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (userInfo == null)
        return Unauthorized();

    // Call backend API to save preference
    var client = _httpClientFactory.CreateClient("BackendAPI");
    var backendUrl = _configuration["BackendBaseUrl"];

    var payload = new { userId = userInfo.UserId, themePreference = request.Theme };
    var json = JsonSerializer.Serialize(payload,
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await client.PostAsync($"{backendUrl}/api/User/SaveThemePreference", content);

    if (response.IsSuccessStatusCode)
    {
        // Update session
        userInfo.ThemePreference = request.Theme;
        HttpContext.Session.SetString("UserSession", JsonSerializer.Serialize(userInfo));

        return Ok(new { success = true, message = "Theme preference saved" });
    }

    return BadRequest(new { success = false, message = "Failed to save preference" });
}

public class SaveThemeRequest
{
    public string Theme { get; set; } // "light" or "dark"
}
```

### Frontend Integration

Update [pcoded.js](wwwroot/assets/js/pcoded.js) `layout_change()` function to also save to server:

```javascript
function layout_change(layout) {
  var control = document.querySelector('.pct-offcanvas');
  document.getElementsByTagName('body')[0].setAttribute('data-pc-theme', layout);

  // Persist theme preference to localStorage (immediate)
  localStorage.setItem('pc-theme', layout);

  // FUTURE: Also save to server (async, fire-and-forget)
  // Uncomment when backend endpoint is ready:
  /*
  fetch('/Account/SaveThemePreference', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
    },
    body: JSON.stringify({ theme: layout })
  }).catch(err => console.error('Failed to save theme preference to server:', err));
  */

  // ... rest of existing code ...
}
```

### Priority/Fallback Logic

The [_Layout.cshtml](Views/Shared/_Layout.cshtml) script (lines 17-28) already implements the correct priority:

1. **localStorage** (highest priority) - User's current session choice
2. **ViewBag.UserThemePreference** (from database) - Only if no localStorage override
3. **Default: 'light'** - If neither exists

This ensures:
- User can temporarily override their saved preference in current session
- On fresh browser/device, user's saved DB preference loads automatically
- Graceful fallback to light mode for new users

---

## Testing the Future Implementation

### Test Scenarios

1. **New user, no preference saved**: Should default to light mode
2. **User with DB preference 'dark'**: Should load dark mode on login
3. **User changes theme in session**: Should update localStorage AND database
4. **User returns on new browser**: Should load their DB preference
5. **User with DB='dark' but localStorage='light'**: localStorage wins (session override)

### Migration Strategy

When ready to implement:

1. Run database migration to add ThemePreference field
2. Default all existing users to 'light' (current behavior)
3. Update UserInfo model
4. Update BaseController (uncomment lines 58-65)
5. Create SaveThemePreference endpoint
6. Update pcoded.js layout_change() to call API
7. Test thoroughly with different scenarios
8. Deploy

---

## Notes

- **Backward compatibility**: Current localStorage implementation continues to work even after DB implementation
- **Performance**: Server-side save is fire-and-forget, doesn't block UI
- **Privacy**: Theme preference is per-user, not shared across devices unless using DB backend
- **Extensibility**: Easy to add more user preferences (font size, sidebar collapsed state, etc.) in the future
