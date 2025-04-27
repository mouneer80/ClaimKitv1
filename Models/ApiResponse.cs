using Newtonsoft.Json.Linq;

namespace ClaimKitv1.Models
{
    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string RequestId { get; set; }
        public JObject Data { get; set; }
        public JArray Review { get; set; }
        public string RawResponse { get; set; } // Store raw response for debugging
    }
}