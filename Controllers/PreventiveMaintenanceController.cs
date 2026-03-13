using cfm_frontend.DTOs;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using cfm_frontend.Services;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace cfm_frontend.Controllers
{
    /// <summary>
    /// Preventive Maintenance Controller
    /// Handles maintenance activity and schedule management with calendar visualization
    /// </summary>
    public class PreventiveMaintenanceController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PreventiveMaintenanceController> _logger;

        public PreventiveMaintenanceController(
            IPrivilegeService privilegeService,
            ILogger<PreventiveMaintenanceController> logger,
            IHttpClientFactory httpClientFactory)
            : base(privilegeService, logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Main Maintenance Management page with calendar view
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Authorization check
            if (!HttpContext.Session.CanView("Preventive Maintenance", "Maintenance Management"))
            {
                TempData["ErrorMessage"] = "You do not have permission to view Maintenance Management.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Get user session for client context
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return RedirectToAction("Login", "Login");
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson);
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Login");
            }

            // Set breadcrumbs
            ViewBag.Title = "Maintenance Management";
            ViewBag.pTitle = "Preventive Maintenance";

            // Initialize ViewModel
            var viewModel = new MaintenanceManagementViewModel
            {
                IdClient = userInfo.PreferredClientId,
                FromDate = new DateTime(DateTime.Now.Year, 1, 1), // Default to start of current year
                ViewMode = "52-Week View",
                PropertyGroups = new List<PropertyGroupDto>(),
                Buildings = new List<BuildingDto>()
            };

            // TODO: Backend API Integration - Load Property Groups
            // Endpoint: GET /api/v1/property-groups
            // Query params: idClient
            // Response: ApiResponseDto<List<PropertyGroupDto>>
            // Uncomment when backend is ready:
            // var propertyGroups = await LoadPropertyGroupsAsync(userInfo.PreferredClientId);
            // viewModel.PropertyGroups = propertyGroups ?? new List<PropertyGroupDto>();

            // TODO: Backend API Integration - Load Buildings
            // Endpoint: GET /api/v1/buildings
            // Query params: idClient, propertyGroupIds (optional)
            // Response: ApiResponseDto<List<BuildingDto>>
            // Uncomment when backend is ready:
            // var buildings = await LoadBuildingsAsync(userInfo.PreferredClientId);
            // viewModel.Buildings = buildings ?? new List<BuildingDto>();

            return View(viewModel);
        }

        /// <summary>
        /// Schedule Detail page - Shows full details of a maintenance schedule
        /// </summary>
        /// <param name="id">Schedule ID</param>
        [HttpGet]
        public async Task<IActionResult> ScheduleDetail(int id)
        {
            // Authorization check
            if (!HttpContext.Session.CanView("Preventive Maintenance", "Maintenance Management"))
            {
                TempData["ErrorMessage"] = "You do not have permission to view Maintenance Schedules.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Get user session for client context
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return RedirectToAction("Login", "Login");
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson);
            if (userInfo == null)
            {
                return RedirectToAction("Login", "Login");
            }

            // Set breadcrumbs
            ViewBag.Title = "Schedule Detail";
            ViewBag.pTitle = "Maintenance Management";
            ViewBag.pTitleUrl = "/PreventiveMaintenance/Index";

            // TODO: Backend API Integration - Get Schedule Detail
            // Endpoint: GET /api/v1/preventive-maintenance/schedules/{id}
            // Query params: cid (client id)
            // Response: ApiResponseDto<MaintenanceScheduleDetailDto>
            // Example implementation:
            /*
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.GetAsync($"/api/v1/preventive-maintenance/schedules/{id}?cid={userInfo.PreferredClientId}");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseDto<MaintenanceScheduleDetailDto>>();
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var viewModel = new MaintenanceScheduleDetailViewModel
                    {
                        IdClient = userInfo.PreferredClientId,
                        Schedule = apiResponse.Data
                    };
                    return View(viewModel);
                }
            }
            */

            // Temporary mock data until backend is ready
            var viewModel = new MaintenanceScheduleDetailViewModel
            {
                IdClient = userInfo.PreferredClientId,
                ScheduleId = id,
                ScheduleCode = $"PM-{id:D6}",
                ActivityName = "Server Cleaning",
                Location = "Central Petroleum Tower II (Jakarta)",
                PlannedStartDate = new DateTime(2026, 1, 1, 8, 0, 0),
                PlannedEndDate = new DateTime(2026, 1, 5, 17, 0, 0),
                ActualStartDate = null,
                ActualEndDate = null,
                Status = "New",
                ServiceProvider = "Self-Performed",
                Notes = "",
                Frequency = "1 Week"
            };

            return View(viewModel);
        }

        /// <summary>
        /// API endpoint to get maintenance activities list with pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMaintenanceActivities(
            int? propertyGroupId,
            int? buildingId,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int limit = 20)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson);
            if (userInfo == null)
            {
                return Json(new { success = false, message = "Invalid session" });
            }

            // TODO: Backend API Integration - Get Maintenance Activities
            // Endpoint: GET /api/v1/preventive-maintenance/activities
            // Query params: idClient, propertyGroupId, buildingId, fromDate, toDate, page, limit
            // Response: ApiResponseDto<PagedResponse<MaintenanceActivityDto>>
            // Example implementation:
            /*
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var queryParams = new Dictionary<string, string>
            {
                ["idClient"] = userInfo.PreferredClientId.ToString(),
                ["page"] = page.ToString(),
                ["limit"] = limit.ToString()
            };

            if (propertyGroupId.HasValue)
                queryParams["propertyGroupId"] = propertyGroupId.Value.ToString();
            if (buildingId.HasValue)
                queryParams["buildingId"] = buildingId.Value.ToString();
            if (fromDate.HasValue)
                queryParams["fromDate"] = fromDate.Value.ToString("yyyy-MM-dd");
            if (toDate.HasValue)
                queryParams["toDate"] = toDate.Value.ToString("yyyy-MM-dd");

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            var response = await client.GetAsync($"/api/v1/preventive-maintenance/activities?{queryString}");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseDto<PagedResponse<MaintenanceActivityDto>>>();
                if (apiResponse?.Success == true)
                {
                    return Json(new { success = true, data = apiResponse.Data });
                }
            }
            */

            // Temporary mock data until backend is ready
            var mockActivities = new List<MaintenanceActivityDto>
            {
                new MaintenanceActivityDto
                {
                    ActivityId = 18,
                    ActivityName = "Server Cleaning",
                    Location = "Central Petroleum Tower II (Jakarta)",
                    Frequency = "1 Week",
                    ServiceProvider = "Self-Performed"
                },
                new MaintenanceActivityDto
                {
                    ActivityId = 19,
                    ActivityName = "Server Cleaning",
                    Location = "Central Petroleum Tower II (Jakarta)",
                    Frequency = "1 Week",
                    ServiceProvider = "Self-Performed"
                },
                new MaintenanceActivityDto
                {
                    ActivityId = 20,
                    ActivityName = "test asset group",
                    Location = "Central Petroleum Tower II (Jakarta)",
                    Frequency = "3 Months",
                    ServiceProvider = "Self-Performed"
                }
            };

            var pagedData = new
            {
                items = mockActivities,
                totalCount = mockActivities.Count,
                currentPage = page,
                pageSize = limit,
                totalPages = 1
            };

            return Json(new { success = true, data = pagedData });
        }

        /// <summary>
        /// API endpoint to get maintenance schedules for calendar visualization
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMaintenanceSchedules(
            DateTime fromDate,
            DateTime toDate,
            int? propertyGroupId = null,
            int? buildingId = null)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson);
            if (userInfo == null)
            {
                return Json(new { success = false, message = "Invalid session" });
            }

            // TODO: Backend API Integration - Get Maintenance Schedules
            // Endpoint: GET /api/v1/preventive-maintenance/schedules
            // Query params: idClient, fromDate, toDate, propertyGroupId, buildingId
            // Response: ApiResponseDto<List<MaintenanceScheduleDto>>
            // Example implementation:
            /*
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var queryParams = new Dictionary<string, string>
            {
                ["idClient"] = userInfo.PreferredClientId.ToString(),
                ["fromDate"] = fromDate.ToString("yyyy-MM-dd"),
                ["toDate"] = toDate.ToString("yyyy-MM-dd")
            };

            if (propertyGroupId.HasValue)
                queryParams["propertyGroupId"] = propertyGroupId.Value.ToString();
            if (buildingId.HasValue)
                queryParams["buildingId"] = buildingId.Value.ToString();

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            var response = await client.GetAsync($"/api/v1/preventive-maintenance/schedules?{queryString}");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseDto<List<MaintenanceScheduleDto>>>();
                if (apiResponse?.Success == true)
                {
                    return Json(new { success = true, data = apiResponse.Data });
                }
            }
            */

            // Temporary mock data - Generate recurring schedules from activities
            var mockActivities = new List<MaintenanceActivityDto>
            {
                new MaintenanceActivityDto
                {
                    ActivityId = 18,
                    ActivityName = "Server Cleaning",
                    Location = "Central Petroleum Tower II (Jakarta)",
                    Frequency = "1 Week",
                    ServiceProvider = "Self-Performed"
                },
                new MaintenanceActivityDto
                {
                    ActivityId = 19,
                    ActivityName = "Server Cleaning",
                    Location = "Central Petroleum Tower II (Jakarta)",
                    Frequency = "1 Week",
                    ServiceProvider = "Self-Performed"
                },
                new MaintenanceActivityDto
                {
                    ActivityId = 20,
                    ActivityName = "test asset group",
                    Location = "Central Petroleum Tower II (Jakarta)",
                    Frequency = "3 Months",
                    ServiceProvider = "Self-Performed"
                }
            };

            // Generate recurring schedules for each activity
            var allSchedules = new List<MaintenanceScheduleDto>();
            foreach (var activity in mockActivities)
            {
                var recurringSchedules = GenerateRecurringSchedules(activity, fromDate, toDate);
                allSchedules.AddRange(recurringSchedules);
            }

            return Json(new { success = true, data = allSchedules });
        }

        /// <summary>
        /// API endpoint to get schedule tooltip data for hover preview
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetScheduleTooltip(int id)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson);
            if (userInfo == null)
            {
                return Json(new { success = false, message = "Invalid session" });
            }

            // TODO: Backend API Integration - Get Schedule Tooltip
            // Endpoint: GET /api/v1/preventive-maintenance/schedules/{id}/tooltip
            // Query params: cid (client id)
            // Response: ApiResponseDto<ScheduleTooltipDto>
            // Example implementation:
            /*
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.GetAsync($"/api/v1/preventive-maintenance/schedules/{id}/tooltip?cid={userInfo.PreferredClientId}");

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponseDto<ScheduleTooltipDto>>();
                if (apiResponse?.Success == true)
                {
                    return Json(new { success = true, data = apiResponse.Data });
                }
            }
            */

            // Temporary mock data until backend is ready
            var mockTooltip = new ScheduleTooltipDto
            {
                ScheduleCode = $"PM-{id:D6}",
                PlannedDate = "01 Jan 2026 08:00 AM - 05 Jan 2026 08:00 AM",
                ActualDate = "-",
                Status = "New"
            };

            return Json(new { success = true, data = mockTooltip });
        }

        /// <summary>
        /// Helper: Generate recurring schedules based on activity frequency
        /// </summary>
        private List<MaintenanceScheduleDto> GenerateRecurringSchedules(
            MaintenanceActivityDto activity,
            DateTime startDate,
            DateTime endDate)
        {
            var schedules = new List<MaintenanceScheduleDto>();
            int scheduleIdCounter = 1000 + (activity.ActivityId * 100);

            // Parse frequency (e.g., "1 Week", "3 Months")
            var (interval, unit) = ParseFrequency(activity.Frequency);

            DateTime currentDate = startDate;

            while (currentDate <= endDate)
            {
                schedules.Add(new MaintenanceScheduleDto
                {
                    ScheduleId = scheduleIdCounter++,
                    ActivityId = activity.ActivityId,
                    ActivityName = activity.ActivityName,
                    PlannedStartDate = currentDate,
                    PlannedEndDate = currentDate,
                    ActualStartDate = null,
                    ActualEndDate = null,
                    Status = "New",
                    Duration = 1
                });

                // Advance to next occurrence based on frequency
                currentDate = unit.ToLower() switch
                {
                    "week" or "weeks" => currentDate.AddDays(7 * interval),
                    "month" or "months" => currentDate.AddMonths(interval),
                    "day" or "days" => currentDate.AddDays(interval),
                    "year" or "years" => currentDate.AddYears(interval),
                    _ => currentDate.AddDays(7) // Default to weekly
                };
            }

            return schedules;
        }

        /// <summary>
        /// Helper: Parse frequency string into interval and unit
        /// </summary>
        private (int interval, string unit) ParseFrequency(string frequency)
        {
            if (string.IsNullOrWhiteSpace(frequency))
                return (1, "Week");

            // Parse "1 Week" → (1, "Week")
            // Parse "3 Months" → (3, "Months")
            var parts = frequency.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int interval = 1;
            if (parts.Length > 0 && int.TryParse(parts[0], out int parsedInterval))
            {
                interval = parsedInterval;
            }

            string unit = "Week"; // Default
            if (parts.Length > 1)
            {
                unit = parts[1];
            }

            return (interval, unit);
        }
    }
}
