using cfm_frontend.Constants;
using cfm_frontend.DTOs;
using cfm_frontend.DTOs.Employee;
using cfm_frontend.DTOs.PIC;
using cfm_frontend.DTOs.PriorityLevel;
using cfm_frontend.DTOs.ServiceProvider;
using cfm_frontend.DTOs.TypeSettings;
using cfm_frontend.DTOs.WorkCategory;
using cfm_frontend.DTOs.WorkRequest;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using cfm_frontend.Models.Asset;
using cfm_frontend.Models.WorkRequest;
using cfm_frontend.Services;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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
        private readonly IFileLoggerService _fileLogger;

        public HelpdeskController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<HelpdeskController> logger,
            IPrivilegeService privilegeService,
            IFileLoggerService fileLogger)
            : base(privilegeService, logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _fileLogger = fileLogger;
        }

        #region Work Request Management

        /// <summary>
        /// GET: Work Request List page
        /// </summary>
        [Authorize]
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
            List<string>? requestMethods = null,
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

                // Build request body with nested filter structure for new API
                var requestBody = new cfm_frontend.Models.WorkRequest.WorkRequestListParam
                {
                    Client_idClient = idClient,
                    page = page,
                    keywords = search,
                    filter = new cfm_frontend.Models.WorkRequest.WorkRequestList_Filter
                    {
                        idPropertyType = propertyGroup ?? -1,
                        idRoomZone = roomZone ?? -1,
                        showDeletedData = showDeleted,
                        requestDateFrom = requestDateFrom,
                        requestDateTo = requestDateTo,
                        workCompletionFrom = workCompletionDateFrom,
                        workCompletionTo = workCompletionDateTo,
                        hasEmailSent = hasSentEmail ?? false,
                        isActiveData = true,
                        locations = locations?.ToArray() ?? Array.Empty<int>(),
                        serviceProviders = serviceProviders?.ToArray() ?? Array.Empty<int>(),
                        workCategories = workCategories?.ToArray() ?? Array.Empty<int>(),
                        otherCategories = otherCategories?.ToArray() ?? Array.Empty<int>(),
                        otherCategories2 = Array.Empty<int>(),
                        priorityLevels = priorities?.Select(int.Parse).ToArray() ?? Array.Empty<int>(),
                        statuses = statuses?.Select(int.Parse).ToArray() ?? Array.Empty<int>(),
                        importantChecklists = checklist?.Select(int.Parse).ToArray() ?? Array.Empty<int>(),
                        feedbackTypes = feedback?.Select(int.Parse).ToArray() ?? Array.Empty<int>(),
                        requestMethods = requestMethods?.Select(int.Parse).ToArray() ?? Array.Empty<int>()
                    }
                };

                // Load work requests and filter options in parallel
                var workRequestTask = GetWorkRequestsAsync(client, backendUrl, requestBody);
                var filterOptionsTask = GetFilterOptionsAsync(client, backendUrl, idClient, search);

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
        /// Pre-loads all static dropdown data server-side to eliminate concurrent API calls.
        /// Includes comprehensive timing and failure logging for diagnostics.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> WorkRequestAdd()
        {
            var totalStopwatch = Stopwatch.StartNew();
            var apiTimingResults = new List<ApiTimingResult>();

            // Check if user has permission to view Work Request Add page
            var accessCheck = this.CheckViewAccess("Helpdesk", "Work Request Management");
            if (accessCheck != null) return accessCheck;

            // Check if user is authenticated (has valid session)
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                _logger.LogWarning("User not authenticated, redirecting to login");
                _fileLogger.LogWarning("WorkRequestAdd: User not authenticated, redirecting to login", "AUTH");
                return RedirectToAction("Index", "Login");
            }

            var viewmodel = new WorkRequestViewModel();

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    _logger.LogWarning("User session not found, redirecting to login");
                    _fileLogger.LogWarning("WorkRequestAdd: User session not found, redirecting to login", "SESSION");
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(
                    userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null)
                    return RedirectToAction("Index", "Login");

                var idClient = userInfo.PreferredClientId;
                var idCompany = userInfo.IdCompany;

                _fileLogger.LogInfo($"WorkRequestAdd: Starting API calls for idClient={idClient}, idCompany={idCompany}", "PAGE-LOAD");

                // Create cancellation token with overall timeout for all parallel tasks (60 seconds)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var ct = cts.Token;

                // Pre-load all static dropdown data in parallel with timing
                var locationsTask = _fileLogger.ExecuteTimedAsync(
                    "Locations",
                    $"{ApiEndpoints.Property.List}?idClient={idClient}",
                    () => GetLocationsAsync(client, backendUrl, idClient, ct),
                    apiTimingResults);

                var workCategoriesTask = _fileLogger.ExecuteTimedAsync(
                    "WorkCategories",
                    $"{ApiEndpoints.Masters.GetTypes(ApiEndpoints.Masters.CategoryTypes.WorkCategory)}?idClient={idClient}",
                    () => GetWorkCategoriesByTypesAsync(client, backendUrl, idClient, ct),
                    apiTimingResults);

                var otherCategoriesTask = _fileLogger.ExecuteTimedAsync(
                    "OtherCategories",
                    $"{ApiEndpoints.Masters.GetTypes("workRequestCustomCategory")}?idClient={idClient}",
                    () => GetOtherCategoriesByTypeAsync(client, backendUrl, idClient, "workRequestCustomCategory", ct),
                    apiTimingResults);

                var otherCategories2Task = _fileLogger.ExecuteTimedAsync(
                    "OtherCategories2",
                    $"{ApiEndpoints.Masters.GetTypes("workRequestCustomCategory2")}?idClient={idClient}",
                    () => GetOtherCategoriesByTypeAsync(client, backendUrl, idClient, "workRequestCustomCategory2", ct),
                    apiTimingResults);

                var serviceProvidersTask = _fileLogger.ExecuteTimedAsync(
                    "ServiceProviders",
                    $"{ApiEndpoints.ServiceProvider.List}?idClient={idClient}&idCompany={idCompany}",
                    () => GetServiceProvidersAsync(client, backendUrl, idClient, idCompany, ct),
                    apiTimingResults);

                var priorityLevelsTask = _fileLogger.ExecuteTimedAsync(
                    "PriorityLevels",
                    $"{ApiEndpoints.PriorityLevelDetail.List}?idClient={idClient}",
                    () => GetPriorityLevelsWithDetailsAsync(client, backendUrl, idClient, ct),
                    apiTimingResults);

                var feedbackTypesTask = _fileLogger.ExecuteTimedAsync(
                    "FeedbackTypes",
                    ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestFeedbackType),
                    () => GetFeedbackTypesAsync(client, backendUrl, ct),
                    apiTimingResults);

                var currenciesTask = _fileLogger.ExecuteTimedAsync(
                    "Currencies",
                    ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.Currency),
                    () => FetchCurrenciesAsync(client, backendUrl, ct),
                    apiTimingResults);

                var requestMethodsTask = _fileLogger.ExecuteTimedAsync(
                    "RequestMethods",
                    ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestMethod),
                    () => GetRequestMethodsAsync(client, backendUrl, ct),
                    apiTimingResults);

                var statusesTask = _fileLogger.ExecuteTimedAsync(
                    "Statuses",
                    ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestStatus),
                    () => GetStatusesAsync(client, backendUrl, ct),
                    apiTimingResults);

                var checklistTask = _fileLogger.ExecuteTimedAsync(
                    "ImportantChecklist",
                    $"{ApiEndpoints.Masters.GetTypes(ApiEndpoints.Masters.CategoryTypes.WorkRequestAdditionalInformation)}?idClient={idClient}",
                    () => GetImportantChecklistAsync(client, backendUrl, idClient, ct),
                    apiTimingResults);

                // Wait for all tasks - use WhenAll to run in parallel
                try
                {
                    await Task.WhenAll(
                        locationsTask, workCategoriesTask, otherCategoriesTask, otherCategories2Task,
                        serviceProvidersTask, priorityLevelsTask, feedbackTypesTask, currenciesTask,
                        requestMethodsTask, statusesTask, checklistTask
                    );
                }
                catch (Exception taskEx)
                {
                    // Log but don't rethrow - we'll check each task individually below
                    _logger.LogWarning(taskEx, "One or more dropdown data tasks failed, will use partial results");
                }

                // Populate ViewModel - safely get result from each completed task
                viewmodel.Locations = locationsTask.IsCompletedSuccessfully ? locationsTask.Result : [];
                viewmodel.WorkCategories = workCategoriesTask.IsCompletedSuccessfully ? workCategoriesTask.Result : [];
                viewmodel.OtherCategories = otherCategoriesTask.IsCompletedSuccessfully ? otherCategoriesTask.Result : [];
                viewmodel.OtherCategories2 = otherCategories2Task.IsCompletedSuccessfully ? otherCategories2Task.Result : [];
                viewmodel.ServiceProviders = serviceProvidersTask.IsCompletedSuccessfully ? serviceProvidersTask.Result : [];
                viewmodel.PriorityLevels = priorityLevelsTask.IsCompletedSuccessfully ? priorityLevelsTask.Result : [];
                viewmodel.FeedbackTypes = feedbackTypesTask.IsCompletedSuccessfully ? feedbackTypesTask.Result : [];
                viewmodel.Currencies = currenciesTask.IsCompletedSuccessfully ? currenciesTask.Result : [];
                viewmodel.RequestMethods = requestMethodsTask.IsCompletedSuccessfully ? requestMethodsTask.Result : [];
                viewmodel.Statuses = statusesTask.IsCompletedSuccessfully ? statusesTask.Result : [];
                viewmodel.ImportantChecklist = checklistTask.IsCompletedSuccessfully ? checklistTask.Result : [];

                // Capture client context at page load for multi-tab session safety
                viewmodel.IdClient = idClient;
                viewmodel.IdCompany = idCompany;

                // Update record counts in timing results
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "Locations", viewmodel.Locations?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "WorkCategories", viewmodel.WorkCategories?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "OtherCategories", viewmodel.OtherCategories?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "OtherCategories2", viewmodel.OtherCategories2?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "ServiceProviders", viewmodel.ServiceProviders?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "PriorityLevels", viewmodel.PriorityLevels?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "FeedbackTypes", viewmodel.FeedbackTypes?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "Currencies", viewmodel.Currencies?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "RequestMethods", viewmodel.RequestMethods?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "Statuses", viewmodel.Statuses?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "ImportantChecklist", viewmodel.ImportantChecklist?.Count);

                _logger.LogInformation("Work Request Add page data loaded successfully: {LocationCount} locations, {CategoryCount} categories, {PriorityCount} priority levels",
                    viewmodel.Locations?.Count ?? 0,
                    viewmodel.WorkCategories?.Count ?? 0,
                    viewmodel.PriorityLevels?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work request add page data");
                _fileLogger.LogError("WorkRequestAdd: Critical error loading page data", ex, "PAGE-LOAD");
                viewmodel = new WorkRequestViewModel();
            }
            finally
            {
                totalStopwatch.Stop();

                // Log comprehensive timing summary to file
                _fileLogger.LogApiTimingBatch("WorkRequestAdd Page Load", apiTimingResults, totalStopwatch.Elapsed);

                // Also log summary to standard logger
                var failedApis = apiTimingResults.Where(r => !r.Success).ToList();
                if (failedApis.Any())
                {
                    _logger.LogWarning(
                        "WorkRequestAdd: {FailedCount}/{TotalCount} API calls failed. Total time: {TotalMs}ms. Failed APIs: {FailedApis}",
                        failedApis.Count,
                        apiTimingResults.Count,
                        totalStopwatch.ElapsedMilliseconds,
                        string.Join(", ", failedApis.Select(f => f.ApiName)));
                }
                else
                {
                    _logger.LogInformation(
                        "WorkRequestAdd: All {TotalCount} API calls succeeded. Total time: {TotalMs}ms",
                        apiTimingResults.Count,
                        totalStopwatch.ElapsedMilliseconds);
                }
            }

            return View("~/Views/Helpdesk/WorkRequest/WorkRequestAdd.cshtml", viewmodel);
        }

        /// <summary>
        /// POST: Create new Work Request
        /// Accepts JSON body from AJAX submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WorkRequestAdd([FromBody] WorkRequestCreateRequest model)
        {
            // Check if user has permission to add Work Requests
            var accessCheck = this.CheckAddAccess("Helpdesk", "Work Request Management");
            if (accessCheck != null)
            {
                return Json(new { success = false, message = "You do not have permission to add work requests." });
            }

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

            // Validate model
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid request data." });
            }

            // Check for client mismatch (multi-tab session safety)
            // Client_idClient is captured at page load - if it doesn't match current session, reject
            if (model.Client_idClient != idClient)
            {
                _logger.LogWarning(
                    "Work request submitted with client mismatch. SubmittedClientId: {SubmittedClientId}, SessionClientId: {SessionClientId}, UserId: {UserId}",
                    model.Client_idClient, idClient, userInfo.IdWebUser);

                return Json(new
                {
                    success = false,
                    message = "Client context has changed. You may have switched clients in another tab. Please refresh the page and try again.",
                    clientMismatch = true
                });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Set system fields from session
                // Note: Client_idClient is already set from page load (for multi-tab safety validation)
                model.IdEmployee = userInfo.IdWebUser;
                model.TimeZone_idTimeZone = userInfo.PreferredTimezoneIdTimezone;

                // Log the payload for debugging
                _logger.LogInformation("Creating work request: Title={Title}, Property={Property}, Requestor={Requestor}",
                    model.workTitle, model.Property_idProperty, model.requestor_Employee_idEmployee);

                // Serialize and send to backend
                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogDebug("Work request payload: {Payload}", jsonPayload);
                //Logged payload to LogFile -- turn off on production -- 
                _fileLogger.LogInfo("Work request payload: {Payload}", jsonPayload);
                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.WorkRequest.Create}", content);

                // Read response body regardless of HTTP status code
                // (backend returns ApiResponseDto for both success and error cases)
                var responseBody = await response.Content.ReadAsStringAsync();

                ApiResponseDto<WorkRequestCreateData> apiResponse = null;
                try
                {
                    apiResponse = JsonSerializer.Deserialize<ApiResponseDto<WorkRequestCreateData>>(
                        responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize work request creation response. Body: {Body}", responseBody);
                }

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    _logger.LogInformation("Work request created successfully. IdWorkRequest: {IdWorkRequest}",
                        apiResponse.Data.IdWorkRequest);
                    return Json(new
                    {
                        success = true,
                        message = "Work Request created successfully!",
                        redirectUrl = "/Helpdesk/Index",
                        idWorkRequest = apiResponse.Data.IdWorkRequest
                    });
                }
                else
                {
                    var errorMessage = apiResponse?.Message ?? "Failed to create work request";
                    var errorDetails = apiResponse?.Errors != null && apiResponse.Errors.Count > 0
                        ? string.Join("; ", apiResponse.Errors)
                        : string.Empty;

                    _logger.LogWarning("Work request creation failed. Status: {StatusCode}, Message: {Message}, Errors: {Errors}",
                        response.StatusCode, errorMessage, errorDetails);

                    var userMessage = !string.IsNullOrEmpty(errorDetails)
                        ? $"{errorMessage}: {errorDetails}"
                        : errorMessage;

                    return Json(new { success = false, message = userMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work request");
                return Json(new { success = false, message = "An error occurred while creating the work request." });
            }
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
        [Authorize]
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
                viewmodel.Locations = await GetLocationsAsync(client, backendUrl, idClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading send new work request page");
                viewmodel.Locations = new List<LocationModel>();
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
        [Authorize]
        public async Task<IActionResult> WorkRequestDetail(int id)
        {
            var totalStopwatch = Stopwatch.StartNew();

            // Check if user has permission to view Work Request Detail
            var accessCheck = this.CheckViewAccess("Helpdesk", "Work Request Management");
            if (accessCheck != null) return accessCheck;

            var viewModel = new WorkRequestDetailViewModel();

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Get user session for client ID
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var idClient = userInfo?.PreferredClientId ?? 0;

                // Fetch work request detail from backend
                var (success, workRequestData, message) = await SafeExecuteApiAsync<WorkRequestFormDetailDto>(
                    () => client.GetAsync($"{backendUrl}{ApiEndpoints.WorkRequest.GetById(id)}?cid={idClient}"),
                    "Failed to load work request details");

                if (!success || workRequestData == null)
                {
                    _logger.LogWarning("Work request {Id} not found or access denied: {Message}", id, message);
                    TempData["ErrorMessage"] = message ?? "Work request not found";
                    return RedirectToAction("Index", "Helpdesk");
                }

                viewModel.WorkRequestDetail = workRequestData;

                // Log timing
                totalStopwatch.Stop();
                _fileLogger?.LogInfo($"WorkRequestDetail {id} loaded in {totalStopwatch.ElapsedMilliseconds}ms", "PAGE-LOAD");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work request detail for ID {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the work request";
                return RedirectToAction("Index", "Helpdesk");
            }

            return View("~/Views/Helpdesk/WorkRequest/WorkRequestDetail.cshtml", viewModel);
        }

        /// <summary>
        /// GET: Work Request Edit page
        /// Combines dropdown data loading (like Add) with existing work request data loading (like Detail)
        /// </summary>
        [Authorize]
        public async Task<IActionResult> WorkRequestEdit(int id)
        {
            var totalStopwatch = Stopwatch.StartNew();
            var apiTimingResults = new List<ApiTimingResult>();

            // Check if user has permission to edit Work Requests
            var accessCheck = this.CheckEditAccess("Helpdesk", "Work Request Management");
            if (accessCheck != null) return accessCheck;

            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                _logger.LogWarning("User not authenticated, redirecting to login");
                _fileLogger.LogWarning("WorkRequestEdit: User not authenticated, redirecting to login", "AUTH");
                return RedirectToAction("Index", "Login");
            }

            var viewmodel = new WorkRequestEditViewModel();
            viewmodel.IdWorkRequest = id;

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                // Get user session
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    _logger.LogWarning("User session not found, redirecting to login");
                    _fileLogger.LogWarning("WorkRequestEdit: User session not found, redirecting to login", "SESSION");
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(
                    userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null)
                    return RedirectToAction("Index", "Login");

                var idClient = userInfo.PreferredClientId;
                var idCompany = userInfo.IdCompany;

                _fileLogger.LogInfo($"WorkRequestEdit: Starting API calls for id={id}, idClient={idClient}, idCompany={idCompany}", "PAGE-LOAD");

                // Create cancellation token with overall timeout for all parallel tasks (60 seconds)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var ct = cts.Token;

                // Load all dropdown data AND existing work request data in parallel

                // Dropdown data (same as WorkRequestAdd)
                var locationsTask = _fileLogger.ExecuteTimedAsync(
                    "Locations",
                    $"{ApiEndpoints.Property.List}?idClient={idClient}",
                    () => GetLocationsAsync(client, backendUrl, idClient, ct),
                    apiTimingResults);

                var workCategoriesTask = _fileLogger.ExecuteTimedAsync(
                    "WorkCategories",
                    $"{ApiEndpoints.Masters.GetTypes(ApiEndpoints.Masters.CategoryTypes.WorkCategory)}?idClient={idClient}",
                    () => GetWorkCategoriesByTypesAsync(client, backendUrl, idClient, ct),
                    apiTimingResults);

                var otherCategoriesTask = _fileLogger.ExecuteTimedAsync(
                    "OtherCategories",
                    $"{ApiEndpoints.Masters.GetTypes("workRequestCustomCategory")}?idClient={idClient}",
                    () => GetOtherCategoriesByTypeAsync(client, backendUrl, idClient, "workRequestCustomCategory", ct),
                    apiTimingResults);

                var otherCategories2Task = _fileLogger.ExecuteTimedAsync(
                    "OtherCategories2",
                    $"{ApiEndpoints.Masters.GetTypes("workRequestCustomCategory2")}?idClient={idClient}",
                    () => GetOtherCategoriesByTypeAsync(client, backendUrl, idClient, "workRequestCustomCategory2", ct),
                    apiTimingResults);

                var serviceProvidersTask = _fileLogger.ExecuteTimedAsync(
                    "ServiceProviders",
                    $"{ApiEndpoints.ServiceProvider.List}?idClient={idClient}&idCompany={idCompany}",
                    () => GetServiceProvidersAsync(client, backendUrl, idClient, idCompany, ct),
                    apiTimingResults);

                var priorityLevelsTask = _fileLogger.ExecuteTimedAsync(
                    "PriorityLevels",
                    $"{ApiEndpoints.PriorityLevelDetail.List}?idClient={idClient}",
                    () => GetPriorityLevelsWithDetailsAsync(client, backendUrl, idClient, ct),
                    apiTimingResults);

                var feedbackTypesTask = _fileLogger.ExecuteTimedAsync(
                    "FeedbackTypes",
                    ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestFeedbackType),
                    () => GetFeedbackTypesAsync(client, backendUrl, ct),
                    apiTimingResults);

                var currenciesTask = _fileLogger.ExecuteTimedAsync(
                    "Currencies",
                    ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.Currency),
                    () => FetchCurrenciesAsync(client, backendUrl, ct),
                    apiTimingResults);

                var requestMethodsTask = _fileLogger.ExecuteTimedAsync(
                    "RequestMethods",
                    ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestMethod),
                    () => GetRequestMethodsAsync(client, backendUrl, ct),
                    apiTimingResults);

                var statusesTask = _fileLogger.ExecuteTimedAsync(
                    "Statuses",
                    ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestStatus),
                    () => GetStatusesAsync(client, backendUrl, ct),
                    apiTimingResults);

                var checklistTask = _fileLogger.ExecuteTimedAsync(
                    "ImportantChecklist",
                    $"{ApiEndpoints.Masters.GetTypes(ApiEndpoints.Masters.CategoryTypes.WorkRequestAdditionalInformation)}?idClient={idClient}",
                    () => GetImportantChecklistAsync(client, backendUrl, idClient, ct),
                    apiTimingResults);

                // Existing work request data (like WorkRequestDetail)
                var workRequestTask = _fileLogger.ExecuteTimedAsync(
                    "WorkRequestData",
                    $"{ApiEndpoints.WorkRequest.GetById(id)}?cid={idClient}",
                    () => client.GetAsync($"{backendUrl}{ApiEndpoints.WorkRequest.GetById(id)}?cid={idClient}", ct),
                    apiTimingResults);

                // Wait for all tasks
                try
                {
                    await Task.WhenAll(
                        locationsTask, workCategoriesTask, otherCategoriesTask, otherCategories2Task,
                        serviceProvidersTask, priorityLevelsTask, feedbackTypesTask, currenciesTask,
                        requestMethodsTask, statusesTask, checklistTask, workRequestTask
                    );
                }
                catch (Exception taskEx)
                {
                    _logger.LogWarning(taskEx, "One or more data loading tasks failed, will use partial results");
                }

                // Populate dropdown data
                viewmodel.Locations = locationsTask.IsCompletedSuccessfully ? locationsTask.Result : [];
                viewmodel.WorkCategories = workCategoriesTask.IsCompletedSuccessfully ? workCategoriesTask.Result : [];
                viewmodel.OtherCategories = otherCategoriesTask.IsCompletedSuccessfully ? otherCategoriesTask.Result : [];
                viewmodel.OtherCategories2 = otherCategories2Task.IsCompletedSuccessfully ? otherCategories2Task.Result : [];
                viewmodel.ServiceProviders = serviceProvidersTask.IsCompletedSuccessfully ? serviceProvidersTask.Result : [];
                viewmodel.PriorityLevels = priorityLevelsTask.IsCompletedSuccessfully ? priorityLevelsTask.Result : [];
                viewmodel.FeedbackTypes = feedbackTypesTask.IsCompletedSuccessfully ? feedbackTypesTask.Result : [];
                viewmodel.Currencies = currenciesTask.IsCompletedSuccessfully ? currenciesTask.Result : [];
                viewmodel.RequestMethods = requestMethodsTask.IsCompletedSuccessfully ? requestMethodsTask.Result : [];
                viewmodel.Statuses = statusesTask.IsCompletedSuccessfully ? statusesTask.Result : [];
                viewmodel.ImportantChecklist = checklistTask.IsCompletedSuccessfully ? checklistTask.Result : [];

                // Populate work request data
                if (workRequestTask.IsCompletedSuccessfully)
                {
                    var response = workRequestTask.Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var responseStream = await response.Content.ReadAsStreamAsync();
                        var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<WorkRequestFormDetailDto>>(
                            responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (apiResponse?.Success == true && apiResponse.Data != null)
                        {
                            viewmodel.WorkRequestData = apiResponse.Data;
                        }
                    }
                }

                // Validate work request was found
                if (viewmodel.WorkRequestData == null)
                {
                    _logger.LogWarning("Work request {Id} not found or access denied", id);
                    TempData["ErrorMessage"] = "Work request not found or access denied";
                    return RedirectToAction("Index", "Helpdesk");
                }

                // Capture client context at page load for multi-tab session safety
                viewmodel.IdClient = idClient;
                viewmodel.IdCompany = idCompany;

                // Update record counts in timing results
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "Locations", viewmodel.Locations?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "WorkCategories", viewmodel.WorkCategories?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "OtherCategories", viewmodel.OtherCategories?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "OtherCategories2", viewmodel.OtherCategories2?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "ServiceProviders", viewmodel.ServiceProviders?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "PriorityLevels", viewmodel.PriorityLevels?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "FeedbackTypes", viewmodel.FeedbackTypes?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "Currencies", viewmodel.Currencies?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "RequestMethods", viewmodel.RequestMethods?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "Statuses", viewmodel.Statuses?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "ImportantChecklist", viewmodel.ImportantChecklist?.Count);
                _fileLogger.UpdateTimingResultRecordCount(apiTimingResults, "WorkRequestData", 1);

                _logger.LogInformation("Work Request Edit page data loaded successfully for ID {Id}: {LocationCount} locations, {CategoryCount} categories",
                    id,
                    viewmodel.Locations?.Count ?? 0,
                    viewmodel.WorkCategories?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work request edit page data for ID {Id}", id);
                _fileLogger.LogError($"WorkRequestEdit: Critical error loading page data for ID {id}", ex, "PAGE-LOAD");
                TempData["ErrorMessage"] = "An error occurred while loading the work request";
                return RedirectToAction("Index", "Helpdesk");
            }
            finally
            {
                totalStopwatch.Stop();

                // Log comprehensive timing summary to file
                _fileLogger.LogApiTimingBatch("WorkRequestEdit Page Load", apiTimingResults, totalStopwatch.Elapsed);

                // Also log summary to standard logger
                var failedApis = apiTimingResults.Where(r => !r.Success).ToList();
                if (failedApis.Any())
                {
                    _logger.LogWarning(
                        "WorkRequestEdit: {FailedCount}/{TotalCount} API calls failed. Total time: {TotalMs}ms. Failed APIs: {FailedApis}",
                        failedApis.Count,
                        apiTimingResults.Count,
                        totalStopwatch.ElapsedMilliseconds,
                        string.Join(", ", failedApis.Select(f => f.ApiName)));
                }
                else
                {
                    _logger.LogInformation(
                        "WorkRequestEdit: All {TotalCount} API calls succeeded. Total time: {TotalMs}ms",
                        apiTimingResults.Count,
                        totalStopwatch.ElapsedMilliseconds);
                }
            }

            return View("~/Views/Helpdesk/WorkRequest/WorkRequestEdit.cshtml", viewmodel);
        }


        #region API Endpoints for Dynamic Data Loading

        /// <summary>
        /// API: Get floors by property ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFloorsByLocation(int locationId)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<FloorModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Property.GetFloors(locationId)}"),
                "Failed to load floors");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get room zones by property ID and floor ID
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRoomsByFloor(int propertyId, int floorId)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<RoomModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Property.GetRoomZones(propertyId, floorId)}"),
                "Failed to load room zones");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Search employees/requestors by name
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchEmployees(string term, int idClient)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<EmployeeModel>>(
                () => client.GetAsync($"{backendUrl}/api/employee/search?term={Uri.EscapeDataString(term)}&idClient={idClient}"),
                "Failed to search employees");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get employees for Person in Charge dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPersonsInCharge(int idClient)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<EmployeeModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.PersonInCharge.Base}?idClient={idClient}"),
                "Failed to load persons in charge");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Search workers from company
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchWorkers(string term, int? idServiceProvider)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var url = $"{backendUrl}/api/worker/search?term={Uri.EscapeDataString(term)}";
            if (idServiceProvider.HasValue)
            {
                url += $"&idServiceProvider={idServiceProvider.Value}";
            }

            var (success, data, message) = await SafeExecuteApiAsync<List<EmployeeModel>>(
                () => client.GetAsync(url),
                "Failed to search workers");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get locations by idClient and userId from session
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLocationsByClient(int? idClient = null)
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

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<LocationModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Property.List}?idClient={effectiveIdClient}"),
                "Failed to load properties");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get persons in charge filtered by work category and location
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPersonsInChargeByFilters(int? idWorkCategory = null, int? idLocation = null, int? idClient = null)
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

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var url = $"{backendUrl}{ApiEndpoints.PersonInCharge.Base}?idClient={effectiveIdClient}&idProperty={idLocation.Value}";

            var (success, data, message) = await SafeExecuteApiAsync<List<PICFormDetailResponse>>(
                () => client.GetAsync(url),
                "Failed to load persons in charge");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Search requestors/employees by term
        /// Optional idCompany parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchRequestors(string term, int? idCompany = null)
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

            // Use passed idCompany if provided, otherwise fall back to session
            var effectiveIdCompany = idCompany ?? userInfo.IdCompany;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<dynamic>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Employee.SearchRequestors}?idCompany={effectiveIdCompany}&prefiks={Uri.EscapeDataString(term)}"),
                "Error searching requestors");

            return Json(new { success, data, message });
        }


        /// <summary>
        /// API: Get service providers
        /// Optional idClient/idCompany parameters for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetServiceProvidersByClient(int? idClient = null, int? idCompany = null)
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

            // Use passed values if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;
            var effectiveIdCompany = idCompany ?? userInfo.IdCompany;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<ServiceProviderFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.ServiceProvider.List}?idClient={effectiveIdClient}&idCompany={effectiveIdCompany}"),
                "Failed to load service providers");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Search workers from company by location
        /// Optional idCompany parameter for multi-tab session safety
        /// Uses backend endpoint: /api/v1/employee/worker
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchWorkersByCompany(string term, int idLocation, int? idCompany = null)
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

            // Use passed idCompany if provided, otherwise fall back to session
            var effectiveIdCompany = idCompany ?? userInfo.IdCompany;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<WorkerFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Employee.SearchWorkers}?idCompany={effectiveIdCompany}&idProperty={idLocation}&prefiks={Uri.EscapeDataString(term)}&idUserCompany={effectiveIdCompany}"),
                "Error searching workers");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Search workers from service provider
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchWorkersByServiceProvider(string term, int idLocation, int idServiceProvider, int? idClient = null,int? idCompany = null)
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

            // Use passed idClient if provided, otherwise fall back to session (not currently used in API call but kept for consistency)
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];



            // Now search workers with the company ID
            var (success, data, message) = await SafeExecuteApiAsync<List<WorkerFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Employee.SearchWorkers}?idCompany={idServiceProvider}&idProperty={idLocation}&prefiks={Uri.EscapeDataString(term)}&idUserCompany={idCompany}"),
                "Failed to search workers");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get important checklist using new Types API
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetImportantChecklistByTypes(int? idClient = null)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var endpoint = ApiEndpoints.Masters.GetTypes(ApiEndpoints.Masters.CategoryTypes.WorkRequestAdditionalInformation);
            var (success, data, message) = await SafeExecuteApiAsync<List<TypeFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{endpoint}?idClient={effectiveIdClient}"),
                "Failed to load important checklist");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get work categories using new Types API
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkCategoriesByTypes(int? idClient = null)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var endpoint = ApiEndpoints.Masters.GetTypes(ApiEndpoints.Masters.CategoryTypes.WorkCategory);
            var (success, data, message) = await SafeExecuteApiAsync<List<TypeFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{endpoint}?idClient={effectiveIdClient}"),
                "Failed to load work categories");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get other categories using new Types API
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOtherCategoriesByTypes(string categoryType, int? idClient = null)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var endpoint = ApiEndpoints.Masters.GetTypes(categoryType);
            var (success, data, message) = await SafeExecuteApiAsync<List<TypeFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{endpoint}?idClient={effectiveIdClient}"),
                "Failed to load categories");

            return Json(new { success, data, message });
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
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPriorityLevels(int? idClient = null)
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

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<DropdownOption>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Lookup.List}?type={ApiEndpoints.Lookup.Types.WorkRequestPriorityLevel}&idClient={effectiveIdClient}"),
                "Failed to load priority levels");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get work request methods using new Enums API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkRequestMethodsByEnums()
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var endpoint = ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestMethod);
            var (success, data, message) = await SafeExecuteApiAsync<List<EnumFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{endpoint}"),
                "Failed to load work request methods");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get work request statuses using new Enums API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkRequestStatusesByEnums()
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var endpoint = ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestStatus);
            var (success, data, message) = await SafeExecuteApiAsync<List<EnumFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{endpoint}"),
                "Failed to load work request statuses");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get feedback types using new Enums API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFeedbackTypesByEnums()
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var endpoint = ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestFeedbackType);
            var (success, data, message) = await SafeExecuteApiAsync<List<EnumFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{endpoint}"),
                "Failed to load feedback types");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get office hours for target date calculations
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOfficeHours(int? idClient = null)
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

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<OfficeHourModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.OfficeHour.List}?idClient={effectiveIdClient}"),
                "Failed to load office hours");

            return Json(new { success, data = data ?? new List<OfficeHourModel>(), message });
        }

        /// <summary>
        /// API: Get public holidays for target date calculations
        /// Optional idClient parameter for multi-tab session safety
        /// Loads current year and next year for 2-year window
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPublicHolidays(int? idClient = null)
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

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            // Load 2-year window (current year + next year) to handle year boundaries
            var currentYear = DateTime.Now.Year;
            var nextYear = currentYear + 1;

            // Fetch both years in parallel using SafeExecuteApiAsync
            // Using /api/v1/masters/public-holidays/{year}?idClient={idClient}
            var currentYearTask = SafeExecuteApiAsync<List<PublicHolidayModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.PublicHoliday.GetByYear(currentYear)}?idClient={effectiveIdClient}"),
                "Failed to load public holidays for current year");

            var nextYearTask = SafeExecuteApiAsync<List<PublicHolidayModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.PublicHoliday.GetByYear(nextYear)}?idClient={effectiveIdClient}"),
                "Failed to load public holidays for next year");

            await Task.WhenAll(currentYearTask, nextYearTask);

            var currentYearResult = await currentYearTask;
            var nextYearResult = await nextYearTask;

            var allPublicHolidays = new List<PublicHolidayModel>();

            if (currentYearResult.Success && currentYearResult.Data != null)
            {
                allPublicHolidays.AddRange(currentYearResult.Data);
            }

            if (nextYearResult.Success && nextYearResult.Data != null)
            {
                allPublicHolidays.AddRange(nextYearResult.Data);
            }

            return Json(new { success = true, data = allPublicHolidays });
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
        /// API: Get priority level by ID for target date calculation
        /// Fetches all priority levels and filters to the requested ID
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPriorityLevelById(int id, int? idClient = null)
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

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            // Fetch all priority levels and find the one with matching ID
            var (success, allData, message) = await SafeExecuteApiAsync<List<Models.PriorityLevelModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.PriorityLevelDetail.List}?idClient={effectiveIdClient}"),
                "Failed to load priority levels");

            if (!success || allData == null)
            {
                return Json(new { success = false, message });
            }

            var priorityLevel = allData.FirstOrDefault(p => p.Id == id);
            if (priorityLevel == null)
            {
                return Json(new { success = false, message = $"Priority level with ID {id} not found" });
            }

            return Json(new { success = true, data = priorityLevel });
        }

        /// <summary>
        /// API: Get dropdown options for priority level forms
        /// Loads reference options and color options based on type parameter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPriorityLevelDropdownOptions(string type)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<DropdownOption>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Settings.PriorityLevel.DropdownOptions}?type={type}"),
                "Failed to load dropdown options");

            return Json(new { success, data, message });
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
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchJobCode(string term, int? idClient = null)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<cfm_frontend.Models.JobCode.JobCodeSearchResult>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.JobCode.Base}?prefiks={Uri.EscapeDataString(term)}&idClient={effectiveIdClient}"),
                "Failed to search job codes");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get currencies for unit price dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCurrenciesAsync()
         {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<EnumFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.Currency)}"),
                "Failed to load currencies");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get measurement units for dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMeasurementUnitsAsync()
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<EnumFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.MeasurementUnit)}"),
                "Failed to load measurement units");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Get labor/material labels for dropdown
        /// Uses Masters Enum API with 'materialLabel' category
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLaborMaterialLabelsAsync()
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var endpoint = ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.MaterialLabel);
            var (success, data, message) = await SafeExecuteApiAsync<List<EnumFormDetailResponse>>(
                () => client.GetAsync($"{backendUrl}{endpoint}"),
                "Failed to load labor/material labels");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Search assets individually by name
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchAsset(string term, int propertyId, int? idClient = null)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<AssetSearchResult>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Asset.GetAsset(propertyId)}?prefiks={Uri.EscapeDataString(term)}&idClient={effectiveIdClient}"),
                "Failed to search assets");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Search asset groups by name
        /// Optional idClient parameter for multi-tab session safety
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchAssetGroup(string term, int propertyId, int? idClient = null)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Use passed idClient if provided, otherwise fall back to session
            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<AssetGroupSearchResult>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Asset.GetAssetByGroup(propertyId)}?prefiks={Uri.EscapeDataString(term)}&idClient={effectiveIdClient}"),
                "Failed to search asset groups");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Check if current session client matches the page load client
        /// Used for tab focus detection to warn users about client mismatch
        /// </summary>
        [HttpGet]
        public IActionResult CheckSessionClient()
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired", sessionExpired = true });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userInfo == null)
            {
                return Json(new { success = false, message = "Session expired", sessionExpired = true });
            }

            return Json(new
            {
                success = true,
                idClient = userInfo.PreferredClientId,
                idCompany = userInfo.IdCompany
            });
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Ensures the HTTP response is successful, throwing HttpRequestException with proper status code for 401 Unauthorized
        /// </summary>
        private void EnsureSuccessOrThrow(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException("Unauthorized - Session expired", null, System.Net.HttpStatusCode.Unauthorized);
            }
            response.EnsureSuccessStatusCode();
        }

        private async Task<WorkRequestListApiResponse?> GetWorkRequestsAsync(
            HttpClient client,
            string backendUrl,
            cfm_frontend.Models.WorkRequest.WorkRequestListParam requestBody)
        {
            var jsonPayload = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var (success, data, _) = await SafeExecuteApiAsync<WorkRequestListApiResponse>(
                () => client.PostAsync($"{backendUrl}{ApiEndpoints.WorkRequest.List}", content),
                "Error fetching work requests");

            return success ? data : null;
        }

        private async Task<FilterOptionsModel?> GetFilterOptionsAsync(
            HttpClient client,
            string backendUrl,
            int idClient,
            string keywords = "")
        {
            try
            {
                // Create request body with idClient and keywords
                var requestBody = new Models.WorkRequest.FilterOptionsRequestModel
                {
                    IdClient = idClient,
                    Keywords = keywords
                };

                // Serialize with camelCase naming policy
                var jsonPayload = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // POST request instead of GET
                var response = await client.PostAsync(
                    $"{backendUrl}{ApiEndpoints.WorkRequest.GetFilterOptions}",
                    content
                );

                var responseStream = await response.Content.ReadAsStreamAsync();
                var apiResponse = await JsonSerializer.DeserializeAsync<DTOs.ApiResponseDto<DTOs.WorkRequest.FilterOptionsDataDto>>(
                    responseStream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    _logger.LogInformation("Filter options loaded successfully: {Message}", apiResponse.Message);
                    // Map DTO to Model (flatten nested structures)
                    return MapToFilterOptionsModel(apiResponse.Data);
                }
                else if (apiResponse != null)
                {
                    _logger.LogWarning(
                        "Filter Options API returned error. Status: {StatusCode}, Message: {Message}, Errors: {Errors}",
                        response.StatusCode,
                        apiResponse.Message,
                        string.Join(", ", apiResponse.Errors)
                    );
                }
                else
                {
                    _logger.LogWarning("Filter Options API returned null or invalid response. Status: {StatusCode}", response.StatusCode);
                }

                return new FilterOptionsModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching filter options");
                // Return empty model so page still renders
                return new FilterOptionsModel();
            }
        }

        /// <summary>
        /// Maps backend filter options DTO to frontend filter options model.
        /// Flattens nested location hierarchy (property groups → properties → room zones)
        /// </summary>
        private FilterOptionsModel MapToFilterOptionsModel(DTOs.WorkRequest.FilterOptionsDataDto dto)
        {
            var model = new FilterOptionsModel();

            // Flatten location hierarchy
            var propertyGroups = new List<PropertyGroupModel>();
            var locations = new List<LocationModel>();
            var roomZones = new List<RoomZoneModel>();

            foreach (var locGroup in dto.Locations)
            {
                // Add property group
                propertyGroups.Add(new PropertyGroupModel
                {
                    propertyGroupId = locGroup.PropertyGroupId,
                    propertyGroupName = locGroup.PropertyGroup
                });

                foreach (var prop in locGroup.Properties)
                {
                    // Add location (property)
                    locations.Add(new LocationModel
                    {
                        idProperty = prop.PropertyId,
                        propertyName = prop.Property,
                        idPropertyType = locGroup.PropertyGroupId
                    });

                    // Add room zones for this property
                    roomZones.AddRange(prop.RoomZones.Select(rz => new RoomZoneModel
                    {
                        Id = rz.RoomZoneId,
                        Name = rz.RoomZone,
                        Description = rz.FloorUnit
                    }));
                }
            }

            // Remove duplicate property groups (in case multiple properties share same group)
            model.PropertyGroups = propertyGroups.DistinctBy(pg => pg.propertyGroupId).ToList();
            model.Locations = locations;
            model.RoomZones = roomZones;

            // Map simple collections
            model.ServiceProviders = dto.ServiceProviders.Select(sp => new ServiceProviderModel
            {
                id = sp.IdServiceProvider,
                name = sp.ServiceProvider
            }).ToList();

            model.WorkCategories = dto.WorkCategories.Select(wc => new WorkCategoryModel
            {
                id = wc.IdWorkCategory,
                name = wc.WorkCategory
            }).ToList();

            model.OtherCategories = dto.OtherCategories.Select(oc => new OtherCategoryModel
            {
                id = oc.IdOtherCategory,
                name = oc.OtherCategory
            }).ToList();

            model.PriorityLevels = dto.PriorityLevels.Select(pl => new FilterPriorityModel
            {
                Value = pl.IdPriorityLevel.ToString(),
                Label = pl.PriorityLevel
            }).ToList();

            model.Statuses = dto.Statuses.Select(s => new FilterStatusModel
            {
                Value = s.IdWorkRequestStatus.ToString(),
                Label = s.WorkRequestStatus
            }).ToList();

            model.ImportantChecklists = dto.ImportantChecklists.Select(ic => new FilterChecklistModel
            {
                Value = ic.IdImportantChecklist.ToString(),
                Label = ic.ImportantChecklist
            }).ToList();

            model.FeedbackTypes = dto.FeedbackTypes.Select(ft => new FilterFeedbackModel
            {
                Value = ft.IdFeedbackType.ToString(),
                Label = ft.FeedbackType
            }).ToList();

            model.RequestMethods = dto.RequestMethods.Select(rm => new FilterRequestMethodModel
            {
                Value = rm.IdRequestMethod.ToString(),
                Label = rm.RequestMethod
            }).ToList();

            return model;
        }

        private async Task<List<LocationModel>> GetLocationsAsync(HttpClient client, string backendUrl, int idClient, CancellationToken cancellationToken = default)
        {
            var (success, data, _) = await SafeExecuteApiAsync<List<LocationModel>>(
                ct => client.GetAsync($"{backendUrl}{ApiEndpoints.Property.List}?idClient={idClient}", ct),
                "Error fetching properties",
                cancellationToken);

            return success && data != null ? data : [];
        }

        private async Task<List<ServiceProviderFormDetailResponse>> GetServiceProvidersAsync(HttpClient client, string backendUrl, int idClient, int idCompany, CancellationToken cancellationToken = default)
        {
            var (success, data, _) = await SafeExecuteApiAsync<List<ServiceProviderFormDetailResponse>>(
                ct => client.GetAsync($"{backendUrl}{ApiEndpoints.ServiceProvider.List}?idClient={idClient}&idCompany={idCompany}", ct),
                "Error fetching service providers",
                cancellationToken);

            return success && data != null ? data : [];
        }

        private async Task<List<WorkCategoryModel>> GetWorkCategoriesAsync(HttpClient client, string backendUrl)
        {
            var (success, data, _) = await SafeExecuteApiAsync<List<WorkCategoryModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.WorkCategory.List}"),
                "Error fetching work categories");

            return success && data != null ? data : new List<WorkCategoryModel>();
        }

        private async Task<List<OtherCategoryModel>> GetOtherCategoriesAsync(HttpClient client, string backendUrl)
        {
            var (success, data, _) = await SafeExecuteApiAsync<List<OtherCategoryModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.OtherCategory.List}"),
                "Error fetching other categories");

            return success && data != null ? data : new List<OtherCategoryModel>();
        }

        private async Task<List<Models.PriorityLevelModel>> GetPriorityLevelsWithDetailsAsync(HttpClient client, string backendUrl, int idClient, CancellationToken cancellationToken = default)
        {
            // Single API call returns all priority levels with full details
            // Used for: dropdown (Id, Name) and target date calculation (duration fields, reference fields, etc.)
            // Note: Backend returns PriorityLevelFormDetailResponse with TimeSpan ticks, which we convert to days/hours/minutes
            var (success, data, message) = await SafeExecuteApiAsync<List<DTOs.PriorityLevel.PriorityLevelFormDetailResponse>>(
                ct => client.GetAsync($"{backendUrl}{ApiEndpoints.PriorityLevelDetail.List}?idClient={idClient}", ct),
                "Failed to load priority levels",
                cancellationToken);

            if (!success || data == null)
            {
                _logger.LogWarning("Priority Levels API failed: {Message}", message);
                return [];
            }

            // Convert API response DTOs to frontend models
            return data.Select(dto => dto.ToModel()).ToList();
        }

        private async Task<List<EnumFormDetailResponse>> GetFeedbackTypesAsync(HttpClient client, string backendUrl, CancellationToken cancellationToken = default)
        {
            var endpoint = ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestFeedbackType);
            var (success, data, _) = await SafeExecuteApiAsync<List<EnumFormDetailResponse>>(
                ct => client.GetAsync($"{backendUrl}{endpoint}", ct),
                "Error fetching feedback types",
                cancellationToken);

            return success && data != null ? data : [];
        }

        private async Task<List<EnumFormDetailResponse>> FetchCurrenciesAsync(HttpClient client, string backendUrl, CancellationToken cancellationToken = default)
        {
            var (success, data, _) = await SafeExecuteApiAsync<List<EnumFormDetailResponse>>(
                ct => client.GetAsync($"{backendUrl}{ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.Currency)}", ct),
                "Error fetching currencies",
                cancellationToken);

            return success && data != null ? data : [];
        }

        private async Task<List<EnumFormDetailResponse>> GetRequestMethodsAsync(HttpClient client, string backendUrl, CancellationToken cancellationToken = default)
        {
            var endpoint = ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestMethod);
            var (success, data, _) = await SafeExecuteApiAsync<List<EnumFormDetailResponse>>(
                ct => client.GetAsync($"{backendUrl}{endpoint}", ct),
                "Error fetching request methods",
                cancellationToken);

            return success && data != null ? data : [];
        }

        private async Task<List<EnumFormDetailResponse>> GetStatusesAsync(HttpClient client, string backendUrl, CancellationToken cancellationToken = default)
        {
            var endpoint = ApiEndpoints.Masters.GetEnums(ApiEndpoints.Masters.CategoryTypes.WorkRequestStatus);
            var (success, data, _) = await SafeExecuteApiAsync<List<EnumFormDetailResponse>>(
                ct => client.GetAsync($"{backendUrl}{endpoint}", ct),
                "Error fetching statuses",
                cancellationToken);

            return success && data != null ? data : [];
        }

        private async Task<List<TypeFormDetailResponse>> GetImportantChecklistAsync(HttpClient client, string backendUrl, int idClient, CancellationToken cancellationToken = default)
        {
            var endpoint = ApiEndpoints.Masters.GetTypes(ApiEndpoints.Masters.CategoryTypes.WorkRequestAdditionalInformation);
            var (success, data, _) = await SafeExecuteApiAsync<List<TypeFormDetailResponse>>(
                ct => client.GetAsync($"{backendUrl}{endpoint}?idClient={idClient}", ct),
                "Error fetching important checklist",
                cancellationToken);

            return success && data != null ? data : [];
        }

        private async Task<List<TypeFormDetailResponse>> GetOtherCategoriesByTypeAsync(HttpClient client, string backendUrl, int idClient, string categoryType, CancellationToken cancellationToken = default)
        {
            var endpoint = ApiEndpoints.Masters.GetTypes(categoryType);
            var (success, data, _) = await SafeExecuteApiAsync<List<TypeFormDetailResponse>>(
                ct => client.GetAsync($"{backendUrl}{endpoint}?idClient={idClient}", ct),
                $"Error fetching {categoryType}",
                cancellationToken);

            return success && data != null ? data : [];
        }

        private async Task<List<TypeFormDetailResponse>> GetWorkCategoriesByTypesAsync(HttpClient client, string backendUrl, int idClient, CancellationToken cancellationToken = default)
        {
            var endpoint = ApiEndpoints.Masters.GetTypes(ApiEndpoints.Masters.CategoryTypes.WorkCategory);
            var (success, data, _) = await SafeExecuteApiAsync<List<TypeFormDetailResponse>>(
                ct => client.GetAsync($"{backendUrl}{endpoint}?idClient={idClient}", ct),
                "Error fetching work categories by types",
                cancellationToken);

            return success && data != null ? data : [];
        }

        #endregion

        #endregion


        #region Settings

        /// <summary>
        /// GET: Settings Hub page
        /// </summary>
        [Authorize]
        public IActionResult Settings()
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;
            return View("~/Views/Helpdesk/Settings/Index.cshtml");
        }

        /// <summary>
        /// GET: Work Category Settings page with pagination
        /// </summary>
        [Authorize]
        public async Task<IActionResult> WorkCategory(int page = 1, string? search = "")
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Work Category";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");

            var viewmodel = new WorkCategoryViewModel
            {
                SearchKeyword = search
            };

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                var idClient = userInfo.PreferredClientId;

                // Capture client context at page load for multi-tab session safety
                viewmodel.IdClient = idClient;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await GetWorkCategoriesPagedAsync(client, backendUrl, idClient, page, search);

                if (response != null)
                {
                    viewmodel.Categories = response.Data;
                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = response.Metadata.CurrentPage,
                        TotalPages = response.Metadata.TotalPages,
                        PageSize = response.Metadata.PageSize,
                        TotalCount = response.Metadata.TotalCount
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading work categories");
                TempData["ErrorMessage"] = "Failed to load work categories. Please try again.";
            }

            return View("~/Views/Helpdesk/Settings/WorkCategory.cshtml", viewmodel);
        }

        /// <summary>
        /// Helper method to fetch paginated work categories from backend API
        /// </summary>
        private async Task<WorkCategoryListResponse?> GetWorkCategoriesPagedAsync(
            HttpClient client,
            string? backendUrl,
            int idClient,
            int page,
            string? keyword)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"cid={idClient}",
                    $"page={page}"
                };

                if (!string.IsNullOrEmpty(keyword))
                {
                    queryParams.Add($"keyword={Uri.EscapeDataString(keyword)}");
                }

                var queryString = string.Join("&", queryParams);
                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.WorkCategory.List}?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<WorkCategoryListResponse>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return apiResponse?.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch work categories. Status: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching work categories from API");
                return null;
            }
        }

        /// <summary>
        /// API: Get paginated work categories for AJAX requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWorkCategories(int page = 1, string? keyword = "")
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userInfo == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var idClient = userInfo.PreferredClientId;
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var queryParams = new List<string>
            {
                $"cid={idClient}",
                $"page={page}"
            };

            if (!string.IsNullOrEmpty(keyword))
            {
                queryParams.Add($"keyword={Uri.EscapeDataString(keyword)}");
            }

            var queryString = string.Join("&", queryParams);

            var (success, data, message) = await SafeExecuteApiAsync<WorkCategoryListResponse>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.WorkCategory.List}?{queryString}"),
                "Failed to load work categories");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Create new work category
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorkCategory([FromBody] WorkCategoryPayloadDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Text))
            {
                return Json(new { success = false, message = "Category name is required" });
            }

            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userInfo == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            // Set client ID from session
            model.IdClient = userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.PostAsync($"{backendUrl}{ApiEndpoints.WorkCategory.Create}", content),
                "Failed to create work category");

            return Json(new { success, message = success ? "Work category created successfully" : message });
        }

        /// <summary>
        /// API: Update work category
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateWorkCategory([FromBody] WorkCategoryPayloadDto model)
        {
            if (model.IdType <= 0)
            {
                return Json(new { success = false, message = "Invalid category ID" });
            }

            if (string.IsNullOrWhiteSpace(model.Text))
            {
                return Json(new { success = false, message = "Category name is required" });
            }

            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userInfo == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            // Set client ID from session
            model.IdClient = userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.PutAsync($"{backendUrl}{ApiEndpoints.WorkCategory.Update}", content),
                "Failed to update work category");

            return Json(new { success, message = success ? "Work category updated successfully" : message });
        }

        /// <summary>
        /// API: Delete work category
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteWorkCategory(int id)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Invalid category ID" });
            }

            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userInfo == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var idClient = userInfo.PreferredClientId;
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.DeleteAsync($"{backendUrl}{ApiEndpoints.WorkCategory.Delete(id)}?cid={idClient}"),
                "Failed to delete work category");

            return Json(new { success, message = success ? "Work category deleted successfully" : message });
        }

        #region Other Category

        /// <summary>
        /// GET: Other Category Settings page with pagination
        /// </summary>
        [Authorize]
        public async Task<IActionResult> OtherCategory(int page = 1, string? search = "")
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Other Category";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");

            var viewmodel = new TypeCategoryViewModel
            {
                SearchKeyword = search,
                CategoryDisplayName = "Other Category",
                CategoryDisplayNamePlural = "Other Categories"
            };

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                viewmodel.IdClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await GetTypeCategoriesPagedAsync(
                    client, backendUrl, userInfo.PreferredClientId, page, search,
                    ApiEndpoints.OtherCategoryV2.List);

                if (response != null)
                {
                    viewmodel.Categories = response.Data;
                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = response.Metadata.CurrentPage,
                        TotalPages = response.Metadata.TotalPages,
                        PageSize = response.Metadata.PageSize,
                        TotalCount = response.Metadata.TotalCount
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading other categories");
                TempData["ErrorMessage"] = "Failed to load other categories. Please try again.";
            }

            return View("~/Views/Helpdesk/Settings/OtherCategory.cshtml", viewmodel);
        }

        /// <summary>
        /// AJAX: Get paginated other categories for AJAX requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOtherCategoriesPaged(int page = 1, string? keyword = "")
        {
            return await GetTypeCategoriesAjax(page, keyword, ApiEndpoints.OtherCategoryV2.List, "other categories");
        }

        /// <summary>
        /// POST: Create new other category
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOtherCategoryV2([FromBody] TypePayloadDto model)
        {
            return await CreateTypeCategoryAsync(model, ApiEndpoints.OtherCategoryV2.Create, "Other category");
        }

        /// <summary>
        /// PUT: Update existing other category
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateOtherCategoryV2([FromBody] TypePayloadDto model)
        {
            return await UpdateTypeCategoryAsync(model, ApiEndpoints.OtherCategoryV2.Update, "Other category");
        }

        /// <summary>
        /// DELETE: Delete other category by ID
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteOtherCategoryV2(int id)
        {
            return await DeleteTypeCategoryAsync(id, ApiEndpoints.OtherCategoryV2.Delete(id), "Other category");
        }

        #endregion

        #region Other Category 2

        /// <summary>
        /// GET: Other Category 2 Settings page with pagination
        /// </summary>
        [Authorize]
        public async Task<IActionResult> OtherCategory2(int page = 1, string? search = "")
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Other Category 2";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");

            var viewmodel = new TypeCategoryViewModel
            {
                SearchKeyword = search,
                CategoryDisplayName = "Other Category 2",
                CategoryDisplayNamePlural = "Other Categories 2"
            };

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                viewmodel.IdClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await GetTypeCategoriesPagedAsync(
                    client, backendUrl, userInfo.PreferredClientId, page, search,
                    ApiEndpoints.OtherCategory2V2.List);

                if (response != null)
                {
                    viewmodel.Categories = response.Data;
                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = response.Metadata.CurrentPage,
                        TotalPages = response.Metadata.TotalPages,
                        PageSize = response.Metadata.PageSize,
                        TotalCount = response.Metadata.TotalCount
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading other categories 2");
                TempData["ErrorMessage"] = "Failed to load other categories 2. Please try again.";
            }

            return View("~/Views/Helpdesk/Settings/OtherCategory2.cshtml", viewmodel);
        }

        /// <summary>
        /// AJAX: Get paginated other categories 2 for AJAX requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOtherCategories2Paged(int page = 1, string? keyword = "")
        {
            return await GetTypeCategoriesAjax(page, keyword, ApiEndpoints.OtherCategory2V2.List, "other categories 2");
        }

        /// <summary>
        /// POST: Create new other category 2
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOtherCategory2V2([FromBody] TypePayloadDto model)
        {
            return await CreateTypeCategoryAsync(model, ApiEndpoints.OtherCategory2V2.Create, "Other category 2");
        }

        /// <summary>
        /// PUT: Update existing other category 2
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateOtherCategory2V2([FromBody] TypePayloadDto model)
        {
            return await UpdateTypeCategoryAsync(model, ApiEndpoints.OtherCategory2V2.Update, "Other category 2");
        }

        /// <summary>
        /// DELETE: Delete other category 2 by ID
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteOtherCategory2V2(int id)
        {
            return await DeleteTypeCategoryAsync(id, ApiEndpoints.OtherCategory2V2.Delete(id), "Other category 2");
        }

        #endregion

        #region Type Category Shared Helpers

        /// <summary>
        /// Helper method to fetch paginated type categories from backend API
        /// </summary>
        private async Task<TypeCategoryListResponse?> GetTypeCategoriesPagedAsync(
            HttpClient client,
            string? backendUrl,
            int idClient,
            int page,
            string? keyword,
            string listEndpoint)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"cid={idClient}",
                    $"page={page}"
                };

                if (!string.IsNullOrEmpty(keyword))
                {
                    queryParams.Add($"keyword={Uri.EscapeDataString(keyword)}");
                }

                var queryString = string.Join("&", queryParams);
                var response = await client.GetAsync($"{backendUrl}{listEndpoint}?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<TypeCategoryListResponse>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return apiResponse?.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch type categories. Status: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching type categories from API");
                return null;
            }
        }

        /// <summary>
        /// AJAX helper for fetching paginated type categories
        /// </summary>
        private async Task<IActionResult> GetTypeCategoriesAjax(int page, string? keyword, string listEndpoint, string entityName)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userInfo == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var idClient = userInfo.PreferredClientId;
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var queryParams = new List<string>
            {
                $"cid={idClient}",
                $"page={page}"
            };

            if (!string.IsNullOrEmpty(keyword))
            {
                queryParams.Add($"keyword={Uri.EscapeDataString(keyword)}");
            }

            var queryString = string.Join("&", queryParams);

            var (success, data, message) = await SafeExecuteApiAsync<TypeCategoryListResponse>(
                () => client.GetAsync($"{backendUrl}{listEndpoint}?{queryString}"),
                $"Failed to load {entityName}");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// Create type category helper
        /// </summary>
        private async Task<IActionResult> CreateTypeCategoryAsync(TypePayloadDto model, string endpoint, string entityName)
        {
            if (string.IsNullOrWhiteSpace(model.Text))
            {
                return Json(new { success = false, message = $"{entityName} name is required" });
            }

            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userInfo == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            // Set client ID from session (server override for security)
            model.IdClient = userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.PostAsync($"{backendUrl}{endpoint}", content),
                $"Failed to create {entityName.ToLower()}");

            return Json(new { success, message = success ? $"{entityName} created successfully" : message });
        }

        /// <summary>
        /// Update type category helper
        /// </summary>
        private async Task<IActionResult> UpdateTypeCategoryAsync(TypePayloadDto model, string endpoint, string entityName)
        {
            if (model.IdType <= 0)
            {
                return Json(new { success = false, message = $"Invalid {entityName.ToLower()} ID" });
            }

            if (string.IsNullOrWhiteSpace(model.Text))
            {
                return Json(new { success = false, message = $"{entityName} name is required" });
            }

            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userInfo == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            // Set client ID from session (server override for security)
            model.IdClient = userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var fullUrl = $"{backendUrl}{endpoint}";
            _logger.LogInformation("PUT {EntityName} to: {Url}", entityName, fullUrl);

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.PutAsync(fullUrl, content),
                $"Failed to update {entityName.ToLower()}");

            if (!success)
            {
                _logger.LogWarning("PUT {EntityName} failed: {Message}", entityName, message);
            }

            return Json(new { success, message = success ? $"{entityName} updated successfully" : message });
        }

        /// <summary>
        /// Delete type category helper
        /// </summary>
        private async Task<IActionResult> DeleteTypeCategoryAsync(int id, string endpoint, string entityName)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = $"Invalid {entityName.ToLower()} ID" });
            }

            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userInfo == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var idClient = userInfo.PreferredClientId;
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var fullUrl = $"{backendUrl}{endpoint}?cid={idClient}";
            _logger.LogInformation("DELETE {EntityName} at: {Url}", entityName, fullUrl);

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.DeleteAsync(fullUrl),
                $"Failed to delete {entityName.ToLower()}");

            if (!success)
            {
                _logger.LogWarning("DELETE {EntityName} failed: {Message}", entityName, message);
            }

            return Json(new { success, message = success ? $"{entityName} deleted successfully" : message });
        }

        #endregion

        #region Job Code Group

        /// <summary>
        /// GET: Job Code Group Settings page with pagination
        /// </summary>
        [Authorize]
        public async Task<IActionResult> JobCodeGroup(int page = 1, string? search = "")
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Job Code Group";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");

            var viewmodel = new TypeCategoryViewModel
            {
                SearchKeyword = search,
                CategoryDisplayName = "Job Code Group",
                CategoryDisplayNamePlural = "Job Code Groups"
            };

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                viewmodel.IdClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await GetTypeCategoriesPagedAsync(
                    client, backendUrl, userInfo.PreferredClientId, page, search,
                    ApiEndpoints.JobCodeGroup.List);

                if (response != null)
                {
                    viewmodel.Categories = response.Data;
                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = response.Metadata.CurrentPage,
                        TotalPages = response.Metadata.TotalPages,
                        PageSize = response.Metadata.PageSize,
                        TotalCount = response.Metadata.TotalCount
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading job code groups");
                TempData["ErrorMessage"] = "Failed to load job code groups. Please try again.";
            }

            return View("~/Views/Helpdesk/Settings/JobCodeGroup.cshtml", viewmodel);
        }

        /// <summary>
        /// AJAX: Get paginated job code groups for AJAX requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetJobCodeGroupsPaged(int page = 1, string? keyword = "")
        {
            return await GetTypeCategoriesAjax(page, keyword, ApiEndpoints.JobCodeGroup.List, "job code groups");
        }

        /// <summary>
        /// POST: Create new job code group
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateJobCodeGroupV2([FromBody] TypePayloadDto model)
        {
            return await CreateTypeCategoryAsync(model, ApiEndpoints.JobCodeGroup.Create, "Job code group");
        }

        /// <summary>
        /// PUT: Update existing job code group
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateJobCodeGroupV2([FromBody] TypePayloadDto model)
        {
            return await UpdateTypeCategoryAsync(model, ApiEndpoints.JobCodeGroup.Update, "Job code group");
        }

        /// <summary>
        /// DELETE: Delete job code group by ID
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteJobCodeGroupV2(int id)
        {
            return await DeleteTypeCategoryAsync(id, ApiEndpoints.JobCodeGroup.Delete(id), "Job code group");
        }

        #endregion

        #region Material Type

        /// <summary>
        /// GET: Material Type Settings page with pagination
        /// </summary>
        [Authorize]
        public async Task<IActionResult> MaterialType(int page = 1, string? search = "")
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Material Type";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");

            var viewmodel = new TypeCategoryViewModel
            {
                SearchKeyword = search,
                CategoryDisplayName = "Material Type",
                CategoryDisplayNamePlural = "Material Types"
            };

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                viewmodel.IdClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await GetTypeCategoriesPagedAsync(
                    client, backendUrl, userInfo.PreferredClientId, page, search,
                    ApiEndpoints.MaterialType.List);

                if (response != null)
                {
                    viewmodel.Categories = response.Data;
                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = response.Metadata.CurrentPage,
                        TotalPages = response.Metadata.TotalPages,
                        PageSize = response.Metadata.PageSize,
                        TotalCount = response.Metadata.TotalCount
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading material types");
                TempData["ErrorMessage"] = "Failed to load material types. Please try again.";
            }

            return View("~/Views/Helpdesk/Settings/MaterialType.cshtml", viewmodel);
        }

        /// <summary>
        /// AJAX: Get paginated material types for AJAX requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMaterialTypesPaged(int page = 1, string? keyword = "")
        {
            return await GetTypeCategoriesAjax(page, keyword, ApiEndpoints.MaterialType.List, "material types");
        }

        /// <summary>
        /// POST: Create new material type
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateMaterialTypeV2([FromBody] TypePayloadDto model)
        {
            return await CreateTypeCategoryAsync(model, ApiEndpoints.MaterialType.Create, "Material type");
        }

        /// <summary>
        /// PUT: Update existing material type
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateMaterialTypeV2([FromBody] TypePayloadDto model)
        {
            return await UpdateTypeCategoryAsync(model, ApiEndpoints.MaterialType.Update, "Material type");
        }

        /// <summary>
        /// DELETE: Delete material type by ID
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteMaterialTypeV2(int id)
        {
            return await DeleteTypeCategoryAsync(id, ApiEndpoints.MaterialType.Delete(id), "Material type");
        }

        #endregion

        #region Important Checklist

        /// <summary>
        /// GET: Important Checklist Settings page with pagination
        /// </summary>
        [Authorize]
        public async Task<IActionResult> ImportantChecklist(int page = 1, string? search = "")
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Important Checklist";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");

            var viewmodel = new TypeCategoryViewModel
            {
                SearchKeyword = search,
                CategoryDisplayName = "Important Checklist Item",
                CategoryDisplayNamePlural = "Important Checklist Items"
            };

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                viewmodel.IdClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await GetTypeCategoriesPagedAsync(
                    client, backendUrl, userInfo.PreferredClientId, page, search,
                    ApiEndpoints.ImportantChecklist.List);

                if (response != null)
                {
                    viewmodel.Categories = response.Data;
                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = response.Metadata.CurrentPage,
                        TotalPages = response.Metadata.TotalPages,
                        PageSize = response.Metadata.PageSize,
                        TotalCount = response.Metadata.TotalCount
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading important checklist items");
                TempData["ErrorMessage"] = "Failed to load important checklist items. Please try again.";
            }

            return View("~/Views/Helpdesk/Settings/ImportantChecklist.cshtml", viewmodel);
        }

        /// <summary>
        /// AJAX: Get paginated important checklist items for AJAX requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetImportantChecklistsPaged(int page = 1, string? keyword = "")
        {
            return await GetTypeCategoriesAjax(page, keyword, ApiEndpoints.ImportantChecklist.List, "important checklist items");
        }

        [HttpPost]
        public async Task<IActionResult> CreateImportantChecklist([FromBody] TypePayloadDto model)
        {
            return await CreateTypeCategoryAsync(model, ApiEndpoints.ImportantChecklist.Create, "Important checklist item");
        }

        [HttpPut]
        public async Task<IActionResult> UpdateImportantChecklist([FromBody] TypePayloadDto model)
        {
            return await UpdateTypeCategoryAsync(model, ApiEndpoints.ImportantChecklist.Update, "Important checklist item");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteImportantChecklist(int id)
        {
            return await DeleteTypeCategoryAsync(id, ApiEndpoints.ImportantChecklist.Delete(id), "Important checklist item");
        }

        [HttpPut]
        public async Task<IActionResult> UpdateImportantChecklistOrder([FromBody] TypeCategoryUpdateOrderRequest request)
        {
            if (request.Items == null || !request.Items.Any())
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

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.PutAsync($"{backendUrl}{ApiEndpoints.ImportantChecklist.UpdateOrder}", content),
                "Failed to update checklist order");

            return Json(new { success, message = success ? "Checklist order updated successfully" : message });
        }

        #endregion

        #region Document Label (Related Document)

        /// <summary>
        /// GET: Document Label settings page with server-side rendering
        /// </summary>
        [Authorize]
        public async Task<IActionResult> RelatedDocument(int page = 1, string? search = "")
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            var viewmodel = new TypeCategoryViewModel
            {
                SearchKeyword = search,
                CategoryDisplayName = "Document Label",
                CategoryDisplayNamePlural = "Document Labels"
            };

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                viewmodel.IdClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await GetTypeCategoriesPagedAsync(
                    client, backendUrl, userInfo.PreferredClientId, page, search,
                    ApiEndpoints.DocumentLabelV2.List);

                if (response != null)
                {
                    viewmodel.Categories = response.Data;
                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = response.Metadata.CurrentPage,
                        TotalPages = response.Metadata.TotalPages,
                        PageSize = response.Metadata.PageSize,
                        TotalCount = response.Metadata.TotalCount
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading document labels");
                TempData["ErrorMessage"] = "Failed to load document labels. Please try again.";
            }

            return View("~/Views/Helpdesk/Settings/RelatedDocument.cshtml", viewmodel);
        }

        /// <summary>
        /// AJAX: Get paginated document labels for AJAX requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDocumentLabelsPaged(int page = 1, string? keyword = "")
        {
            return await GetTypeCategoriesAjax(page, keyword, ApiEndpoints.DocumentLabelV2.List, "document labels");
        }

        /// <summary>
        /// POST: Create new document label
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDocumentLabelV2([FromBody] TypePayloadDto model)
        {
            return await CreateTypeCategoryAsync(model, ApiEndpoints.DocumentLabelV2.Create, "Document label");
        }

        /// <summary>
        /// PUT: Update existing document label
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateDocumentLabelV2([FromBody] TypePayloadDto model)
        {
            return await UpdateTypeCategoryAsync(model, ApiEndpoints.DocumentLabelV2.Update, "Document label");
        }

        /// <summary>
        /// DELETE: Delete document label by ID
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteDocumentLabelV2(int id)
        {
            return await DeleteTypeCategoryAsync(id, ApiEndpoints.DocumentLabelV2.Delete(id), "Document label");
        }

        #endregion

        #region Generic CRUD Helpers

        private async Task<IActionResult> GetCategoriesGeneric(string endpoint, string entityName)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<object>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.GenericCategory.List(endpoint)}"),
                $"Failed to load {entityName}");

            return Json(new { success, data, message });
        }

        private async Task<IActionResult> CreateCategoryGeneric(dynamic model, string endpoint, string entityName)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.PostAsync($"{backendUrl}{ApiEndpoints.GenericCategory.Create(endpoint)}", content),
                $"Failed to create {entityName}");

            var successMessage = $"{char.ToUpper(entityName[0]) + entityName.Substring(1)} created successfully";
            return Json(new { success, message = success ? successMessage : message });
        }

        private async Task<IActionResult> UpdateCategoryGeneric(dynamic model, string endpoint, string entityName)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.PutAsync($"{backendUrl}{ApiEndpoints.GenericCategory.Update(endpoint)}", content),
                $"Failed to update {entityName}");

            var successMessage = $"{char.ToUpper(entityName[0]) + entityName.Substring(1)} updated successfully";
            return Json(new { success, message = success ? successMessage : message });
        }

        private async Task<IActionResult> DeleteCategoryGeneric(int id, string endpoint, string entityName)
        {
            if (id <= 0)
            {
                return Json(new { success = false, message = "Invalid ID" });
            }

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.DeleteAsync($"{backendUrl}{ApiEndpoints.GenericCategory.Delete(endpoint, id)}"),
                $"Failed to delete {entityName}");

            var successMessage = $"{char.ToUpper(entityName[0]) + entityName.Substring(1)} deleted successfully";
            return Json(new { success, message = success ? successMessage : message });
        }

        #endregion

        #region Person in Charge

        [Authorize]
        public async Task<IActionResult> PersonInCharge(int page = 1, string? search = "")
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Settings");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Person in Charge";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");

            var viewmodel = new PersonInChargeViewModel
            {
                SearchKeyword = search
            };

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                var idClient = userInfo.PreferredClientId;
                viewmodel.IdClient = idClient;
                viewmodel.IdCompany = userInfo.IdCompany;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await GetPicListPagedAsync(client, backendUrl, idClient, page, search);

                if (response != null)
                {
                    viewmodel.PersonsInCharge = response.Data;
                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = response.Metadata.CurrentPage,
                        TotalPages = response.Metadata.TotalPages,
                        PageSize = response.Metadata.PageSize,
                        TotalCount = response.Metadata.TotalCount
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading persons in charge");
                TempData["ErrorMessage"] = "Failed to load persons in charge. Please try again.";
            }

            return View("~/Views/Helpdesk/Settings/PersonInCharge.cshtml", viewmodel);
        }

        /// <summary>
        /// Helper method to fetch paginated PIC list from backend API
        /// </summary>
        private async Task<PicListResponse?> GetPicListPagedAsync(
            HttpClient client,
            string? backendUrl,
            int idClient,
            int page,
            string? keyword)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"cid={idClient}",
                    $"page={page}",
                    $"limit=20"
                };

                if (!string.IsNullOrEmpty(keyword))
                {
                    queryParams.Add($"keyword={Uri.EscapeDataString(keyword)}");
                }

                var queryString = string.Join("&", queryParams);
                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Settings.PersonInCharge.List}?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<PicListResponse>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return apiResponse?.Data;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch PIC list. Status: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching PIC list from API");
                return null;
            }
        }

        /// <summary>
        /// API: Get paginated PIC list for AJAX requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPersonsInChargePaged(int page = 1, string? keyword = "", int? idClient = null)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var response = await GetPicListPagedAsync(client, backendUrl, effectiveIdClient, page, keyword);

            if (response != null)
            {
                return Json(new { success = true, data = response.Data, paging = response.Metadata });
            }

            return Json(new { success = false, message = "Failed to load persons in charge" });
        }

        /// <summary>
        /// API: Get PIC details with property assignments
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPicDetails(int employeeId, int? idClient = null)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<PicPropertyAssignmentDto>(
                () => client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Settings.PersonInCharge.GetDetails(employeeId)}?cid={effectiveIdClient}"),
                "Failed to load PIC details");

            return Json(new { success, data, message });
        }

        [HttpGet]
        public async Task<IActionResult> GetProperties()
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

            var (success, data, message) = await SafeExecuteApiAsync<List<object>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Settings.Properties}?idClient={userInfo.PreferredClientId}"),
                "Failed to load properties");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Create PIC property assignment
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePersonInCharge([FromBody] PicPropertyPayloadDto model)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var (success, _, message) = await SafeExecuteApiAsync<PicPropertySummaryDto>(
                () => client.PostAsync($"{backendUrl}{ApiEndpoints.Settings.PersonInCharge.Create}", content),
                "Failed to add person in charge");

            return Json(new { success, message = success ? "Person in charge added" : message });
        }

        /// <summary>
        /// API: Update PIC property assignment
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdatePersonInCharge([FromBody] PicPropertyPayloadDto model)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var (success, _, message) = await SafeExecuteApiAsync<PicPropertySummaryDto>(
                () => client.PutAsync($"{backendUrl}{ApiEndpoints.Settings.PersonInCharge.Update}", content),
                "Failed to update person in charge");

            return Json(new { success, message = success ? "Person in charge updated" : message });
        }

        /// <summary>
        /// API: Delete PIC by employee ID
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeletePersonInCharge(int employeeId, int? idClient = null)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var effectiveIdClient = idClient ?? userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, _, message) = await SafeExecuteApiAsync<bool>(
                () => client.DeleteAsync(
                    $"{backendUrl}{ApiEndpoints.Settings.PersonInCharge.Delete(employeeId)}?cid={effectiveIdClient}"),
                "Failed to delete person in charge");

            return Json(new { success, message = success ? "Person in charge deleted" : message });
        }

        #endregion

        #region Cost Approver Group
        [Authorize]
        public IActionResult CostApprover()
        {
            ViewBag.Title = "Cost Approver Group";
            ViewBag.pTitle = "Settings";
            ViewBag.pTitleUrl = Url.Action("Settings", "Helpdesk");
            return View("~/Views/Helpdesk/Settings/CostApprover.cshtml");
        }
        [Authorize]
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
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var idClient = userInfo.PreferredClientId;

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<object>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.Settings.CostApproverGroup.List}?idClient={idClient}"),
                "Failed to load cost approver groups");

            return Json(new { success, data, message });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCostApproverGroup([FromBody] dynamic model)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.PostAsync($"{backendUrl}{ApiEndpoints.Settings.CostApproverGroup.Create}", content),
                "Failed to create cost approver group");

            return Json(new { success, message = success ? "Cost approver group created successfully" : message });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCostApproverGroup([FromBody] dynamic model)
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var id = ((JsonElement)model.GetProperty("id")).GetInt32();

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.DeleteAsync($"{backendUrl}{ApiEndpoints.Settings.CostApproverGroup.Delete(id)}"),
                "Failed to delete cost approver group");

            return Json(new { success, message = success ? "Cost approver group deleted successfully" : message });
        }

        #region Email Distribution List Management

        /// <summary>
        /// GET: Email Distribution List management page
        /// </summary>
        [HttpGet]
        [Authorize]
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
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<List<cfm_frontend.DTOs.EmailDistribution.EmailDistributionReferenceModel>>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.EmailDistribution.GetPageReferences}"),
                "Failed to load email distribution list");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// GET: Setup new email distribution list page
        /// </summary>
        [HttpGet]
        [Authorize]
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
        [Authorize]
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
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var (success, data, message) = await SafeExecuteApiAsync<cfm_frontend.DTOs.EmailDistribution.EmailDistributionDetailModel>(
                () => client.GetAsync($"{backendUrl}{ApiEndpoints.EmailDistribution.GetById(id)}"),
                "Failed to load email distribution details");

            return Json(new { success, data, message });
        }

        /// <summary>
        /// API: Create new email distribution
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateEmailDistribution([FromBody] cfm_frontend.DTOs.EmailDistribution.EmailDistributionDetailModel model)
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

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.PostAsync($"{backendUrl}{ApiEndpoints.EmailDistribution.Create}", content),
                "Failed to create email distribution");

            return Json(new { success, message = success ? "Email distribution created successfully" : message });
        }

        /// <summary>
        /// API: Update email distribution
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateEmailDistribution([FromBody] cfm_frontend.DTOs.EmailDistribution.EmailDistributionDetailModel model, int id)
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

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.PutAsync($"{backendUrl}{ApiEndpoints.EmailDistribution.Update(id)}", content),
                "Failed to update email distribution");

            return Json(new { success, message = success ? "Email distribution updated successfully" : message });
        }

        /// <summary>
        /// API: Delete email distribution
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteEmailDistribution([FromBody] dynamic model)
        {
            var accessCheck = this.CheckDeleteAccess("Helpdesk", "Settings");
            if (accessCheck != null)
                return Json(new { success = false, message = "You do not have permission to delete" });

            var client = _httpClientFactory.CreateClient("BackendAPI");
            var backendUrl = _configuration["BackendBaseUrl"];

            var id = ((JsonElement)model.GetProperty("id")).GetInt32();

            var (success, _, message) = await SafeExecuteApiAsync<object>(
                () => client.DeleteAsync($"{backendUrl}{ApiEndpoints.EmailDistribution.Delete(id)}"),
                "Failed to delete email distribution");

            return Json(new { success, message = success ? "Email distribution deleted successfully" : message });
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