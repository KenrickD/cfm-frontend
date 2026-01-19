# MD-002-Helpdesk-Target-Date-Calculation

## 1. Objective
Implement dynamic target date calculation for Work Requests on the frontend. This replaces the server-side calculation (currently in `CalculateTarget.cs`) with client-side logic to provide immediate feedback to users as they change priority levels or request dates.

## 2. Context & Requirements
-   **Logic Source**: `CalculateTarget.cs` (Legacy C# WebForms logic).
-   **Core Algorithm**: `SharedFunctions.addDateBasedOnOfficeHour` (calculates target date/time given a start date, duration, and working hour constraints).
-   **Data Sources**: 
    -   Priority Level Details (contains rules and durations).
    -   Public Holidays (Client-specific).
    -   Office Hours (Client-specific).

## 3. Backend Changes

### 3.1. DTOs (Data Transfer Objects)
Create the following classes in `cfm_frontend.DTOs.Helpdesk`. These must match the backend API response structure.

**Class: `PriorityLevelFormDetailResponse`**
```csharp
public class PriorityLevelFormDetailResponse
{
    public int idPriorityLevel { get; set; }
    public string name { get; set; }
    // Helpdesk Response
    public Int64? helpdeskResponseTarget { get; set; } // Ticks
    public bool? helpdeskResponseTargetIsWithinOfficeHours { get; set; }
    public bool? helpdeskResponseTargetIsMandatory { get; set; }
    
    // Initial Follow Up
    public Int64? initialFollowUpTarget { get; set; }
    public int? initialFollowUpTargetCalculation_Enum_idEnum { get; set; } // 1="After Helpdesk Response Target", 2="After Request Date"
    public bool? initialFollowUpTargetIsWithinOfficeHours { get; set; }
    
    // Quotation Submission
    public Int64? quotationSubmissionTarget { get; set; }
    public int? quotationSubmissionTargetCalculation_Enum_idEnum { get; set; }
    public bool? quotationSubmissionTargetIsWithinOfficeHours { get; set; }
    
    // Cost Approval
    public Int64? costApprovalTarget { get; set; }
    public int? costApprovalTargetCalculation_Enum_idEnum { get; set; }
    public bool? costApprovalTargetIsWithinOfficeHours { get; set; }

    // Work Completion
    public Int64? workCompletionTarget { get; set; }
    public int? workCompletionTargetCalculation_Enum_idEnum { get; set; }
    public bool? workCompletionTargetIsWithinOfficeHours { get; set; }

    // After Work Follow Up
    public Int64? afterWorkFollowUpTarget { get; set; }
    public int? afterWorkFollowUpTargetCalculation_Enum_idEnum { get; set; }
    public bool? afterWorkFollowUpTargetIsWithinOfficeHours { get; set; }
    // Add other properties found in the API response as needed
}
```

**Class: `PublicHolidayResponse`**
```csharp
public class PublicHolidayResponse
{
    public int IdPublicHoliday { get; set; }
    public DateTime Date { get; set; } // Date only
    public string Name { get; set; }
}
```

**Class: `OfficeHourResponse`**
```csharp
public class OfficeHourResponse
{
    public int IdOfficeHour { get; set; }
    public int OfficeDay { get; set; } // 0=Sunday, 1=Monday...
    public bool IsWorkingHour { get; set; }
    public TimeOnly FromHour { get; set; }
    public TimeOnly ToHour { get; set; }
}
```

### 3.2. Controller (`HelpdeskController.cs`)
Implement the following proxy methods. Ensure they use `HttpClient` to call the backend API.

1.  **`GetPriorityLevelById(int id)`**
    -   Target: `GET /api/v{version}/priority-level?idClient={idClient}&id={id}`
    -   Return: `PriorityLevelFormDetailResponse`

2.  **`GetPublicHolidays(int year)`**
    -   Target: `GET /api/v{version}/masters/public-holidays/{year}?idClient={idClient}`
    -   Return: `List<PublicHolidayResponse>`

3.  **`GetOfficeHours()`**
    -   Target: `GET /api/v{version}/masters/office-hours?idClient={idClient}`
    -   Return: `List<OfficeHourResponse>`

## 4. Frontend Implementation

### 4.1. `business-date-calculator.js`
This file should encapsulate the core logic derived from `SharedFunctions.cs`.

**Class Structure:**
```javascript
class BusinessDateCalculator {
    constructor(officeHours, publicHolidays) {
        // Prepare data (sort office hours, map holidays)
    }

    /**
     * Ports SharedFunctions.addDateBasedOnOfficeHour
     * @param {Date} inputDate - Starting date
     * @param {number} durationTicks - Duration in Ticks (.NET Ticks, 100ns units) 
     *                                 OR convert Ticks to Minutes before calling.
     * @param {boolean} isWithinOfficeHours - Constraint flag
     */
    calculateTargetDate(inputDate, durationTicks, isWithinOfficeHours) {
        // Logic details in Section 6
    }
}
```

### 4.2. `work-request-add.js`
This file manages the UI events and orchestrates the calculation chain.

**Key Logic Flow:**
1.  **Initialization**:
    -   On page load, fetch `OfficeHours` and `PublicHolidays` (for current & next year).
    -   Store them in global/module scope.

2.  **Event Listeners**:
    -   `#ddlPriorityLevel` (Change):
        -   Fetch `PriorityLevelFormDetailResponse` from new endpoint.
        -   Cache the response.
        -   Trigger `recalculateTargets()`.
    -   `#requestDate`, `#requestTime` (Change):
        -   Trigger `recalculateTargets()`.

3.  **`recalculateTargets()` Logic**:
    -   Check if Priority Level and Request Date are valid.
    -   Instantiate `BusinessDateCalculator`.
    -   **Chain Execution** (mimicking `CalculateTarget.cs`):
        1.  **Helpdesk Response Target**: 
            -   Start: Request Date
            -   Rule: `priorityLevel.helpdeskResponseTarget`
            -   Output: Update UI `#btnHelpdeskResponseTarget`
        2.  **Initial Follow Up Target**:
            -   Start: Depend on `priorityLevel.initialFollowUpTargetCalculation_Enum` (e.g., "After Helpdesk Response" vs "After Request Date").
            -   Output: Update UI `#btnInitialFollowUpTarget`
        3.  **Quotation Submission Target**:
            -   Start: Depend on rule (e.g., "After Initial Follow Up").
            -   Output: Update UI `#btnQuotationSubmissionTarget`
        4.  **Cost Approval Target**:
            -   Start: Depend on rule.
            -   Output: Update UI `#btnCostApprovalTarget`
        5.  **Work Completion Target**:
            -   Start: Depend on rule.
            -   Output: Update UI `#btnWorkCompletionTarget`
        6.  **After Work Follow Up Target**:
            -   Start: Depend on rule (usually "After Work Completion").
            -   Output: Update UI `#btnAfterWorkFollowUpTarget`

## 5. UI Elements Mapping (Reference)
The legacy code uses these IDs. Ensure functionality maps to the corresponding Bootstrap/MVC elements in `WorkRequestAdd.cshtml`.

| Logic Target | Start Date Source | Legacy UI ID | Note |
| :--- | :--- | :--- | :--- |
| **Helpdesk Response** | Request Date | `btnHelpdeskResponseTarget` | |
| **Initial Follow Up** | Helpdesk Response Target OR Request Date | `btnInitialFollowUpTarget` | Check Enum logic |
| **Quotation Submission** | Initial Follow Up OR Request Date | `btnQuotationSubmissionTarget` | |
| **Cost Approval** | Quotation Submission OR Request Date | `btnCostApprovalTarget` | |
| **Work Completion** | Cost Approval OR Request Date | `btnWorkCompletionTarget` | |
| **After Work Follow Up** | Work Completion | `btnAfterWorkFollowUpTarget` | |

## 6. Reference Logic: `addDateBasedOnOfficeHour` (C#)

```csharp
public static DateTime addDateBasedOnOfficeHour(DateTime inputDate, TimeSpan duration, bool isWithinOfficeHours)
{
    DateTime resultDate = inputDate;
    TimeSpan remainingTimeSpan = duration;

    if (isWithinOfficeHours)
    {
        // ... (Logic for Office Hours) ...
        // Note: 1 office day = 8 office hours
        if (remainingTimeSpan.Days > 0)
             remainingTimeSpan = new TimeSpan(0, remainingTimeSpan.Days * 8 + remainingTimeSpan.Hours, remainingTimeSpan.Minutes, 1);

        // Loop while remainingTimeSpan > 0
        // Cases: 
        // 1. Is Public Holiday? -> Skip to next day 00:00
        // 2. Is Non-Working Hour? -> Skip to next working period
        // 3. Is Working Hour? -> Deduct time up to next boundary
    }
    else 
    {
        // 24/7 Logic (Simple Add)
        resultDate = resultDate.Add(duration);
    }
    return resultDate;
}
```

## 7. Next Steps for Agent
1.  Verify the exact property names for `PriorityLevelFormDetailResponse` against the backend API documentation or code.
2.  Implement the DTOs and Controller methods.
3.  Implement the JS logic, paying close attention to the "Wait" loop in the office hour calculation to avoid infinite loops.
