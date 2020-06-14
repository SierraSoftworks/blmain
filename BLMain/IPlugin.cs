namespace BLMain
{
    using System.Threading.Tasks;
    using Octokit;

    public interface IPlugin
    {
        Task Apply(Repository repo, bool readOnly);
    }
}