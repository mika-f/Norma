﻿using Newtonsoft.Json;

namespace Norma.Gamma.Models
{
    [AppVersion("1.0.41")]
    public class User
    {
        [JsonProperty("profile")]
        public Profile Profile { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}