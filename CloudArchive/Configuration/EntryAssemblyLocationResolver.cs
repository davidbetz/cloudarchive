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
                var directoryInfo = new FileInfo(Assembly.GetEntryAssembly().Location).Directory;
                if (directoryInfo != null)
                {
                    var entryAssemblyLocation = directoryInfo.FullName;
                    name = new Regex(@"\[entryassembly\]\\?").Replace(name, entryAssemblyLocation + "\\");
                }
            }
            return name;
        }
    }
}