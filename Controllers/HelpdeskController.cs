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

                // TODO: Get these from session/authentication
                var idClient = 1; //get from sessioninfo
                var idActor = 1; //currently unused
                var idEmployee = 1; // get from sessioninfo

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

                // Get idClient from session
                var idClient = 1; // TODO: Get from session/authentication

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
            if (!ModelState.IsValid)
            {
                // Reload dropdown data
                var viewmodel = new WorkRequestViewModel();
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];
                var idClient = 1; // TODO: Get from session

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
                model.IdClient = 1; // TODO: Get from session/authentication
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
            var failIdClient = 1; // TODO: Get from session

            failViewModel.Locations = await GetLocationsAsync(failClient, failBackendUrl, failIdClient);
            failViewModel.ServiceProviders = await GetServiceProvidersAsync(failClient, failBackendUrl, failIdClient);
            failViewModel.WorkCategories = await GetWorkCategoriesAsync(failClient, failBackendUrl);
            failViewModel.OtherCategories = await GetOtherCategoriesAsync(failClient, failBackendUrl);

            return View("~/Views/Helpdesk/WorkRequest/WorkRequestAdd.cshtml", failViewModel);
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