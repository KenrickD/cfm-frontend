using cfm_frontend.Constants;
using cfm_frontend.DTOs;
using cfm_frontend.DTOs.Inventory;
using cfm_frontend.DTOs.JobCode;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using cfm_frontend.Models.WorkRequest;
using cfm_frontend.Services;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace cfm_frontend.Controllers.Helpdesk
{
    [Authorize]
    public class InventoryController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InventoryController> _controllerLogger;

        public InventoryController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<InventoryController> logger,
            IPrivilegeService privilegeService)
            : base(privilegeService, logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _controllerLogger = logger;
        }

        #region Inventory Management

        /// <summary>
        /// GET: Inventory Management Index page with list and filters
        /// </summary>
        public async Task<IActionResult> Index(
            int page = 1,
            string search = "",
            string? movementTypes = null,
            DateTime? transactionDateStart = null,
            DateTime? transactionDateEnd = null)
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Inventory Management");
            if (accessCheck != null) return accessCheck;

            var viewmodel = new InventoryViewModel();

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (string.IsNullOrEmpty(userSessionJson))
                {
                    return RedirectToAction("Index", "Login");
                }

                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userInfo == null)
                {
                    return RedirectToAction("Index", "Login");
                }

                var idClient = userInfo.PreferredClientId;

                // Parse movement types filter
                int[]? movementTypesArray = null;
                if (!string.IsNullOrEmpty(movementTypes))
                {
                    movementTypesArray = movementTypes.Split(',')
                        .Where(s => int.TryParse(s, out _))
                        .Select(int.Parse)
                        .ToArray();
                }

                // Build request payload
                var requestBody = new InventoryListParamDto
                {
                    IdClient = idClient,
                    Keywords = search,
                    Page = page,
                    PageSize = 20,
                    Filters = new InventoryListFilterParamDto
                    {
                        MovementTypes = movementTypesArray,
                        TransactionDateStart = transactionDateStart,
                        TransactionDateEnd = transactionDateEnd
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Inventory.List}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<PagedResponse<InventoryListDto>>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        viewmodel.Transactions = apiResponse.Data.Data?.ToList() ?? new List<InventoryListDto>();
                        viewmodel.Paging = new PagingInfo
                        {
                            CurrentPage = apiResponse.Data.Metadata?.CurrentPage ?? 1,
                            TotalPages = apiResponse.Data.Metadata?.TotalPages ?? 1,
                            PageSize = apiResponse.Data.Metadata?.PageSize ?? 20,
                            TotalCount = apiResponse.Data.Metadata?.TotalCount ?? 0
                        };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _controllerLogger.LogWarning("Failed to load inventory list. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                }

                viewmodel.SearchKeyword = search;
                viewmodel.IdClient = idClient;
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error loading inventory management index page");
                viewmodel.Transactions = new List<InventoryListDto>();
            }

            return View("~/Views/Helpdesk/Inventory/Index.cshtml", viewmodel);
        }

        /// <summary>
        /// GET: Get transaction status options for dropdowns
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetTransactionStatuses()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Masters.GetEnums("inventoryTransactionStatus")}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<List<EnumFormDetailResponse>>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return Json(new { success = true, data = apiResponse.Data });
                    }
                }

                return Json(new { success = false, message = "Failed to load transaction statuses" });
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error loading transaction statuses");
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        /// <summary>
        /// GET: Search materials for typeahead dropdown
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> SearchMaterials(string term)
        {
            var accessCheck = this.CheckViewAccess("Helpdesk", "Inventory Management");
            if (accessCheck != null) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var userSessionJson = HttpContext.Session.GetString("UserSession");
                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                var idClient = userInfo.PreferredClientId;

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.JobCode.Materials}?cid={idClient}&prefix={Uri.EscapeDataString(term)}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<List<JobCodeLookupDto>>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return Json(new { success = true, data = apiResponse.Data });
                    }
                }

                return Json(new { success = false, message = "Failed to search materials" });
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error searching materials");
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        /// <summary>
        /// POST: Create new inventory transaction
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] InventoryPayloadDto request)
        {
            // Debug: Log request headers
            _controllerLogger.LogInformation("=== Create Action Called ===");
            _controllerLogger.LogInformation("Request Headers:");
            foreach (var header in Request.Headers)
            {
                _controllerLogger.LogInformation("{Key}: {Value}", header.Key, header.Value);
            }

            var accessCheck = this.CheckAddAccess("Helpdesk", "Inventory Management");
            if (accessCheck != null) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var userSessionJson = HttpContext.Session.GetString("UserSession");
                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                request.IdClient = userInfo.PreferredClientId;

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Inventory.Create}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Transaction created successfully" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _controllerLogger.LogWarning("Create inventory transaction failed. Status: {StatusCode}, Response: {Response}",
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

                    return Json(new { success = false, message = "Failed to create transaction" });
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error creating inventory transaction");
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        /// <summary>
        /// GET: Get transaction by ID for editing
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetById(int id)
        {
            var accessCheck = this.CheckEditAccess("Helpdesk", "Inventory Management");
            if (accessCheck != null) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var userSessionJson = HttpContext.Session.GetString("UserSession");
                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                var idClient = userInfo.PreferredClientId;

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Inventory.GetById(id)}?cid={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponseDto<InventoryDetailsDto>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return Json(new { success = true, data = apiResponse.Data });
                    }
                }

                return Json(new { success = false, message = "Transaction not found" });
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error fetching transaction {Id}", id);
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        /// <summary>
        /// POST: Update inventory transaction
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Update([FromBody] InventoryPayloadDto request)
        {
            var accessCheck = this.CheckEditAccess("Helpdesk", "Inventory Management");
            if (accessCheck != null) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var userSessionJson = HttpContext.Session.GetString("UserSession");
                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                request.IdClient = userInfo.PreferredClientId;

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{backendUrl}{ApiEndpoints.Inventory.Update}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Transaction updated successfully" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _controllerLogger.LogWarning("Update inventory transaction failed. Status: {StatusCode}, Response: {Response}",
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

                    return Json(new { success = false, message = "Failed to update transaction" });
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error updating inventory transaction");
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        /// <summary>
        /// POST: Delete inventory transaction
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Delete(int id)
        {
            var accessCheck = this.CheckDeleteAccess("Helpdesk", "Inventory Management");
            if (accessCheck != null) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var userSessionJson = HttpContext.Session.GetString("UserSession");
                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo == null)
                {
                    return Json(new { success = false, message = "Session expired" });
                }

                var idClient = userInfo.PreferredClientId;

                var response = await client.DeleteAsync($"{backendUrl}{ApiEndpoints.Inventory.Delete(id)}?cid={idClient}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Transaction deleted successfully" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _controllerLogger.LogWarning("Delete inventory transaction failed. Status: {StatusCode}, Response: {Response}",
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

                    return Json(new { success = false, message = "Failed to delete transaction" });
                }
            }
            catch (Exception ex)
            {
                _controllerLogger.LogError(ex, "Error deleting inventory transaction {Id}", id);
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        #endregion
    }
}
