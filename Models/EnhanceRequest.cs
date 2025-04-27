using Newtonsoft.Json;

namespace ClaimKitv1.Models
{
    public class EnhanceRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; } = "enhance";

        [JsonProperty("hospital_id")]
        public int HospitalId { get; set; }

        [JsonProperty("claimkit_api_key")]
        public string ClaimKitApiKey { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }
    }
}