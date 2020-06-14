namespace BLMain.Plugins
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Octokit;

    [DependsOn(typeof(DefaultBranchPlugin))]
    public class AzurePipelinesPlugin : IPlugin
    {
        private readonly GitHubClient client;
        private readonly ILogger<AzurePipelinesPlugin> logger;

        public AzurePipelinesPlugin(GitHubClient client, ILogger<AzurePipelinesPlugin> logger)
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
                    var items = await this.client.Repository.Content.GetAllContents(repo.Id, "azure-pipelines.yml");
                    if (!items.Any(i => i.Type.Value == ContentType.File))
                    {
                        logger.LogDebug("Repo {Repo} does not have an azure-pipelines.yml file.", repo.FullName);
                    }

                    var pipelinesFile = items.First(i => i.Type.Value == ContentType.File);
                    var replacementRegex = new Regex(@"\bmaster\b", RegexOptions.Compiled);
                    var replacedContent = replacementRegex.Replace(pipelinesFile.Content, "main");

                    if (replacedContent == pipelinesFile.Content)
                    {
                        logger.LogDebug("Repo {Repo} has already had its azure-pipelines.yml file updated, skipping.", repo.FullName);
                        return;
                    }

                    if (!readOnly)
                    {
                        var newPR = await this.client.CreateFileChangePR(repo, "azure-pipelines.yml", new UpdateFileRequest(
                            "ci: Migrate to main branch for azure-pipelines.yml", replacedContent, pipelinesFile.Sha, "fix/move-to-main-branch/azuredevops", true
                        ));

                        logger.LogInformation("Repo {Repo} has had a new PR created (#{PR}) to update its azure-pipelines.yml file.", repo.FullName, newPR.Number);
                    }
                    else
                    {
                        logger.LogInformation("Repo {Repo} has had a new PR created (#{PR}) to update its azure-pipelines.yml file.", repo.FullName, "?");
                    }
                }
                catch (NotFoundException)
                {
                    logger.LogDebug("Repo {Repo} does not have an azure-pipelines.yml file.", repo.FullName);
                }
            }
        }
    }
}