using Newtonsoft.Json;
using System.Collections.Generic;

namespace CloudArchive
{
    public class RawFilePackage
    {
        public RawFilePackage()
        {
            AssetDataList = new List<RawFileData>();
            AssetStabilityInformation = new List<SelectorSummary>();
        }

        [JsonProperty(PropertyName = "area")]
        public string Area { get; set; }

        [JsonProperty(PropertyName = "assetMetadata")]
        public List<SelectorSummary> AssetStabilityInformation { set; get; }

        [JsonProperty(PropertyName = "assetData")]
        public List<RawFileData> AssetDataList { set; get; }
    }
}
