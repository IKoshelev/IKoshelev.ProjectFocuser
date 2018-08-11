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
        Task<Guid[]> GetRecursivelyReferencedProjectGuids(string solutionFilePath, Guid[] rootProjects);
    }

    public class RoslynSolutionAnalysis : IRoslynSolutionAnalysis
    {
        public async Task<Guid[]> GetRecursivelyReferencedProjectGuids(string solutionFilePath, Guid[] rootProjects)
        {
            var workspace = MSBuildWorkspace.Create();

            var solution = await workspace.OpenSolutionAsync(solutionFilePath);

            var references = new ConcurrentDictionary<Guid, object>();

            var tasks = rootProjects
                            .Select(async (projId) =>
                            {
                                await FillReferencedProjectSetRecursively(solution, projId, references);
                            });

            await Task.WhenAll(tasks);

            return references.Keys.ToArray();
        }

        private async Task FillReferencedProjectSetRecursively(Solution solution, Guid projectGuid, ConcurrentDictionary<Guid, object> knownReferences)
        {
            var addedFirst = knownReferences.TryAdd(projectGuid, null);

            if(!addedFirst)
            {
                return;
            }

            var project = solution
                            .Projects
                            .Single(proj => proj.Id.Id == projectGuid);

            var tasks = project
                            .ProjectReferences
                            .Select(async (reference) =>
                            {
                                await FillReferencedProjectSetRecursively(solution, reference.ProjectId.Id, knownReferences);
                            });

            await Task.WhenAll(tasks); 
        }
    }
}
