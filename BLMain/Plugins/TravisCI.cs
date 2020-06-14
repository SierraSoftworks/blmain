namespace BLMain.Plugins
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Octokit;

    [DependsOn(typeof(DefaultBranchPlugin))]
    public class TravisCIPlugin : IPlugin
    {
        private readonly GitHubClient client;
        private readonly ILogger<TravisCIPlugin> logger;

        public TravisCIPlugin(GitHubClient client, ILogger<TravisCIPlugin> logger)
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
                    var items = await this.client.Repository.Content.GetAllContents(repo.Id, ".travis.yml");
                    if (!items.Any(i => i.Type.Value == ContentType.File))
                    {
                        logger.LogDebug("Repo {Repo} does not have a .travis.yml file", repo.FullName);
                    }

                    var pipelinesFile = items.First(i => i.Type.Value == ContentType.File);
                    var replacementRegex = new Regex(@"\bmaster\b", RegexOptions.Compiled);
                    var replacedContent = replacementRegex.Replace(pipelinesFile.Content, "main");

                    if (replacedContent == pipelinesFile.Content)
                    {
                        logger.LogDebug("Repo {Repo} has already had its .travis.yml file updated, skipping.", repo.FullName);
                        return;
                    }

                    if (!readOnly)
                    {
                        var newPR = await this.client.CreateFileChangePR(repo, ".travis.yml", new UpdateFileRequest(
                            "ci: Migrate to main branch for .travis.yml", replacedContent, pipelinesFile.Sha, "fix/move-to-main-branch/travisci", true
                        ));

                        logger.LogInformation("Repo {Repo} has had a new PR created (#{PR}) to update its .travis.yml file.", repo.FullName, newPR.Number);
                    }
                    else
                    {
                        logger.LogInformation("Repo {Repo} has had a new PR created (#{PR}) to update its .travis.yml file.", repo.FullName, "?");
                    }
                }
                catch (NotFoundException)
                {
                    logger.LogDebug("Repo {Repo} does not have a .travis.yml file.", repo.FullName);
                }
            }
        }
    }
}