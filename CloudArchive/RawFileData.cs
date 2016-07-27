using Nalarium;
using Nalarium.Cryptography;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace CloudArchive
{
    [DebuggerDisplay("{Path}; {Name}")]
    public class RawFileData
    {
        private readonly string _fullname;

        public RawFileData()
        {
        }

        public RawFileData(string fullname)
        {
            _fullname = fullname;
        }

        [JsonProperty(PropertyName = "modified")]
        public DateTime? ModifiedDateTime { get; set; }

        [JsonProperty(PropertyName = "created")]
        public DateTime? CreatedDateTime { get; set; }

        public string RelativePath => Url.Join(Path, Name);

        public string Hash => QuickHash.Hash(Content + RelativePath, HashMethod.SHA256);

        public string OldHash { get; set; }

        public string Extension { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public string Content { get; set; }

        public bool IsExplicitMaster { get; internal set; }

        public byte[] Read()
        {
            return File.Exists(_fullname) ? File.ReadAllBytes(_fullname) : null;
        }
    }
}
