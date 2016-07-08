namespace CloudArchive.Configuration
{
    public class LiteralLocationResolver : ILocationResolver
    {
        public string Resolve(string name)
        {
            return name;
        }
    }
}