using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Octokit;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BLMain
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>{
                    { "Auth:Token", "" },
                    { "Target:User", "" },
                    { "Target:Organization", "" },
                    { "Target:Forks", "false" },
                    { "Target:Pattern", ".*" },
                    { "Target:MakeChanges", "false" }
                })
                .AddCommandLine(args, new Dictionary<string, string> {
                    { "--token", "Auth:Token" },
                    { "--user", "Target:User" },
                    { "--org", "Target:Organization" },
                    { "--filter", "Target:Pattern" },
                    { "--apply", "Target:MakeChanges" },
                    { "--update-forks", "Target:Forks" }
                })
                .Build();

            var serviceBuilder = new ServiceCollection();

            Setup(serviceBuilder, configuration);
            Run(serviceBuilder.BuildServiceProvider(), configuration).Wait();
        }

        static void Setup(ServiceCollection services, IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            services
                .AddLogging(builder => {
                    builder
                        .AddSerilog(dispose: true)
                        .SetMinimumLevel(LogLevel.Information);
                })
                .AddSingleton(_ => configuration.GetValue<string>("Auth:Token") is string token && !string.IsNullOrEmpty(token) ? new Credentials(token) : default)
                .AddSingleton(services => {
                    var client = new GitHubClient(new ProductHeaderValue("sierrasoftworks-blmain", "1.0"));
                    if (services.GetService<Credentials>() is Credentials credentials)
                    {
                        client.Credentials = credentials;
                    }
                    return client;
                });

            services
                .AddSingleton<IPlugin, Plugins.CopyBranchPlugin>()
                .AddSingleton<IPlugin, Plugins.DefaultBranchPlugin>()
                .AddSingleton<IPlugin, Plugins.MovePRsPlugin>()
                .AddSingleton<IPlugin, Plugins.AzurePipelinesPlugin>()
                .AddSingleton<IPlugin, Plugins.TravisCIPlugin>();
        }

        static async Task Run(IServiceProvider services, IConfiguration configuration)
        {

            var client = services.GetRequiredService<GitHubClient>();
            var plugins = new DependencyRunner(services.GetServices<IPlugin>());
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            if (string.IsNullOrWhiteSpace(configuration.GetValue<string>("Auth:Token")))
            {
                logger.LogWarning(@"You have not provided a personal access token.
                This will prevent you from making changes to your repositories and may result in rate limiting.
                To create a new personal access token, visit: https://github.com/settings/tokens/new
                
                NOTE: You will need to grant access to the `repo` scope if you want this tool to run correctly.");
            }

            if (!configuration.GetValue<bool>("Target::MakeChanges"))
            {
                logger.LogWarning("You have not specified the --apply flag, so no changes will be made to your repositories (only logs explaining what would be performed will be printed).");
            }

            var patternRegex = new Regex(configuration.GetValue<string>("Target:Pattern"), RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var org = configuration.GetValue<string>("Target:Organization");
            var user = configuration.GetValue<string>("Target:User");

            try
            {
                IEnumerable<Repository> repos;

                if (!string.IsNullOrEmpty(org)) {
                    logger.LogDebug("Fetching list of repositories for organization {Org}.", org);
                    repos = await client.Repository.GetAllForOrg(org);
                } else if (!string.IsNullOrEmpty(user)) {
                    logger.LogDebug("Fetching list of repositories for user {User}.", user);
                    repos = await client.Repository.GetAllForUser(user);

                } else {
                    logger.LogCritical("You have not provided an organization or user against which to make changes.");
                    return;
                }
                foreach (var repo in repos)
                {
                    if (repo.Fork && !configuration.GetValue<bool>("Target:Forks", false))
                    {
                        logger.LogWarning("Skipping repo {Repo} because it is a fork.", repo.FullName);
                        continue;
                    }

                    if (!patternRegex.IsMatch(repo.Name))
                    {
                        logger.LogDebug("Skipping repo {Repo} because it does not match the provided pattern.", repo.FullName);
                        continue;
                    }

                    logger.LogDebug("Applying changes to repo {Repo}.", repo.FullName);
                    using (logger.BeginScope("Repository {Repo}", repo.FullName))
                    {
                        try
                        {
                            await plugins.Run(repo, !configuration.GetValue<bool>("Target:MakeChanges", true));
                        }
                        catch (AggregateException ex)
                        {
                            logger.LogError("Failed to apply changes to repo {Repo}.", repo.FullName);
                            foreach (var iex in ex.InnerExceptions)
                            {
                                logger.LogError(iex, "Failed to apply changes to repo {Repo} (in {Method})", repo.FullName, iex.TargetSite.Name);
                            }
                        }
                        catch (ApiException ex)
                        {
                            using (logger.BeginScope("Failed to apply changes to repo {Repo}.", repo.FullName))
                            {
                                logger.LogError(ex, "Error updating repo {Repo}, received an HTTP {StatusCode} response from GitHub saying: {Message}.", repo.FullName, ex.StatusCode, ex.ApiError.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to run the tool due to a critical failure.");
                Environment.ExitCode = 1;
            }
        }
    }
}
