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
    public static class Util
    {
        public static bool IsUnloaded(this Project proj)
        {
            var isUnloaded = string.Compare(EnvDTE.Constants.vsProjectKindUnmodeled, proj.Kind, StringComparison.OrdinalIgnoreCase) == 0;

            return isUnloaded;
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

        public static Guid GetProjectGuid(IVsSolution solutionService, Project proj)
        {
            IVsHierarchy selectedHierarchy;

            ErrorHandler.ThrowOnFailure(solutionService.GetProjectOfUniqueName(proj.UniqueName, out selectedHierarchy));

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
