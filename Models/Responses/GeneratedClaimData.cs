﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClaimKitv1.Models.Responses
{
    public class GeneratedClaimData
    {
        [JsonProperty("generated_claim")]
        public object GeneratedClaim { get; set; }
    }
}