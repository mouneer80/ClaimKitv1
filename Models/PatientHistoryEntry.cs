using System;
using Newtonsoft.Json;

namespace ClaimKitv1.Models
{
    public class PatientHistoryEntry
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("doctor")]
        public string Doctor { get; set; }

        [JsonProperty("diagnosis")]
        public string Diagnosis { get; set; }

        [JsonProperty("treatment")]
        public string Treatment { get; set; }
    }
}