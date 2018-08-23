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

namespace IKoshelev.ProjectFocuser.Commands
{
    internal sealed class CompileAllInBackgroundCommand : CommandBase
    {
        public override int CommandId => 0x0500;

        private CompileAllInBackgroundCommand(Package package) : base(package)
        {
            AddCommandToMenu(this.MenuItemCallback);
        }

        public static CompileAllInBackgroundCommand Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new CompileAllInBackgroundCommand(package);
        }

        public void MenuItemCallback(object sender, EventArgs e)
        {
            if (ShowErrorMessageAndReturnTrueIfNoSolutionOpen())
            {
                return;
            }

            var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            var slnPath = dte.Solution.FileName;

            IVsOutputWindowPane customPane = DteUtil.GetThisExtensionOutputPane();

            customPane.OutputStringThreadSafe($"Starting full compilation of {slnPath}\r\n");

            IRoslynSolutionAnalysis roslyn = new RoslynSolutionAnalysis();
            roslyn.CompileFullSolutionInBackgroundAndReportErrors(slnPath, (message) => customPane.OutputStringThreadSafe(message));
        }

        
    }
}