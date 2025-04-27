using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using ClaimKit_v1.Models.Responses;
using ClaimKitv1.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClaimKitv1.Services
{
    public class ClaimKitApiService : IClaimKitApiService
    {
        private readonly string _apiUrl;
        private readonly int _timeout;

        public ClaimKitApiService()
        {
            _apiUrl = WebConfigurationManager.AppSettings["ClaimKitApiUrl"];
            _timeout = int.Parse(WebConfigurationManager.AppSettings["ApiTimeoutSeconds"]) * 1000;
        }

        public async Task<ReviewResponse> ReviewNotesAsync(ReviewRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            return await CallApiAsync<ReviewResponse>(jsonPayload);
        }

        public async Task<EnhanceResponse> EnhanceNotesAsync(EnhanceRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            return await CallApiAsync<EnhanceResponse>(jsonPayload);
        }

        public async Task<GenerateClaimResponse> GenerateClaimAsync(GenerateClaimRequest request)
        {
            var jsonPayload = JsonConvert.SerializeObject(request);
            return await CallApiAsync<GenerateClaimResponse>(jsonPayload);
        }

        private async Task<T> CallApiAsync<T>(string jsonPayload) where T : BaseResponse, new()
        {
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

                // For debugging purposes
                System.Diagnostics.Debug.WriteLine($"API Request: {jsonPayload}");

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
                            var responseString = streamReader.ReadToEnd();

                            // For debugging purposes
                            System.Diagnostics.Debug.WriteLine($"API Response: {responseString}");

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
                                System.Diagnostics.Debug.WriteLine($"JSON Deserialization Error: {ex.Message}");
                                return new T
                                {
                                    Status = "error",
                                    Message = $"Error parsing API response: {ex.Message}",
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
                                string errorText = reader.ReadToEnd();
                                System.Diagnostics.Debug.WriteLine($"API Error: {errorText}");

                                return new T
                                {
                                    Status = "error",
                                    Message = $"API Error: {(int)errorResponse.StatusCode} ({errorResponse.StatusDescription}) - {errorText}",
                                    RawResponse = errorText
                                };
                            }
                        }
                    }

                    return new T
                    {
                        Status = "error",
                        Message = $"API Error: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                System.Diagnostics.Debug.WriteLine($"API Call Exception: {ex.Message}");
                return new T
                {
                    Status = "error",
                    Message = $"API Call Error: {ex.Message}"
                };
            }
        }
    }
}