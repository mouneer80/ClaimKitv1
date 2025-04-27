using Newtonsoft.Json;

namespace ClaimKitv1.Models
{
    public class GenerateClaimRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; } = "generate_claim";

        [JsonProperty("hospital_id")]
        public int HospitalId { get; set; }

        [JsonProperty("claimkit_api_key")]
        public string ClaimKitApiKey { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }

        [JsonProperty("patient_checkout_time")]
        public long PatientCheckoutTime { get; set; }

        [JsonProperty("hospital_patient_id")]
        public string HospitalPatientId { get; set; }

        [JsonProperty("insurance_company")]
        public string InsuranceCompany { get; set; }

        [JsonProperty("policy_band")]
        public string PolicyBand { get; set; }

        [JsonProperty("policy_id")]
        public string PolicyId { get; set; }
    }
}