using CommandLine;
using CommandLine.Text;

namespace CloudArchive.Console
{
    internal class Options
    {
        [Option('a', "area", Required = true, HelpText = "Area to use.")]
        public string Area { get; set; }

        [Option('v', null, HelpText = "Verbose.")]
        public bool Verbose { get; set; }

        [Option('l', null, HelpText = "Live run. Without this, it's a simulation.")]
        public bool Live { get; set; }

        [Option('f', "full", HelpText = "Full update, default is incremental.")]
        public bool FullUpdate { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}