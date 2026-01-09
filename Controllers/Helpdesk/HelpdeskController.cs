using cfm_frontend.Constants;
using cfm_frontend.DTOs.WorkRequest;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using cfm_frontend.Models.WorkRequest;
using cfm_frontend.Services;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using static cfm_frontend.Models.WorkRequest.WorkRequestFilterModel;

namespace cfm_frontend.Controllers.Helpdesk
{
    public class HelpdeskController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HelpdeskController> _logger;

        public HelpdeskController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<HelpdeskController> logger,
            IPrivilegeService privilegeService)
            : base(privilegeService, logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        #region Work Request Management

        /// <summary>
        /// GET: Work Request List page
        /// </summary>
        public async Task<IActionResult> Index(
            int page = 1,
            string search = "",
            int? propertyGroup = null,
            List<int>? locations = null,
            List<int>? serviceProviders = null,
            int? roomZone = null,
            List<int>? workCategories = null,
            List<int>? otherCategories = null,
            List<string>? priorities = null,
            List<string>? statuses = null,
            DateTime? requestDateFrom = null,
            DateTime? requestDateTo = null,
            DateTime? workCompletionDateFrom = null,
            DateTime? workCompletionDateTo = null,
            List<string>? checklist = null,
            List<string>? feedback = null,
            bool? hasSentEmail = null,
            bool showDeleted = false)
        {
            // Check if user has permission to view Work Request Management
            var accessCheck = this.CheckViewAccess("Helpdesk", "Work Request Management");
            if (accessCheck != null) return accessCheck;

            var viewmodel = new WorkRequestViewModel();

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return RedirectToAction("Index", "Login");
                }

                var idClient = userInfo.PreferredClientId;

                // Build request body with all filters (using backend API naming convention)
                var requestBody = new WorkRequestBodyModel
                {
                    Client_idClient = idClient,
                    page = page,
                    keyWordSearch = search,
                    idPropertyType = propertyGroup ?? -1,
                    LocationIds = locations ?? new List<int>(),
                    ServiceProviderIds = serviceProviders ?? new List<int>(),
                    RoomZone_idRoomZone = roomZone ?? -1,
                    WorkCategoryIds = workCategories ?? new List<int>(),
                    OtherCategoryIds = otherCategories ?? new List<int>(),
                    PriorityLevels = priorities ?? new List<string>(),
                    Statuses = statuses ?? new List<string>(),
                    requestDateFrom = requestDateFrom,
                    requestDateTo = requestDateTo,
                    workCompletionFrom = workCompletionDateFrom,
                    workCompletionTo = workCompletionDateTo,
                    ImportantChecklists = checklist ?? new List<string>(),
                    FeedbackTypes = feedback ?? new List<string>(),
                    isSendEmail = hasSentEmail ?? false,
                    showDeleted = showDeleted
                };

                // Load work requests and filter options in parallel
                var workRequestTask = GetWorkRequestsAsync(client, backendUrl, requestBody);
                var filterOptionsTask = GetFilterOptionsAsync(client, backendUrl, idClient);

                await Task.WhenAll(workRequestTask, filterOptionsTask);

                // Populate ViewModel with results
                var workRequestResponse = await workRequestTask;
                if (workRequestResponse != null)
                {
                    // Sort by request date descending (most recent first)
                    viewmodel.WorkRequest = workRequestResponse.data?
                        .OrderByDescending(wr => wr.requestDate)
                        .ToList() ?? new List<WorkRequestResponseModel>();

                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = workRequestResponse.Metadata.CurrentPage,
                        TotalPages = workRequestResponse.Metadata.TotalPages,
                        PageSize = workRequestResponse.Metadata.PageSize,
                        TotalCount = workRequestResponse.Metadata.TotalCount
                    };
                }

                viewmodel.FilterOptions = await filterOptionsTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work request index page");
                viewmodel.FilterOptions = new FilterOptionsModel();
            }

            return View("~/Views/Helpdesk/WorkRequest/Index.cshtml", viewmodel);
        }

        /// <summary>
        /// GET: Work Request Add page
        /// </summary>
        //[Authorize]
        public IActionResult WorkRequestAdd()
        {
            // Check if user has permission to view Work Request Add page
            var accessCheck = this.CheckViewAccess("Helpdesk", "Work Request Management");
            if (accessCheck != null) return accessCheck;

            // Return view - all dropdown data is loaded client-side via JavaScript
            return View("~/Views/Helpdesk/WorkRequest/WorkRequestAdd.cshtml");
        }

        /// <summary>
        /// POST: Create new Work Request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize]
        public async Task<IActionResult> WorkRequestAdd(
            WorkRequestCreateRequest model,
            string Material_JobcodeJson = null,
            string Material_AdhocJson = null,
            string AssetsJson = null,
            string ImportantChecklistJson = null,
            string WorkersJson = null)
        {
            // Check if user has permission to add Work Requests
            var accessCheck = this.CheckAddAccess("Helpdesk", "Work Request Management");
            if (accessCheck != null) return accessCheck;

            // Get user session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return RedirectToAction("Index", "Login");
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (userInfo == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var idClient = userInfo.PreferredClientId;
            if (!ModelState.IsValid)
            {
                // Return validation errors as JSON for client-side handling
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new { success = false, errors });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Set system fields from session
                model.Client_idClient = idClient;
                model.IdEmployee = 1; // TODO: Get from session/authentication
                model.TimeZone_idTimeZone = userInfo.PreferredTimezoneIdTimezone;

                // Parse JSON fields if provided
                if (!string.IsNullOrEmpty(Material_JobcodeJson))
                {
                    try
                    {
                        model.Material_Jobcode = JsonSerializer.Deserialize<List<MaterialJobCodeDto>>(
                            Material_JobcodeJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        ) ?? new List<MaterialJobCodeDto>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse Material_JobcodeJson");
                        model.Material_Jobcode = new List<MaterialJobCodeDto>();
                    }
                }

                if (!string.IsNullOrEmpty(Material_AdhocJson))
                {
                    try
                    {
                        model.Material_Adhoc = JsonSerializer.Deserialize<List<MaterialAdhocDto>>(
                            Material_AdhocJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        ) ?? new List<MaterialAdhocDto>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse Material_AdhocJson");
                        model.Material_Adhoc = new List<MaterialAdhocDto>();
                    }
                }

                if (!string.IsNullOrEmpty(AssetsJson))
                {
                    try
                    {
                        model.Assets = JsonSerializer.Deserialize<List<AssetDto>>(
                            AssetsJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        ) ?? new List<AssetDto>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse AssetsJson");
                        model.Assets = new List<AssetDto>();
                    }
                }

                if (!string.IsNullOrEmpty(ImportantChecklistJson))
                {
                    try
                    {
                        model.ImportantChecklist = JsonSerializer.Deserialize<List<AdditionalInformationDto>>(
                            ImportantChecklistJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        ) ?? new List<AdditionalInformationDto>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse ImportantChecklistJson");
                        model.ImportantChecklist = new List<AdditionalInformationDto>();
                    }
                }

                if (!string.IsNullOrEmpty(WorkersJson))
                {
                    try
                    {
                        model.Workers = JsonSerializer.Deserialize<List<WorkerDto>>(
                            WorkersJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        ) ?? new List<WorkerDto>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse WorkersJson");
                        model.Workers = new List<WorkerDto>();
                    }
                }

                // Serialize and send to backend
                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.WorkRequest.Create}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<WorkRequestCreateResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (result != null && result.success)
                    {
                        TempData["SuccessMessage"] = $"Work Request {result.workRequestCode} created successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, result?.message ?? "Failed to create work request");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to create work request. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, errorContent);
                    ModelState.AddModelError(string.Empty, "Failed to create work request. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work request");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the work request.");
            }

            // If we got here, something failed - return JSON error for JavaScript to handle
            return Json(new
            {
                success = false,
                message = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "An error occurred while creating the work request."
            });
        }

        /// <summary>
        /// POST: Create Ad-hoc Job Code
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateAdHocJobCode([FromBody] AdHocJobCodeRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                    return Json(new { success = false, message = "Session expired" });

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                request.Client_idClient = userInfo.PreferredClientId;

                var jsonPayload = JsonSerializer.Serialize(request,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.JobCode.Create}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<AdHocJobCodeResponse>(
                        responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return Json(new { success = true, data = result });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to create ad-hoc job code. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);
                return Json(new { success = false, message = "Failed to create job code" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad-hoc job code");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// GET: Send New Work Request page
        /// </summary>
        //[Authorize]
        public async Task<IActionResult> SendNewWorkRequest()
        {
            // Check if user has permission to view Send Work Request page
            var accessCheck = this.CheckViewAccess("Helpdesk", "Send Work Request");
            if (accessCheck != null) return accessCheck;

            var viewmodel = new WorkRequestViewModel();

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return RedirectToAction("Index", "Login");
                }

                var idClient = userInfo.PreferredClientId;

                // Load initial data for dropdowns
                var locationsTask = GetLocationsAsync(client, backendUrl, idClient);
                var workCategoriesTask = GetWorkCategoriesAsync(client, backendUrl);

                await Task.WhenAll(locationsTask, workCategoriesTask);

                viewmodel.Locations = await locationsTask;
                viewmodel.WorkCategories = await workCategoriesTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading send new work request page");
                viewmodel.Locations = new List<LocationModel>();
                viewmodel.WorkCategories = new List<WorkCategoryModel>();
            }

            return View("~/Views/Helpdesk/WorkRequest/SendNewWorkRequest.cshtml", viewmodel);
        }

        /// <summary>
        /// POST: Submit New Work Request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize]
        public async Task<IActionResult> SendNewWorkRequest(
            int IdLocation,
            int IdFloor,
            int IdRoom,
            string ForWhom,
            int IdWorkCategory,
            string RequestDetail,
            List<IFormFile> RelatedPhotos)
        {
            // Check if user has permission to add Work Requests
            var accessCheck = this.CheckAddAccess("Helpdesk", "Send Work Request");
            if (accessCheck != null) return accessCheck;

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                // Temporary send, no idea what is the dto yet
                // Prepare work request data
                var workRequest = new WorkRequestCreateRequest
                {
                    Client_idClient = userInfo.PreferredClientId,
                    IdEmployee = userInfo.IdWebUser,
                    Property_idProperty = IdLocation,
                    PropertyFloor_idPropertyFloor = IdFloor,
                    RoomZone_idRoomZone = IdRoom,
                    workCategory_Type_idType = IdWorkCategory,
                    requestDetail = RequestDetail,
                    workTitle = $"Work Request - {RequestDetail.Substring(0, Math.Min(50, RequestDetail.Length))}...",
                    requestMethod_Enum_idEnum = 1,
                    status_Enum_idEnum = 1,
                    PriorityLevel_idPriorityLevel = 1,
                    requestDate = DateTime.UtcNow,
                    requestor_Employee_idEmployee = userInfo.IdWebUser
                };

                // For now, we're using the logged-in user as the requestor

                // TODO: Handle file uploads (RelatedPhotos)
                // This would typically involve uploading files to a storage service
                // and storing the file paths/URLs in the work request

                // Serialize and send to backend
                var jsonPayload = JsonSerializer.Serialize(workRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.WorkRequest.Create}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<WorkRequestCreateResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (result != null && result.success)
                    {
                        return Json(new
                        {
                            success = true,
                            message = $"Work Request {result.workRequestCode} sent successfully!",
                            redirectUrl = "/Helpdesk/Index"
                        });
                    }
                    else
                    {
                        return Json(new { success = false, message = result?.message ?? "Failed to send work request" });
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to send work request. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, errorContent);
                    return Json(new { success = false, message = "Failed to send work request. Please try again." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending work request");
                return Json(new { success = false, message = "An error occurred while sending the work request." });
            }
        }

        /// <summary>
        /// GET: Work Request Detail page
        /// </summary>
        //[Authorize]
        public IActionResult WorkRequestDetail(int id)
        {
            // Check if user has permission to view Work Request Detail
            var accessCheck = this.CheckViewAccess("Helpdesk", "Work Request Management");
            if (accessCheck != null) return accessCheck;

            // Return view - all dropdown data is loaded client-side via JavaScript
            // TODO: When backend is ready, fetch work request detail and pass to view
            return View("~/Views/Helpdesk/WorkRequest/WorkRequestDetail.cshtml");
        }


        #region API Endpoints for Dynamic Data Loading

        /// <summary>
        /// API: Get floors by property ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFloorsByLocation(int locationId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Property.GetFloors(locationId)}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var floors = await JsonSerializer.DeserializeAsync<List<FloorModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = floors });
                }

                return Json(new { success = false, message = "Failed to load floors" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching floors for property {LocationId}", locationId);
                return Json(new { success = false, message = "Error loading floors" });
            }
        }

        /// <summary>
        /// API: Get room zones by property ID and floor ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRoomsByFloor(int propertyId, int floorId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Property.GetRoomZones(propertyId, floorId)}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var rooms = await JsonSerializer.DeserializeAsync<List<RoomModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = rooms });
                }

                return Json(new { success = false, message = "Failed to load room zones" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching room zones for property {PropertyId} and floor {FloorId}", propertyId, floorId);
                return Json(new { success = false, message = "Error loading room zones" });
            }
        }

        /// <summary>
        /// API: Search employees/requestors by name
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchEmployees(string term, int idClient)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}/api/employee/search?term={Uri.EscapeDataString(term)}&idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var employees = await JsonSerializer.DeserializeAsync<List<EmployeeModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = employees });
                }

                return Json(new { success = false, message = "Failed to search employees" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching employees with term {Term}", term);
                return Json(new { success = false, message = "Error searching employees" });
            }
        }

        /// <summary>
        /// API: Get employees for Person in Charge dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPersonsInCharge(int idClient)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Employee.PersonsInCharge}?idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var employees = await JsonSerializer.DeserializeAsync<List<EmployeeModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = employees });
                }

                return Json(new { success = false, message = "Failed to load persons in charge" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching persons in charge for client {IdClient}", idClient);
                return Json(new { success = false, message = "Error loading persons in charge" });
            }
        }

        /// <summary>
        /// API: Search workers from company
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchWorkers(string term, int? idServiceProvider)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var url = $"{backendUrl}/api/worker/search?term={Uri.EscapeDataString(term)}";
                if (idServiceProvider.HasValue)
                {
                    url += $"&idServiceProvider={idServiceProvider.Value}";
                }

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var workers = await JsonSerializer.DeserializeAsync<List<EmployeeModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = workers });
                }

                return Json(new { success = false, message = "Failed to search workers" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching workers with term {Term}", term);
                return Json(new { success = false, message = "Error searching workers" });
            }
        }

        /// <summary>
        /// API: Get locations by idClient and userId from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLocationsByClient()
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;
                var userId = userInfo.IdWebUser;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Property.List}?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var locations = await JsonSerializer.DeserializeAsync<List<LocationModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = locations });
                }

                return Json(new { success = false, message = "Failed to load properties" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching locations");
                return Json(new { success = false, message = "Error loading locations" });
            }
        }

        /// <summary>
        /// API: Get persons in charge filtered by work category and location
        /// idClient is retrieved from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPersonsInChargeByFilters(int? idWorkCategory = null, int? idLocation = null)
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var url = $"{backendUrl}{ApiEndpoints.Employee.PersonsInCharge}?idClient={idClient}";
                if (idWorkCategory.HasValue)
                {
                    url += $"&idWorkCategory={idWorkCategory.Value}";
                }
                if (idLocation.HasValue)
                {
                    url += $"&idLocation={idLocation.Value}";
                }

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var employees = await JsonSerializer.DeserializeAsync<List<EmployeeModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = employees });
                }

                return Json(new { success = false, message = "Failed to load persons in charge" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching persons in charge with filters");
                return Json(new { success = false, message = "Error loading persons in charge" });
            }
        }

        /// <summary>
        /// API: Search requestors/employees by term
        /// idCompany is retrieved from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchRequestors(string term)
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idCompany = userInfo.IdCompany;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Employee.SearchRequestors}?term={Uri.EscapeDataString(term)}&idCompany={idCompany}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var requestors = await JsonSerializer.DeserializeAsync<List<EmployeeModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = requestors });
                }

                return Json(new { success = false, message = "Failed to search requestors" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching requestors with term {Term}", term);
                return Json(new { success = false, message = "Error searching requestors" });
            }
        }


        /// <summary>
        /// API: Get service providers
        /// idClient and idCompany are retrieved from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetServiceProvidersByClient()
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;
                var idCompany = userInfo.IdCompany;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.ServiceProvider.List}?idClient={idClient}&idCompany={idCompany}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var providers = await JsonSerializer.DeserializeAsync<List<ServiceProviderModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = providers });
                }

                return Json(new { success = false, message = "Failed to load service providers" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching service providers");
                return Json(new { success = false, message = "Error loading service providers" });
            }
        }

        /// <summary>
        /// API: Search workers from company by location
        /// idCompany is retrieved from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchWorkersByCompany(string term, int idLocation)
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idCompany = userInfo.IdCompany;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}/api/worker/search-by-company?term={Uri.EscapeDataString(term)}&idLocation={idLocation}&idCompany={idCompany}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var workers = await JsonSerializer.DeserializeAsync<List<EmployeeModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = workers });
                }

                return Json(new { success = false, message = "Failed to search workers" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching workers by company with term {Term}", term);
                return Json(new { success = false, message = "Error searching workers" });
            }
        }

        /// <summary>
        /// API: Search workers from service provider
        /// idClient is retrieved from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchWorkersByServiceProvider(string term, int idLocation, int idServiceProvider)
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // First get the service provider's company ID
                var spResponse = await client.GetAsync(
                    $"{backendUrl}/api/serviceprovider/get-company-id?idClient={idClient}&idServiceProvider={idServiceProvider}"
                );

                if (!spResponse.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Failed to get service provider details" });
                }

                var spStream = await spResponse.Content.ReadAsStreamAsync();
                var spData = await JsonSerializer.DeserializeAsync<dynamic>(
                    spStream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                int idCompany = spData.GetProperty("idCompany").GetInt32();

                // Now search workers with the company ID
                var response = await client.GetAsync(
                    $"{backendUrl}/api/worker/search-by-company?term={Uri.EscapeDataString(term)}&idLocation={idLocation}&idCompany={idCompany}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var workers = await JsonSerializer.DeserializeAsync<List<EmployeeModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = workers });
                }

                return Json(new { success = false, message = "Failed to search workers" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching workers by service provider");
                return Json(new { success = false, message = "Error searching workers" });
            }
        }

        /// <summary>
        /// API: Get important checklist using new Types API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetImportantChecklistByTypes()
        {
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var endpoint = ApiEndpoints.Masters.GetTypes(ApiEndpoints.Masters.CategoryTypes.WorkRequestAdditionalInformation);
                var response = await client.GetAsync($"{backendUrl}{endpoint}?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var types = await JsonSerializer.DeserializeAsync<List<TypeFormDetailResponse>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = types });
                }

                return Json(new { success = false, message = "Failed to load important checklist" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching important checklist");
                return Json(new { success = false, message = "Error loading important checklist" });
            }
        }

        /// <summary>
        /// API: Get work categories using new Types API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkCategoriesByTypes()
        {
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var endpoint = ApiEndpoints.Masters.GetTypes(ApiEndpoints.Masters.CategoryTypes.WorkCategory);
                var response = await client.GetAsync($"{backendUrl}{endpoint}?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var types = await JsonSerializer.DeserializeAsync<List<TypeFormDetailResponse>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = types });
                }

                return Json(new { success = false, message = "Failed to load work categories" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching work categories");
                return Json(new { success = false, message = "Error loading work categories" });
            }
        }

        /// <summary>
        /// API: Get other categories using new Types API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOtherCategoriesByTypes(string categoryType)
        {
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var endpoint = ApiEndpoints.Masters.GetTypes(categoryType);
                var response = await client.GetAsync($"{backendUrl}{endpoint}?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var types = await JsonSerializer.DeserializeAsync<List<TypeFormDetailResponse>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = types });
                }

                return Json(new { success = false, message = "Failed to load categories" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching categories for type {categoryType}");
                return Json(new { success = false, message = "Error loading categories" });
            }
        }

        /// <summary>
        /// GET: Priority Level Settings page
        /// </summary>
        public IActionResult PriorityLevel()
        {
            ViewBag.Title = "Priority Level Management";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/PriorityLevel.cshtml");
        }

        /// <summary>
        /// API: Get priority levels from lookup table
        /// idClient is retrieved from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPriorityLevels()
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Lookup.List}?type={ApiEndpoints.Lookup.Types.WorkRequestPriorityLevel}&idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var priorities = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = priorities });
                }

                return Json(new { success = false, message = "Failed to load priority levels" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching priority levels");
                return Json(new { success = false, message = "Error loading priority levels" });
            }
        }

        /// <summary>
        /// API: Get work request methods using new Enums API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkRequestMethodsByEnums()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var endpoint = ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestMethod);
                var response = await client.GetAsync($"{backendUrl}{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var enums = await JsonSerializer.DeserializeAsync<List<EnumFormDetailResponse>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = enums });
                }

                return Json(new { success = false, message = "Failed to load work request methods" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching work request methods");
                return Json(new { success = false, message = "Error loading work request methods" });
            }
        }

        /// <summary>
        /// API: Get work request statuses using new Enums API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkRequestStatusesByEnums()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var endpoint = ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestStatus);
                var response = await client.GetAsync($"{backendUrl}{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var enums = await JsonSerializer.DeserializeAsync<List<EnumFormDetailResponse>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = enums });
                }

                return Json(new { success = false, message = "Failed to load work request statuses" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching work request statuses");
                return Json(new { success = false, message = "Error loading work request statuses" });
            }
        }

        /// <summary>
        /// API: Get feedback types using new Enums API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFeedbackTypesByEnums()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var endpoint = ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestFeedbackType);
                var response = await client.GetAsync($"{backendUrl}{endpoint}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var enums = await JsonSerializer.DeserializeAsync<List<EnumFormDetailResponse>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = enums });
                }

                return Json(new { success = false, message = "Failed to load feedback types" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching feedback types");
                return Json(new { success = false, message = "Error loading feedback types" });
            }
        }

        /// <summary>
        /// API: Get office hours for target date calculations
        /// idClient is retrieved from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOfficeHours()
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.OfficeHour.List}?idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var officeHours = await JsonSerializer.DeserializeAsync<List<OfficeHourModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = officeHours ?? new List<OfficeHourModel>() });
                }

                _logger.LogWarning("Failed to load office hours. Status: {StatusCode}", response.StatusCode);
                return Json(new { success = false, message = "Failed to load office hours" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching office hours");
                return Json(new { success = false, message = "Error loading office hours" });
            }
        }

        /// <summary>
        /// API: Get public holidays for target date calculations
        /// idClient is retrieved from session
        /// Loads current year and next year for 2-year window
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPublicHolidays()
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Load 2-year window (current year + next year) to handle year boundaries
                var currentYear = DateTime.Now.Year;
                var nextYear = currentYear + 1;

                // Fetch both years in parallel
                var currentYearTask = client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.PublicHoliday.List}?idClient={idClient}&year={currentYear}&isActiveData=true"
                );
                var nextYearTask = client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.PublicHoliday.List}?idClient={idClient}&year={nextYear}&isActiveData=true"
                );

                await Task.WhenAll(currentYearTask, nextYearTask);

                var currentYearResponse = await currentYearTask;
                var nextYearResponse = await nextYearTask;

                var allPublicHolidays = new List<PublicHolidayModel>();

                if (currentYearResponse.IsSuccessStatusCode)
                {
                    var responseStream = await currentYearResponse.Content.ReadAsStreamAsync();
                    var holidays = await JsonSerializer.DeserializeAsync<List<PublicHolidayModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (holidays != null)
                    {
                        allPublicHolidays.AddRange(holidays);
                    }
                }

                if (nextYearResponse.IsSuccessStatusCode)
                {
                    var responseStream = await nextYearResponse.Content.ReadAsStreamAsync();
                    var holidays = await JsonSerializer.DeserializeAsync<List<PublicHolidayModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (holidays != null)
                    {
                        allPublicHolidays.AddRange(holidays);
                    }
                }

                return Json(new { success = true, data = allPublicHolidays });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching public holidays");
                return Json(new { success = false, message = "Error loading public holidays" });
            }
        }

        /// <summary>
        /// GET: Priority Level Add page
        /// </summary>
        public IActionResult PriorityLevelAdd()
        {
            ViewBag.Title = "Add New Priority Level";
            ViewBag.pTitle = "Priority Level Management";
            ViewBag.pTitleUrl = Url.Action("PriorityLevel", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/PriorityLevelAdd.cshtml");
        }

        /// <summary>
        /// GET: Priority Level Detail page
        /// </summary>
        public IActionResult PriorityLevelDetail(int id)
        {
            ViewBag.Title = "Priority Level Detail";
            ViewBag.pTitle = "Priority Level Management";
            ViewBag.pTitleUrl = Url.Action("PriorityLevel", "Helpdesk");
            ViewBag.PriorityLevelId = id;
            return View("~/Views/Helpdesk/Settings/PriorityLevelDetail.cshtml");
        }

        /// <summary>
        /// API: Get priority level by ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPriorityLevelById(int id)
        {
            try
            {
                // Get user session for idClient
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Settings.PriorityLevel.GetById(id)}?idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var priorityLevel = await JsonSerializer.DeserializeAsync<dynamic>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = priorityLevel });
                }

                return Json(new { success = false, message = "Failed to load priority level details" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching priority level by ID {Id}", id);
                return Json(new { success = false, message = "Error loading priority level details" });
            }
        }

        /// <summary>
        /// API: Get dropdown options for priority level forms
        /// Loads reference options and color options based on type parameter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPriorityLevelDropdownOptions(string type)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Settings.PriorityLevel.DropdownOptions}?type={type}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var options = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = options });
                }

                return Json(new { success = false, message = "Failed to load dropdown options" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dropdown options for type {Type}", type);
                return Json(new { success = false, message = "Error loading dropdown options" });
            }
        }

        /// <summary>
        /// POST: Create new priority level
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePriorityLevel([FromBody] dynamic priorityLevelData)
        {
            try
            {
                // Get user session for idClient
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Serialize the priority level data
                var jsonPayload = JsonSerializer.Serialize(priorityLevelData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    $"{backendUrl}{ApiEndpoints.Settings.PriorityLevel.Create}",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Priority level created successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to create priority level. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to create priority level" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating priority level");
                return Json(new { success = false, message = "Error creating priority level" });
            }
        }


        /// <summary>
        /// API: Search Job Code by name
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchJobCode(string term)
        {
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}/api/jobcode/search?term={Uri.EscapeDataString(term)}&idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var jobCodes = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = jobCodes });
                }

                return Json(new { success = false, message = "Failed to search job codes" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching job codes");
                return Json(new { success = false, message = "Error searching job codes" });
            }
        }

        /// <summary>
        /// API: Get currencies for unit price dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCurrencies()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}/api/lookup/list?type=currency"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var currencies = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = currencies });
                }

                return Json(new { success = false, message = "Failed to load currencies" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching currencies");
                return Json(new { success = false, message = "Error loading currencies" });
            }
        }

        /// <summary>
        /// API: Get measurement units for dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMeasurementUnits()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}/api/lookup/list?type=measurementUnit"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var units = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = units });
                }

                return Json(new { success = false, message = "Failed to load measurement units" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching measurement units");
                return Json(new { success = false, message = "Error loading measurement units" });
            }
        }

        /// <summary>
        /// API: Get labor/material label enum
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLaborMaterialLabels()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}/api/lookup/list?type=laborMaterialLabel"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var labels = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = labels });
                }

                return Json(new { success = false, message = "Failed to load labor/material labels" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching labor/material labels");
                return Json(new { success = false, message = "Error loading labor/material labels" });
            }
        }

        /// <summary>
        /// API: Search assets individually by name
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchAsset(string term)
        {
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}/api/asset/search?term={Uri.EscapeDataString(term)}&idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var assets = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = assets });
                }

                return Json(new { success = false, message = "Failed to search assets" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching assets");
                return Json(new { success = false, message = "Error searching assets" });
            }
        }

        /// <summary>
        /// API: Search asset groups by name
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchAssetGroup(string term)
        {
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}/api/assetgroup/search?term={Uri.EscapeDataString(term)}&idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var assetGroups = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = assetGroups });
                }

                return Json(new { success = false, message = "Failed to search asset groups" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching asset groups");
                return Json(new { success = false, message = "Error searching asset groups" });
            }
        }

        #endregion

        #region Helper Functions 

        private async Task<WorkRequestListApiResponse?> GetWorkRequestsAsync(   
            HttpClient client,
            string backendUrl,
            WorkRequestBodyModel requestBody)
        {
            try
            {
                var jsonPayload = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    $"{backendUrl}{ApiEndpoints.WorkRequest.List}",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    return await JsonSerializer.DeserializeAsync<WorkRequestListApiResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }

                _logger.LogWarning("Work Request API returned status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching work requests");
                return null;
            }
        }

        private async Task<FilterOptionsModel?> GetFilterOptionsAsync(HttpClient client, string backendUrl, int idClient)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.WorkRequest.GetFilterOptions}?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var filterOptions = await JsonSerializer.DeserializeAsync<FilterOptionsModel>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return filterOptions;
                }

                _logger.LogWarning("Filter Options API returned status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching filter options");
                return null;
            }
        }

        // Legacy helper methods (still used by WorkRequestAdd, SendNewWorkRequest, etc.)
        private async Task<List<LocationModel>> GetLocationsAsync(HttpClient client, string backendUrl, int idClient)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Property.List}?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<List<LocationModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return result ?? new List<LocationModel>();
                }

                _logger.LogWarning("Properties API returned status: {StatusCode}", response.StatusCode);
                return new List<LocationModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching properties");
                return new List<LocationModel>();
            }
        }

        private async Task<List<ServiceProviderModel>> GetServiceProvidersAsync(HttpClient client, string backendUrl, int idClient)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.ServiceProvider.List}?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<List<ServiceProviderModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return result ?? new List<ServiceProviderModel>();
                }

                _logger.LogWarning("Service Providers API returned status: {StatusCode}", response.StatusCode);
                return new List<ServiceProviderModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching service providers");
                return new List<ServiceProviderModel>();
            }
        }

        private async Task<List<WorkCategoryModel>> GetWorkCategoriesAsync(HttpClient client, string backendUrl)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.WorkCategory.List}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<List<WorkCategoryModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return result ?? new List<WorkCategoryModel>();
                }

                _logger.LogWarning("Work Categories API returned status: {StatusCode}", response.StatusCode);
                return new List<WorkCategoryModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching work categories");
                return new List<WorkCategoryModel>();
            }
        }

        private async Task<List<OtherCategoryModel>> GetOtherCategoriesAsync(HttpClient client, string backendUrl)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.OtherCategory.List}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<List<OtherCategoryModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return result ?? new List<OtherCategoryModel>();
                }

                _logger.LogWarning("Other Categories API returned status: {StatusCode}", response.StatusCode);
                return new List<OtherCategoryModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching other categories");
                return new List<OtherCategoryModel>();
            }
        }

        #endregion

        #endregion


        #region Settings

        /// <summary>
        /// GET: Settings Hub page
        /// </summary>
        //[Authorize]
        public IActionResult Settings()
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;
            return View("~/Views/Helpdesk/Settings/Index.cshtml");
        }

        /// <summary>
        /// GET: Work Category Settings page
        /// </summary>
        //[Authorize]
        public IActionResult WorkCategory()
        {
            ViewBag.Title = "Work Category";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/WorkCategory.cshtml");
        }

        /// <summary>
        /// API: Get all work categories
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkCategories()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.WorkCategory.List}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var categories = await JsonSerializer.DeserializeAsync<List<WorkCategoryModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = categories });
                }

                return Json(new { success = false, message = "Failed to load work categories" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching work categories");
                return Json(new { success = false, message = "Error loading work categories" });
            }
        }

        /// <summary>
        /// API: Create new work category
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateWorkCategory([FromBody] WorkCategoryModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.name))
                {
                    return Json(new { success = false, message = "Category name is required" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.WorkCategory.Create}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Work category created successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to create work category. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to create work category" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work category");
                return Json(new { success = false, message = "Error creating work category" });
            }
        }

        /// <summary>
        /// API: Update work category
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateWorkCategory([FromBody] WorkCategoryModel model)
        {
            try
            {
                if (model.id <= 0)
                {
                    return Json(new { success = false, message = "Invalid category ID" });
                }

                if (string.IsNullOrWhiteSpace(model.name))
                {
                    return Json(new { success = false, message = "Category name is required" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{backendUrl}{ApiEndpoints.WorkCategory.Update}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Work category updated successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update work category. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to update work category" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating work category");
                return Json(new { success = false, message = "Error updating work category" });
            }
        }

        /// <summary>
        /// API: Delete work category
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteWorkCategory([FromBody] WorkCategoryModel model)
        {
            try
            {
                if (model.id <= 0)
                {
                    return Json(new { success = false, message = "Invalid category ID" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.DeleteAsync($"{backendUrl}{ApiEndpoints.WorkCategory.Delete(model.id)}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Work category deleted successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to delete work category. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to delete work category" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting work category");
                return Json(new { success = false, message = "Error deleting work category" });
            }
        }

        #region Other Category

        public IActionResult OtherCategory()
        {
            ViewBag.Title = "Other Category";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/OtherCategory.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetOtherCategories() => await GetCategoriesGeneric("othercategory", "other categories");

        [HttpPost]
        public async Task<IActionResult> CreateOtherCategory([FromBody] OtherCategoryModel model) => await CreateCategoryGeneric(model, "othercategory", "other category");

        [HttpPut]
        public async Task<IActionResult> UpdateOtherCategory([FromBody] OtherCategoryModel model) => await UpdateCategoryGeneric(model, "othercategory", "other category");

        [HttpDelete]
        public async Task<IActionResult> DeleteOtherCategory([FromBody] OtherCategoryModel model) => await DeleteCategoryGeneric(model.id, "othercategory", "other category");

        #endregion

        #region Other Category 2

        public IActionResult OtherCategory2()
        {
            ViewBag.Title = "Other Category 2";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/OtherCategory2.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetOtherCategories2() => await GetCategoriesGeneric("othercategory2", "other categories 2");

        [HttpPost]
        public async Task<IActionResult> CreateOtherCategory2([FromBody] OtherCategoryModel model) => await CreateCategoryGeneric(model, "othercategory2", "other category 2");

        [HttpPut]
        public async Task<IActionResult> UpdateOtherCategory2([FromBody] OtherCategoryModel model) => await UpdateCategoryGeneric(model, "othercategory2", "other category 2");

        [HttpDelete]
        public async Task<IActionResult> DeleteOtherCategory2([FromBody] OtherCategoryModel model) => await DeleteCategoryGeneric(model.id, "othercategory2", "other category 2");

        #endregion

        #region Job Code Group

        public IActionResult JobCodeGroup()
        {
            ViewBag.Title = "Job Code Group";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/JobCodeGroup.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetJobCodeGroups() => await GetCategoriesGeneric("jobcodegroup", "job code groups");

        [HttpPost]
        public async Task<IActionResult> CreateJobCodeGroup([FromBody] dynamic model) => await CreateCategoryGeneric(model, "jobcodegroup", "job code group");

        [HttpPut]
        public async Task<IActionResult> UpdateJobCodeGroup([FromBody] dynamic model) => await UpdateCategoryGeneric(model, "jobcodegroup", "job code group");

        [HttpDelete]
        public async Task<IActionResult> DeleteJobCodeGroup([FromBody] dynamic model) => await DeleteCategoryGeneric((int)model.id, "jobcodegroup", "job code group");

        #endregion

        #region Material Type

        public IActionResult MaterialType()
        {
            ViewBag.Title = "Material Type";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/MaterialType.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetMaterialTypes() => await GetCategoriesGeneric("materialtype", "material types");

        [HttpPost]
        public async Task<IActionResult> CreateMaterialType([FromBody] dynamic model) => await CreateCategoryGeneric(model, "materialtype", "material type");

        [HttpPut]
        public async Task<IActionResult> UpdateMaterialType([FromBody] dynamic model) => await UpdateCategoryGeneric(model, "materialtype", "material type");

        [HttpDelete]
        public async Task<IActionResult> DeleteMaterialType([FromBody] dynamic model) => await DeleteCategoryGeneric((int)model.id, "materialtype", "material type");

        #endregion

        #region Important Checklist

        public IActionResult ImportantChecklist()
        {
            ViewBag.Title = "Important Checklist";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/ImportantChecklist.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetImportantChecklists()
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Lookup.List}?type={ApiEndpoints.Lookup.Types.WorkRequestAdditionalInformation}&idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var checklist = await JsonSerializer.DeserializeAsync<List<ImportantChecklistItemModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    // Sort by displayOrder
                    var sortedChecklist = checklist?.OrderBy(x => x.displayOrder).ToList();

                    return Json(new { success = true, data = sortedChecklist });
                }

                return Json(new { success = false, message = "Failed to load important checklist" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching important checklist");
                return Json(new { success = false, message = "Error loading important checklist" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateImportantChecklist([FromBody] ImportantChecklistItemModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.name))
                {
                    return Json(new { success = false, message = "Checklist name is required" });
                }

                // Get user session for idClient
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Set label to name if not provided
                if (string.IsNullOrWhiteSpace(model.label))
                {
                    model.label = model.name;
                }

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Lookup.Create}?type={ApiEndpoints.Lookup.Types.WorkRequestAdditionalInformation}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Important checklist item created successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to create important checklist. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to create important checklist item" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating important checklist");
                return Json(new { success = false, message = "Error creating important checklist item" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateImportantChecklist([FromBody] ImportantChecklistItemModel model)
        {
            try
            {
                if (model.id <= 0)
                {
                    return Json(new { success = false, message = "Invalid checklist ID" });
                }

                if (string.IsNullOrWhiteSpace(model.name))
                {
                    return Json(new { success = false, message = "Checklist name is required" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Set label to name if not provided
                if (string.IsNullOrWhiteSpace(model.label))
                {
                    model.label = model.name;
                }

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{backendUrl}{ApiEndpoints.Lookup.Update}?type={ApiEndpoints.Lookup.Types.WorkRequestAdditionalInformation}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Important checklist item updated successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update important checklist. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to update important checklist item" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating important checklist");
                return Json(new { success = false, message = "Error updating important checklist item" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteImportantChecklist([FromBody] ImportantChecklistItemModel model)
        {
            try
            {
                if (model.id <= 0)
                {
                    return Json(new { success = false, message = "Invalid checklist ID" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.DeleteAsync($"{backendUrl}{ApiEndpoints.Lookup.Delete(model.id)}?type={ApiEndpoints.Lookup.Types.WorkRequestAdditionalInformation}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Important checklist item deleted successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to delete important checklist. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to delete important checklist item" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting important checklist");
                return Json(new { success = false, message = "Error deleting important checklist item" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateImportantChecklistOrder([FromBody] ImportantChecklistUpdateOrderRequest request)
        {
            try
            {
                if (request.items == null || !request.items.Any())
                {
                    return Json(new { success = false, message = "No items to update" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{backendUrl}{ApiEndpoints.Lookup.UpdateOrder}?type={ApiEndpoints.Lookup.Types.WorkRequestAdditionalInformation}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Checklist order updated successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update checklist order. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to update checklist order" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating checklist order");
                return Json(new { success = false, message = "Error updating checklist order" });
            }
        }

        #endregion

        #region Related Document

        public IActionResult RelatedDocument()
        {
            ViewBag.Title = "Related Document";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/RelatedDocument.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetRelatedDocuments()
        {
            try
            {
                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Lookup.List}?type={ApiEndpoints.Lookup.Types.WorkRequestDocument}&idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var documents = await JsonSerializer.DeserializeAsync<List<RelatedDocumentModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = documents });
                }

                return Json(new { success = false, message = "Failed to load related documents" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching related documents");
                return Json(new { success = false, message = "Error loading related documents" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRelatedDocument([FromBody] RelatedDocumentModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.name))
                {
                    return Json(new { success = false, message = "Document name is required" });
                }

                // Get user session for idClient
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Set label to name if not provided
                if (string.IsNullOrWhiteSpace(model.label))
                {
                    model.label = model.name;
                }

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Lookup.Create}?type={ApiEndpoints.Lookup.Types.WorkRequestDocument}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Related document created successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to create related document. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to create related document" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating related document");
                return Json(new { success = false, message = "Error creating related document" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRelatedDocument([FromBody] RelatedDocumentModel model)
        {
            try
            {
                if (model.id <= 0)
                {
                    return Json(new { success = false, message = "Invalid document ID" });
                }

                if (string.IsNullOrWhiteSpace(model.name))
                {
                    return Json(new { success = false, message = "Document name is required" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Set label to name if not provided
                if (string.IsNullOrWhiteSpace(model.label))
                {
                    model.label = model.name;
                }

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{backendUrl}{ApiEndpoints.Lookup.Update}?type={ApiEndpoints.Lookup.Types.WorkRequestDocument}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Related document updated successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update related document. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to update related document" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating related document");
                return Json(new { success = false, message = "Error updating related document" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteRelatedDocument([FromBody] RelatedDocumentModel model)
        {
            try
            {
                if (model.id <= 0)
                {
                    return Json(new { success = false, message = "Invalid document ID" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.DeleteAsync($"{backendUrl}{ApiEndpoints.Lookup.Delete(model.id)}?type={ApiEndpoints.Lookup.Types.WorkRequestDocument}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Related document deleted successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to delete related document. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to delete related document" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting related document");
                return Json(new { success = false, message = "Error deleting related document" });
            }
        }

        #endregion

        #region Generic CRUD Helpers

        private async Task<IActionResult> GetCategoriesGeneric(string endpoint, string entityName)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.GenericCategory.List(endpoint)}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var categories = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = categories });
                }

                return Json(new { success = false, message = $"Failed to load {entityName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching {entityName}");
                return Json(new { success = false, message = $"Error loading {entityName}" });
            }
        }

        private async Task<IActionResult> CreateCategoryGeneric(dynamic model, string endpoint, string entityName)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.GenericCategory.Create(endpoint)}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = $"{char.ToUpper(entityName[0]) + entityName.Substring(1)} created successfully" });
                }

                return Json(new { success = false, message = $"Failed to create {entityName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating {entityName}");
                return Json(new { success = false, message = $"Error creating {entityName}" });
            }
        }

        private async Task<IActionResult> UpdateCategoryGeneric(dynamic model, string endpoint, string entityName)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{backendUrl}{ApiEndpoints.GenericCategory.Update(endpoint)}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = $"{char.ToUpper(entityName[0]) + entityName.Substring(1)} updated successfully" });
                }

                return Json(new { success = false, message = $"Failed to update {entityName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating {entityName}");
                return Json(new { success = false, message = $"Error updating {entityName}" });
            }
        }

        private async Task<IActionResult> DeleteCategoryGeneric(int id, string endpoint, string entityName)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid ID" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.DeleteAsync($"{backendUrl}{ApiEndpoints.GenericCategory.Delete(endpoint, id)}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = $"{char.ToUpper(entityName[0]) + entityName.Substring(1)} deleted successfully" });
                }

                return Json(new { success = false, message = $"Failed to delete {entityName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting {entityName}");
                return Json(new { success = false, message = $"Error deleting {entityName}" });
            }
        }

        #endregion

        #region Person in Charge

        public IActionResult PersonInCharge()
        {
            ViewBag.Title = "Person in Charge";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/PersonInCharge.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonsInCharge()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Settings.PersonInCharge.List}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var data = await JsonSerializer.DeserializeAsync<List<object>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data });
                }

                return Json(new { success = false, message = "Failed to load persons in charge" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading persons in charge");
                return Json(new { success = false, message = "An error occurred while loading persons in charge" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonInChargeById(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Settings.PersonInCharge.GetById(id)}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var data = await JsonSerializer.DeserializeAsync<object>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data });
                }

                return Json(new { success = false, message = "Failed to load person in charge details" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading person in charge details");
                return Json(new { success = false, message = "An error occurred while loading person in charge details" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProperties()
        {
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Settings.Properties}?idClient={userInfo.PreferredClientId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var data = await JsonSerializer.DeserializeAsync<List<object>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data });
                }

                return Json(new { success = false, message = "Failed to load properties" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading properties");
                return Json(new { success = false, message = "An error occurred while loading properties" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePersonInCharge([FromBody] dynamic model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Settings.PersonInCharge.Create}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Person in charge added successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Failed to add person in charge: {errorContent}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating person in charge");
                return Json(new { success = false, message = "An error occurred while adding person in charge" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdatePersonInCharge([FromBody] dynamic model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"{backendUrl}{ApiEndpoints.Settings.PersonInCharge.Update}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Person in charge updated successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Failed to update person in charge: {errorContent}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating person in charge");
                return Json(new { success = false, message = "An error occurred while updating person in charge" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePersonInCharge([FromBody] dynamic model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var id = ((JsonElement)model.GetProperty("id")).GetInt32();

                var response = await client.DeleteAsync($"{backendUrl}{ApiEndpoints.Settings.PersonInCharge.Delete(id)}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Person in charge deleted successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Failed to delete person in charge: {errorContent}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting person in charge");
                return Json(new { success = false, message = "An error occurred while deleting person in charge" });
            }
        }

        #endregion

        #region Cost Approver Group

        public IActionResult CostApprover()
        {
            ViewBag.Title = "Cost Approver Group";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/CostApprover.cshtml");
        }

        public IActionResult CostApproverAdd()
        {
            ViewBag.Title = "Add Cost Approver Group";
            ViewBag.pTitle = "Cost Approver Group";
            ViewBag.pTitleUrl = Url.Action("CostApprover", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/CostApproverAdd.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetCostApproverGroups()
        {
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Settings.CostApproverGroup.List}?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var costApproverGroups = await JsonSerializer.DeserializeAsync<List<object>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = costApproverGroups });
                }

                return Json(new { success = false, message = "Failed to load cost approver groups" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cost approver groups");
                return Json(new { success = false, message = "Error loading cost approver groups" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCostApproverGroup([FromBody] dynamic model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Settings.CostApproverGroup.Create}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Cost approver group created successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Failed to create cost approver group: {errorContent}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cost approver group");
                return Json(new { success = false, message = "An error occurred while creating cost approver group" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCostApproverGroup([FromBody] dynamic model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var id = ((JsonElement)model.GetProperty("id")).GetInt32();

                var response = await client.DeleteAsync($"{backendUrl}{ApiEndpoints.Settings.CostApproverGroup.Delete(id)}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Cost approver group deleted successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Failed to delete cost approver group: {errorContent}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cost approver group");
                return Json(new { success = false, message = "An error occurred while deleting cost approver group" });
            }
        }

        #region Email Distribution List Management

        /// <summary>
        /// GET: Email Distribution List management page
        /// </summary>
        [HttpGet]
        public IActionResult EmailDistributionList()
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Email Distribution List Management";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");

            return View("~/Views/Helpdesk/Settings/EmailDistributionList.cshtml");
        }

        /// <summary>
        /// API: Get all email distribution page references with status
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEmailDistributionList()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.EmailDistribution.GetPageReferences}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var data = await JsonSerializer.DeserializeAsync<List<cfm_frontend.DTOs.EmailDistribution.EmailDistributionReferenceModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data });
                }

                _logger.LogError("Failed to load email distribution list. Status: {StatusCode}", response.StatusCode);
                return Json(new { success = false, message = "Failed to load email distribution list" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading email distribution list");
                return Json(new { success = false, message = "An error occurred while loading the data" });
            }
        }

        /// <summary>
        /// GET: Setup new email distribution list page
        /// </summary>
        [HttpGet]
        public IActionResult EmailDistributionListSetup(string pageReference)
        {
            var accessCheck = this.CheckAddAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            if (string.IsNullOrWhiteSpace(pageReference))
            {
                TempData["ErrorMessage"] = "Invalid page reference";
                return RedirectToAction("EmailDistributionList");
            }

            ViewBag.Title = "Set Up Email Distribution";
            ViewBag.pTitle = "Email Distribution List Management";
            ViewBag.pTitleUrl = Url.Action("EmailDistributionList", "Helpdesk");
            ViewBag.PageReference = pageReference;
            ViewBag.Mode = "setup";

            return View("~/Views/Helpdesk/Settings/EmailDistributionListDetail.cshtml");
        }

        /// <summary>
        /// GET: Edit existing email distribution list page
        /// </summary>
        [HttpGet]
        public IActionResult EmailDistributionListEdit(int id)
        {
            var accessCheck = this.CheckEditAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Edit Email Distribution";
            ViewBag.pTitle = "Email Distribution List Management";
            ViewBag.pTitleUrl = Url.Action("EmailDistributionList", "Helpdesk");
            ViewBag.DistributionListId = id;
            ViewBag.Mode = "edit";

            return View("~/Views/Helpdesk/Settings/EmailDistributionListDetail.cshtml");
        }

        /// <summary>
        /// API: Get email distribution by ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEmailDistributionById(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.EmailDistribution.GetById(id)}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var data = await JsonSerializer.DeserializeAsync<cfm_frontend.DTOs.EmailDistribution.EmailDistributionDetailModel>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data });
                }

                _logger.LogError("Failed to load email distribution. ID: {Id}, Status: {StatusCode}", id, response.StatusCode);
                return Json(new { success = false, message = "Failed to load email distribution details" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading email distribution. ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while loading the data" });
            }
        }

        /// <summary>
        /// API: Create new email distribution
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateEmailDistribution([FromBody] cfm_frontend.DTOs.EmailDistribution.EmailDistributionDetailModel model)
        {
            try
            {
                var accessCheck = this.CheckAddAccess("Helpdesk", "Settings");
                if (accessCheck != null)
                    return Json(new { success = false, message = "You do not have permission to create" });

                if (string.IsNullOrWhiteSpace(model.PageReference))
                {
                    return Json(new { success = false, message = "Page reference is required" });
                }

                if (model.Recipients == null || !model.Recipients.Any(r => r.Type == "TO"))
                {
                    return Json(new { success = false, message = "At least one 'To' recipient is required" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.EmailDistribution.Create}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Email distribution created successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create email distribution. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to create email distribution" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email distribution");
                return Json(new { success = false, message = "An error occurred while creating" });
            }
        }

        /// <summary>
        /// API: Update email distribution
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateEmailDistribution([FromBody] cfm_frontend.DTOs.EmailDistribution.EmailDistributionDetailModel model, int id)
        {
            try
            {
                var accessCheck = this.CheckEditAccess("Helpdesk", "Settings");
                if (accessCheck != null)
                    return Json(new { success = false, message = "You do not have permission to update" });

                if (model.Recipients == null || !model.Recipients.Any(r => r.Type == "TO"))
                {
                    return Json(new { success = false, message = "At least one 'To' recipient is required" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{backendUrl}{ApiEndpoints.EmailDistribution.Update(id)}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Email distribution updated successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update email distribution. ID: {Id}, Status: {StatusCode}, Error: {Error}",
                    id, response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to update email distribution" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email distribution. ID: {Id}", id);
                return Json(new { success = false, message = "An error occurred while updating" });
            }
        }

        /// <summary>
        /// API: Delete email distribution
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteEmailDistribution([FromBody] dynamic model)
        {
            try
            {
                var accessCheck = this.CheckDeleteAccess("Helpdesk", "Settings");
                if (accessCheck != null)
                    return Json(new { success = false, message = "You do not have permission to delete" });

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var id = ((JsonElement)model.GetProperty("id")).GetInt32();

                var response = await client.DeleteAsync($"{backendUrl}{ApiEndpoints.EmailDistribution.Delete(id)}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Email distribution deleted successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete email distribution. ID: {Id}, Status: {StatusCode}, Error: {Error}",
                    id, response.StatusCode, errorContent);

                return Json(new { success = false, message = "Failed to delete email distribution" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting email distribution");
                return Json(new { success = false, message = "An error occurred while deleting" });
            }
        }

        #endregion

        #endregion

        #endregion



        #region Other Helpdesk Views 

        public IActionResult CourseCourseAdd()
        {
            return View();
        }

        public IActionResult CourseCourseView()
        {
            return View();
        }

        public IActionResult CourseDashboard()
        {
            return View();
        }

        public IActionResult CoursePricing()
        {
            return View();
        }

        public IActionResult CourseSettingNotifications()
        {
            return View();
        }

        public IActionResult CourseSettingPayment()
        {
            return View();
        }

        public IActionResult CourseSettingPricing()
        {
            return View();
        }

        public IActionResult CourseSite()
        {
            return View();
        }

        public IActionResult CourseStudentAdd()
        {
            return View();
        }

        public IActionResult CourseStudentApply()
        {
            return View();
        }

        public IActionResult CourseStudentList()
        {
            return View();
        }

        public IActionResult CourseTeacherAdd()
        {
            return View();
        }

        public IActionResult CourseTeacherApply()
        {
            return View();
        }

        public IActionResult CourseTeacherList()
        {
            return View();
        }

        public IActionResult HelpdeskCreateTicket()
        {
            return View();
        }

        public IActionResult HelpdeskCustomer()
        {
            return View();
        }

        public IActionResult WorkRequestDashboard()
        {
            return View();
        }

        public IActionResult HelpdeskTicketDetails()
        {
            return View();
        }

        public IActionResult HelpdeskTicket()
        {
            return View();
        }

        public IActionResult InvoiceCreate()
        {
            return View();
        }

        public IActionResult InvoiceDashboard()
        {
            return View();
        }

        public IActionResult InvoiceEdit()
        {
            return View();
        }

        public IActionResult InvoiceList()
        {
            return View();
        }

        public IActionResult InvoiceView()
        {
            return View();
        }

        public IActionResult MembershipDashboard()
        {
            return View();
        }

        public IActionResult MembershipList()
        {
            return View();
        }

        public IActionResult MembershipPricing()
        {
            return View();
        }

        public IActionResult MembershipSetting()
        {
            return View();
        }

        #endregion
    }
}