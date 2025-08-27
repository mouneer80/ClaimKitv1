using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Configuration;
using ClaimKitv1.Models.Responses;
using ClaimKitv1.Models;
using Newtonsoft.Json;

namespace ClaimKitv1.Services
{
    public class ClaimKitApiService : IClaimKitApiService
    {
        private readonly string _apiUrl;
        private readonly string _newApiBaseUrl;  
        private readonly string _reviewEndpoint;   
        private readonly string _enhanceEndpoint;
        private readonly string _apiKey;
        private readonly int _timeout;
        private readonly LoggingService _logger;

        public ClaimKitApiService()
        {
            _apiUrl = WebConfigurationManager.AppSettings["ClaimKitApiUrl"];
            _newApiBaseUrl = WebConfigurationManager.AppSettings["NewClaimKitApiUrl"];
            _reviewEndpoint = WebConfigurationManager.AppSettings["ClaimKitApiReviewEndPoint"];
            _enhanceEndpoint = WebConfigurationManager.AppSettings["ClaimKitApiEnhanceEndPoint"];
            _apiKey = ConfigurationService.ClaimKitApiKey;
            _timeout = int.Parse(WebConfigurationManager.AppSettings["ApiTimeoutSeconds"]) * 1000;
            _logger = LoggingService.Instance;
        }

        public async Task<DoctorNotesReviewResponse> ReviewNotesAsync(DoctorNotesReviewRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            var fullUrl = _newApiBaseUrl + _reviewEndpoint;  // Combine base + endpoint

            _logger.LogUserAction("Review Notes (New API)", $"Claim Reference: {request.ClaimReference}, Doctor: {request.Doctor?.FullName}");
            return await CallNewApiAsync<DoctorNotesReviewResponse>(jsonPayload, fullUrl);
        }

        // ADD THIS - New API Enhance method (if needed)
        public async Task<DoctorNotesEnhanceResponse> EnhanceNotesAsync(DoctorNotesEnhanceRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            var fullUrl = _newApiBaseUrl + _enhanceEndpoint;  // Combine base + endpoint

            _logger.LogUserAction("Enhance Notes (New API)", $"Claim Reference: {request.ClaimReference}");
            return await CallNewApiAsync<DoctorNotesEnhanceResponse>(jsonPayload, fullUrl);
        }

        public async Task<ReviewResponse> ReviewNotesAsync(ReviewRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            _logger.LogUserAction("Review Notes", $"Patient ID: {request.HospitalPatientId}, Doctor: {request.DoctorName}");
            return await CallApiAsync<ReviewResponse>(jsonPayload);
        }

        public async Task<EnhanceResponse> EnhanceNotesAsync(EnhanceRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            _logger.LogUserAction("Enhance Notes", $"Request ID: {request.RequestId}");
            return await CallApiAsync<EnhanceResponse>(jsonPayload);
        }

        public async Task<GenerateClaimResponse> GenerateClaimAsync(GenerateClaimRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            _logger.LogUserAction("Generate Claim", $"Request ID: {request.RequestId}, Patient ID: {request.HospitalPatientId}");
            return await CallApiAsync<GenerateClaimResponse>(jsonPayload);
        }

        private async Task<T> CallNewApiAsync<T>(string jsonPayload, string endpoint) where T : class, new()
        {
            bool isSuccess = false;
            string responseString = string.Empty;

            try
            {
                // Create web request for NEW API
                var request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Timeout = _timeout;
                request.ReadWriteTimeout = _timeout;

                // ADD X-API-KEY HEADER (key difference from legacy API)
                request.Headers.Add("X-API-KEY", _apiKey);

                // Write JSON payload to request
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(jsonPayload);
                    streamWriter.Flush();
                }

                // Get response (same pattern as existing)
                try
                {
                    using (var response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(
                        request.BeginGetResponse,
                        request.EndGetResponse,
                        null))
                    {
                        using (var streamReader = new StreamReader(response.GetResponseStream()))
                        {
                            responseString = streamReader.ReadToEnd();
                            isSuccess = true;

                            try
                            {
                                var result = JsonConvert.DeserializeObject<T>(responseString);
                                return result;
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError("New API JSON Deserialization Error", ex, responseString);

                                // Return appropriate error response based on type
                                if (typeof(T) == typeof(DoctorNotesReviewResponse))
                                {
                                    return new DoctorNotesReviewResponse
                                    {
                                        Status = "error",
                                        Message = new MessageInfo { En = "Could not process server response" }
                                    } as T;
                                }
                                else if (typeof(T) == typeof(DoctorNotesEnhanceResponse))
                                {
                                    return new DoctorNotesEnhanceResponse
                                    {
                                        Status = "error",
                                        Message = new MessageInfo { En = "Could not process server response" }
                                    } as T;
                                }

                                return new T();
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    // Handle error response (same pattern as existing)
                    if (ex.Response != null)
                    {
                        using (var errorResponse = (HttpWebResponse)ex.Response)
                        {
                            using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                            {
                                responseString = reader.ReadToEnd();
                                _logger.LogError($"New API Error ({(int)errorResponse.StatusCode})", ex, responseString);

                                // Return appropriate error response
                                if (typeof(T) == typeof(DoctorNotesReviewResponse))
                                {
                                    return new DoctorNotesReviewResponse
                                    {
                                        Status = "error",
                                        Message = new MessageInfo { En = "System temporarily unavailable" }
                                    } as T;
                                }
                                else if (typeof(T) == typeof(DoctorNotesEnhanceResponse))
                                {
                                    return new DoctorNotesEnhanceResponse
                                    {
                                        Status = "error",
                                        Message = new MessageInfo { En = "System temporarily unavailable" }
                                    } as T;
                                }

                                return new T();
                            }
                        }
                    }

                    _logger.LogError("New API Connection Error", ex);
                    return GetErrorResponse<T>("Connection error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("New API Call Exception", ex, jsonPayload);
                return GetErrorResponse<T>("Unexpected error occurred");
            }
            finally
            {
                _logger.LogApiEndPointCall(endpoint, jsonPayload, responseString, isSuccess);
            }
        }

        // Helper method to create error responses
        private T GetErrorResponse<T>(string message) where T : class, new()
        {
            if (typeof(T) == typeof(DoctorNotesReviewResponse))
            {
                return new DoctorNotesReviewResponse
                {
                    Status = "error",
                    Message = new MessageInfo { En = message }
                } as T;
            }
            else if (typeof(T) == typeof(DoctorNotesEnhanceResponse))
            {
                return new DoctorNotesEnhanceResponse
                {
                    Status = "error",
                    Message = new MessageInfo { En = message }
                } as T;
            }

            return new T();
        }

        private async Task<T> CallApiAsync<T>(string jsonPayload) where T : BaseResponse, new()
        {
            bool isSuccess = false;
            string responseString = string.Empty;

            try
            {
                // Create web request
                var request = (HttpWebRequest)WebRequest.Create(_apiUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Timeout = _timeout;
                request.ReadWriteTimeout = _timeout;

                // Write JSON payload to request
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(jsonPayload);
                    streamWriter.Flush();
                }

                // Get response
                try
                {
                    using (var response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(
                        request.BeginGetResponse,
                        request.EndGetResponse,
                        null))
                    {
                        // Read response
                        using (var streamReader = new StreamReader(response.GetResponseStream()))
                        {
                            responseString = streamReader.ReadToEnd();
                            isSuccess = true;

                            try
                            {
                                // Deserialize to the appropriate type
                                var result = JsonConvert.DeserializeObject<T>(responseString);

                                // Store raw response for debugging if needed
                                result.RawResponse = responseString;

                                return result;
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError("JSON Deserialization Error", ex, responseString);

                                return new T
                                {
                                    Status = "error",
                                    Message = "We couldn't process the response from our medical records system. Our technical team has been notified.",
                                    RawResponse = responseString
                                };
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    // Handle error response
                    if (ex.Response != null)
                    {
                        using (var errorResponse = (HttpWebResponse)ex.Response)
                        {
                            using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                            {
                                responseString = reader.ReadToEnd();
                                _logger.LogError($"API Error ({(int)errorResponse.StatusCode})", ex, responseString);

                                return new T
                                {
                                    Status = "error",
                                    Message = "The system is currently unavailable. Please try again in a few minutes.",
                                    RawResponse = responseString
                                };
                            }
                        }
                    }

                    _logger.LogError("API Connection Error", ex);
                    return new T
                    {
                        Status = "error",
                        Message = "We couldn't connect to our medical records system. Please check your internet connection and try again."
                    };
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                _logger.LogError("API Call Exception", ex, jsonPayload);

                return new T
                {
                    Status = "error",
                    Message = "An unexpected error occurred. Please try again or contact support if the problem persists."
                };
            }
            finally
            {
                // Always log the API call
                _logger.LogApiCall(_apiUrl, jsonPayload, responseString, isSuccess);
            }
        }
    }
}