using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace CloudArchive.Configuration
{
    public class ConfigContent
    {
        public ConfigContent()
        {
            Areas = new List<Area>();
            StorageList = new List<Storage>();
        }

        [YamlMember(Alias = "storageAccounts")]
        public List<Storage> StorageList { get; set; }

        public List<Area> Areas { get; set; }
    }
}