using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClaimKitv1.Models.Responses
{
    public class EnhancedData
    {
        [JsonProperty("enhanced_notes")]
        public object EnhancedNotes { get; set; }
    }
}