using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CloudArchive.Client;
using CloudArchive.Configuration;
using Serilog;

namespace CloudArchive.Console
{
    public static class AssetClient
    {
        private static readonly Dictionary<string, string> ContentTypeDictionary;

        static AssetClient()
        {
            ContentTypeDictionary = new Dictionary<string, string>
            {
                {"css", "text/css"},
                {"js", "text/javascript"},
                {"xml", "text/xml"},
                {"json", "application/json"},
                {"svg", "image/svg+xml"},
                {"png", "image/svg"},
                {"jpg", "image/jpeg"},
                {"jpeg", "image/jpeg"},
                {"gif", "image/gif"},
                {"pdf", "application/pdf"},
                {"txt", "text/plain"},
                {"md", "text/plain"},
                {"htm", "text/html"},
                {"html", "text/html"}
            };
        }

        public static async Task<IEnumerable<SelectorSummary>> Update(string area, RawFilePackage package, bool live = false, bool force = false)
        {
            var list = new List<SelectorSummary>();
            var client = AssetProviderBuilder.Create(area);
            var areaConfig = Config.LoadArea(area);
            foreach (var asset in package.AssetDataList)
            {
                string selector = asset.RelativePath;
                if (!String.IsNullOrEmpty(areaConfig.RemoteBranch))
                {
                    selector = Nalarium.Url.Join(areaConfig.RemoteBranch, selector);
                }
                if (live)
                {
                    var code = await client.Check(area, selector, asset.Content);
                    if (force || code == AssetStatusCode.DoesNotExist || code == AssetStatusCode.Different)
                    {
                        list.Add(new SelectorSummary
                        {
                            Category = "asset",
                            FileType = asset.Extension,
                            Selector = asset.RelativePath,
                            Hash = await client.Update(area, selector, GetContentType(asset.Extension), asset.Read())
                        });
                        Log.Logger.Debug($"Updated file {selector}");

                    }
                }
                else
                {
                    Log.Logger.Debug($"File {selector} would have been updated in live mode.");
                }
            }
            if (live)
            {
                await client.EnsureAccess(area);
            }
            return list;
        }

        private static string GetContentType(string extension)
        {
            if (ContentTypeDictionary.ContainsKey(extension))
            {
                return ContentTypeDictionary[extension];
            }
            return string.Empty;
        }
    }
}