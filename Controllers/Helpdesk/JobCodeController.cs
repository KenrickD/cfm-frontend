using cfm_frontend.Constants;
using cfm_frontend.DTOs;
using cfm_frontend.DTOs.JobCode;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using cfm_frontend.Models.JobCode;
using cfm_frontend.Models.WorkRequest;
using cfm_frontend.Services;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Web;

namespace cfm_frontend.Controllers.Helpdesk
{
    public class JobCodeController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JobCodeController> _controllerLogger;

        public JobCodeController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<JobCodeController> logger,
            IPrivilegeService privilegeService)
            : base(privilegeService, logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _controllerLogger = logger;
        }

        public async Task<IActionResult> Index(int page = 1, string? search = "", string? group = "", bool? showDeleted = false)
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Job Code Management");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Job Code Management";
            ViewBag.pTitle = "Helpdesk";
            ViewBag.pTitleUrl = Url.Action("Index", "Helpdesk");

            var viewmodel = new JobCodeViewModel
            {
                SearchKeyword = search,
                SelectedGroup = group,
                ShowDeletedData = showDeleted
            };

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<Models.UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jobCodesTask = GetJobCodesAsync(client, backendUrl, idClient, page, search, group);
                var groupsTask = GetJobCodeListFilterAsync(client, backendUrl, idClient);

                await Task.WhenAll(jobCodesTask, groupsTask);

                var response = await jobCodesTask;
                var groups = await groupsTask;

                if (response?.Success == true && response.Data != null)
                {
                    // Decode HTML entities in Label field (e.g., "Labor &amp; Material" -> "Labor & Material")
                    var jobCodes = response.Data.Data?.ToList();
                    if (jobCodes != null)
                    {
                        foreach (var jobCode in jobCodes)
                        {
                            if (!string.IsNullOrEmpty(jobCode.Label))
                            {
                                jobCode.Label = HttpUtility.HtmlDecode(jobCode.Label);
                            }
                        }
                    }
                    viewmodel.JobCodes = jobCodes;

                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = response.Data.Metadata?.CurrentPage ?? 1,
                        TotalPages = response.Data.Metadata?.TotalPages ?? 1,
                        PageSize = response.Data.Metadata?.PageSize ?? 25,
                        TotalCount = response.Data.Metadata?.TotalCount ?? 0
                    };
                }

                viewmodel.Groups = groups;
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error loading job codes");
                TempData["ErrorMessage"] = "Failed to load job codes. Please try again.";
            }

            return View(viewmodel);
        }

        private async Task<ApiResponseDto<PagedResponse<JobCodeListResponseDto>>?> GetJobCodesAsync(
            HttpClient client,
            string? backendUrl,
            int idClient,
            int page,
            string? search,
            string? group)
        {
            try
            {
                var requestBody = new JobCodeListParamDto
                {
                    IdClient = idClient,
                    Keywords = search,
                    Page = page,
                    PageSize = 25
                };

                // Add group filter if specified
                if (!string.IsNullOrEmpty(group) && int.TryParse(group, out int groupId))
                {
                    requestBody.Filter = new JobCodeListFilterParamDto
                    {
                        Groups = new int[] { groupId }
                    };
                }

                var jsonPayload = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Backend has bug: uses [HttpGet] with [FromBody], which doesn't work
                // We'll use POST as a workaround (or wait for backend fix)
                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.JobCode.List}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    return await JsonSerializer.DeserializeAsync<ApiResponseDto<PagedResponse<JobCodeListResponseDto>>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                else
                {
                    _controllerLogger.LogWarning("Failed to fetch job codes. Status: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error fetching job codes from API");
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Job Code Management");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Job Code Detail";
            ViewBag.pTitle = "Job Code Management";
            ViewBag.pTitleUrl = Url.Action("Index", "JobCode");

            var viewmodel = new JobCodeDetailViewModel();

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<Models.UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                var idClient = userInfo.PreferredClientId;
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jobCodeTask = client.GetAsync($"{backendUrl}{ApiEndpoints.JobCode.GetById(id)}?cid={idClient}");
                var changeHistoryTask = GetChangeHistoryAsync(client, backendUrl, id, "jobCodeDetail", "JOBCODE");

                await Task.WhenAll(jobCodeTask, changeHistoryTask);

                var jobCodeResponse = await jobCodeTask;
                if (jobCodeResponse.IsSuccessStatusCode)
                {
                    var responseStream = await jobCodeResponse.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<JobCodeDTO>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var dto = apiResponse.Data;

                        // Map JobCodeDTO to JobCodeModel with null safety
                        viewmodel.JobCode = new JobCodeModel
                        {
                            IdJobCode = dto.IdJobCode,
                            Name = dto.Name,
                            Description = dto.Description,
                            Group = dto.Group?.Name ?? "N/A",
                            LaborOrMaterial = HttpUtility.HtmlDecode(dto.Label?.Name ?? "N/A"),
                            MaterialType = dto.MaterialType?.Name,
                            EstimationTime = dto.EstimationTime != null
                                ? $"{dto.EstimationTime.Days}d {dto.EstimationTime.Hours}h {dto.EstimationTime.Minutes}m"
                                : "0d 0h 0m",
                            MeasurementUnit = dto.MeasurementUnit?.Name ?? "N/A",
                            UnitPrice = dto.UnitPrice != null ? (decimal)dto.UnitPrice.Price : 0,
                            Currency = dto.UnitPrice?.Currency ?? "N/A",
                            MinimumStock = (decimal?)dto.MinimumStock
                        };
                    }
                    else
                    {
                        TempData["ErrorMessage"] = apiResponse?.Message ?? "Job code not found.";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Job code not found.";
                    return RedirectToAction("Index");
                }

                viewmodel.ChangeHistory = await changeHistoryTask;

                return View(viewmodel);
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error loading job code detail");
                TempData["ErrorMessage"] = "Failed to load job code details. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var accessCheck = this.CheckEditAccess("Helpdesk", "Job Code Management");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Edit Job Code";
            ViewBag.pTitle = "Job Code Management";
            ViewBag.pTitleUrl = Url.Action("Index", "JobCode");

            var viewmodel = new JobCodeEditViewModel();

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<Models.UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jobCodeTask = client.GetAsync($"{backendUrl}{ApiEndpoints.JobCode.GetById(id)}?cid={idClient}");
                var groupsTask = GetJobCodeListFilterAsync(client, backendUrl, idClient);
                var currenciesTask = GetLookupDataAsync(client, backendUrl, "currency");
                var measurementUnitsTask = GetLookupDataAsync(client, backendUrl, "measurementUnit");
                var materialLabelsTask = GetLookupDataAsync(client, backendUrl, "materialLabel");

                await Task.WhenAll(jobCodeTask, groupsTask, currenciesTask, measurementUnitsTask, materialLabelsTask);

                var jobCodeResponse = await jobCodeTask;
                if (jobCodeResponse.IsSuccessStatusCode)
                {
                    var responseStream = await jobCodeResponse.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<JobCodeDTO>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var dto = apiResponse.Data;

                        // Map JobCodeDTO to JobCodeModel with null safety
                        viewmodel.JobCode = new JobCodeModel
                        {
                            IdJobCode = dto.IdJobCode,
                            Name = dto.Name,
                            Description = dto.Description,
                            Group = dto.Group?.Name ?? "", // Store name for comparison in dropdown
                            LaborOrMaterial = HttpUtility.HtmlDecode(dto.Label?.Name ?? ""), // Store name for comparison in radio
                            MaterialType = dto.MaterialType?.Name,
                            MaterialTypeId = dto.MaterialType?.Id,
                            EstimationTime = dto.EstimationTime != null
                                ? $"{dto.EstimationTime.Days},{dto.EstimationTime.Hours},{dto.EstimationTime.Minutes}"
                                : "0,0,0", // Store as comma-separated for parsing
                            MeasurementUnit = dto.MeasurementUnit?.Name ?? "", // Store name for comparison
                            UnitPrice = dto.UnitPrice != null ? (decimal)dto.UnitPrice.Price : 0,
                            Currency = dto.UnitPrice?.Currency ?? "", // Store currency code for comparison
                            MinimumStock = (decimal?)dto.MinimumStock
                        };
                    }
                    else
                    {
                        TempData["ErrorMessage"] = apiResponse?.Message ?? "Job code not found.";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Job code not found.";
                    return RedirectToAction("Index");
                }

                viewmodel.Groups = await groupsTask;
                viewmodel.Currencies = await currenciesTask;
                viewmodel.MeasurementUnits = await measurementUnitsTask;
                viewmodel.MaterialLabels = await materialLabelsTask;

                return View(viewmodel);
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error loading job code edit page");
                TempData["ErrorMessage"] = "Failed to load job code. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] JobCodeUpdateRequest model)
        {
            var accessCheck = this.CheckEditAccess("Helpdesk", "Job Code Management");
            if (accessCheck != null)
            {
                return Json(new { success = false, message = "You don't have permission to edit job codes." });
            }

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<Models.UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    return Json(new { success = false, message = "Invalid session. Please login again." });
                }

                model.IdClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{backendUrl}{ApiEndpoints.JobCode.Update}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Job code updated successfully." });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _controllerLogger.LogWarning("Failed to update job code. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, errorContent);

                    // Parse backend error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponseDto<object>>(
                            errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (errorResponse?.Errors != null && errorResponse.Errors.Any())
                        {
                            // Return all error messages from backend
                            var errorMessages = string.Join(", ", errorResponse.Errors);
                            return Json(new { success = false, message = errorMessages });
                        }
                        else if (!string.IsNullOrEmpty(errorResponse?.Message))
                        {
                            return Json(new { success = false, message = errorResponse.Message });
                        }
                    }
                    catch (Exception ex)
                    {
                        _controllerLogger.LogError(ex, "Error parsing backend error response");
                    }

                    return Json(new { success = false, message = "Failed to update job code." });
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error updating job code");
                return Json(new { success = false, message = "An error occurred while updating the job code." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var accessCheck = this.CheckAddAccess("Helpdesk", "Job Code Management");
            if (accessCheck != null) return accessCheck;

            ViewBag.Title = "Add Job Code";
            ViewBag.pTitle = "Job Code Management";
            ViewBag.pTitleUrl = Url.Action("Index", "JobCode");

            var viewmodel = new JobCodeAddViewModel();

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<Models.UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    TempData["ErrorMessage"] = "User session is invalid. Please login again.";
                    return RedirectToAction("Index", "Login");
                }

                var idClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var groupsTask = GetJobCodeListFilterAsync(client, backendUrl, idClient);
                var currenciesTask = GetLookupDataAsync(client, backendUrl, "currency");
                var measurementUnitsTask = GetLookupDataAsync(client, backendUrl, "measurementUnit");
                var materialLabelsTask = GetLookupDataAsync(client, backendUrl, "materialLabel");

                await Task.WhenAll(groupsTask, currenciesTask, measurementUnitsTask, materialLabelsTask);

                viewmodel.Groups = await groupsTask;
                viewmodel.Currencies = await currenciesTask;
                viewmodel.MeasurementUnits = await measurementUnitsTask;
                viewmodel.MaterialLabels = await materialLabelsTask;
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error loading job code add page data");
                TempData["ErrorMessage"] = "Failed to load page data. Please try again.";
            }

            return View(viewmodel);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] JobCodeCreateRequest model)
        {
            var accessCheck = this.CheckAddAccess("Helpdesk", "Job Code Management");
            if (accessCheck != null)
            {
                return Json(new { success = false, message = "You don't have permission to add job codes." });
            }

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<Models.UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    return Json(new { success = false, message = "Invalid session. Please login again." });
                }

                model.IdClient = userInfo.PreferredClientId;

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var jsonPayload = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.JobCode.Create}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Job code created successfully." });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _controllerLogger.LogWarning("Failed to create job code. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, errorContent);

                    // Parse backend error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiResponseDto<object>>(
                            errorContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (errorResponse?.Errors != null && errorResponse.Errors.Any())
                        {
                            // Return all error messages from backend
                            var errorMessages = string.Join(", ", errorResponse.Errors);
                            return Json(new { success = false, message = errorMessages });
                        }
                        else if (!string.IsNullOrEmpty(errorResponse?.Message))
                        {
                            return Json(new { success = false, message = errorResponse.Message });
                        }
                    }
                    catch (Exception ex)
                    {
                        _controllerLogger.LogError(ex, "Error parsing backend error response");
                    }

                    return Json(new { success = false, message = "Failed to create job code." });
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error creating job code");
                return Json(new { success = false, message = "An error occurred while creating the job code." });
            }
        }

        private async Task<List<JobCodeGroupDto>?> GetJobCodeListFilterAsync(
            HttpClient client,
            string? backendUrl,
            int idClient,
            string? keywords = null)
        {
            try
            {
                var url = $"{backendUrl}{ApiEndpoints.JobCode.ListFilter}?cid={idClient}";
                if (!string.IsNullOrEmpty(keywords))
                {
                    url += $"&keywords={Uri.EscapeDataString(keywords)}";
                }

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<JobCodeListFilterDto>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return apiResponse?.Data?.Groups ?? new List<JobCodeGroupDto>();
                }
                else
                {
                    _controllerLogger.LogWarning("Failed to fetch job code filters. Status: {StatusCode}", response.StatusCode);
                    return new List<JobCodeGroupDto>();
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error fetching job code filters from API");
                return new List<JobCodeGroupDto>();
            }
        }

        private async Task<List<JobCodeGroupModel>?> GetJobCodeGroupsAsync(
            HttpClient client,
            string? backendUrl,
            int idClient)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.GenericCategory.List("jobcodegroup")}?idClient={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    return await JsonSerializer.DeserializeAsync<List<JobCodeGroupModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                else
                {
                    _controllerLogger.LogWarning("Failed to fetch job code groups. Status: {StatusCode}", response.StatusCode);
                    return new List<JobCodeGroupModel>();
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error fetching job code groups from API");
                return new List<JobCodeGroupModel>();
            }
        }

        private async Task<List<LookupModel>?> GetLookupDataAsync(
            HttpClient client,
            string? backendUrl,
            string enumCategory)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Masters.GetEnums(enumCategory)}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<IEnumerable<EnumFormDetailResponse>>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        // Map EnumFormDetailResponse to LookupModel
                        return apiResponse.Data.Select(e => new LookupModel
                        {
                            Id = e.IdEnum,
                            Value = e.EnumName,
                            Label = e.EnumName,
                            OrderIndex = e.DisplayOrder,
                            IsActiveData = true // Enums don't have IsActiveData, assume all are active
                        }).ToList();
                    }

                    return new List<LookupModel>();
                }
                else
                {
                    _controllerLogger.LogWarning("Failed to fetch enum data for {Category}. Status: {StatusCode}", enumCategory, response.StatusCode);
                    return new List<LookupModel>();
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error fetching enum data for {Category}", enumCategory);
                return new List<LookupModel>();
            }
        }

        private async Task<List<ChangeHistoryModel>?> GetChangeHistoryAsync(
            HttpClient client,
            string? backendUrl,
            int id,
            string pageReference,
            string module)
        {
            try
            {
                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.JobCode.ChangeHistory}?id={id}&pageReference={pageReference}&module={module}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    return await JsonSerializer.DeserializeAsync<List<ChangeHistoryModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                else
                {
                    _controllerLogger.LogWarning("Failed to fetch change history. Status: {StatusCode}", response.StatusCode);
                    return new List<ChangeHistoryModel>();
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error fetching change history");
                return new List<ChangeHistoryModel>();
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var accessCheck = this.CheckDeleteAccess("Helpdesk", "Job Code Management");
            if (accessCheck != null)
            {
                return Json(new { success = false, message = "You don't have permission to delete job codes." });
            }

            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }

                var userInfo = JsonSerializer.Deserialize<Models.UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null || userInfo.PreferredClientId == 0)
                {
                    return Json(new { success = false, message = "Invalid session. Please login again." });
                }

                var idClient = userInfo.PreferredClientId;
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.DeleteAsync($"{backendUrl}{ApiEndpoints.JobCode.Delete(id)}?cid={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Job code deleted successfully." });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _controllerLogger.LogWarning("Failed to delete job code {Id}. Status: {StatusCode}, Response: {Response}",
                        id, response.StatusCode, errorContent);
                    return Json(new { success = false, message = "Failed to delete job code." });
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error deleting job code {Id}", id);
                return Json(new { success = false, message = "An error occurred while deleting the job code." });
            }
        }
    }
}
