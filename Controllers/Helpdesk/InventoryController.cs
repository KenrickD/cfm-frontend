using cfm_frontend.Constants;
using cfm_frontend.Extensions;
using cfm_frontend.Models;
using cfm_frontend.Models.Inventory;
using cfm_frontend.Services;
using cfm_frontend.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace cfm_frontend.Controllers.Helpdesk
{
    public class InventoryController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<InventoryController> logger,
            IPrivilegeService privilegeService)
            : base(privilegeService, logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        #region Inventory Management

        /// <summary>
        /// GET: Inventory Management Index page
        /// </summary>
        public async Task<IActionResult> Index(
            int page = 1,
            string search = "",
            List<string>? transactionStatuses = null,
            List<int>? workRequestIds = null,
            DateTime? transactionDateFrom = null,
            DateTime? transactionDateTo = null)
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

                var requestBody = new InventoryFilterModel
                {
                    Client_idClient = idClient,
                    page = page,
                    keyWordSearch = search,
                    TransactionStatuses = transactionStatuses ?? new List<string>(),
                    WorkRequestIds = workRequestIds ?? new List<int>(),
                    transactionDateFrom = transactionDateFrom,
                    transactionDateTo = transactionDateTo
                };

                var transactionsTask = GetInventoryTransactionsAsync(client, backendUrl, requestBody);
                var filterOptionsTask = GetFilterOptionsAsync(client, backendUrl, idClient);

                await Task.WhenAll(transactionsTask, filterOptionsTask);

                var transactionsResponse = await transactionsTask;
                if (transactionsResponse != null)
                {
                    viewmodel.Transactions = transactionsResponse.data?
                        .OrderByDescending(t => t.transactionDate)
                        .ToList() ?? new List<InventoryTransactionResponseDto>();

                    viewmodel.Paging = new PagingInfo
                    {
                        CurrentPage = transactionsResponse.Metadata.CurrentPage,
                        TotalPages = transactionsResponse.Metadata.TotalPages,
                        PageSize = transactionsResponse.Metadata.PageSize,
                        TotalCount = transactionsResponse.Metadata.TotalCount
                    };
                }

                viewmodel.FilterOptions = await filterOptionsTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory management index page");
                viewmodel.FilterOptions = new InventoryFilterOptionsModel();
            }

            return View("~/Views/Helpdesk/Inventory/Index.cshtml", viewmodel);
        }

        /// <summary>
        /// GET: Material search for autocomplete
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

                var idClient = userInfo.PreferredClientId;

                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Inventory.SearchMaterials}?idClient={idClient}&term={Uri.EscapeDataString(term)}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<MaterialSearchResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return Json(result);
                }

                return Json(new { success = false, message = "Failed to search materials" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching materials");
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        /// <summary>
        /// POST: Create new inventory transaction
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Create(InventoryTransactionCreateRequest request)
        {
            var accessCheck = this.CheckAddAccess("Helpdesk", "Inventory Management");
            if (accessCheck != null) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var userSessionJson = HttpContext.Session.GetString("UserSession");
                var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                request.Client_idClient = userInfo.PreferredClientId;

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Inventory.Create}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<InventoryTransactionSaveResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return Json(result);
                }

                return Json(new { success = false, message = "Failed to create transaction" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory transaction");
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

                var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.Inventory.GetById(id)}");

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var transaction = await JsonSerializer.DeserializeAsync<InventoryTransactionResponseDto>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return Json(new { success = true, data = transaction });
                }

                return Json(new { success = false, message = "Transaction not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transaction {Id}", id);
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        /// <summary>
        /// PUT: Update inventory transaction
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Update(int id, InventoryTransactionCreateRequest request)
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

                request.Client_idClient = userInfo.PreferredClientId;

                var jsonPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{backendUrl}{ApiEndpoints.Inventory.Update(id)}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<InventoryTransactionSaveResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return Json(result);
                }

                return Json(new { success = false, message = "Failed to update transaction" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory transaction {Id}", id);
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        /// <summary>
        /// DELETE: Delete inventory transaction
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

                var response = await client.DeleteAsync($"{backendUrl}{ApiEndpoints.Inventory.Delete(id)}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Transaction deleted successfully" });
                }

                return Json(new { success = false, message = "Failed to delete transaction" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inventory transaction {Id}", id);
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper: Get inventory transactions from backend API
        /// </summary>
        private async Task<InventoryListApiResponse?> GetInventoryTransactionsAsync(
            HttpClient client,
            string backendUrl,
            InventoryFilterModel requestBody)
        {
            try
            {
                var jsonPayload = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}{ApiEndpoints.Inventory.List}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    return await JsonSerializer.DeserializeAsync<InventoryListApiResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to load inventory transactions. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory transactions");
                return null;
            }
        }

        /// <summary>
        /// Helper: Get filter options from backend API
        /// </summary>
        private async Task<InventoryFilterOptionsModel> GetFilterOptionsAsync(
            HttpClient client,
            string backendUrl,
            int idClient)
        {
            try
            {
                var response = await client.GetAsync(
                    $"{backendUrl}{ApiEndpoints.Inventory.GetFilterOptions}?idClient={idClient}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var options = await JsonSerializer.DeserializeAsync<InventoryFilterOptionsModel>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return options ?? new InventoryFilterOptionsModel();
                }

                _logger.LogWarning("Failed to load filter options. Status: {StatusCode}", response.StatusCode);
                return new InventoryFilterOptionsModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory filter options");
                return new InventoryFilterOptionsModel();
            }
        }

        #endregion
    }
}
