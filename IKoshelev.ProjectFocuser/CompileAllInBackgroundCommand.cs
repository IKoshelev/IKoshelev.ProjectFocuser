using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
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
    internal sealed class CompileAllInBackgroundCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0500;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("EABB9F00-FE55-4D18-9791-EF16FB5B0688");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompileAllInBackgroundCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private CompileAllInBackgroundCommand(Package package)
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
        public static CompileAllInBackgroundCommand Instance
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
            Instance = new CompileAllInBackgroundCommand(package);
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
            var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            var slnPath = dte.Solution.FileName;

            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            // Use e.g. Tools -> Create GUID to make a stable, but unique GUID for your pane.
            // Also, in a real project, this should probably be a static constant, and not a local variable
            Guid customGuid = new Guid("1D5FDBCA-EE91-4203-BE44-38E3654AF3F6");
            string customTitle = "Full solution background compilation errors";

            //todo try get before creating? nope, works as is
            outWindow.CreatePane(ref customGuid, customTitle, 1, 1);

            IVsOutputWindowPane customPane;

            outWindow.GetPane(ref customGuid, out customPane);

            customPane.OutputStringThreadSafe($"Starting full compilation of {slnPath}\r\n");

            System.Threading.Tasks.Task.Factory.StartNew(async () => 
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
                        customPane.OutputStringThreadSafe($"Compiling {project.Name}\r\n");
                        Compilation projectCompilation = await project.GetCompilationAsync();
                        if (projectCompilation == null)
                        {
                            customPane.OutputStringThreadSafe($"Error, could not get compilation of {project.Name}\r\n");
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
                            customPane.OutputStringThreadSafe(diagItem.ToString() + "\r\n");
                        }
                    }

                    if (success)
                    {
                        customPane.OutputStringThreadSafe($"Compilation successful\r\n");
                    }
                    else
                    {
                        customPane.OutputStringThreadSafe($"Compilation erros found; You can double-click file path in this pane to open it in VS\r\n");
                    }
                }
                catch (Exception ex)
                {
                    customPane.OutputStringThreadSafe($"Error: {ex.Message}\r\n");
                }
            });
        }
    }
}