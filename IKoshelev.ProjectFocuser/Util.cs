using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IKoshelev.ProjectFocuser
{
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

        private static void ForEach(this Projects projects, Action<Project> action)
        {
            for (int count = 1; count <= projects.Count; count++)
            {
                var proj = projects.Item(count);

                action(proj);
            }
        }

        public static ProjectItem[] GetProjectItemsRecursively(IVsSolution solutionService, DTE2 dte)
        {
            var allProjects = new List<ProjectItem>();
            dte.Solution.Projects.ForEach((proj) =>
            {            
                if (proj.IsFolder())
                {
                    GetSolutionFolderProjects(solutionService, proj, allProjects);
                }
                else
                {
                    var path = proj.Name; ;
                    var item = dte.Solution.FindProjectItem(path);
                    //allProjects.Add(item);
                }
            });
            return allProjects.ToArray();
        }

        public static string[] GetSelectedItemNames(DTE dte)
        {
            return dte.SelectedItems
                        .Cast<SelectedItem>()
                        .Select(item => item.Name)
                        .ToArray();
        }

        private static void GetSolutionFolderProjects(IVsSolution solutionService, Project solutionFolder, List<ProjectItem> allProject)
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
                    GetSolutionFolderProjects(solutionService, projectItem, allProject);
                }
                else
                {
                    allProject.Add(projectItem);
                }
            }
        }

        private static void GetSolutionFolderProjects(IVsSolution solutionService, ProjectItem solutionFolder, List<ProjectItem> allProject)
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
                    GetSolutionFolderProjects(solutionService, projectItem, allProject);
                }
                else
                {
                    allProject.Add(projectItem);
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

        public static void WriteExtensionOutput(string message, int retyrCount = 20)
        {
            try
            {
                IVsOutputWindowPane customPane = GetThisExtensionOutputPane();
                customPane.OutputStringThreadSafe(message);

            }
            catch (InvalidOperationException ex)
            {
                if (retyrCount > 0 && ex.Message.Contains("has not been loaded yet"))
                {
                    System.Threading.Tasks.Task.Factory.StartNew(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(5000);
                        WriteExtensionOutput(message, retyrCount - 1);
                    });
                }
            }
        }

        public static IVsOutputWindowPane GetThisExtensionOutputPane()
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            Window window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            Guid customGuid = new Guid("BC12B8A0-1678-48D5-B8C0-D3B5EB4D9064");
            string customTitle = "IKoshelev.ProjectFocuser";

            outWindow.CreatePane(ref customGuid, customTitle, 1, 1);

            IVsOutputWindowPane customPane;

            outWindow.GetPane(ref customGuid, out customPane);
            return customPane;
        }
    }
}
