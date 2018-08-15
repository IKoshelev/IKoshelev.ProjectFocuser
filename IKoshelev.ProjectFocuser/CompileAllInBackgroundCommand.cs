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

            IVsOutputWindowPane customPane = Util.GetThisExtensionOutputPane();

            customPane.OutputStringThreadSafe($"Starting full compilation of {slnPath}\r\n");

            IRoslynSolutionAnalysis roslyn = new RoslynSolutionAnalysis();
            roslyn.CompileFullSolutionInBackgroundAndReportErrors(slnPath, (message) => customPane.OutputStringThreadSafe(message));
        }

        
    }
}