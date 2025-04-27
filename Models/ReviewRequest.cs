using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClaimKitv1.Models
{
    public class ReviewRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; } = "review";

        [JsonProperty("hospital_id")]
        public int HospitalId { get; set; }

        [JsonProperty("claimkit_api_key")]
        public string ClaimKitApiKey { get; set; }

        [JsonProperty("hospital_patient_id")]
        public string HospitalPatientId { get; set; }

        [JsonProperty("doctor_notes")]
        public string DoctorNotes { get; set; }

        [JsonProperty("insurance_company")]
        public string InsuranceCompany { get; set; }

        [JsonProperty("policy_band")]
        public string PolicyBand { get; set; }

        [JsonProperty("policy_id")]
        public string PolicyId { get; set; }

        [JsonProperty("patient_checkin_time")]
        public long PatientCheckinTime { get; set; }

        [JsonProperty("doctor_name")]
        public string DoctorName { get; set; }

        [JsonProperty("doctor_specialization")]
        public string DoctorSpecialization { get; set; }

        [JsonProperty("hospital_doctor_id")]
        public string HospitalDoctorId { get; set; }

        [JsonProperty("patient_history")]
        public JArray PatientHistory { get; set; }
    }
}