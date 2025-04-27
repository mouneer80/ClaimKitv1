using System;
using System.IO;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClaimKitv1.Services
{
    /// <summary>
    /// Service for handling application logging in a standardized format
    /// </summary>
    public class LoggingService
    {
        private readonly string _logDirectory;
        private readonly string _apiLogFile;
        private readonly string _errorLogFile;
        private readonly string _userActionLogFile;

        // Singleton instance
        private static LoggingService _instance;

        // Thread-safe singleton implementation
        public static LoggingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (typeof(LoggingService))
                    {
                        if (_instance == null)
                        {
                            _instance = new LoggingService();
                        }
                    }
                }
                return _instance;
            }
        }

        private LoggingService()
        {
            // Base log directory in the application's folder
            _logDirectory = Path.Combine(HttpContext.Current.Server.MapPath("~/"), "Logs");

            // Create log directory if it doesn't exist
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            // Define log file paths
            _apiLogFile = Path.Combine(_logDirectory, "api_log.txt");
            _errorLogFile = Path.Combine(_logDirectory, "error_log.txt");
            _userActionLogFile = Path.Combine(_logDirectory, "user_actions.txt");
        }

        /// <summary>
        /// Logs an API request and response
        /// </summary>
        /// <param name="requestUrl">The API endpoint URL</param>
        /// <param name="requestData">The request payload</param>
        /// <param name="responseData">The response received</param>
        /// <param name="isSuccess">Whether the API call was successful</param>
        public void LogApiCall(string requestUrl, string requestData, string responseData, bool isSuccess)
        {
            try
            {
                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] API CALL");
                logEntry.AppendLine($"URL: {requestUrl}");
                logEntry.AppendLine($"Success: {isSuccess}");

                // Format request JSON for readability if possible
                logEntry.AppendLine("REQUEST:");
                try
                {
                    var formattedRequest = JToken.Parse(requestData).ToString(Formatting.Indented);
                    logEntry.AppendLine(formattedRequest);
                }
                catch
                {
                    // If not valid JSON, log as is
                    logEntry.AppendLine(requestData);
                }

                // Format response JSON for readability if possible
                logEntry.AppendLine("RESPONSE:");
                try
                {
                    var formattedResponse = JToken.Parse(responseData).ToString(Formatting.Indented);
                    logEntry.AppendLine(formattedResponse);
                }
                catch
                {
                    // If not valid JSON, log as is
                    logEntry.AppendLine(responseData);
                }

                logEntry.AppendLine(new string('-', 80));

                // Write to the API log file
                File.AppendAllText(_apiLogFile, logEntry.ToString());
            }
            catch (Exception ex)
            {
                // If logging itself fails, write to error log
                LogError("Failed to log API call", ex);
            }
        }

        /// <summary>
        /// Logs an error that occurred in the application
        /// </summary>
        /// <param name="message">A human-readable error message</param>
        /// <param name="exception">The exception that occurred, if any</param>
        /// <param name="additionalData">Any additional data that might help diagnose the issue</param>
        public void LogError(string message, Exception exception = null, string additionalData = null)
        {
            try
            {
                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR");
                logEntry.AppendLine($"Message: {message}");

                if (exception != null)
                {
                    logEntry.AppendLine($"Exception: {exception.GetType().Name}");
                    logEntry.AppendLine($"Exception Message: {exception.Message}");
                    logEntry.AppendLine($"Stack Trace: {exception.StackTrace}");

                    // Include inner exception details if available
                    if (exception.InnerException != null)
                    {
                        logEntry.AppendLine($"Inner Exception: {exception.InnerException.GetType().Name}");
                        logEntry.AppendLine($"Inner Exception Message: {exception.InnerException.Message}");
                    }
                }

                if (!string.IsNullOrEmpty(additionalData))
                {
                    logEntry.AppendLine("Additional Data:");
                    logEntry.AppendLine(additionalData);
                }

                // Include current HttpContext info if available
                if (HttpContext.Current != null)
                {
                    var request = HttpContext.Current.Request;
                    logEntry.AppendLine($"URL: {request.Url.ToString()}");
                    logEntry.AppendLine($"HTTP Method: {request.HttpMethod}");
                    logEntry.AppendLine($"User IP: {request.UserHostAddress}");
                    logEntry.AppendLine($"User Agent: {request.UserAgent}");
                }

                logEntry.AppendLine(new string('-', 80));

                // Write to the error log file
                File.AppendAllText(_errorLogFile, logEntry.ToString());
            }
            catch
            {
                // If logging itself fails, there's not much we can do
                // In a production environment, this might trigger an alert
            }
        }

        /// <summary>
        /// Logs a user action for audit purposes
        /// </summary>
        /// <param name="action">The action the user performed</param>
        /// <param name="details">Additional details about the action</param>
        public void LogUserAction(string action, string details = null)
        {
            try
            {
                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] USER ACTION");
                logEntry.AppendLine($"Action: {action}");

                if (!string.IsNullOrEmpty(details))
                {
                    logEntry.AppendLine($"Details: {details}");
                }

                // Include user information if available
                if (HttpContext.Current != null && HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    logEntry.AppendLine($"User: {HttpContext.Current.User.Identity.Name}");
                }
                else if (HttpContext.Current != null)
                {
                    logEntry.AppendLine($"User IP: {HttpContext.Current.Request.UserHostAddress}");
                }

                logEntry.AppendLine(new string('-', 50));

                // Write to the user action log file
                File.AppendAllText(_userActionLogFile, logEntry.ToString());
            }
            catch (Exception ex)
            {
                // If logging itself fails, write to error log
                LogError("Failed to log user action", ex);
            }
        }

        /// <summary>
        /// Logs application lifecycle events
        /// </summary>
        /// <param name="eventType">The type of lifecycle event</param>
        /// <param name="details">Details about the event</param>
        public void LogApplicationEvent(string eventType, string details)
        {
            try
            {
                var logFilePath = Path.Combine(_logDirectory, "application_events.txt");

                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {eventType.ToUpper()}");
                logEntry.AppendLine($"Details: {details}");
                logEntry.AppendLine(new string('-', 50));

                // Write to the application events log file
                File.AppendAllText(logFilePath, logEntry.ToString());
            }
            catch (Exception ex)
            {
                // If logging itself fails, write to error log
                LogError("Failed to log application event", ex);
            }
        }

        /// <summary>
        /// Gets the path to the log directory, creating it if it doesn't exist
        /// </summary>
        /// <returns>The full path to the log directory</returns>
        public string GetLogDirectoryPath()
        {
            // Ensure the directory exists
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            return _logDirectory;
        }
    }
}