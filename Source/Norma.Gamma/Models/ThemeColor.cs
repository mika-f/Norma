﻿using System;

using Newtonsoft.Json;

using Norma.Gamma.Converters;

namespace Norma.Gamma.Models
{
    [AppVersion("1.0.46")]
    public class ThemeColor
    {
        [JsonProperty("primary")]
        public string Primary { get; set; }

        [JsonProperty("secondary")]
        public string Secondary { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }

        [JsonProperty("background")]
        public string Background { get; set; }

        [JsonProperty("updatedAt")]
        [JsonConverter(typeof(UnixTimeDateTimeConverter))]
        public DateTime UpdatedAt { get; set; }
    }
}