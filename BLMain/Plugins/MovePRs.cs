namespace BLMain.Plugins
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Octokit;

    [DependsOn(typeof(DefaultBranchPlugin))]
    public class MovePRsPlugin : IPlugin
    {
        private readonly GitHubClient client;
        private readonly ILogger<MovePRsPlugin> logger;

        public MovePRsPlugin(GitHubClient client, ILogger<MovePRsPlugin> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        public async Task Apply(Repository repo, bool readOnly)
        {
            using (logger.BeginScope("Repository {Repo}", repo.FullName))
            {
                var openPRs = await this.client.Repository.PullRequest.GetAllForRepository(repo.Id, new PullRequestRequest {
                    State = ItemStateFilter.Open
                });

                foreach (var pr in openPRs) {
                    if (pr.Base.Ref != "refs/heads/master") {
                        this.logger.LogDebug("Pull Request {Repo}#{PullRequest} does not target the master branch, skipping it.", repo.FullName, pr.Number);
                        continue;
                    }

                    if (!readOnly)
                    {
                        await this.client.PullRequest.Update(repo.Id, pr.Number, new PullRequestUpdate {
                            Base = "main"
                        });
                    }
                
                    this.logger.LogInformation("Pull Request {Repo}#{PullRequest} has been re-targetted at the main branch.", repo.FullName, pr.Number);
                }
            }
        }
    }
}