using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClaimKitv1.Models.Responses
{
    public class ReviewResponse : BaseResponse
    {
        [JsonProperty("review")]
        public List<ReviewCategory> Review { get; set; }

        [JsonIgnore]
        public TimeSpan ResponseTime { get; set; }
    }
}