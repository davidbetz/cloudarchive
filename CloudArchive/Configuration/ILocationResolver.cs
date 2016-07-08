namespace CloudArchive.Configuration
{
    public interface ILocationResolver
    {
        string Resolve(string name);
    }
}