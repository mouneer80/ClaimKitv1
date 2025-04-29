using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClaimKitv1.Models.Responses
{
    public class GenerateClaimResponse : BaseResponse
    {
        [JsonProperty("data")]
        public GeneratedClaimData Data { get; set; }
    }
}