using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClaimKit_v1.Models.Responses
{
    public class BaseResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }

        [JsonIgnore]
        public string RawResponse { get; set; }

        [JsonIgnore]
        public bool IsSuccess => Status == "success";
    }
}