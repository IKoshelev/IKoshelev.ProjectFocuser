using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IKoshelev.ProjectFocuser
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class LoadAllProjectsCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0200;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("82cb990d-f919-4fbe-8af5-f325882b6bed");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadAllProjectsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private LoadAllProjectsCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static LoadAllProjectsCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new LoadAllProjectsCommand(package);
        }

        private const uint VSITEMID_ROOT = 0xFFFFFFFE;

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            var projNumber = 1;
            var proj = workspace.CurrentSolution.Projects.ElementAt(projNumber);
            var references = proj.ProjectReferences.ToArray();

            //// Development Tools Extensibility
            //var dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            //IVsSolution solutionService = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            //for (int count = 1; count <= dte.Solution.Projects.Count; count++)
            //{
            //    IVsHierarchy selectedHierarchy;

            //    var proj = dte.Solution.Projects.Item(count);

            //    if (proj.IsUnloaded() == false)
            //    {
            //        continue;
            //    }

            //    proj.DTE.ExecuteCommand("Project.ReloadProject", "");

            //    //ErrorHandler.ThrowOnFailure(solutionService.GetProjectOfUniqueName(proj.UniqueName, out selectedHierarchy));

            //    //ErrorHandler.ThrowOnFailure(solutionService.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_LoadProject, selectedHierarchy, 0));
            //}

            ReloadLastSolutionProject();

            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "Reload all projects complete";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void ReloadLastSolutionProject()
        {
            IVsSolution4 solution;
            IVsHierarchy projectHierarchy;
            Guid projectGuid;

            // Get the first project of the solution and reload it
            try
            {
                solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution4;

                projectHierarchy = GetLastProjectHierarchy(solution);

                if (projectHierarchy != null)
                {
                    projectGuid = GetProjectGuid(projectHierarchy);

                    ReloadHierarchy(solution, projectGuid);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        private IVsHierarchy GetLastProjectHierarchy(IVsSolution4 solution)
        {
            IVsHierarchy solutionHierarchy;
            int hr;
            IntPtr nestedHierarchyValue = IntPtr.Zero;
            uint nestedItemIdValue = 0;
            Guid nestedHierarchyGuid;
            IVsHierarchy projectHierarchy = null;
            uint firstChildNode;
            object value = null;

            solutionHierarchy = solution as IVsHierarchy;

            // Get the first visible child node of the solution
            hr = solutionHierarchy.GetProperty(VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_FirstVisibleChild, out value);
            int nexthr = hr;
            while (nexthr != VSConstants.VSITEMID_NIL)
            {
                hr = nexthr;
                nexthr = solutionHierarchy.GetProperty(VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_NextVisibleSibling, out value);
            }

            ErrorHandler.ThrowOnFailure(hr);

            if (value != null)
            {
                firstChildNode = Convert.ToUInt32(value);

                // Try to get the hierarchy of the node
                nestedHierarchyGuid = typeof(IVsHierarchy).GUID;
                hr = solutionHierarchy.GetNestedHierarchy(firstChildNode, ref nestedHierarchyGuid,
                   out nestedHierarchyValue, out nestedItemIdValue);
                ErrorHandler.ThrowOnFailure(hr);

                if (nestedHierarchyValue != IntPtr.Zero && nestedItemIdValue == VSITEMID_ROOT)
                {
                    // Get the new hierarchy
                    projectHierarchy = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(nestedHierarchyValue) as IVsHierarchy;
                    System.Runtime.InteropServices.Marshal.Release(nestedHierarchyValue);
                }
            }
            return projectHierarchy;
        }

        private Guid GetProjectGuid(IVsHierarchy projectHierarchy)
        {
            Guid projectGuid;
            int hr;

            hr = projectHierarchy.GetGuidProperty(VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out projectGuid);
            ErrorHandler.ThrowOnFailure(hr);

            return projectGuid;
        }

        private void ReloadHierarchy(IVsSolution4 solution, Guid projectGuid)
        {
            int hr;

            hr = solution.ReloadProject(ref projectGuid);
            ErrorHandler.ThrowOnFailure(hr);
        }
    }
}
