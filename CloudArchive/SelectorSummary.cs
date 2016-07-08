using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace CloudArchive
{
    [DebuggerDisplay("{Selector}")]
    public class SelectorSummary
    {
        [JsonProperty(PropertyName = "selector")]
        public string Selector { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string FileType { get; set; }

        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; }

        [JsonProperty(PropertyName = "created")]
        public DateTime Created { get; set; }

        [JsonProperty(PropertyName = "updated")]
        public DateTime Updated { get; set; }
    }
}
