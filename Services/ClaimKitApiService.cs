using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Configuration;
using ClaimKit_v1.Models.Responses;
using ClaimKitv1.Models;
using Newtonsoft.Json;

namespace ClaimKitv1.Services
{
    /// <summary>
    /// Service for communicating with the ClaimKit API
    /// </summary>
    public class ClaimKitApiService : IClaimKitApiService
    {
        private readonly string _apiUrl;
        private readonly int _timeout;
        private readonly LoggingService _logger;

        public ClaimKitApiService()
        {
            _apiUrl = WebConfigurationManager.AppSettings["ClaimKitApiUrl"];
            _timeout = int.Parse(WebConfigurationManager.AppSettings["ApiTimeoutSeconds"]) * 1000;
            _logger = LoggingService.Instance;
        }

        /// <summary>
        /// Sends a review request to analyze doctor's notes
        /// </summary>
        /// <param name="request">The review request object</param>
        /// <returns>The API response</returns>
        public async Task<ReviewResponse> ReviewNotesAsync(ReviewRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            _logger.LogUserAction("Review Notes", $"Patient ID: {request.HospitalPatientId}, Doctor: {request.DoctorName}");
            return await CallApiAsync<ReviewResponse>(jsonPayload);
        }

        /// <summary>
        /// Sends a request to enhance doctor's notes
        /// </summary>
        /// <param name="request">The enhance request object</param>
        /// <returns>The API response with enhanced notes</returns>
        public async Task<EnhanceResponse> EnhanceNotesAsync(EnhanceRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            _logger.LogUserAction("Enhance Notes", $"Request ID: {request.RequestId}");
            return await CallApiAsync<EnhanceResponse>(jsonPayload);
        }

        /// <summary>
        /// Sends a request to generate an insurance claim
        /// </summary>
        /// <param name="request">The generate claim request object</param>
        /// <returns>The API response with generated claim data</returns>
        public async Task<GenerateClaimResponse> GenerateClaimAsync(GenerateClaimRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            _logger.LogUserAction("Generate Claim", $"Request ID: {request.RequestId}, Patient ID: {request.HospitalPatientId}");
            return await CallApiAsync<GenerateClaimResponse>(jsonPayload);
        }

        /// <summary>
        /// Generic method to call the API with the appropriate request and process the response
        /// </summary>
        /// <typeparam name="T">Response type to deserialize to</typeparam>
        /// <param name="jsonPayload">The JSON payload to send</param>
        /// <returns>The deserialized response object</returns>
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