namespace BLMain.Plugins
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Octokit;

    [DependsOn(typeof(CopyBranchPlugin))]
    public class DefaultBranchPlugin : IPlugin
    {
        private readonly GitHubClient client;
        private readonly ILogger<DefaultBranchPlugin> logger;

        public DefaultBranchPlugin(GitHubClient client, ILogger<DefaultBranchPlugin> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        public async Task Apply(Repository repo, bool readOnly)
        {
            using (logger.BeginScope("Repository {Repo}", repo.FullName))
            {
                if (repo.DefaultBranch != "master") {
                    this.logger.LogInformation("Repo {Repo} has its default branch set to {DefaultBranch}, skipping.", repo.FullName, repo.DefaultBranch);
                    return;
                }

                if (!readOnly)
                {
                    await this.client.Repository.Edit(repo.Id, new RepositoryUpdate(repo.Name) {
                        DefaultBranch = "main"
                    });
                }

                this.logger.LogInformation("Repo {Repo} had its default branch set to main.", repo.FullName);
            }
        }
    }
}