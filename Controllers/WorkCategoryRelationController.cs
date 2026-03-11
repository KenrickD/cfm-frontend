using cfm_frontend.Constants;
using cfm_frontend.DTOs;
using cfm_frontend.DTOs.WorkCategoryRelation;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using cfm_frontend.Models.WorkRequest;
using cfm_frontend.Services;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace cfm_frontend.Controllers
{
    /// <summary>
    /// Work Category Relation Management Controller
    /// Handles mapping of Work Categories to Priority Levels, PICs, and accessible Properties
    /// </summary>
    [Authorize]
    public class WorkCategoryRelationController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WorkCategoryRelationController> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public WorkCategoryRelationController(
            IHttpClientFactory httpClientFactory,
            IPrivilegeService privilegeService,
            ILogger<WorkCategoryRelationController> logger)
            : base(privilegeService, logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        #region View Actions

        /// <summary>
        /// GET: /WorkCategoryRelation or /WorkCategoryRelation/Index
        /// Display paginated list of work category relations
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int page = 1, int limit = 10)
        {
            // TODO: Add privilege check when privilege system is implemented
            // Example: if (!HttpContext.Session.CanView("Settings", "WorkCategoryRelation")) return Forbid();

            // Get user session
            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var viewModel = new WorkCategoryRelationViewModel
            {
                SearchKeyword = search,
                IdClient = userInfo.PreferredClientId,
                Relations = new List<WorkCategoryRelationListDto>(),
                Paging = new PagingInfo
                {
                    CurrentPage = page,
                    PageSize = limit,
                    TotalCount = 0,
                    TotalPages = 0
                }
            };

            try
            {
                // TODO: Replace with actual backend API call when implemented
                // For now, return empty list as placeholder
                _logger.LogInformation("Loading work category relations list - API not yet implemented");

                // Placeholder mock data structure for testing UI
                // Uncomment when backend API is ready:
                // var client = _httpClientFactory.CreateClient("BackendAPI");
                // var url = $"{ApiEndpoints.WorkCategoryRelation.List}?cid={userInfo.PreferredClientId}&keyword={search}&page={page}&limit={limit}";
                // var (success, response, _) = await SafeExecuteApiAsync<ApiResponseDto<PagedResponse<WorkCategoryRelationListDto>>>(
                //     () => client.GetAsync(url),
                //     "Failed to load work category relations list"
                // );
                //
                // if (success && response?.Data != null)
                // {
                //     viewModel.Relations = response.Data.Items ?? new List<WorkCategoryRelationListDto>();
                //     viewModel.Paging = new PagingInfo
                //     {
                //         CurrentPage = response.Data.Page,
                //         PageSize = response.Data.PageSize,
                //         TotalCount = response.Data.TotalCount,
                //         TotalPages = response.Data.TotalPages
                //     };
                // }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work category relations list");
                TempData["ErrorMessage"] = "An error occurred while loading the work category relations list.";
            }

            ViewBag.Title = "Work Category Relation List";
            ViewBag.pTitle = "Settings";
            return View("~/Views/Helpdesk/WorkCategoryRelation/Index.cshtml", viewModel);
        }

        /// <summary>
        /// GET: /WorkCategoryRelation/Add
        /// Display add form for new work category relation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            // TODO: Add privilege check when privilege system is implemented
            // Example: if (!HttpContext.Session.CanAdd("Settings", "WorkCategoryRelation")) return Forbid();

            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var viewModel = new WorkCategoryRelationAddViewModel
            {
                IdClient = userInfo.PreferredClientId
            };

            try
            {
                // TODO: Load dropdown data from backend API
                // For now, return empty lists as placeholders
                _logger.LogInformation("Loading add form dropdowns - API not yet implemented");

                // Placeholder for future implementation:
                // var client = _httpClientFactory.CreateClient("BackendAPI");
                // Load Work Categories, Priority Levels, PICs, Properties in parallel
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading add form data");
                TempData["ErrorMessage"] = "An error occurred while loading the form.";
            }

            ViewBag.Title = "Add New Work Category Relation";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = "/WorkCategoryRelation";
            return View("~/Views/Helpdesk/WorkCategoryRelation/Add.cshtml", viewModel);
        }

        /// <summary>
        /// GET: /WorkCategoryRelation/Edit/{id}
        /// Display edit form for existing work category relation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // TODO: Add privilege check when privilege system is implemented
            // Example: if (!HttpContext.Session.CanEdit("Settings", "WorkCategoryRelation")) return Forbid();

            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return RedirectToAction("Index", "Login");
            }

            try
            {
                // TODO: Load existing relation data from backend API
                _logger.LogInformation("Loading edit form for relation {Id} - API not yet implemented", id);

                var viewModel = new WorkCategoryRelationEditViewModel
                {
                    IdWorkCategoryRelation = id,
                    IdClient = userInfo.PreferredClientId
                };

                // Placeholder for future implementation:
                // var client = _httpClientFactory.CreateClient("BackendAPI");
                // Load relation details + dropdown data

                ViewBag.Title = "Edit Work Category Relation";
                ViewBag.pTitle = "Settings";
                ViewBag.pTitleUrl = "/WorkCategoryRelation";
                return View("~/Views/Helpdesk/WorkCategoryRelation/Edit.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for relation {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the relation details.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// GET: /WorkCategoryRelation/Detail/{id}
        /// Display detailed view of work category relation with all target sections
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            // TODO: Add privilege check when privilege system is implemented
            // Example: if (!HttpContext.Session.CanView("Settings", "WorkCategoryRelation")) return Forbid();

            var userInfo = HttpContext.Session.GetUserInfo();
            if (userInfo == null)
            {
                return RedirectToAction("Index", "Login");
            }

            try
            {
                // TODO: Load relation detail from backend API
                _logger.LogInformation("Loading detail for relation {Id} - API not yet implemented", id);

                var viewModel = new WorkCategoryRelationDetailViewModel
                {
                    IdWorkCategoryRelation = id,
                    IdClient = userInfo.PreferredClientId,
                    WorkCategoryName = "Access Control",
                    PriorityLevelName = "High",
                    AssignedPIC = "Fajri Oen",
                    Locations = new List<string> { "Central Petroleum Tower II", "Giant Fatmawati" }
                };

                ViewBag.Title = "Work Category Relation Detail";
                ViewBag.pTitle = "Work Category Relation List";
                ViewBag.pTitleUrl = "/WorkCategoryRelation";
                return View("~/Views/Helpdesk/WorkCategoryRelation/Detail.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading detail for relation {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the relation details.";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region API Actions - CRUD

        /// <summary>
        /// POST: /WorkCategoryRelation/Create
        /// Create new work category relation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] WorkCategoryRelationPayloadDto payload)
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                // Validate client context for multi-tab session safety
                if (payload.IdClient != userInfo.PreferredClientId)
                {
                    return Json(new { success = false, message = "Client context mismatch. Please refresh the page." });
                }

                // TODO: Call backend API to create relation
                _logger.LogInformation("Creating work category relation - API not yet implemented");

                // Placeholder for future implementation:
                // var client = _httpClientFactory.CreateClient("BackendAPI");
                // var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                // var (success, response, message) = await SafeExecuteApiAsync<ApiResponseDto<int>>(
                //     () => client.PostAsync(ApiEndpoints.WorkCategoryRelation.Create, jsonContent),
                //     "Failed to create work category relation"
                // );
                //
                // if (success && response?.Data > 0)
                // {
                //     return Json(new { success = true, message = "Work category relation created successfully", id = response.Data });
                // }
                //
                // return Json(new { success = false, message = message ?? "Failed to create work category relation" });

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work category relation");
                return Json(new { success = false, message = "An error occurred while creating the relation." });
            }
        }

        /// <summary>
        /// PUT: /WorkCategoryRelation/Update
        /// Update existing work category relation
        /// </summary>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] WorkCategoryRelationPayloadDto payload)
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                // Validate client context
                if (payload.IdClient != userInfo.PreferredClientId)
                {
                    return Json(new { success = false, message = "Client context mismatch. Please refresh the page." });
                }

                // TODO: Call backend API to update relation
                _logger.LogInformation("Updating work category relation {Id} - API not yet implemented", payload.IdWorkCategoryRelation);

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating work category relation {Id}", payload.IdWorkCategoryRelation);
                return Json(new { success = false, message = "An error occurred while updating the relation." });
            }
        }

        /// <summary>
        /// DELETE: /WorkCategoryRelation/Delete?id={id}
        /// Delete work category relation
        /// </summary>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                // TODO: Call backend API to delete relation
                _logger.LogInformation("Deleting work category relation {Id} - API not yet implemented", id);

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting work category relation {Id}", id);
                return Json(new { success = false, message = "An error occurred while deleting the relation." });
            }
        }

        #endregion

        #region API Actions - Dropdown Data

        /// <summary>
        /// GET: /WorkCategoryRelation/GetWorkCategories
        /// Get work categories for dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkCategories()
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                // TODO: Load from backend API
                _logger.LogInformation("Loading work categories dropdown - API not yet implemented");

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work categories");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// GET: /WorkCategoryRelation/GetPriorityLevels
        /// Get priority levels for dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPriorityLevels()
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                // TODO: Load from backend API
                _logger.LogInformation("Loading priority levels dropdown - API not yet implemented");

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading priority levels");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// GET: /WorkCategoryRelation/GetPICs
        /// Get PICs for dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPICs()
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                // TODO: Load from backend API
                _logger.LogInformation("Loading PICs dropdown - API not yet implemented");

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading PICs");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// GET: /WorkCategoryRelation/GetAllProperties
        /// Get all properties for dual listbox (left side)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllProperties()
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                // TODO: Load from backend API
                _logger.LogInformation("Loading all properties - API not yet implemented");

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading properties");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// GET: /WorkCategoryRelation/GetPropertiesByPIC?idPIC={idPIC}
        /// Get properties accessible by selected PIC (right side of dual listbox)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPropertiesByPIC(int idPIC)
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                // TODO: Load from backend API
                _logger.LogInformation("Loading properties for PIC {IdPIC} - API not yet implemented", idPIC);

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading properties for PIC {IdPIC}", idPIC);
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        #endregion

        #region API Actions - PIC Target Management (Detail Page)

        /// <summary>
        /// POST: /WorkCategoryRelation/AddPICToTarget
        /// Add PIC to specific target section
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPICToTarget([FromBody] AddPICToTargetRequest request)
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                // TODO: Call backend API
                _logger.LogInformation("Adding PIC to target section - API not yet implemented");

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding PIC to target");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// DELETE: /WorkCategoryRelation/RemovePICFromTarget
        /// Remove PIC from target section
        /// </summary>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePICFromTarget(int id, int idPIC, string targetType)
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                // TODO: Call backend API
                _logger.LogInformation("Removing PIC from target section - API not yet implemented");

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing PIC from target");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// PUT: /WorkCategoryRelation/MovePICUp
        /// Move PIC up in target section order
        /// </summary>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MovePICUp(int id, int idPIC, string targetType)
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                // TODO: Call backend API
                _logger.LogInformation("Moving PIC up - API not yet implemented");

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving PIC up");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// PUT: /WorkCategoryRelation/MovePICDown
        /// Move PIC down in target section order
        /// </summary>
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MovePICDown(int id, int idPIC, string targetType)
        {
            try
            {
                var userInfo = HttpContext.Session.GetUserInfo();
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                // TODO: Call backend API
                _logger.LogInformation("Moving PIC down - API not yet implemented");

                return Json(new { success = false, message = "Backend API not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving PIC down");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        #endregion
    }

    /// <summary>
    /// Request model for adding PIC to target section
    /// </summary>
    public class AddPICToTargetRequest
    {
        public int IdWorkCategoryRelation { get; set; }
        public int IdPIC { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
