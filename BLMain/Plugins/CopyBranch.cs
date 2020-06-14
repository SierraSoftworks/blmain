namespace BLMain.Plugins
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Octokit;

    public class CopyBranchPlugin : IPlugin
    {
        private readonly GitHubClient client;
        private readonly ILogger<CopyBranchPlugin> logger;

        public CopyBranchPlugin(GitHubClient client, ILogger<CopyBranchPlugin> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        public async Task Apply(Repository repo, bool readOnly)
        {
            using (logger.BeginScope("Repository {Repo}", repo.FullName))
            {
                try
                {
                    var mainRef = await this.client.Git.Reference.Get(repo.Id, "refs/heads/main");
                    this.logger.LogInformation("Repo {Repo} already uses a main branch, skipping.", repo.FullName);

                    return;
                }
                catch (NotFoundException)
                {
                    this.logger.LogDebug("Repo {Repo} does not have a main branch yet.", repo.FullName);
                }

                Reference masterRef;
                try
                {
                    masterRef = await this.client.Git.Reference.Get(repo.Id, "refs/heads/master");
                }
                catch (NotFoundException)
                {
                    this.logger.LogInformation("Repo {Repo} does not have a master branch, skipping.", repo.FullName);
                    return;
                }

                if (!readOnly)
                {
                    await this.client.Git.Reference.Create(repo.Id, new NewReference("refs/heads/main", masterRef.Object.Sha));
                }

                this.logger.LogInformation("Repo {Repo} has had its master branch copied to 'main'.", repo.FullName);
            }
        }
    }
}