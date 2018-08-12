using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IKoshelev.ProjectFocuser
{
    public class ProjectInfo
    {
        public ProjectInfo(Guid guid, string name)
        {
            Guid = guid;
            Name = name;
        }

        public Guid Guid { get; set; }
        public string Name { get; set; } 
    }

    public static class Util
    {
        public static bool IsCsproj(this Project proj)
        {
            var isCsproj = proj.UniqueName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);

            return isCsproj;
        }

        public static bool IsUnloaded(this Project proj)
        {
            var isUnloaded = string.Compare(EnvDTE.Constants.vsProjectKindUnmodeled, proj.Kind, StringComparison.OrdinalIgnoreCase) == 0;

            return isUnloaded;
        }

        public static bool IsUnloaded(this ProjectItem proj)
        {
            var isUnloaded = string.Compare(EnvDTE.Constants.vsProjectKindUnmodeled, proj.Kind, StringComparison.OrdinalIgnoreCase) == 0;

            return isUnloaded;
        }

        public static bool IsFolder(this Project proj)
        {
            var isFolder = proj.Kind == EnvDTE.Constants.vsProjectKindSolutionItems;

            return isFolder;
        }
        public static bool IsFolder(this ProjectItem proj)
        {
            var isFolder = proj.Kind == EnvDTE.Constants.vsProjectKindSolutionItems;

            return isFolder;
        }

        private static void ForEach(this Projects projects, bool? isLoaded, Action<Project> action)
        {
            for (int count = 1; count <= projects.Count; count++)
            {
                var proj = projects.Item(count);

                if (proj.IsFolder() == false
                    && isLoaded.HasValue 
                    && !proj.IsUnloaded() != isLoaded)
                {
                    continue;
                }

                action(proj);
            }
        }

        public static ProjectInfo[] GetProjectInfosRecursively(IVsSolution solutionService, Projects rootProjects, bool? isLoaded = null)
        {
            var allProjects = new List<ProjectInfo>();
            rootProjects.ForEach(isLoaded, (proj) =>
            {            
                if (proj.IsFolder())
                {
                    GetSolutionFolderProjects(solutionService, proj, isLoaded, allProjects);
                }
                else
                {
                    var guid = GetProjectGuid(solutionService, proj.UniqueName);
                    var info = new ProjectInfo(guid, proj.Name);
                    allProjects.Add(info);
                }
            });
            return allProjects.ToArray();
        }

        public static string[] GetSelectedProjectNames(DTE dte, IVsSolution solutionService)
        {
            return dte.SelectedItems
                        .Cast<SelectedItem>()
                        .Select(item =>
                        {
                            var project = dte.Solution.Projects.Cast<Project>().SingleOrDefault(x => x.Name == item.Name);
                            if (project == null)
                            {
                                return null;
                            }
                            return project.Name;
                        })
                        .Where(guid => guid != null)
                        .ToArray();
        }

        private static void GetSolutionFolderProjects(IVsSolution solutionService, Project solutionFolder, bool? isLoaded, List<ProjectInfo> allProject)
        {
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var projectItem = solutionFolder.ProjectItems.Item(i);
                if (projectItem == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (projectItem.IsFolder())
                {
                    GetSolutionFolderProjects(solutionService, projectItem, isLoaded, allProject);
                }
                else if (isLoaded.HasValue
                            && !projectItem.IsUnloaded() == isLoaded)
                {
                    var guid = GetProjectGuid(solutionService, projectItem.Name);
                    var info = new ProjectInfo(guid, projectItem.Name);
                    allProject.Add(info);
                }
            }
        }

        private static void GetSolutionFolderProjects(IVsSolution solutionService, ProjectItem solutionFolder, bool? isLoaded, List<ProjectInfo> allProject)
        {
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var projectItem = solutionFolder.ProjectItems.Item(i);
                if (projectItem == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (projectItem.IsFolder())
                {
                    GetSolutionFolderProjects(solutionService, projectItem, isLoaded, allProject);
                }
                else if (isLoaded.HasValue
                            && !projectItem.IsUnloaded() == isLoaded)
                {
                    var guid = GetProjectGuid(solutionService, projectItem.Name);
                    var info = new ProjectInfo(guid, projectItem.Name);
                    allProject.Add(info);
                }
            }
        }

        public static Guid GetProjectGuid(IVsSolution solutionService, Project proj)
        {
            IVsHierarchy selectedHierarchy;

            ErrorHandler.ThrowOnFailure(solutionService.GetProjectOfUniqueName(proj.UniqueName, out selectedHierarchy));

            var guid = GetProjectGuid(selectedHierarchy);
            return guid;
        }

        public static Guid GetProjectGuid(IVsSolution solutionService, string uniqueName)
        {
            IVsHierarchy selectedHierarchy;

            ErrorHandler.ThrowOnFailure(solutionService.GetProjectOfUniqueName(uniqueName, out selectedHierarchy));

            var guid = GetProjectGuid(selectedHierarchy);
            return guid;
        }

        public static Guid GetProjectGuid(IVsHierarchy projectHierarchy)
        {
            Guid projectGuid;
            int hr;

            hr = projectHierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out projectGuid);
            ErrorHandler.ThrowOnFailure(hr);

            return projectGuid;
        }
    }
}
