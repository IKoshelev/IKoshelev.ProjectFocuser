using System;
using System.ComponentModel.Design;
using System.Globalization;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IKoshelev.ProjectFocuser
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class UnloadAllProjectsCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("b97309f8-343e-445a-adaa-16db2957a3b2");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnloadAllProjectsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private UnloadAllProjectsCommand(Package package)
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
        public static UnloadAllProjectsCommand Instance
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
            Instance = new UnloadAllProjectsCommand(package);
        }

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

            // Development Tools Extensibility
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            var projTest = dte.Solution.Projects.Item(1);

            IVsSolution solutionService = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            IVsSolution4 solutionService4 = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution4;

            //UnloadFirstSolutionProject();

            for (int count = 1; count <= dte.Solution.Projects.Count; count++)
            {
                IVsHierarchy selectedHierarchy;

                var proj = dte.Solution.Projects.Item(count);

                if (proj.IsUnloaded())
                {
                    continue;
                }

                //proj.DTE.ExecuteCommand("Project.UnloadProject", "");

                ErrorHandler.ThrowOnFailure(solutionService.GetProjectOfUniqueName(proj.UniqueName, out selectedHierarchy));

                ErrorHandler.ThrowOnFailure(solutionService.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_UnloadProject, selectedHierarchy, 0));
            }

            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "Unload all projects complete";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        //private void UnloadFirstSolutionProject()
        //{
        //    IVsSolution4 solution;
        //    IVsHierarchy projectHierarchy;
        //    Guid projectGuid;

        //    // Get the first project of the solution and unload it
        //    try
        //    {
        //        solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution4;

        //        // Get the first project of the solution and unload it
        //        projectHierarchy = GetFirstProjectHierarchy(solution);
        //        if (projectHierarchy != null)
        //        {
        //            projectGuid = GetProjectGuid(projectHierarchy);

        //            UnloadProject(solution, projectGuid);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Windows.Forms.MessageBox.Show(ex.ToString());
        //    }
        //}

        //private const uint VSITEMID_ROOT = 0xFFFFFFFE;

        //private IVsHierarchy GetFirstProjectHierarchy(IVsSolution4 solution)
        //{
        //    IVsHierarchy solutionHierarchy;
        //    int hr;
        //    IntPtr nestedHierarchyValue = IntPtr.Zero;
        //    uint nestedItemIdValue = 0;
        //    Guid nestedHierarchyGuid;
        //    IVsHierarchy projectHierarchy = null;
        //    uint firstChildNode;
        //    object value = null;

        //    solutionHierarchy = solution as IVsHierarchy;

        //    // Get the first visible child node of the solution
        //    hr = solutionHierarchy.GetProperty(VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_FirstVisibleChild, out value);
        //    ErrorHandler.ThrowOnFailure(hr);

        //    if (value != null)
        //    {
        //        firstChildNode = Convert.ToUInt32(value);

        //        // Try to get the hierarchy of the node
        //        nestedHierarchyGuid = typeof(IVsHierarchy).GUID;
        //        hr = solutionHierarchy.GetNestedHierarchy(firstChildNode, ref nestedHierarchyGuid,
        //           out nestedHierarchyValue, out nestedItemIdValue);
        //        ErrorHandler.ThrowOnFailure(hr);

        //        if (nestedHierarchyValue != IntPtr.Zero && nestedItemIdValue == VSITEMID_ROOT)
        //        {
        //            // Get the new hierarchy
        //            projectHierarchy = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(nestedHierarchyValue) as IVsHierarchy;
        //            System.Runtime.InteropServices.Marshal.Release(nestedHierarchyValue);
        //        }
        //    }
        //    return projectHierarchy;
        //}

        private Guid GetProjectGuid(IVsHierarchy projectHierarchy)
        {
            Guid projectGuid;
            int hr;

            hr = projectHierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out projectGuid);
            ErrorHandler.ThrowOnFailure(hr);

            return projectGuid;
        }

        //private void UnloadProject(IVsSolution4 solution, Guid projectGuid)
        //{
        //    int hr;

        //    hr = solution.UnloadProject(ref projectGuid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);
        //    ErrorHandler.ThrowOnFailure(hr);
        //}
    }
}
