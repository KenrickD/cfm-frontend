using cfm_frontend.DTOs.WorkRequest;
using cfm_frontend.Models;
using cfm_frontend.Models.WorkRequest;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using static cfm_frontend.Models.WorkRequest.WorkRequestFilterModel;

namespace Mvc.Controllers
{
    public class HelpdeskController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HelpdeskController> _logger;

        public HelpdeskController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<HelpdeskController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        #region Work Request Management

        /// <summary>
        /// GET: Work Request List page
        /// </summary>
        //[Authorize]
        public async Task<IActionResult> Index(int page = 0, int pageSize = 50)
        {
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
                var idActor = 1; //currently unused
                var idEmployee = userInfo.UserId;

                // Load all data in parallel for better performance
                var workRequestTask = GetWorkRequestsAsync(client, backendUrl, page, pageSize, idClient, idActor, idEmployee);
                var propertyGroupsTask = GetPropertyGroupsAsync(client, backendUrl, idClient);
                var statusesTask = GetStatusesAsync(client, backendUrl);
                var locationsTask = GetLocationsAsync(client, backendUrl, idClient);
                var serviceProvidersTask = GetServiceProvidersAsync(client, backendUrl, idClient);
                var workCategoriesTask = GetWorkCategoriesAsync(client, backendUrl);
                var otherCategoriesTask = GetOtherCategoriesAsync(client, backendUrl);

                // Wait for all tasks to complete
                await Task.WhenAll(
                    workRequestTask,
                    propertyGroupsTask,
                    statusesTask,
                    locationsTask,
                    serviceProvidersTask,
                    workCategoriesTask,
                    otherCategoriesTask
                );

                // Populate ViewModel with results
                var workRequestResponse = await workRequestTask;
                if (workRequestResponse != null)
                {
                    viewmodel.WorkRequest = workRequestResponse.data;
                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = workRequestResponse.currentPage,
                        TotalPages = workRequestResponse.totalPages,
                        PageSize = workRequestResponse.pageSize,
                        TotalRecords = workRequestResponse.totalRecords
                    };
                }

                viewmodel.PropertyGroups = await propertyGroupsTask;
                viewmodel.Status = await statusesTask;
                viewmodel.Locations = await locationsTask;
                viewmodel.ServiceProviders = await serviceProvidersTask;
                viewmodel.WorkCategories = await workCategoriesTask;
                viewmodel.OtherCategories = await otherCategoriesTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work request index page");
                viewmodel.PropertyGroups = new List<PropertyGroupModel>();
                viewmodel.Status = new List<WRStatusModel>();
                viewmodel.Locations = new List<LocationModel>();
                viewmodel.ServiceProviders = new List<ServiceProviderModel>();
                viewmodel.WorkCategories = new List<WorkCategoryModel>();
                viewmodel.OtherCategories = new List<OtherCategoryModel>();
            }

            return View("~/Views/Helpdesk/WorkRequest/Index.cshtml", viewmodel);
        }

        /// <summary>
        /// GET: Work Request Add page
        /// </summary>
        //[Authorize]
        public async Task<IActionResult> WorkRequestAdd()
        {
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
                var serviceProvidersTask = GetServiceProvidersAsync(client, backendUrl, idClient);
                var workCategoriesTask = GetWorkCategoriesAsync(client, backendUrl);
                var otherCategoriesTask = GetOtherCategoriesAsync(client, backendUrl);

                await Task.WhenAll(
                    locationsTask,
                    serviceProvidersTask,
                    workCategoriesTask,
                    otherCategoriesTask
                );

                viewmodel.Locations = await locationsTask;
                viewmodel.ServiceProviders = await serviceProvidersTask;
                viewmodel.WorkCategories = await workCategoriesTask;
                viewmodel.OtherCategories = await otherCategoriesTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work request add page");
                viewmodel.Locations = new List<LocationModel>();
                viewmodel.ServiceProviders = new List<ServiceProviderModel>();
                viewmodel.WorkCategories = new List<WorkCategoryModel>();
                viewmodel.OtherCategories = new List<OtherCategoryModel>();
            }

            return View("~/Views/Helpdesk/WorkRequest/WorkRequestAdd.cshtml", viewmodel);
        }

        /// <summary>
        /// POST: Create new Work Request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize]
        public async Task<IActionResult> WorkRequestAdd(WorkRequestCreateRequest model)
        {
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
                // Reload dropdown data
                var viewmodel = new WorkRequestViewModel();
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];
                

                viewmodel.Locations = await GetLocationsAsync(client, backendUrl, idClient);
                viewmodel.ServiceProviders = await GetServiceProvidersAsync(client, backendUrl, idClient);
                viewmodel.WorkCategories = await GetWorkCategoriesAsync(client, backendUrl);
                viewmodel.OtherCategories = await GetOtherCategoriesAsync(client, backendUrl);

                return View("~/Views/Helpdesk/WorkRequest/WorkRequestAdd.cshtml", viewmodel);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Set system fields from session
                model.IdClient = idClient; 
                model.IdEmployee = 1; // TODO: Get from session/authentication

                // Serialize and send to backend
                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}/api/workrequest/create", content);

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

            // If we got here, something failed, reload the form
            var failViewModel = new WorkRequestViewModel();
            var failClient = _httpClientFactory.CreateClient("BackendAPI");
            var failBackendUrl = _configuration["BackendBaseUrl"];
            var failIdClient = idClient; 

            failViewModel.Locations = await GetLocationsAsync(failClient, failBackendUrl, failIdClient);
            failViewModel.ServiceProviders = await GetServiceProvidersAsync(failClient, failBackendUrl, failIdClient);
            failViewModel.WorkCategories = await GetWorkCategoriesAsync(failClient, failBackendUrl);
            failViewModel.OtherCategories = await GetOtherCategoriesAsync(failClient, failBackendUrl);

            return View("~/Views/Helpdesk/WorkRequest/WorkRequestAdd.cshtml", failViewModel);
        }

        /// <summary>
        /// GET: Send New Work Request page 
        /// </summary>
        //[Authorize]
        public async Task<IActionResult> SendNewWorkRequest()
        {
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
                    IdClient = userInfo.PreferredClientId,
                    IdEmployee = userInfo.UserId,
                    IdLocation = IdLocation,
                    IdFloor = IdFloor,
                    IdRoom = IdRoom,
                    IdWorkCategory = IdWorkCategory,
                    RequestDetail = RequestDetail,
                    WorkTitle = $"Work Request - {RequestDetail.Substring(0, Math.Min(50, RequestDetail.Length))}...",
                    RequestMethod = "Web",
                    Status = "New",
                    PriorityLevel = "Medium", 
                    RequestDate = DateTime.UtcNow,
                    IdRequestor = userInfo.UserId,
                    RequestorName = userInfo.FullName
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

                var response = await client.PostAsync($"{backendUrl}/api/workrequest/create", content);

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
        public async Task<IActionResult> WorkRequestDetail(int id)
        {
            var viewmodel = new WorkRequestDetailViewModel();

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

                // TODO: Replace with actual API endpoints when backend is ready
                // For now, load dropdown data and create empty change history
                var locationsTask = GetLocationsAsync(client, backendUrl, idClient);
                var serviceProvidersTask = GetServiceProvidersAsync(client, backendUrl, idClient);
                var workCategoriesTask = GetWorkCategoriesAsync(client, backendUrl);
                var otherCategoriesTask = GetOtherCategoriesAsync(client, backendUrl);

                await Task.WhenAll(
                    locationsTask,
                    serviceProvidersTask,
                    workCategoriesTask,
                    otherCategoriesTask
                );

                viewmodel.Locations = await locationsTask;
                viewmodel.ServiceProviders = await serviceProvidersTask;
                viewmodel.WorkCategories = await workCategoriesTask;
                viewmodel.OtherCategories = await otherCategoriesTask;

                // TODO: When backend is ready, fetch work request detail
                // var workRequestResponse = await client.GetAsync($"{backendUrl}/api/workrequest/{id}");
                // var changeHistoryResponse = await client.GetAsync($"{backendUrl}/api/workrequest/{id}/changehistory");

                // For now, initialize empty change history list
                viewmodel.ChangeHistories = new List<ChangeHistory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work request detail page");
                viewmodel.Locations = new List<LocationModel>();
                viewmodel.ServiceProviders = new List<ServiceProviderModel>();
                viewmodel.WorkCategories = new List<WorkCategoryModel>();
                viewmodel.OtherCategories = new List<OtherCategoryModel>();
                viewmodel.ChangeHistories = new List<ChangeHistory>();
            }

            return View("~/Views/Helpdesk/WorkRequest/WorkRequestDetail.cshtml", viewmodel);
        }

        #endregion

        #region API Endpoints for Dynamic Data Loading

        /// <summary>
        /// API: Get floors by location ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFloorsByLocation(int locationId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}/api/floor/list?locationId={locationId}");

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
                _logger.LogError(ex, "Error fetching floors for location {LocationId}", locationId);
                return Json(new { success = false, message = "Error loading floors" });
            }
        }

        /// <summary>
        /// API: Get rooms by floor ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRoomsByFloor(int floorId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}/api/room/list?floorId={floorId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var rooms = await JsonSerializer.DeserializeAsync<List<RoomModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = rooms });
                }

                return Json(new { success = false, message = "Failed to load rooms" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching rooms for floor {FloorId}", floorId);
                return Json(new { success = false, message = "Error loading rooms" });
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
                    $"{backendUrl}/api/employee/persons-in-charge?idClient={idClient}"
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
                var userId = userInfo.UserId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}/api/location/list?idClient={idClient}&userId={userId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var locations = await JsonSerializer.DeserializeAsync<List<LocationModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = locations });
                }

                return Json(new { success = false, message = "Failed to load locations" });
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

                var url = $"{backendUrl}/api/employee/persons-in-charge?idClient={idClient}";
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
                    $"{backendUrl}/api/employee/search-requestors?term={Uri.EscapeDataString(term)}&idCompany={idCompany}"
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
        /// API: Get work request methods from lookup table
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkRequestMethods()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}/api/lookup/list?type=workRequestMethod");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var methods = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = methods });
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
        /// API: Get work request statuses from lookup table
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkRequestStatuses()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}/api/lookup/list?type=workRequestStatus");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var statuses = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = statuses });
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

                var response = await client.GetAsync($"{backendUrl}/api/serviceprovider/list?idClient={idClient}&idCompany={idCompany}");

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
        /// API: Get important checklist items for work request
        /// idClient is retrieved from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetImportantChecklist()
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
                    $"{backendUrl}/api/lookup/list?type=workRequestAdditionalInformation&idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var checklist = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = checklist });
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
        /// API: Get work categories by category type
        /// idClient is retrieved from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkCategoriesByClient(string categoryType = "workCategory")
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
                    $"{backendUrl}/api/workcategory/list?idClient={idClient}&categoryType={categoryType}"
                );

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
        /// API: Get other categories by category type
        /// idClient is retrieved from session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOtherCategoriesByClient(string categoryType)
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
                    $"{backendUrl}/api/othercategory/list?idClient={idClient}&categoryType={categoryType}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var categories = await JsonSerializer.DeserializeAsync<List<OtherCategoryModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = categories });
                }

                return Json(new { success = false, message = "Failed to load other categories" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching other categories for type {CategoryType}", categoryType);
                return Json(new { success = false, message = "Error loading other categories" });
            }
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
                    $"{backendUrl}/api/lookup/list?type=workRequestPriorityLevel&idClient={idClient}"
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
        /// API: Get feedback types from lookup table
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFeedbackTypes()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}/api/lookup/list?type=workRequestFeedbackType"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var feedbackTypes = await JsonSerializer.DeserializeAsync<List<dynamic>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Json(new { success = true, data = feedbackTypes });
                }

                return Json(new { success = false, message = "Failed to load feedback types" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching feedback types");
                return Json(new { success = false, message = "Error loading feedback types" });
            }
        }

        #endregion

        #region Helper Functions 

        private async Task<WorkRequestListApiResponse?> GetWorkRequestsAsync(
            HttpClient client,
            string backendUrl,
            int page,
            int pageSize,
            int idClient,
            int idActor,
            int idEmployee)
        {
            try
            {
                var requestBody = new WorkRequestBodyModel
                {
                    idClient = idClient,
                    idEmployee = idEmployee,
                    idStatus = 0,
                    fromDate = string.Empty,
                    toDate = string.Empty,
                    filterWorkCategory = string.Empty,
                    filterLocation = string.Empty,
                    filterStatus = 0
                };

                var jsonPayload = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    $"{backendUrl}/api/workrequest/list?page={page}&pageSize={pageSize}",
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

        private async Task<List<PropertyGroupModel>> GetPropertyGroupsAsync(HttpClient client, string backendUrl, int idClient)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}/api/propertygroup/list?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<List<PropertyGroupModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return result ?? new List<PropertyGroupModel>();
                }

                _logger.LogWarning("Property Groups API returned status: {StatusCode}", response.StatusCode);
                return new List<PropertyGroupModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching property groups");
                return new List<PropertyGroupModel>();
            }
        }

        private async Task<List<WRStatusModel>> GetStatusesAsync(HttpClient client, string backendUrl)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}/api/workrequest/statuses");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<List<WRStatusModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return result ?? new List<WRStatusModel>();
                }

                _logger.LogWarning("Statuses API returned status: {StatusCode}", response.StatusCode);
                return new List<WRStatusModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching statuses");
                return new List<WRStatusModel>();
            }
        }

        private async Task<List<LocationModel>> GetLocationsAsync(HttpClient client, string backendUrl, int idClient)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}/api/location/list?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<List<LocationModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return result ?? new List<LocationModel>();
                }

                _logger.LogWarning("Locations API returned status: {StatusCode}", response.StatusCode);
                return new List<LocationModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching locations");
                return new List<LocationModel>();
            }
        }

        private async Task<List<ServiceProviderModel>> GetServiceProvidersAsync(HttpClient client, string backendUrl, int idClient)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}/api/serviceprovider/list?idClient={idClient}");

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
                var response = await client.GetAsync($"{backendUrl}/api/workcategory/list");

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
                var response = await client.GetAsync($"{backendUrl}/api/othercategory/list");

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

        #region Settings

        /// <summary>
        /// GET: Settings Hub page
        /// </summary>
        //[Authorize]
        public IActionResult Settings()
        {
            return View("~/Views/Helpdesk/Settings/Index.cshtml");
        }

        /// <summary>
        /// GET: Work Category Settings page
        /// </summary>
        //[Authorize]
        public IActionResult WorkCategory()
        {
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

                var response = await client.GetAsync($"{backendUrl}/api/workcategory/list");

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

                var response = await client.PostAsync($"{backendUrl}/api/workcategory/create", content);

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

                var response = await client.PutAsync($"{backendUrl}/api/workcategory/update", content);

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

                var response = await client.DeleteAsync($"{backendUrl}/api/workcategory/delete/{model.id}");

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

        public IActionResult OtherCategory() => View("~/Views/Helpdesk/Settings/OtherCategory.cshtml");

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

        public IActionResult OtherCategory2() => View("~/Views/Helpdesk/Settings/OtherCategory2.cshtml");

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

        public IActionResult JobCodeGroup() => View("~/Views/Helpdesk/Settings/JobCodeGroup.cshtml");

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

        public IActionResult MaterialType() => View("~/Views/Helpdesk/Settings/MaterialType.cshtml");

        [HttpGet]
        public async Task<IActionResult> GetMaterialTypes() => await GetCategoriesGeneric("materialtype", "material types");

        [HttpPost]
        public async Task<IActionResult> CreateMaterialType([FromBody] dynamic model) => await CreateCategoryGeneric(model, "materialtype", "material type");

        [HttpPut]
        public async Task<IActionResult> UpdateMaterialType([FromBody] dynamic model) => await UpdateCategoryGeneric(model, "materialtype", "material type");

        [HttpDelete]
        public async Task<IActionResult> DeleteMaterialType([FromBody] dynamic model) => await DeleteCategoryGeneric((int)model.id, "materialtype", "material type");

        #endregion

        #region Generic CRUD Helpers

        private async Task<IActionResult> GetCategoriesGeneric(string endpoint, string entityName)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}/api/{endpoint}/list");

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

                var response = await client.PostAsync($"{backendUrl}/api/{endpoint}/create", content);

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

                var response = await client.PutAsync($"{backendUrl}/api/{endpoint}/update", content);

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

                var response = await client.DeleteAsync($"{backendUrl}/api/{endpoint}/delete/{id}");

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
            return View("~/Views/Helpdesk/Settings/PersonInCharge.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonsInCharge()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync($"{backendUrl}/api/settings/persons-in-charge");

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

                var response = await client.GetAsync($"{backendUrl}/api/settings/persons-in-charge/{id}");

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

                var response = await client.GetAsync($"{backendUrl}/api/settings/properties?idClient={userInfo.PreferredClientId}");

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
                var response = await client.PostAsync($"{backendUrl}/api/settings/persons-in-charge", content);

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
                var response = await client.PutAsync($"{backendUrl}/api/settings/persons-in-charge", content);

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

                var response = await client.DeleteAsync($"{backendUrl}/api/settings/persons-in-charge/{id}");

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