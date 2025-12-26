using cfm_frontend.Constants;
using cfm_frontend.DTOs.Privilege;
using cfm_frontend.Models.Privilege;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;

namespace cfm_frontend.Services
{
    public interface IPrivilegeService
    {
        Task<UserPrivileges?> LoadUserPrivilegesAsync();
        Task<UserPrivileges?> LoadUserPrivilegesAsync(string accessToken);
    }

    public class PrivilegeService : IPrivilegeService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PrivilegeService> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public PrivilegeService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PrivilegeService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Load user privileges using token from authenticated session (for background refresh)
        /// </summary>
        public async Task<UserPrivileges?> LoadUserPrivilegesAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var accessToken = await context.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(accessToken))
                {
                    return await LoadUserPrivilegesAsync(accessToken);
                }
            }

            _logger.LogWarning("Cannot load privileges: No access token available in context");
            return null;
        }

        /// <summary>
        /// Load user privileges with explicit access token (for login flow)
        /// </summary>
        public async Task<UserPrivileges?> LoadUserPrivilegesAsync(string accessToken)
        {
            const int maxRetries = 2;
            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                try
                {
                    // Use plain HttpClient (not BackendAPI) to avoid AuthTokenHandler dependency
                    var client = _httpClientFactory.CreateClient();
                    var backendUrl = _configuration["BackendBaseUrl"];

                    // Manually add bearer token
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    _logger.LogInformation("Loading user privileges from API: {Endpoint} (Attempt {Attempt}/{MaxAttempts})",
                        ApiEndpoints.UserInfo.GetUserPrivileges,
                        retryCount + 1,
                        maxRetries + 1);

                    var response = await client.GetAsync($"{backendUrl}{ApiEndpoints.UserInfo.GetUserPrivileges}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (string.IsNullOrWhiteSpace(responseContent))
                        {
                            _logger.LogWarning("Privilege API returned empty response");
                            return null;
                        }

                        _logger.LogDebug("Received privilege response: {ContentLength} characters", responseContent.Length);

                        // Deserialize from string instead of stream to avoid premature stream closure
                        var privilegesDto = JsonSerializer.Deserialize<List<UserPrivilegesResponse>>(
                            responseContent,
                            _jsonOptions
                        );

                        if (privilegesDto != null && privilegesDto.Count > 0)
                        {
                            // Map DTO to domain model
                            var userPrivileges = new UserPrivileges
                            {
                                Modules = [.. privilegesDto.Select(m => new ModulePrivilege
                                {
                                    ModuleName = m.ModuleName,
                                    Pages = [.. m.Pages.Select(p => new PagePrivilege
                                    {
                                        PageName = p.PageName,
                                        CanView = p.CanView,
                                        CanAdd = p.CanAdd,
                                        CanEdit = p.CanEdit,
                                        CanDelete = p.CanDelete
                                    })]
                                })],
                                LoadedAt = DateTime.UtcNow
                            };

                            _logger.LogInformation("Successfully loaded user privileges: {ModuleCount} modules, {PageCount} pages",
                                userPrivileges.Modules.Count,
                                userPrivileges.Modules.Sum(m => m.Pages.Count));

                            return userPrivileges;
                        }
                        else
                        {
                            _logger.LogWarning("Privilege API returned null or empty privilege list");
                            return null;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to load user privileges. Status: {StatusCode}, Reason: {ReasonPhrase}",
                            response.StatusCode,
                            response.ReasonPhrase);
                        return null;
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON deserialization error loading user privileges (Attempt {Attempt}/{MaxAttempts})",
                        retryCount + 1,
                        maxRetries + 1);

                    // JSON errors are not transient, don't retry
                    return null;
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError(httpEx, "HTTP request error loading user privileges (Attempt {Attempt}/{MaxAttempts})",
                        retryCount + 1,
                        maxRetries + 1);

                    // Network errors might be transient, retry
                    retryCount++;
                    if (retryCount <= maxRetries)
                    {
                        var delayMs = retryCount * 500; // 500ms, 1000ms
                        _logger.LogInformation("Retrying in {DelayMs}ms...", delayMs);
                        await Task.Delay(delayMs);
                    }
                }
                catch (TaskCanceledException timeoutEx)
                {
                    _logger.LogError(timeoutEx, "Request timeout loading user privileges (Attempt {Attempt}/{MaxAttempts})",
                        retryCount + 1,
                        maxRetries + 1);

                    // Timeout might be transient, retry
                    retryCount++;
                    if (retryCount <= maxRetries)
                    {
                        var delayMs = retryCount * 500;
                        _logger.LogInformation("Retrying in {DelayMs}ms...", delayMs);
                        await Task.Delay(delayMs);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error loading user privileges (Attempt {Attempt}/{MaxAttempts})",
                        retryCount + 1,
                        maxRetries + 1);

                    // Unknown errors, retry once
                    retryCount++;
                    if (retryCount <= maxRetries)
                    {
                        var delayMs = retryCount * 500;
                        _logger.LogInformation("Retrying in {DelayMs}ms...", delayMs);
                        await Task.Delay(delayMs);
                    }
                }
            }

            _logger.LogError("Failed to load user privileges after {MaxRetries} retries", maxRetries + 1);
            return null;
        }
    }
}
