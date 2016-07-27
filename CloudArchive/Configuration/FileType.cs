using System.Diagnostics;

namespace CloudArchive.Configuration
{
    [DebuggerDisplay("{Extension}")]
    public class FileType
    {
        public string Extension { get; set; }
    }
}