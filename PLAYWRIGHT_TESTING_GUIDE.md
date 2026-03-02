# Playwright Testing Guide for CFM Frontend

Comprehensive guide for writing end-to-end tests for the CFM Frontend application using Playwright and NUnit.

## Table of Contents

1. [Overview](#overview)
2. [Test Project Architecture](#test-project-architecture)
3. [Getting Started](#getting-started)
4. [Authentication Management](#authentication-management)
5. [Page Object Model Pattern](#page-object-model-pattern)
6. [Writing Tests](#writing-tests)
7. [Common Test Patterns](#common-test-patterns)
8. [Locator Strategies](#locator-strategies)
9. [Test Organization](#test-organization)
10. [Assertions & Expectations](#assertions--expectations)
11. [Debugging Tests](#debugging-tests)
12. [Integration with Frontend](#integration-with-frontend)
13. [Integration with Backend](#integration-with-backend)
14. [Best Practices](#best-practices)
15. [Troubleshooting](#troubleshooting)
16. [Recipes](#recipes)

---

## Overview

### Testing Stack

**Framework:** Playwright + NUnit + .NET 8.0
**Project:** `CfmFrontend.Tests` (separate test project)
**Test Type:** End-to-end (E2E) browser automation tests
**Browser:** Chromium (headless by default)

### What We Test

- Settings CRUD pages (Work Category, Person in Charge, Priority Level, etc.)
- Form validation and error handling
- Modal dialogs and confirmations
- Search and filter functionality
- Toast notifications
- Page navigation and routing
- Authentication flows

### Key Features

✅ **Saved authentication state** - Login once, reuse across all tests
✅ **Page Object Model** - Maintainable, reusable page interactions
✅ **Base test class** - Shared setup and helper methods
✅ **Headless execution** - Fast CI/CD integration
✅ **Screenshot on failure** - Automatic debugging artifacts

---

## Test Project Architecture

### Project Structure

```
c:\Repos\CfmFrontend.Tests\
├── Auth/
│   └── AuthState.cs                    # Authentication state management
├── PageObjects/
│   └── Settings/
│       ├── WorkCategoryPage.cs         # Page object for Work Category
│       └── PersonInChargePage.cs       # Page object for Person in Charge
├── Tests/
│   └── Settings/
│       ├── WorkCategoryTests.cs        # Test class for Work Category
│       └── PersonInChargeTests.cs      # Test class for Person in Charge
├── PlaywrightSetup.cs                  # Base class for all tests
├── .runsettings                        # Playwright configuration
├── CfmFrontend.Tests.csproj            # Project file
└── README.md                           # Quick reference
```

### Dependencies

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="Microsoft.Playwright" Version="1.58.0" />
<PackageReference Include="Microsoft.Playwright.NUnit" Version="1.58.0" />
<PackageReference Include="NUnit" Version="3.14.0" />
<PackageReference Include="NUnit.Analyzers" Version="3.9.0" />
<PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
```

### Key Components

| Component | Purpose | File |
|-----------|---------|------|
| `PlaywrightSetup` | Base test class with auth & helpers | `PlaywrightSetup.cs` |
| `AuthState` | Login once, save cookies | `Auth/AuthState.cs` |
| Page Objects | UI interaction abstraction | `PageObjects/*Page.cs` |
| Test Classes | Test scenarios using AAA pattern | `Tests/*Tests.cs` |

---

## Getting Started

### Prerequisites

1. **.NET 8.0 SDK** installed
2. **CFM Frontend** running on `http://localhost:5099`
3. **Valid test credentials** configured in `Auth/AuthState.cs`

### Initial Setup

```bash
# Navigate to test project
cd "c:\Repos\CfmFrontend.Tests"

# Install Playwright browsers (first time only)
pwsh bin\Debug\net8.0\playwright.ps1 install

# Run all tests
dotnet test

# Run tests in a specific class
dotnet test --filter "WorkCategoryTests"

# Run a specific test
dotnet test --filter "WorkCategory_CreateCategory_AppearsInList"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Configuration

**Test Credentials** (`Auth/AuthState.cs`):
```csharp
private const string TestUsername = "your.username";
private const string TestPassword = "YourPassword";
private const string BaseUrl = "http://localhost:5099";
```

**Browser Settings** (`.runsettings`):
```xml
<Playwright>
  <BrowserName>chromium</BrowserName>
  <LaunchOptions>
    <Headless>true</Headless>
    <SlowMo>0</SlowMo>
  </LaunchOptions>
  <ExpectTimeout>10000</ExpectTimeout>
  <Timeout>30000</Timeout>
</Playwright>
```

---

## Authentication Management

### How It Works

1. **First test run:** Logs in, saves cookies to `bin/Debug/net8.0/.auth/state.json`
2. **Subsequent runs:** Reuses saved authentication state
3. **No login needed:** All tests start already authenticated

### Auth State Lifecycle

```
Test Suite Start
      ↓
Check for state.json
      ↓
┌─────┴──────┐
│ Exists?    │
└─────┬──────┘
      ↓
  ┌───┴───┐
  │  Yes  │ → Use saved state → Run tests
  │  No   │ → Login & save → Run tests
  └───────┘
```

### When Auth Expires

**Symptoms:**
- Page title shows "Sign In | CFM System" instead of expected page
- Tests fail with "Expected: String containing X, But was: Sign In"
- Console warning: `[WRN] Both access and refresh tokens expired`

**Solution:**
```bash
# Windows (PowerShell/CMD)
del bin\Debug\net8.0\.auth\state.json

# Git Bash / Unix
rm -f bin/Debug/net8.0/.auth/state.json

# Then re-run tests - auth will be recreated
dotnet test
```

### Manual Auth State Management

```csharp
// Clear auth state (e.g., for testing login flow)
AuthState.ClearState();

// Check if auth state exists
var hasState = AuthState.HasSavedState();

// Force re-authentication
AuthState.ClearState();
await AuthState.EnsureAuthenticatedAsync(browser);
```

---

## Page Object Model Pattern

### What is POM?

**Page Object Model (POM)** separates page UI structure from test logic:
- **Page Objects** define locators and interactions
- **Tests** use Page Objects without knowing UI details
- **Benefits:** Maintainability, reusability, readability

### POM Structure

```csharp
public class WorkCategoryPage
{
    private readonly IPage _page;

    // LOCATORS - UI element references
    private ILocator AddButton => _page.Locator("#showAddFormBtn");
    private ILocator CategoryRows => _page.Locator(".category-row[data-id]");

    public WorkCategoryPage(IPage page)
    {
        _page = page;
    }

    // NAVIGATION
    public async Task NavigateAsync()
    {
        await _page.GotoAsync("/Helpdesk/WorkCategory");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // ACTIONS
    public async Task CreateCategoryAsync(string name)
    {
        await ShowAddFormAsync();
        await NewCategoryInput.FillAsync(name);
        await SaveNewButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // QUERIES
    public async Task<bool> CategoryExistsAsync(string name)
    {
        var category = _page.Locator($".category-description:text-is('{name}')");
        return await category.CountAsync() > 0;
    }
}
```

### Best Practices

✅ **One Page Object per page** (e.g., `WorkCategoryPage`, `PersonInChargePage`)
✅ **Locators as properties** - Lazy evaluation, not cached
✅ **Return types:** Actions return `Task`, queries return `Task<T>`
✅ **Wait for states** - Always wait for `NetworkIdle` or element states
✅ **Meaningful method names** - `CreateCategoryAsync()`, not `ClickSaveButton()`
❌ **No assertions in Page Objects** - Only in test classes

---

## Writing Tests

### Test Class Template

```csharp
using CfmFrontend.Tests.PageObjects.Settings;

namespace CfmFrontend.Tests.Tests.Settings;

/// <summary>
/// End-to-end tests for [Page Name].
/// Tests CRUD operations and UI behavior.
/// </summary>
[TestFixture]
public class MyPageTests : PlaywrightSetup
{
    private MyPage _myPage = null!;

    [SetUp]
    public void SetUp()
    {
        _myPage = new MyPage(Page);
    }

    [Test]
    public async Task MyPage_PageLoads_Successfully()
    {
        // Arrange (if needed)

        // Act
        await _myPage.NavigateAsync();

        // Assert
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Expected Title"));
    }
}
```

### AAA Pattern (Arrange-Act-Assert)

All tests follow the **AAA pattern**:

```csharp
[Test]
public async Task WorkCategory_CreateCategory_AppearsInList()
{
    // Arrange - Set up test data
    var testName = $"Test Category {DateTime.Now:yyyyMMdd_HHmmss}";
    await _workCategoryPage.NavigateAsync();
    var initialCount = await _workCategoryPage.GetCategoryCountAsync();

    // Act - Perform the action being tested
    await _workCategoryPage.CreateCategoryAsync(testName);

    // Assert - Verify the outcome
    var exists = await _workCategoryPage.CategoryExistsAsync(testName);
    Assert.That(exists, Is.True, $"Category '{testName}' should exist after creation");

    var newCount = await _workCategoryPage.GetCategoryCountAsync();
    Assert.That(newCount, Is.EqualTo(initialCount + 1), "Category count should increase by 1");
}
```

### Test Naming Convention

**Pattern:** `[PageName]_[Scenario]_[ExpectedResult]`

**Examples:**
- `WorkCategory_PageLoads_Successfully`
- `WorkCategory_CreateCategory_AppearsInList`
- `WorkCategory_SubmitEmptyName_ShowsValidationError`
- `PersonInCharge_DeletePic_RemovesFromList`
- `WorkCategory_CancelDelete_KeepsCategory`

### Using Regions

Organize tests into logical regions:

```csharp
[TestFixture]
public class WorkCategoryTests : PlaywrightSetup
{
    #region Page Load Tests

    [Test]
    public async Task WorkCategory_PageLoads_Successfully() { }

    #endregion

    #region Add Form Tests

    [Test]
    public async Task WorkCategory_ClickAddButton_ShowsAddForm() { }

    #endregion

    #region CRUD Tests

    [Test]
    public async Task WorkCategory_CreateCategory_AppearsInList() { }

    [Test]
    public async Task WorkCategory_EditCategory_UpdatesName() { }

    [Test]
    public async Task WorkCategory_DeleteCategory_RemovesFromList() { }

    #endregion

    #region Delete Modal Tests

    [Test]
    public async Task WorkCategory_ClickDeleteButton_ShowsConfirmationModal() { }

    #endregion
}
```

---

## Common Test Patterns

### 1. Page Load Test

```csharp
[Test]
public async Task PageName_PageLoads_Successfully()
{
    // Act
    await _page.NavigateAsync();

    // Assert
    var title = await Page.TitleAsync();
    Assert.That(title, Does.Contain("Expected Page Title"));
}
```

### 2. Create/Add Test

```csharp
[Test]
public async Task PageName_CreateItem_AppearsInList()
{
    // Arrange
    var testName = $"Test Item {DateTime.Now:yyyyMMdd_HHmmss}";
    await _page.NavigateAsync();
    var initialCount = await _page.GetItemCountAsync();

    // Act
    await _page.CreateItemAsync(testName);

    // Assert
    var exists = await _page.ItemExistsAsync(testName);
    Assert.That(exists, Is.True, $"Item '{testName}' should exist");

    var newCount = await _page.GetItemCountAsync();
    Assert.That(newCount, Is.EqualTo(initialCount + 1));
}
```

### 3. Edit/Update Test

```csharp
[Test]
public async Task PageName_EditItem_UpdatesName()
{
    // Arrange - Create item to edit
    var originalName = $"Original {DateTime.Now:yyyyMMdd_HHmmss}";
    var newName = $"Updated {DateTime.Now:yyyyMMdd_HHmmss}";

    await _page.NavigateAsync();
    await _page.CreateItemAsync(originalName);

    var itemId = await _page.GetItemIdByNameAsync(originalName);
    Assert.That(itemId, Is.Not.Null);

    // Act
    await _page.EditItemAsync(itemId!.Value, newName);

    // Assert
    var oldExists = await _page.ItemExistsAsync(originalName);
    var newExists = await _page.ItemExistsAsync(newName);

    Assert.That(oldExists, Is.False, "Old name should no longer exist");
    Assert.That(newExists, Is.True, "New name should exist");
}
```

### 4. Delete Test

```csharp
[Test]
public async Task PageName_DeleteItem_RemovesFromList()
{
    // Arrange - Create item to delete
    var testName = $"Delete Test {DateTime.Now:yyyyMMdd_HHmmss}";

    await _page.NavigateAsync();
    await _page.CreateItemAsync(testName);

    var itemId = await _page.GetItemIdByNameAsync(testName);
    Assert.That(itemId, Is.Not.Null);

    var initialCount = await _page.GetItemCountAsync();

    // Act
    await _page.DeleteItemAsync(itemId!.Value);

    // Assert
    var exists = await _page.ItemExistsAsync(testName);
    Assert.That(exists, Is.False, "Item should no longer exist");

    var newCount = await _page.GetItemCountAsync();
    Assert.That(newCount, Is.EqualTo(initialCount - 1));
}
```

### 5. Validation Test

```csharp
[Test]
public async Task PageName_SubmitEmptyName_ShowsValidationError()
{
    // Arrange
    await _page.NavigateAsync();

    // Act
    await _page.TrySaveEmptyItemAsync();

    // Assert
    var hasError = await _page.HasValidationErrorAsync();
    Assert.That(hasError, Is.True, "Validation error should be shown");
}
```

### 6. Modal Confirmation Test

```csharp
[Test]
public async Task PageName_ClickDeleteButton_ShowsConfirmationModal()
{
    // Arrange
    await _page.NavigateAsync();
    var itemId = await _page.GetFirstItemIdAsync();
    Assert.That(itemId, Is.Not.Null);

    // Act
    await _page.OpenDeleteModalAsync(itemId!.Value);

    // Assert
    var isVisible = await _page.IsDeleteModalVisibleAsync();
    Assert.That(isVisible, Is.True, "Delete modal should be visible");

    // Cleanup
    await _page.CloseDeleteModalAsync();
}

[Test]
public async Task PageName_CancelDelete_KeepsItem()
{
    // Arrange
    await _page.NavigateAsync();
    var itemId = await _page.GetFirstItemIdAsync();
    var itemName = await _page.GetFirstItemNameAsync();

    // Act
    await _page.OpenDeleteModalAsync(itemId!.Value);
    await _page.CloseDeleteModalAsync();

    // Assert
    var exists = await _page.ItemExistsAsync(itemName!);
    Assert.That(exists, Is.True, "Item should still exist after cancel");
}
```

### 7. Search/Filter Test

```csharp
[Test]
public async Task PageName_SearchWithKeyword_FiltersResults()
{
    // Arrange
    var uniquePrefix = $"Search{DateTime.Now:HHmmss}";
    var testName = $"{uniquePrefix} Test Item";

    await _page.NavigateAsync();
    await _page.CreateItemAsync(testName);

    // Act
    await _page.SearchAsync(uniquePrefix);

    // Assert
    var names = await _page.GetAllItemNamesAsync();
    Assert.That(names, Has.All.Contain(uniquePrefix),
        "All results should contain search keyword");

    // Cleanup
    await _page.NavigateAsync(); // Clear search
    var itemId = await _page.GetItemIdByNameAsync(testName);
    if (itemId.HasValue)
    {
        await _page.DeleteItemAsync(itemId.Value);
    }
}
```

### 8. Toast Notification Test

```csharp
[Test]
public async Task PageName_SuccessfulAction_ShowsToastNotification()
{
    // Arrange
    await _page.NavigateAsync();

    // Act
    await _page.PerformActionAsync();

    // Assert
    var toastMessage = await _page.GetToastMessageAsync();
    Assert.That(toastMessage, Does.Contain("Success").Or.Contain("saved"),
        "Success toast should appear");
}
```

### 9. Empty State Test

```csharp
[Test]
public async Task PageName_NoItems_ShowsEmptyState()
{
    // Arrange
    await _page.NavigateAsync();

    // Delete all items if any
    while (await _page.GetItemCountAsync() > 0)
    {
        var firstId = await _page.GetFirstItemIdAsync();
        if (firstId.HasValue)
        {
            await _page.DeleteItemAsync(firstId.Value);
        }
    }

    // Act & Assert
    var isEmpty = await _page.IsEmptyStateShownAsync();
    Assert.That(isEmpty, Is.True, "Empty state should be shown");
}
```

### 10. Conditional Test (Skip if No Data)

```csharp
[Test]
public async Task PageName_DeleteItem_RemovesFromList()
{
    // Arrange
    await _page.NavigateAsync();
    var count = await _page.GetItemCountAsync();

    // Skip test if no data exists
    if (count == 0)
    {
        Assert.Ignore("No items available to test deletion");
        return;
    }

    var firstItemId = await _page.GetFirstItemIdAsync();

    // Act & Assert...
}
```

---

## Locator Strategies

### Understanding Frontend Markup

Tests rely on **CSS selectors** from the frontend Views. Always check the View to find the correct selectors.

**Example View Structure** (`WorkCategory.cshtml`):
```html
<button id="showAddFormBtn" class="btn btn-primary">+ Add New</button>

<div id="addNewForm" style="display: none;">
    <input type="text" id="newCategoryName" class="form-control" />
    <button id="saveNewBtn" class="btn btn-success">Save</button>
    <button id="cancelNewBtn" class="btn btn-secondary">Cancel</button>
</div>

<div class="category-row" data-id="123">
    <span class="category-description">Cleaning Services</span>
    <button class="btn btn-sm btn-edit"><i class="ti-edit"></i></button>
    <button class="btn btn-sm btn-delete"><i class="ti-trash"></i></button>
</div>
```

### Locator Types

| Locator Type | Syntax | Use Case |
|--------------|--------|----------|
| By ID | `#elementId` | Most stable - prefer when available |
| By Class | `.className` | For collections or common elements |
| By Data Attribute | `[data-id="123"]` | For dynamic items (rows, cards) |
| By Text | `:text-is("Exact Text")` | Find by visible text |
| By Icon | `:has(i.ti-edit)` | Find button containing icon |
| Combination | `.row[data-id="5"]` | Combine for specificity |

### Locator Examples

```csharp
// By ID (preferred - stable)
private ILocator AddButton => _page.Locator("#showAddFormBtn");
private ILocator SearchInput => _page.Locator("#searchInput");

// By class (for collections)
private ILocator CategoryRows => _page.Locator(".category-row[data-id]");
private ILocator ToastMessage => _page.Locator(".toast-message");

// By data attribute (for specific items)
var row = _page.Locator($".category-row[data-id='{id}']");

// By text content
var category = _page.Locator($".category-description:text-is('{name}')");

// By nested elements (button with specific icon)
await row.Locator("button:has(i.ti-edit)").ClickAsync();
await row.Locator("button:has(i.ti-trash)").ClickAsync();

// Combination selectors
private ILocator EmptyState => _page.Locator("#emptyState.alert");
```

### Waiting Strategies

Always wait for elements to be in the correct state:

```csharp
// Wait for page load
await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

// Wait for element to be visible
await AddForm.WaitForAsync(new LocatorWaitForOptions
{
    State = WaitForSelectorState.Visible
});

// Wait for element to be hidden
await DeleteModal.WaitForAsync(new LocatorWaitForOptions
{
    State = WaitForSelectorState.Hidden
});

// Wait for selector to exist
await _page.WaitForSelectorAsync(".category-row, #emptyState", new PageWaitForSelectorOptions
{
    Timeout = 10000
});

// Wait for API response (AJAX calls)
var responseTask = _page.WaitForResponseAsync(
    response => response.Url.Contains("CreateWorkCategory"),
    new PageWaitForResponseOptions { Timeout = 15000 }
);
await SaveButton.ClickAsync();
var response = await responseTask;
```

### Finding Dynamic Elements

```csharp
// Get all rows and iterate
var rows = await CategoryRows.AllAsync();
foreach (var row in rows)
{
    var description = await row.Locator(".category-description").TextContentAsync();
    var id = await row.GetAttributeAsync("data-id");

    if (description?.Trim() == searchName)
    {
        return int.Parse(id!);
    }
}

// Count elements
var count = await CategoryRows.CountAsync();

// Check if element exists
var exists = await _page.Locator($".item:text-is('{name}')").CountAsync() > 0;
```

---

## Test Organization

### File Naming

| Type | Location | Naming | Example |
|------|----------|--------|---------|
| Test Class | `Tests/{Area}/` | `{Page}Tests.cs` | `WorkCategoryTests.cs` |
| Page Object | `PageObjects/{Area}/` | `{Page}Page.cs` | `WorkCategoryPage.cs` |

### Test Class Organization

```csharp
[TestFixture]
public class WorkCategoryTests : PlaywrightSetup
{
    // 1. Fields
    private WorkCategoryPage _workCategoryPage = null!;

    // 2. Setup
    [SetUp]
    public void SetUp()
    {
        _workCategoryPage = new WorkCategoryPage(Page);
    }

    // 3. Tests organized by regions
    #region Page Load Tests
    // ...
    #endregion

    #region CRUD Tests
    // ...
    #endregion

    #region Validation Tests
    // ...
    #endregion

    // 4. Teardown (if needed)
    [TearDown]
    public void TearDown()
    {
        // Cleanup if needed
    }
}
```

### Suggested Regions

- `#region Page Load Tests` - Page loads successfully, shows correct elements
- `#region Add Form Tests` - Form show/hide, validation
- `#region CRUD Tests` - Create, edit, delete operations
- `#region Delete Modal Tests` - Modal behavior, confirmation, cancellation
- `#region Search Tests` - Search and filter functionality
- `#region Validation Tests` - Input validation, error messages
- `#region Navigation Tests` - Routing, breadcrumbs, links

---

## Assertions & Expectations

### NUnit Assertion Patterns

```csharp
// String assertions
Assert.That(title, Does.Contain("Work Category"));
Assert.That(name, Is.EqualTo("Expected Name"));
Assert.That(message, Does.StartWith("Success"));

// Boolean assertions
Assert.That(exists, Is.True, "Item should exist");
Assert.That(isVisible, Is.False, "Form should be hidden");

// Numeric assertions
Assert.That(count, Is.EqualTo(5));
Assert.That(newCount, Is.GreaterThan(initialCount));
Assert.That(count, Is.LessThanOrEqualTo(10));

// Null/Not Null
Assert.That(itemId, Is.Not.Null, "Should find item ID");
Assert.That(result, Is.Null, "Should return null for missing item");

// Collection assertions
Assert.That(names, Has.Count.EqualTo(3));
Assert.That(names, Has.All.Contain("Test"));
Assert.That(names, Does.Contain("Specific Name"));
Assert.That(list, Is.Empty);

// Conditional assertions
Assert.That(count > 0 || isEmpty, Is.True, "Should show items or empty state");

// Multiple assertions (all must pass)
Assert.Multiple(() =>
{
    Assert.That(oldExists, Is.False, "Old name should be gone");
    Assert.That(newExists, Is.True, "New name should exist");
    Assert.That(count, Is.EqualTo(1), "Count should remain the same");
});

// Custom messages
Assert.That(exists, Is.True, $"Category '{testName}' should exist after creation");
```

### Playwright Expect API

```csharp
// Wait for element to be visible
await Expect(_page.Locator("#addNewForm")).ToBeVisibleAsync();

// Wait for element to have text
await Expect(_page.Locator(".toast-message")).ToContainTextAsync("Success");

// Wait for element to be hidden
await Expect(_page.Locator("#deleteModal")).ToBeHiddenAsync();

// Wait for count
await Expect(_page.Locator(".category-row")).ToHaveCountAsync(5);
```

---

## Debugging Tests

### Console Logging

Add console output for debugging:

```csharp
public async Task CreateCategoryAsync(string name)
{
    Console.WriteLine($"[WorkCategoryPage] Creating category: {name}");

    await ShowAddFormAsync();
    await NewCategoryInput.FillAsync(name);
    await SaveNewButton.ClickAsync();

    Console.WriteLine($"[WorkCategoryPage] Current URL: {_page.Url}");

    var names = await GetAllCategoryNamesAsync();
    Console.WriteLine($"[WorkCategoryPage] Found {names.Count} categories");
}
```

### Screenshots

**Automatic on failure:**
Playwright automatically saves screenshots to:
```
bin/Debug/net8.0/playwright-results/{TestName}-{timestamp}/screenshot.png
```

**Manual screenshot:**
```csharp
await _page.ScreenshotAsync(new PageScreenshotOptions
{
    Path = "debug-screenshot.png",
    FullPage = true
});
```

### Slow Motion Mode

For debugging, run tests in slow motion:

```xml
<!-- .runsettings -->
<SlowMo>500</SlowMo> <!-- 500ms delay between actions -->
```

### Headed Mode (Show Browser)

```xml
<!-- .runsettings -->
<Headless>false</Headless>
```

Or via command:
```bash
HEADED=1 dotnet test
```

### Trace Viewer

Record test execution trace:

```csharp
// In PlaywrightSetup.cs
public override BrowserNewContextOptions ContextOptions()
{
    return new BrowserNewContextOptions
    {
        RecordVideoDir = "videos/",
        RecordTracePath = "traces/"
    };
}
```

View trace:
```bash
pwsh bin\Debug\net8.0\playwright.ps1 show-trace traces/trace.zip
```

### Common Debugging Steps

1. **Add Console.WriteLine** to understand test flow
2. **Take screenshot** at failure point
3. **Run in headed mode** to see browser actions
4. **Use slow motion** to observe interactions
5. **Check page URL** after navigation
6. **Verify element counts** before assertions
7. **Inspect response bodies** for AJAX calls

---

## Integration with Frontend

### Understanding MVC Routing

Tests navigate using the same routes as users:

```csharp
// Route pattern: /{Controller}/{Action}
await _page.GotoAsync("/Helpdesk/WorkCategory");
await _page.GotoAsync("/Helpdesk/PersonInCharge");
await _page.GotoAsync("/Helpdesk/PriorityLevel");
```

**Frontend Controller:** `HelpdeskController.cs`
**Actions:** `WorkCategory()`, `PersonInCharge()`, `PriorityLevel()`

### Understanding View Selectors

Tests must match View markup exactly:

**View** (`Views/Helpdesk/Settings/WorkCategory.cshtml`):
```html
<button id="showAddFormBtn">Add New</button>
<div id="addNewForm" style="display: none;">
    <input id="newCategoryName" />
</div>
```

**Page Object:**
```csharp
private ILocator AddButton => _page.Locator("#showAddFormBtn");
private ILocator AddForm => _page.Locator("#addNewForm");
private ILocator NewCategoryInput => _page.Locator("#newCategoryName");
```

### Understanding JavaScript Behaviors

#### Page Reload Pattern

Many CRUD operations trigger `window.location.reload()`:

```javascript
// Frontend JavaScript (work-category.js)
if (response.success) {
    showNotification('Category saved', 'success');
    window.location.reload();
}
```

**Test must wait for reload:**
```csharp
await SaveNewButton.ClickAsync();
await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
```

#### Toast Notifications

Frontend uses `toastr` library:

```javascript
// Frontend
showNotification('Saved successfully', 'success');
```

**Test waits for toast:**
```csharp
public async Task<string?> GetToastMessageAsync(int timeoutMs = 5000)
{
    try
    {
        var toast = _page.Locator(".toast-message");
        await toast.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = timeoutMs
        });
        return await toast.TextContentAsync();
    }
    catch (TimeoutException)
    {
        return null;
    }
}
```

#### AJAX Requests

Frontend makes AJAX calls without page reload:

```javascript
// Frontend
$.ajax({
    url: '/Helpdesk/CreateWorkCategory',
    method: 'POST',
    data: JSON.stringify(payload),
    success: function(response) {
        if (response.success) {
            window.location.reload();
        }
    }
});
```

**Test waits for response:**
```csharp
var responseTask = _page.WaitForResponseAsync(
    response => response.Url.Contains("CreateWorkCategory"),
    new PageWaitForResponseOptions { Timeout = 15000 }
);

await SaveNewButton.ClickAsync();

var response = await responseTask;
var body = await response.TextAsync();
Console.WriteLine($"Response: {body}");
```

### Understanding Components

#### SearchableDropdown

Frontend uses custom searchable dropdown component:

```html
<select data-searchable="true" id="categorySelect">
    <option value="1">Category 1</option>
</select>
```

**Test interaction:**
```csharp
// Wait for dropdown to initialize
await _page.WaitForSelectorAsync("#categorySelect");

// Click to open
await _page.Locator("#categorySelect").ClickAsync();

// Select option
await _page.Locator("option:text-is('Category 1')").ClickAsync();
```

---

## Integration with Backend

### Understanding API Response Format

All backend responses use `ApiResponseDto<T>`:

```json
{
  "success": true,
  "message": "success",
  "timestamp": "2026-02-26T10:30:45.123Z",
  "data": { /* actual data */ },
  "errors": []
}
```

### Testing CRUD Operations

#### Create Flow

```
User clicks "Save"
      ↓
Frontend AJAX POST → /Helpdesk/CreateWorkCategory
      ↓
Controller validates → Backend API POST → /api/v1.0/masters/types/workCategory
      ↓
Backend returns ApiResponseDto<TypeFormDetailResponse>
      ↓
Controller returns JSON { success: true/false, message: "..." }
      ↓
Frontend JavaScript checks response.success
      ↓
If success: showNotification() + window.location.reload()
      ↓
Test waits for page reload
      ↓
Test verifies item exists
```

**Test Pattern:**
```csharp
// Wait for AJAX response
var responseTask = _page.WaitForResponseAsync(
    response => response.Url.Contains("CreateWorkCategory")
);

await SaveButton.ClickAsync();

var response = await responseTask;
var status = response.Status; // Should be 200
var body = await response.TextAsync(); // { success: true, ... }

// Wait for page reload (triggered by JavaScript on success)
await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

// Verify item was created
var exists = await _page.ItemExistsAsync(testName);
Assert.That(exists, Is.True);
```

#### Update Flow

```
User edits inline → Clicks save
      ↓
AJAX PUT → /Helpdesk/UpdateWorkCategory
      ↓
Backend API PUT → /api/v1.0/masters/types/workCategory/{id}
      ↓
Response → window.location.reload()
      ↓
Test verifies updated name exists
```

#### Delete Flow

```
User clicks delete → Modal appears → Confirms
      ↓
AJAX DELETE → /Helpdesk/DeleteWorkCategory/{id}
      ↓
Backend API soft delete (sets IsActiveData = false)
      ↓
Response → window.location.reload()
      ↓
Test verifies item no longer visible
```

### Client Context Validation

Backend validates that user has access to the specified client:

```csharp
// Backend validation
await SharedFunctions.ValidateClientAccessAsync(_dbContext, idWebUser, dto.Client_idClient);
```

**Tests should use valid client:** Tests run as authenticated user with access to specific clients. Backend will reject requests for clients the test user doesn't have access to.

### Understanding Master Data (Type vs Enum)

**Type** - Client-specific master data:
- Work Categories
- Other Categories
- Priority Levels (via Type table)

**Enum** - System-wide master data:
- Work Request Status (New, In Progress, Completed)
- Request Method (Email, Phone, Walk-in)
- Feedback Type

**Tests primarily interact with Type-based data** (client-specific settings).

---

## Best Practices

### 1. Test Isolation

Each test should be independent:

✅ **Good - Creates own test data:**
```csharp
[Test]
public async Task CreateCategory_Test()
{
    var testName = $"Test {DateTime.Now:yyyyMMdd_HHmmss}"; // Unique name
    await _page.CreateCategoryAsync(testName);
    // ... assertions
}
```

❌ **Bad - Depends on existing data:**
```csharp
[Test]
public async Task EditCategory_Test()
{
    // Assumes "Cleaning Services" exists - fragile!
    await _page.EditCategoryAsync("Cleaning Services", "New Name");
}
```

### 2. Test Cleanup

Always clean up test data:

```csharp
[Test]
public async Task CreateCategory_Test()
{
    var testName = $"Test {DateTime.Now:yyyyMMdd_HHmmss}";

    // Create
    await _page.CreateCategoryAsync(testName);

    // Assert creation
    var exists = await _page.CategoryExistsAsync(testName);
    Assert.That(exists, Is.True);

    // Cleanup - delete test data
    var id = await _page.GetCategoryIdByNameAsync(testName);
    if (id.HasValue)
    {
        await _page.DeleteCategoryAsync(id.Value);
    }
}
```

### 3. Unique Test Data

Use timestamps to ensure uniqueness:

```csharp
var testName = $"Test Category {DateTime.Now:yyyyMMdd_HHmmss}";
var uniquePrefix = $"Search{DateTime.Now:HHmmss}";
```

### 4. Meaningful Assertions

Always provide assertion messages:

✅ **Good:**
```csharp
Assert.That(exists, Is.True, $"Category '{testName}' should exist after creation");
```

❌ **Bad:**
```csharp
Assert.That(exists, Is.True);
```

### 5. Wait Strategies

Always wait for the correct state:

```csharp
// After navigation
await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

// After AJAX
var response = await _page.WaitForResponseAsync(/* ... */);
await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

// For modals
await modal.WaitForAsync(new LocatorWaitForOptions
{
    State = WaitForSelectorState.Visible
});
```

### 6. Page Object Reusability

Extract common patterns to page object methods:

```csharp
// Instead of repeating this in tests:
await _page.Locator("#searchInput").FillAsync(keyword);
await _page.Locator("#searchBtn").ClickAsync();
await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

// Create a reusable method:
public async Task SearchAsync(string keyword)
{
    await SearchInput.FillAsync(keyword);
    await SearchButton.ClickAsync();
    await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
}
```

### 7. Avoid Hard-Coded Waits

❌ **Bad:**
```csharp
await _page.WaitForTimeoutAsync(5000); // Brittle!
```

✅ **Good:**
```csharp
await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
```

### 8. Test One Thing Per Test

Each test should verify one behavior:

✅ **Good - Separate tests:**
```csharp
[Test]
public async Task CreateCategory_AppearsInList() { }

[Test]
public async Task CreateCategory_ShowsSuccessToast() { }
```

❌ **Bad - Testing multiple things:**
```csharp
[Test]
public async Task CreateCategory_WorksCorrectly()
{
    // Create
    // Check it appears
    // Check toast
    // Check count
    // Edit it
    // Delete it
    // Way too much!
}
```

### 9. Use Regions for Organization

Group related tests:

```csharp
#region Page Load Tests
// All page load tests here
#endregion

#region CRUD Tests
// All CRUD tests here
#endregion
```

### 10. Handle Optional Data Gracefully

```csharp
[Test]
public async Task DeleteItem_Test()
{
    var count = await _page.GetItemCountAsync();

    if (count == 0)
    {
        Assert.Ignore("No items available to test deletion");
        return;
    }

    // Proceed with test...
}
```

---

## Troubleshooting

### Common Issues

| Issue | Symptom | Solution |
|-------|---------|----------|
| **Auth Expired** | Page title shows "Sign In" | Delete `bin/Debug/net8.0/.auth/state.json` |
| **App Not Running** | `net::ERR_CONNECTION_REFUSED` | Start CFM Frontend: `dotnet run` |
| **Element Not Found** | `TimeoutException` waiting for selector | Check View for correct selector |
| **Test Timeout** | Test hangs after action | Add `await _page.WaitForLoadStateAsync()` |
| **Flaky Test** | Sometimes passes, sometimes fails | Add proper waits, avoid hard-coded delays |
| **Wrong Data** | Test sees old data | Ensure `WaitForLoadStateAsync(NetworkIdle)` |
| **Modal Not Closing** | Modal stays visible | Wait for `Hidden` state, not just click close |

### Debugging Checklist

1. ✅ **Frontend running?** Check `http://localhost:5099`
2. ✅ **Auth valid?** Delete state.json if expired
3. ✅ **Selector correct?** Inspect View for actual HTML
4. ✅ **Waited for page load?** Add `WaitForLoadStateAsync(NetworkIdle)`
5. ✅ **AJAX completed?** Wait for response
6. ✅ **Element visible?** Check display:none, visibility
7. ✅ **Unique test data?** Use timestamp in names
8. ✅ **Console output?** Check test logs for errors

### Auth Troubleshooting

**Symptom: Tests redirect to login**
```
Expected: String containing "Work Category"
But was:  "Sign In | CFM System"
```

**Solution:**
```bash
rm -f bin/Debug/net8.0/.auth/state.json
dotnet test
```

**Symptom: Login fails during auth state creation**
```
[AuthState] Login failed: TimeoutException
```

**Check:**
1. Credentials correct in `Auth/AuthState.cs`?
2. Frontend running on `http://localhost:5099`?
3. Backend API responding?

### Element Not Found

**Symptom:**
```
TimeoutException: Timeout 30000ms exceeded
```

**Debug Steps:**
1. Take screenshot to see current page state
2. Check console output for page URL
3. Verify selector matches View HTML
4. Check if element is hidden (display:none)
5. Ensure page fully loaded before looking for element

**Example Fix:**
```csharp
// Before
await _page.Locator("#someElement").ClickAsync(); // Fails!

// After
await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
await _page.Locator("#someElement").ClickAsync(); // Works!
```

### Flaky Tests

**Causes:**
- Not waiting for page load
- Not waiting for AJAX responses
- Using hard-coded delays
- Race conditions

**Solutions:**
```csharp
// Bad - flaky
await _page.WaitForTimeoutAsync(1000);
await _page.LocatorAsync("#element").ClickAsync();

// Good - reliable
await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
await _page.Locator("#element").WaitForAsync(new LocatorWaitForOptions
{
    State = WaitForSelectorState.Visible
});
await _page.Locator("#element").ClickAsync();
```

---

## Recipes

### Recipe: Testing a New CRUD Settings Page

This recipe shows how to create complete E2E tests for a new settings page (e.g., Priority Level, Job Code, etc.).

#### Step 1: Create Page Object

**File:** `PageObjects/Settings/PriorityLevelPage.cs`

```csharp
using Microsoft.Playwright;

namespace CfmFrontend.Tests.PageObjects.Settings;

public class PriorityLevelPage
{
    private readonly IPage _page;

    // LOCATORS - Inspect the View to find these selectors
    private ILocator AddButton => _page.Locator("#showAddFormBtn");
    private ILocator AddForm => _page.Locator("#addNewForm");
    private ILocator NewLevelInput => _page.Locator("#newLevelName");
    private ILocator SaveNewButton => _page.Locator("#saveNewBtn");
    private ILocator CancelNewButton => _page.Locator("#cancelNewBtn");
    private ILocator LevelRows => _page.Locator(".level-row[data-id]");
    private ILocator DeleteModal => _page.Locator("#deleteConfirmModal");
    private ILocator ConfirmDeleteButton => _page.Locator("#confirmDeleteBtn");
    private ILocator SearchInput => _page.Locator("#searchInput");
    private ILocator SearchButton => _page.Locator("#searchBtn");
    private ILocator EmptyState => _page.Locator("#emptyState");

    public PriorityLevelPage(IPage page)
    {
        _page = page;
    }

    // NAVIGATION
    public async Task NavigateAsync()
    {
        await _page.GotoAsync("/Helpdesk/PriorityLevel");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    // QUERIES
    public async Task<int> GetLevelCountAsync()
    {
        return await LevelRows.CountAsync();
    }

    public async Task<bool> IsEmptyStateShownAsync()
    {
        return await EmptyState.IsVisibleAsync();
    }

    public async Task<bool> LevelExistsAsync(string name)
    {
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var level = _page.Locator($".level-description:text-is('{name}')");
        return await level.CountAsync() > 0;
    }

    public async Task<int?> GetLevelIdByNameAsync(string name)
    {
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var rows = await LevelRows.AllAsync();

        foreach (var row in rows)
        {
            var description = await row.Locator(".level-description").TextContentAsync();
            if (description?.Trim() == name)
            {
                var id = await row.GetAttributeAsync("data-id");
                return int.TryParse(id, out var result) ? result : null;
            }
        }
        return null;
    }

    public async Task<int?> GetFirstLevelIdAsync()
    {
        var firstRow = LevelRows.First;
        if (await firstRow.CountAsync() == 0)
            return null;

        var id = await firstRow.GetAttributeAsync("data-id");
        return int.TryParse(id, out var result) ? result : null;
    }

    // ACTIONS
    public async Task ShowAddFormAsync()
    {
        await AddButton.ClickAsync();
        await AddForm.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
    }

    public async Task HideAddFormAsync()
    {
        await CancelNewButton.ClickAsync();
        await AddForm.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });
    }

    public async Task CreateLevelAsync(string name)
    {
        await ShowAddFormAsync();
        await NewLevelInput.FillAsync(name);

        // Wait for AJAX response
        var responseTask = _page.WaitForResponseAsync(
            response => response.Url.Contains("CreatePriorityLevel"),
            new PageWaitForResponseOptions { Timeout = 15000 }
        );

        await SaveNewButton.ClickAsync();
        await responseTask;

        // Wait for page reload
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task EditLevelAsync(int id, string newName)
    {
        var row = _page.Locator($".level-row[data-id='{id}']");
        await row.Locator("button:has(i.ti-edit)").ClickAsync();

        var editInput = _page.Locator($"#edit-{id}");
        await editInput.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        await editInput.FillAsync(newName);
        await row.Locator(".btn-success").ClickAsync();

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task DeleteLevelAsync(int id)
    {
        var row = _page.Locator($".level-row[data-id='{id}']");
        await row.Locator("button:has(i.ti-trash)").ClickAsync();

        await DeleteModal.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        await ConfirmDeleteButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task SearchAsync(string keyword)
    {
        await SearchInput.FillAsync(keyword);
        await SearchButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

#### Step 2: Create Test Class

**File:** `Tests/Settings/PriorityLevelTests.cs`

```csharp
using CfmFrontend.Tests.PageObjects.Settings;

namespace CfmFrontend.Tests.Tests.Settings;

[TestFixture]
public class PriorityLevelTests : PlaywrightSetup
{
    private PriorityLevelPage _priorityLevelPage = null!;

    [SetUp]
    public void SetUp()
    {
        _priorityLevelPage = new PriorityLevelPage(Page);
    }

    #region Page Load Tests

    [Test]
    public async Task PriorityLevel_PageLoads_Successfully()
    {
        await _priorityLevelPage.NavigateAsync();

        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Priority Level"));
    }

    [Test]
    public async Task PriorityLevel_PageLoads_ShowsLevelList()
    {
        await _priorityLevelPage.NavigateAsync();

        var count = await _priorityLevelPage.GetLevelCountAsync();
        var isEmpty = await _priorityLevelPage.IsEmptyStateShownAsync();

        Assert.That(count > 0 || isEmpty, Is.True,
            "Page should show either priority levels or empty state");
    }

    #endregion

    #region CRUD Tests

    [Test]
    public async Task PriorityLevel_CreateLevel_AppearsInList()
    {
        var testName = $"Test Level {DateTime.Now:yyyyMMdd_HHmmss}";
        await _priorityLevelPage.NavigateAsync();
        var initialCount = await _priorityLevelPage.GetLevelCountAsync();

        await _priorityLevelPage.CreateLevelAsync(testName);

        var exists = await _priorityLevelPage.LevelExistsAsync(testName);
        Assert.That(exists, Is.True, $"Level '{testName}' should exist");

        var newCount = await _priorityLevelPage.GetLevelCountAsync();
        Assert.That(newCount, Is.EqualTo(initialCount + 1));

        // Cleanup
        var id = await _priorityLevelPage.GetLevelIdByNameAsync(testName);
        if (id.HasValue)
        {
            await _priorityLevelPage.DeleteLevelAsync(id.Value);
        }
    }

    [Test]
    public async Task PriorityLevel_EditLevel_UpdatesName()
    {
        var originalName = $"Edit Test {DateTime.Now:yyyyMMdd_HHmmss}";
        var newName = $"Updated {DateTime.Now:yyyyMMdd_HHmmss}";

        await _priorityLevelPage.NavigateAsync();
        await _priorityLevelPage.CreateLevelAsync(originalName);

        var levelId = await _priorityLevelPage.GetLevelIdByNameAsync(originalName);
        Assert.That(levelId, Is.Not.Null);

        await _priorityLevelPage.EditLevelAsync(levelId!.Value, newName);

        var oldExists = await _priorityLevelPage.LevelExistsAsync(originalName);
        var newExists = await _priorityLevelPage.LevelExistsAsync(newName);

        Assert.That(oldExists, Is.False, "Old name should be gone");
        Assert.That(newExists, Is.True, "New name should exist");

        // Cleanup
        var id = await _priorityLevelPage.GetLevelIdByNameAsync(newName);
        if (id.HasValue)
        {
            await _priorityLevelPage.DeleteLevelAsync(id.Value);
        }
    }

    [Test]
    public async Task PriorityLevel_DeleteLevel_RemovesFromList()
    {
        var testName = $"Delete Test {DateTime.Now:yyyyMMdd_HHmmss}";

        await _priorityLevelPage.NavigateAsync();
        await _priorityLevelPage.CreateLevelAsync(testName);

        var levelId = await _priorityLevelPage.GetLevelIdByNameAsync(testName);
        Assert.That(levelId, Is.Not.Null);

        var initialCount = await _priorityLevelPage.GetLevelCountAsync();

        await _priorityLevelPage.DeleteLevelAsync(levelId!.Value);

        var exists = await _priorityLevelPage.LevelExistsAsync(testName);
        Assert.That(exists, Is.False, "Level should be deleted");

        var newCount = await _priorityLevelPage.GetLevelCountAsync();
        Assert.That(newCount, Is.EqualTo(initialCount - 1));
    }

    #endregion
}
```

#### Step 3: Run Tests

```bash
cd "c:\Repos\CfmFrontend.Tests"

# Run all Priority Level tests
dotnet test --filter "PriorityLevelTests"

# Run specific test
dotnet test --filter "PriorityLevel_CreateLevel_AppearsInList"

# Run with verbose output
dotnet test --filter "PriorityLevelTests" --logger "console;verbosity=detailed"
```

#### Step 4: Iterate and Expand

Add more test scenarios:
- Validation tests (empty name, duplicate name)
- Modal confirmation tests
- Search/filter tests
- Pagination tests (if applicable)
- Permission tests (if applicable)

---

## Summary

This guide covers everything needed to write effective E2E tests for CFM Frontend:

✅ **Setup** - Install, configure, run tests
✅ **Architecture** - Page Object Model, base classes, organization
✅ **Patterns** - CRUD, validation, modals, search
✅ **Integration** - Frontend (MVC, JavaScript) + Backend (APIs, responses)
✅ **Best Practices** - Isolation, cleanup, waits, assertions
✅ **Debugging** - Logs, screenshots, traces
✅ **Recipes** - Step-by-step guide for new tests

### Quick Reference Card

| Task | Command |
|------|---------|
| Run all tests | `dotnet test` |
| Run one class | `dotnet test --filter "WorkCategoryTests"` |
| Run one test | `dotnet test --filter "WorkCategory_CreateCategory"` |
| Clear auth | `rm -f bin/Debug/net8.0/.auth/state.json` |
| Verbose output | `dotnet test --logger "console;verbosity=detailed"` |
| Start frontend | `cd "c:\Repos\CFM Frontend" && dotnet run` |

### File Templates

**Page Object:**
```csharp
public class MyPage
{
    private readonly IPage _page;
    private ILocator Element => _page.Locator("#selector");

    public MyPage(IPage page) => _page = page;

    public async Task NavigateAsync() =>
        await _page.GotoAsync("/Controller/Action");
}
```

**Test Class:**
```csharp
[TestFixture]
public class MyTests : PlaywrightSetup
{
    private MyPage _myPage = null!;

    [SetUp]
    public void SetUp() => _myPage = new MyPage(Page);

    [Test]
    public async Task Test_Name() { /* AAA pattern */ }
}
```

For more examples, see:
- `CfmFrontend.Tests/PageObjects/Settings/WorkCategoryPage.cs`
- `CfmFrontend.Tests/Tests/Settings/WorkCategoryTests.cs`
- `CfmFrontend.Tests/README.md`
