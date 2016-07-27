using CloudArchive.Configuration;
using Nalarium;
using Nalarium.Cryptography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudArchive
{
    public static class FileStructure
    {
        public static RawFilePackage CreatePackage(Area areaConfig, bool fullUpdate = false)
        {
            if (areaConfig.FileTypes?.Count > 0)
            {
                return CreatePackage(areaConfig, areaConfig.FileTypes, fullUpdate);
            }
            return CreatePackage(areaConfig, areaConfig.FileTypes, fullUpdate);
        }

        private static RawFilePackage CreatePackage(Area areaConfig, IEnumerable<FileType> assetTypeData, bool fullUpdate = false)
        {
            if (areaConfig == null)
            {
                throw new ArgumentNullException(nameof(areaConfig));
            }
            if (string.IsNullOrEmpty(areaConfig.Name))
            {
                throw new ArgumentException("Required area name");
            }
            if (string.IsNullOrEmpty(areaConfig.Folder))
            {
                throw new ArgumentException("Required area folder");
            }

            var package = new RawFilePackage()
            {
                Area = areaConfig.Name
            };
            var stabilizationPath = System.IO.Path.Combine(areaConfig.Folder, Constants.Dates);
            if (File.Exists(stabilizationPath))
            {
                package.AssetStabilityInformation = JsonConvert.DeserializeObject<List<SelectorSummary>>(File.ReadAllText(stabilizationPath));
            }
            var result = LoadRawStructure(assetTypeData, areaConfig.Folder, areaConfig.Folder, package, fullUpdate);
            package.AssetDataList.AddRange(result.Item2);
            return package;
        }

        private static Tuple<List<RawFileData>, List<RawFileData>> LoadRawStructure(IEnumerable<FileType> fileTypeData, string baseFolder, string folder, RawFilePackage package, bool fullUpdate = false)
        {
            var context = Url.FromPath(Nalarium.Path.Clean(folder.Substring(baseFolder.Length, folder.Length - baseFolder.Length)).ToLower(CultureInfo.InvariantCulture));
            var partArray = Url.GetUrlPartArray(context) ?? new string[] { };
            var partList = new List<string>();
            foreach (var part in partArray)
            {
                partList.Add(part.StartsWith("$") ? part.Substring(1) : part);
            }
            context = string.Join("/", partList.ToArray());
            var list = new List<RawFileData>();
            var assetList = new List<RawFileData>();
            var assetTypeList = fileTypeData?.ToList() ?? new List<FileType>();
            if (assetTypeList.Count > 0)
            {
                var filteredBlobData =
                    Directory
                        .GetFiles(folder)
                        .Select(p => new FileInfo(p))
                        .Where(p => !p.Name.StartsWith("_", StringComparison.InvariantCultureIgnoreCase) && !p.Name.StartsWith(".", StringComparison.InvariantCultureIgnoreCase));

                foreach (var p in filteredBlobData)
                {
                    var extension = p.Extension.Substring(1);
                    if (!assetTypeList.Any(o => o.Extension.Equals(extension, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }
                    var created = package.AssetStabilityInformation.FirstOrDefault(o => o.Category == "asset" && o.Selector.Equals(Url.Join(context, p.Name)))?.Created ?? p.CreationTime.ToUniversalTime();
                    var rfd = new RawFileData(p.FullName)
                    {
                        CreatedDateTime = created,
                        ModifiedDateTime = p.LastWriteTime.ToUniversalTime(),
                        Name = p.Name.ToLower(),
                        IsExplicitMaster = context.ToLower().Contains(Constants.Master),
                        Extension = p.Extension.Substring(1),
                        Path = Url.Clean(context.ToLower().Replace(Constants.Master, string.Empty)),
                        Content = File.Exists(p.FullName) ? QuickHash.HashFile(p.FullName, HashMethod.SHA256) : string.Empty
                    };
                    var existing = package.AssetStabilityInformation.FirstOrDefault(o => o.Category == "asset" && o.Selector.Equals(Url.Join(context, p.Name), StringComparison.InvariantCultureIgnoreCase));
                    if (existing == null || fullUpdate)
                    {
                        assetList.Add(rfd);
                    }
                    else if (!(existing.Hash ?? string.Empty).Equals(rfd.Content))
                    {
                        rfd.OldHash = existing.Hash;
                        assetList.Add(rfd);
                    }
                }
            }
            //+ sub-structure
            foreach (var d in Directory
                .GetDirectories(folder)
                .Select(p => new DirectoryInfo(p))
                .Where(p => !p.Name.StartsWith(".", StringComparison.CurrentCultureIgnoreCase)))
            {
                var result = LoadRawStructure(fileTypeData, baseFolder, d.FullName.ToLower(), package, fullUpdate);
                package.AssetDataList.AddRange(result.Item2);
            }
            return new Tuple<List<RawFileData>, List<RawFileData>>(list, assetList);
        }

        public static void Finalize(string baseFolder, IEnumerable<SelectorSummary> updatedSelectorList, List<SelectorSummary> list, DateTime datetime)
        {
            var utc = datetime.ToUniversalTime();
            var stabilizationPath = System.IO.Path.Combine(baseFolder, Constants.Dates);
            //var jilStabilizationPath = Path.Combine(baseFolder, ".jil" + Constants.Dates);
            if (list == null && File.Exists(stabilizationPath))
            {
                list = JsonConvert.DeserializeObject<List<SelectorSummary>>(File.ReadAllText(stabilizationPath));
            }
            //else if (File.Exists(jilStabilizationPath))
            //{
            //    list = JsonConvert.DeserializeObject<List<SelectorSummary>>(File.ReadAllText(jilStabilizationPath));
            //}
            if (list == null)
            {
                list = new List<SelectorSummary>();
            }
            foreach (var summary in updatedSelectorList)
            {
                var entry = list.FirstOrDefault(p => p.Selector.Equals(summary.Selector) && p.FileType == null);
                if (entry != null)
                {
                    entry.FileType = summary.FileType;
                }
                //++ FileType == null => default selector
                entry = list.FirstOrDefault(p => p.Selector.Equals(summary.Selector) && (p.FileType?.Equals(summary.FileType) ?? false));
                if (entry != null)
                {
                    entry.Updated = utc;
                    entry.Hash = summary.Hash;
                    entry.Category = summary.Category;
                }
                else
                {
                    //++ TODO: add time zone setting
                    list.Add(new SelectorSummary
                    {
                        Selector = summary.Selector,
                        FileType = summary.FileType,
                        Created = utc,
                        Updated = utc,
                        Category = summary.Category,
                        Hash = summary.Hash
                    });
                }
            }
            var entryData = list.Where(p => p.Category == "entry").OrderByDescending(p => p.Created);
            var assetData = list.Where(p => p.Category == "asset").OrderByDescending(p => p.Created);
            list = new List<SelectorSummary>(entryData);
            list.AddRange(assetData);
            //++ still testing jil, this is primary
            File.WriteAllText(System.IO.Path.Combine(baseFolder, Constants.Dates), JsonConvert.SerializeObject(list, Formatting.Indented), Encoding.UTF8);
        }
    }
}
