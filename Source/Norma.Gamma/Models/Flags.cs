﻿using Newtonsoft.Json;

namespace Norma.Gamma.Models
{
    [AppVersion("1.0.41")]
    public class Flags
    {
        [JsonProperty("share")]
        public bool IsShare { get; set; }

        [JsonProperty("timeshift")]
        public bool IsTimeShift { get; set; }
    }
}