using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CloudArchive.Configuration
{
    public class EntryAssemblyLocationResolver : ILocationResolver
    {
        public string Resolve(string name)
        {
            if (name.Contains("[entryassembly]"))
            {
                var entryAssemblyLocation = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName;
                name = new Regex(@"\[entryassembly\]\\?").Replace(name, entryAssemblyLocation + "\\");
            }
            return name;
        }
    }
}