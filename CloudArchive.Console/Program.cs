using CloudArchive.Configuration;
using CommandLine;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudArchive.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var task = Task.Run(async () => await MainAsync(args));
            task.Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                //options.Area = "books";
                //options.FullUpdate = true;
                //options.Live = true;
                var full = options.FullUpdate ? "full " : string.Empty;
                System.Console.WriteLine($"Beginning {full}scan...");
                try
                {
                    if (options.Verbose)
                    {
                        Log.Logger = new LoggerConfiguration()
                            .MinimumLevel
                            .Debug()
                            .WriteTo
                            .ColoredConsole()
                            //.Trace()
                            .CreateLogger();
                    }
                    await Run(options);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
                }
                System.Console.WriteLine("Finished. Press enter to exit.");
                System.Console.ReadLine();
            }
        }

        public static async Task Run(Options options)
        {
            var config = Config.Load();
            IEnumerable<Area> areaData;
            if (string.IsNullOrEmpty(options.Area))
            {
                System.Console.WriteLine("Area is required.");
                return;
            }
            else
            {
                areaData = config.Areas.Where(p => p.Name.Equals(options.Area, StringComparison.InvariantCultureIgnoreCase));
            }
            var areaList = areaData.ToList();
            foreach (var area in areaList)
            {
                await RunIndexing(options, area.Name, options.FullUpdate);
            }
        }

        private static async Task RunIndexing(Options options, string area, bool fullUpdate)
        {
            var areaConfig = Config.LoadArea(area);
            if (areaConfig == null)
            {
                return;
            }

            if (options.Verbose)
            {
                System.Console.WriteLine($"Storage: {areaConfig.StorageName}");
            }
            //+
            RawFilePackage package = null;
            package = FileStructure.CreatePackage(areaConfig, fullUpdate);
            //+
            if (package != null && package.AssetDataList.Count > 0)
            {
                if (options.Live)
                {
                    System.Console.WriteLine($"Publishing {area}...");
                }
                var now = DateTime.Now;
                var updatedList = new List<SelectorSummary>();
                if (package.AssetDataList.Count > 0 && string.IsNullOrEmpty(areaConfig.Storage.Provider))
                {
                    System.Console.WriteLine($"Warning: AssetStorageProvider is not set and EnableAssets it false, but there are assets to deploy. ({package.AssetDataList.Count}).");
                }
                else if (package.AssetDataList.Count > 0)
                {
                    updatedList.AddRange(await AssetClient.Update(area, package, options.Live, options.FullUpdate));
                }
                if (updatedList.Count > 0)
                {
                    var later = DateTime.Now;
                    if (options.Verbose)
                    {
                        System.Console.WriteLine($"Publish complete ({(later - now).Milliseconds}ms)");
                    }
                    if (options.Verbose)
                    {
                        System.Console.WriteLine($"Updating timestamps and hashes...");
                    }
                    FileStructure.Finalize(areaConfig.Folder, updatedList, package.AssetStabilityInformation, DateTime.Now);
                }
                System.Console.WriteLine($"{area} complete. Items indexed: {updatedList.Count}");
            }
            else
            {
                System.Console.WriteLine($"{area} complete. No items updated");
            }
            System.Console.WriteLine();
        }
    }
}