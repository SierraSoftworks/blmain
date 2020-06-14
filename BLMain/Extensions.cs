namespace BLMain
{
    using Octokit;
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    public static class GitClientExtensions
    {
        public static async Task<PullRequest> CreateFileChangePR(this GitHubClient client, Repository repo, string fileName, UpdateFileRequest updateRequest, string sourceRef = "refs/heads/main")
        {
            var mainRef = await client.Git.Reference.Get(repo.Id, sourceRef);

            try
            {   
                var owner = repo.FullName.Split("/", 2).First();

                var prs = await client.PullRequest.GetAllForRepository(repo.Id, new PullRequestRequest {
                    Head = $"{owner}:{updateRequest.Branch}",
                    State = ItemStateFilter.All
                });

                if (prs.Any())
                {
                    // The branch already exists with a fix
                    return prs.First();
                }
            }
            catch (NotFoundException)
            {
            }

            try
            {
                await client.Git.Reference.Create(repo.Id, new NewReference($"refs/heads/{updateRequest.Branch}", mainRef.Object.Sha));
            }
            catch (ApiValidationException ex)
            {
                if (ex.ApiError.Message != "Reference already exists")
                {
                    throw;
                }
            }

            updateRequest.Author = new Committer("BlackLivesMatter Migration Tool", "contact@sierrasoftworks.com", DateTimeOffset.UtcNow);

            try
            {
                await client.Repository.Content.UpdateFile(repo.Id, fileName, updateRequest);
            }
            catch (ApiException ex)
            {
                // Check if the file has already been modified on this branch
                if (ex.StatusCode != HttpStatusCode.Conflict)
                {
                    throw;
                }
            }

            return await client.PullRequest.Create(repo.Id, new NewPullRequest($"Update {fileName} to use main branch", updateRequest.Branch, "main") {
                Body = $"This PR updates the {fileName} file to use the `main` branch instead of `master`. It has been automatically generated and this might not have worked correctly, so please check it carefully.",
                MaintainerCanModify = true,
            });
        }
    }
}