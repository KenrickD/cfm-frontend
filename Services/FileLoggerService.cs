using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace cfm_frontend.Services
{
    /// <summary>
    /// Interface for file-based logging service.
    /// Provides methods to write structured logs to rotating log files.
    /// </summary>
    public interface IFileLoggerService
    {
        /// <summary>
        /// Logs an informational message to the log file.
        /// </summary>
        void LogInfo(string message, string? category = null);

        /// <summary>
        /// Logs a warning message to the log file.
        /// </summary>
        void LogWarning(string message, string? category = null);

        /// <summary>
        /// Logs an error message to the log file.
        /// </summary>
        void LogError(string message, Exception? exception = null, string? category = null);

        /// <summary>
        /// Logs API timing information including success/failure status and duration.
        /// </summary>
        void LogApiTiming(ApiTimingResult result);

        /// <summary>
        /// Logs a batch of API timing results (for parallel API calls).
        /// </summary>
        void LogApiTimingBatch(string operationName, IEnumerable<ApiTimingResult> results, TimeSpan totalDuration);

        /// <summary>
        /// Executes an async operation with timing measurement and records the result.
        /// Use this for individual API calls that need timing tracked.
        /// </summary>
        /// <typeparam name="T">The return type of the async operation</typeparam>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="endpoint">The API endpoint being called</param>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="results">Collection to store the timing result</param>
        /// <returns>The result of the operation</returns>
        Task<T> ExecuteTimedAsync<T>(
            string operationName,
            string endpoint,
            Func<Task<T>> operation,
            List<ApiTimingResult> results);

        /// <summary>
        /// Executes an async operation with timing measurement (without collecting results).
        /// Use this for standalone timed operations where you just want logging.
        /// </summary>
        /// <typeparam name="T">The return type of the async operation</typeparam>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="endpoint">The API endpoint being called</param>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="category">Optional log category</param>
        /// <returns>The result of the operation</returns>
        Task<T> ExecuteTimedAsync<T>(
            string operationName,
            string endpoint,
            Func<Task<T>> operation,
            string? category = null);

        /// <summary>
        /// Updates the record count for a specific API timing result.
        /// </summary>
        void UpdateTimingResultRecordCount(List<ApiTimingResult> results, string operationName, int? count);
    }

    /// <summary>
    /// Represents the result of a timed API call.
    /// </summary>
    public class ApiTimingResult
    {
        public string ApiName { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int? RecordCount { get; set; }
    }

    /// <summary>
    /// File-based logging service that writes structured logs to rotating daily log files.
    /// Thread-safe implementation using concurrent queues and background flushing.
    /// </summary>
    public class FileLoggerService : IFileLoggerService, IDisposable
    {
        private readonly string _logDirectory;
        private readonly ILogger<FileLoggerService> _logger;
        private readonly ConcurrentQueue<string> _logQueue;
        private readonly Timer _flushTimer;
        private readonly object _fileLock = new();
        private readonly string _applicationName;
        private bool _disposed;

        private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB max file size
        private const int FlushIntervalMs = 1000; // Flush every second

        public FileLoggerService(IConfiguration configuration, ILogger<FileLoggerService> logger)
        {
            _logger = logger;
            _logQueue = new ConcurrentQueue<string>();
            _applicationName = configuration["ApplicationName"] ?? "CFM-Frontend";

            // Default to Logs folder in app directory, can be overridden in appsettings.json
            _logDirectory = configuration["Logging:FileLogger:Path"]
                ?? Path.Combine(AppContext.BaseDirectory, "Logs");

            // Ensure log directory exists
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create log directory: {LogDirectory}", _logDirectory);
            }

            // Start background flush timer
            _flushTimer = new Timer(FlushLogs, null, FlushIntervalMs, FlushIntervalMs);
        }

        public void LogInfo(string message, string? category = null)
        {
            EnqueueLog("INFO", message, category);
        }

        public void LogWarning(string message, string? category = null)
        {
            EnqueueLog("WARN", message, category);
        }

        public void LogError(string message, Exception? exception = null, string? category = null)
        {
            var fullMessage = exception != null
                ? $"{message} | Exception: {exception.GetType().Name}: {exception.Message}"
                : message;

            EnqueueLog("ERROR", fullMessage, category);

            // Also log stack trace for errors
            if (exception?.StackTrace != null)
            {
                EnqueueLog("ERROR", $"StackTrace: {exception.StackTrace}", category);
            }
        }

        public void LogApiTiming(ApiTimingResult result)
        {
            var statusIcon = result.Success ? "✓" : "✗";
            var recordInfo = result.RecordCount.HasValue ? $" | Records: {result.RecordCount}" : "";
            var errorInfo = !string.IsNullOrEmpty(result.ErrorMessage) ? $" | Error: {result.ErrorMessage}" : "";

            var message = $"[{statusIcon}] {result.ApiName} | {result.Duration.TotalMilliseconds:F2}ms | Endpoint: {result.Endpoint}{recordInfo}{errorInfo}";

            var level = result.Success ? "INFO" : "WARN";
            EnqueueLog(level, message, "API-TIMING");
        }

        public void LogApiTimingBatch(string operationName, IEnumerable<ApiTimingResult> results, TimeSpan totalDuration)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== {operationName} API Timing Summary ===");
            sb.AppendLine($"Total Duration: {totalDuration.TotalMilliseconds:F2}ms");
            sb.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
            sb.AppendLine(new string('-', 80));

            var resultList = results.ToList();
            var successCount = resultList.Count(r => r.Success);
            var failCount = resultList.Count - successCount;

            sb.AppendLine($"Results: {successCount} succeeded, {failCount} failed out of {resultList.Count} total");
            sb.AppendLine(new string('-', 80));

            foreach (var result in resultList.OrderByDescending(r => r.Duration))
            {
                var statusIcon = result.Success ? "✓" : "✗";
                var recordInfo = result.RecordCount.HasValue ? $" ({result.RecordCount} records)" : "";
                var errorInfo = !string.IsNullOrEmpty(result.ErrorMessage) ? $" - {result.ErrorMessage}" : "";

                sb.AppendLine($"  [{statusIcon}] {result.ApiName,-35} {result.Duration.TotalMilliseconds,10:F2}ms{recordInfo}{errorInfo}");
            }

            sb.AppendLine(new string('=', 80));

            EnqueueLog("INFO", sb.ToString(), "API-TIMING-BATCH");

            // Also log failures separately at WARNING level for easier filtering
            foreach (var failedResult in resultList.Where(r => !r.Success))
            {
                EnqueueLog("WARN",
                    $"API FAILED: {failedResult.ApiName} | Duration: {failedResult.Duration.TotalMilliseconds:F2}ms | Error: {failedResult.ErrorMessage ?? "Unknown error"}",
                    "API-FAILURE");
            }
        }

        private void EnqueueLog(string level, string message, string? category)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var categoryPart = !string.IsNullOrEmpty(category) ? $"[{category}] " : "";
            var logLine = $"[{timestamp}] [{level,-5}] {categoryPart}{message}";

            _logQueue.Enqueue(logLine);
        }

        private void FlushLogs(object? state)
        {
            if (_logQueue.IsEmpty) return;

            var logs = new List<string>();
            while (_logQueue.TryDequeue(out var log))
            {
                logs.Add(log);
            }

            if (logs.Count == 0) return;

            try
            {
                var logFileName = GetCurrentLogFileName();
                var logFilePath = Path.Combine(_logDirectory, logFileName);

                lock (_fileLock)
                {
                    // Check if we need to rotate the file
                    if (File.Exists(logFilePath))
                    {
                        var fileInfo = new FileInfo(logFilePath);
                        if (fileInfo.Length > MaxFileSizeBytes)
                        {
                            RotateLogFile(logFilePath);
                        }
                    }

                    // Append logs to file
                    File.AppendAllLines(logFilePath, logs, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                // Log to standard logger if file logging fails
                _logger.LogError(ex, "Failed to write to log file. Logs count: {LogCount}", logs.Count);
            }
        }

        private string GetCurrentLogFileName()
        {
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return $"{_applicationName}_{date}.log";
        }

        private void RotateLogFile(string currentPath)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("HHmmss");
                var directory = Path.GetDirectoryName(currentPath)!;
                var fileName = Path.GetFileNameWithoutExtension(currentPath);
                var extension = Path.GetExtension(currentPath);

                var rotatedPath = Path.Combine(directory, $"{fileName}_{timestamp}{extension}");
                File.Move(currentPath, rotatedPath);

                _logger.LogInformation("Log file rotated: {OldPath} -> {NewPath}", currentPath, rotatedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rotate log file: {Path}", currentPath);
            }
        }

        public async Task<T> ExecuteTimedAsync<T>(
            string operationName,
            string endpoint,
            Func<Task<T>> operation,
            List<ApiTimingResult> results)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ApiTimingResult
            {
                ApiName = operationName,
                Endpoint = endpoint,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                var data = await operation();
                stopwatch.Stop();

                result.Success = true;
                result.Duration = stopwatch.Elapsed;

                lock (results)
                {
                    results.Add(result);
                }

                return data;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                result.Success = false;
                result.Duration = stopwatch.Elapsed;
                result.ErrorMessage = ex.Message;

                lock (results)
                {
                    results.Add(result);
                }

                LogError($"Timed operation failed: {operationName}", ex, "API-ERROR");

                throw;
            }
        }

        public async Task<T> ExecuteTimedAsync<T>(
            string operationName,
            string endpoint,
            Func<Task<T>> operation,
            string? category = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var data = await operation();
                stopwatch.Stop();

                LogInfo($"[✓] {operationName} | {stopwatch.Elapsed.TotalMilliseconds:F2}ms | Endpoint: {endpoint}", category ?? "API-TIMING");

                return data;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                LogWarning($"[✗] {operationName} | {stopwatch.Elapsed.TotalMilliseconds:F2}ms | Endpoint: {endpoint} | Error: {ex.Message}", category ?? "API-TIMING");
                LogError($"Timed operation failed: {operationName}", ex, "API-ERROR");

                throw;
            }
        }

        public void UpdateTimingResultRecordCount(List<ApiTimingResult> results, string operationName, int? count)
        {
            var result = results.FirstOrDefault(r => r.ApiName == operationName);
            if (result != null)
            {
                result.RecordCount = count;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _flushTimer.Dispose();

            // Final flush
            FlushLogs(null);
        }
    }
}
