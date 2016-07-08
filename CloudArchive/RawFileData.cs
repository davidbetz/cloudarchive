using Nalarium;
using Nalarium.Cryptography;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
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

        public RawFileData(string baseFolder, FileInfo fileInfo)
        {
            Name = fileInfo.Name;
            ModifiedDateTime = fileInfo.LastWriteTime.ToUniversalTime();
            CreatedDateTime = fileInfo.CreationTime.ToUniversalTime();
            //RelativePath = fileInfo.FullName.Substring(baseFolder.Length, fileInfo.FullName.Length - baseFolder.Length);
            Path = Nalarium.Path.Clean(fileInfo.DirectoryName.Substring(baseFolder.Length, fileInfo.DirectoryName.Length - baseFolder.Length)).ToLower(CultureInfo.InvariantCulture);
            Content = File.ReadAllText(fileInfo.FullName);
        }

        [JsonProperty(PropertyName = "modified")]
        public DateTime? ModifiedDateTime { get; set; }

        [JsonProperty(PropertyName = "created")]
        public DateTime? CreatedDateTime { get; set; }

        //[JsonIgnore]
        //public string FullPath { get; set; }

        public string RelativePath => Url.Join(Path, Name);

        public string Hash => QuickHash.Hash(Content + RelativePath, HashMethod.SHA256);

        public string OldHash { get; set; }

        //[JsonIgnore]
        //public string ComputedSelector => Jampad.Content.Selector.CreateFromFileName(RelativePath, allowHyphensInSelector);

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
