using System.Diagnostics;

namespace CloudArchive.Configuration
{
    [DebuggerDisplay("{Extension}")]
    public class FileType
    {
        public bool SystemDefault { get; set; }
        public string Extension { get; set; }
        //public string Wrapping { get; set; }
        public string Formatter { get; set; }
    }
}