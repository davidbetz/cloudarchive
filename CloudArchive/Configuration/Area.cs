using System.Collections.Generic;
using System.Diagnostics;
using YamlDotNet.Serialization;

namespace CloudArchive.Configuration
{
    [DebuggerDisplay("{Name}")]
    public class Area
    {
        public Area()
        {
            FileTypes = new List<FileType>();
        }

        [YamlIgnore]
        public Storage Storage { get; set; }

        public string RemoteBranch { get; set; }

        public string Name { get; set; }

        public string Container { get; set; }

        public string Folder { get; set; }

        [YamlMember(Alias = "storage")]
        public string StorageName { get; set; }

        public List<FileType> FileTypes { get; set; }
    }
}