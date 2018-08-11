using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace IKoshelev.ProjectFocuser
{
    public interface IRoslynSolutionAnalysis
    {
        Task<HashSet<string>> GetRecursivelyReferencedProjectGuids(string solutionFilePath, string[] rootProjects);
    }

    public class RoslynSolutionAnalysis : IRoslynSolutionAnalysis
    {
        public async Task<HashSet<string>> GetRecursivelyReferencedProjectGuids(string solutionFilePath, string[] rootProjectNames)
        {
            var workspace = MSBuildWorkspace.Create();

            var solution = workspace.OpenSolutionAsync(solutionFilePath).Result;

            var references = new ConcurrentDictionary<string, object>();

            var tasks = rootProjectNames
                            .Select(async (projName) =>
                            {
                                await FillReferencedProjectSetRecursively(solution, projName, references);
                            });

                await Task.WhenAll(tasks);

            return new HashSet<string>(references.Keys);
        }

        private async Task FillReferencedProjectSetRecursively(Solution solution, string projectName, ConcurrentDictionary<string, object> knownReferences)
        {
            var addedFirst = knownReferences.TryAdd(projectName, null);

            if(!addedFirst)
            {
                return;
            }

            var a = solution.Projects.ToArray();

            var project = solution
                            .Projects
                            .Single(proj => proj.Name == projectName);

            var tasks = project
                            .ProjectReferences
                            .Select(async (reference) =>
                            {
                                var refProject = solution
                                                        .Projects
                                                        .Single(proj => proj.Id.Id == reference.ProjectId.Id);

                                await FillReferencedProjectSetRecursively(solution, refProject.Name, knownReferences);
                            });

            await Task.WhenAll(tasks); 
        }
    }
}
