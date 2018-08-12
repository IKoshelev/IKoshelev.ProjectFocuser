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
    internal sealed class EnsureOnlySelectedProjReferencesAreLoadedCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0300;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("281c0281-1dba-43a7-8aae-496cef9936ba");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadAllProjectsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private EnsureOnlySelectedProjReferencesAreLoadedCommand(Package package)
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
        public static EnsureOnlySelectedProjReferencesAreLoadedCommand Instance
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
            Instance = new EnsureOnlySelectedProjReferencesAreLoadedCommand(package);
        }

        private const uint VSITEMID_ROOT = 0xFFFFFFFE;

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        public void MenuItemCallback(object sender, EventArgs e)
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            IVsSolution solutionService = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            IVsSolution4 solutionService4 = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution4;

            var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            var workspace = componentModel.GetService<VisualStudioWorkspace>();

            string[] selectedProjectNames = Util.GetSelectedProjectNames(dte, solutionService);

            var allGuidsToLoad = new RoslynSolutionAnalysis()
                                        .GetRecursivelyReferencedProjectGuids(workspace.CurrentSolution.FilePath, selectedProjectNames)
                                        .Result;

            var projectGuids = Util.GetProjectInfosRecursively(solutionService, dte.Solution.Projects);
            foreach (var proj in projectGuids)
            {
                var shouldBeLoaded = allGuidsToLoad.Contains(proj.Name);

                var guid = proj.Guid;

                int res = 0;
                if (shouldBeLoaded)
                {
                    res = solutionService4.ReloadProject(ref guid);                    
                }
                else if (!shouldBeLoaded)
                {
                    res = solutionService4.UnloadProject(ref guid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);
                }
                //ErrorHandler.ThrowOnFailure(res);
            }

            string message = "Ensure only selected projects loaded recurisvely complete";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                UnloadAllProjectsCommandPackage.MessageBoxName,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
