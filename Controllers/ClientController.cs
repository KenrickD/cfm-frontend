using cfm_frontend.DTOs;
using cfm_frontend.DTOs.Client;
using cfm_frontend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace cfm_frontend.Controllers
{
    public class ClientController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClientController> _logger;

        public ClientController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ClientController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var payload = new
                {
                    accessToken = accessToken,
                    refreshToken = refreshToken
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}/api/client/list", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var clientList = await JsonSerializer.DeserializeAsync<ClientListResponse>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return Ok(clientList);
                }
                else
                {
                    _logger.LogWarning("Failed to fetch client list. Status: {Status}", response.StatusCode);
                    return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to fetch client list" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching client list");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SwitchClient([FromBody] SwitchClientRequest request)
        {
            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                var refreshToken = await HttpContext.GetTokenAsync("refresh_token");

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var client = _httpClientFactory.CreateClient("BackendAPI");
                var backendUrl = _configuration["BackendBaseUrl"];

                var payload = new
                {
                    accessToken = accessToken,
                    refreshToken = refreshToken,
                    clientId = request.ClientId
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{backendUrl}/api/client/switch", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    var switchResponse = await JsonSerializer.DeserializeAsync<ApiResponse<object>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (switchResponse != null && switchResponse.success)
                    {
                        var userSessionJson = HttpContext.Session.GetString("UserSession");
                        if (!string.IsNullOrEmpty(userSessionJson))
                        {
                            var userInfo = JsonSerializer.Deserialize<UserInfo>(userSessionJson);
                            if (userInfo != null)
                            {
                                userInfo.PreferredClientId = request.ClientId;

                                HttpContext.Session.SetString("UserSession", JsonSerializer.Serialize(userInfo));
                            }
                        }

                        _logger.LogInformation("User switched to client {ClientId}", request.ClientId);
                        return Ok(new { success = true, message = "Client switched successfully"});
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = switchResponse?.message ?? "Failed to switch client" });
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to switch client. Status: {Status}", response.StatusCode);
                    return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to switch client" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching client");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}