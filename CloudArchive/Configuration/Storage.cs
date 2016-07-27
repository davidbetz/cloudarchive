using System.Diagnostics;

namespace CloudArchive.Configuration
{
    [DebuggerDisplay("{Name}, {Provider}")]
    public class Storage
    {
        public string Name { get; set; }

        public string Provider { get; set; }

        public string Key1 { get; set; }

        public string Key2 { get; set; }
    }
}