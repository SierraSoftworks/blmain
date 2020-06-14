using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace BLMain
{
    public class DependencyRunner
    {
        private readonly IEnumerable<IPlugin> plugins;

        public DependencyRunner(IEnumerable<IPlugin> plugins)
        {
            this.plugins = plugins;
        }
        
        public async Task Run(Repository repo, bool readOnly)
        {
            var outstanding = new HashSet<IPlugin>(this.plugins);
            var completed = new HashSet<IPlugin>();
            var inFlight = new Dictionary<Task, IPlugin>();

            while (outstanding.Any())
            {
                var addedTask = false;
                foreach (var plugin in outstanding.ToArray()) {
                    if (CanRun(plugin, completed))
                    {
                        outstanding.Remove(plugin);
                        inFlight.Add(plugin.Apply(repo, readOnly), plugin);
                        addedTask = true;
                    }
                }

                if (!addedTask && !inFlight.Any())
                {
                    throw new InvalidOperationException("The dependency graph could not be completed because one or more plugins depended on others which could not be completed.");
                }

                var completedTask = await Task.WhenAny(inFlight.Keys);
                completed.Add(inFlight[completedTask]);
                inFlight.Remove(completedTask);
            }

            await Task.WhenAll(inFlight.Keys);
        }

        private bool CanRun(IPlugin plugin, HashSet<IPlugin> completed)
        {
            var dependencies = plugin.GetType().GetCustomAttributes(typeof(DependsOnAttribute), true).Cast<DependsOnAttribute>();

            return dependencies.All(d => completed.Any(c => d.Dependency.IsAssignableFrom(c.GetType())));
        }
    }
}