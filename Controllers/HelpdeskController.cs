using cfm_frontend.Controllers;
using cfm_frontend.Models;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static cfm_frontend.Models.WorkRequestFilterModel;
namespace Mvc.Controllers
{
    public class HelpdeskController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        public HelpdeskController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<LoginController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }
        // GET: Helpdesk
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
        //Work Request Management List page
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var viewmodel = new WorkRequestViewModel();

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // TODO: Get these from session/authentication
                var idClient = 1;
                var idActor = 1;
                var idEmployee = 1;

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

            return View(viewmodel);
        }

        public class PagedResult<T>
        {
            public List<T> Data { get; set; }
            public int TotalCount { get; set; }
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


        #region helper function
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
                    idActor = idActor,
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
                // Adjust endpoint 
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
                // Adjust endpoint 
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
                // Adjust endpoint 
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
                // Adjust endpoint 
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
                // Adjust endpoint 
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
                // Adjust endpoint 
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
    }
}