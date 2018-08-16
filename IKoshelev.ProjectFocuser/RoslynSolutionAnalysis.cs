using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Locator;

namespace IKoshelev.ProjectFocuser
{
    public interface IRoslynSolutionAnalysis
    {
        Task<HashSet<string>> GetRecursivelyReferencedProjects(string solutionFilePath, string[] rootProjects);

        HashSet<string> GetProjectsDirectlyReferencing(string solutionFilePath, string[] rootProjectNames);

        Task CompileFullSolutionInBackgroundAndReportErrors(string slnPath, Action<string> writeOutput);      
    }

    public class RoslynSolutionAnalysis : IRoslynSolutionAnalysis
    {
        private static object MaxVisualStudionVersionRegistrationLock = new object();

        private static bool MaxVisualStudionVersionHasBeenRegistered = false;

        public static void EnsureMaxVisualStudioMsBuildVersionHasBeenRegistered()
        {
            try
            {
                if (MaxVisualStudionVersionHasBeenRegistered)
                {
                    return;
                }

                lock (MaxVisualStudionVersionRegistrationLock)
                {
                    if (MaxVisualStudionVersionHasBeenRegistered)
                    {
                        return;
                    }

                    var maxVisualStudioInstance = MSBuildLocator
                                                        .QueryVisualStudioInstances()
                                                        .OrderByDescending(x => x.Version.Major)
                                                        .First();

                    MSBuildLocator.RegisterInstance(maxVisualStudioInstance);

                    MaxVisualStudionVersionHasBeenRegistered = true;

                    Util.WriteExtensionOutput($"Using msbuild version from Visual Studio version {maxVisualStudioInstance.Version}, " + 
                                        $"path: {maxVisualStudioInstance.MSBuildPath}");
                }
            }
            catch (Exception ex)
            {
                Util.WriteExtensionOutput($"Error during Visual Studio msbuild registration. {ex.Message}" );
            }
        }

        public Task CompileFullSolutionInBackgroundAndReportErrors(string slnPath, Action<string> writeOutput)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    var workspace = MSBuildWorkspace.Create();

                    var solution = await workspace.OpenSolutionAsync(slnPath);

                    ProjectDependencyGraph projectGraph = solution.GetProjectDependencyGraph();

                    var projectIds = projectGraph.GetTopologicallySortedProjects().ToArray();
                    var success = true;
                    foreach (ProjectId projectId in projectIds)
                    {
                        var project = solution.GetProject(projectId);
                        writeOutput($"Compiling {project.Name}\r\n");
                        Compilation projectCompilation = await project.GetCompilationAsync();
                        if (projectCompilation == null)
                        {
                            writeOutput($"Error, could not get compilation of {project.Name}\r\n");
                            continue;
                        }

                        var diag = projectCompilation.GetDiagnostics().Where(x => x.IsSuppressed == false
                                                                                  && x.Severity == DiagnosticSeverity.Error);

                        if (diag.Any())
                        {
                            success = false;
                        }

                        foreach (var diagItem in diag)
                        {
                            writeOutput(diagItem.ToString() + "\r\n");
                        }
                    }

                    if (success)
                    {
                        writeOutput($"Compilation successful\r\n");
                    }
                    else
                    {
                        writeOutput($"Compilation erros found; You can double-click file path in this pane to open it in VS\r\n");
                    }
                }
                catch (Exception ex)
                {
                    writeOutput($"Error: {ex.Message}\r\n");
                }
            });
        }

        public async Task<HashSet<string>> GetRecursivelyReferencedProjects(string solutionFilePath, string[] rootProjectNames)
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

        public HashSet<string> GetProjectsDirectlyReferencing(string solutionFilePath, string[] rootProjectNames)
        {
            var workspace = MSBuildWorkspace.Create();

            var solution = workspace.OpenSolutionAsync(solutionFilePath).Result;

            var rootProjectIds = solution.Projects
                                        .Where(proj => rootProjectNames.Contains(proj.Name))
                                        .Select(proj => proj.Id.Id)
                                        .ToArray();

            var referencingProjects =  solution
                                            .Projects
                                            .Where(proj => proj.ProjectReferences.Any(reference => rootProjectIds.Contains(reference.ProjectId.Id)))
                                            .Distinct()
                                            .Select(proj => proj.Name);

            return new HashSet<string>(referencingProjects);
        }
    }
}
