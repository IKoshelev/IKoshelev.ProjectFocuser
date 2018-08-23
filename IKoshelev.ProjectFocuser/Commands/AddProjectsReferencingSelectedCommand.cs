using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IKoshelev.ProjectFocuser.Commands
{
    internal sealed class AddProjectsReferencingSelectedCommand : CommandBase
    {
        public override int CommandId => 0x0600;

        private AddProjectsReferencingSelectedCommand(Package package): base(package)
        {
            AddCommandToMenu(this.MenuItemCallback);
        }

        public static AddProjectsReferencingSelectedCommand Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new AddProjectsReferencingSelectedCommand(package);
        }

        public void MenuItemCallback(object sender, EventArgs e)
        {
            if (ShowErrorMessageAndReturnTrueIfNoSolutionOpen())
            {
                return;
            }

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            string[] selectedProjectNames = DteUtil.GetSelectedItemNames(dte);

            IRoslynSolutionAnalysis roslyn = new RoslynSolutionAnalysis();

            var allProjectNamesToLoad = roslyn.GetProjectsDirectlyReferencing(dte.Solution.FileName, selectedProjectNames);

            DteUtil.EnsureProjectsLoadedByNames(dte, allProjectNamesToLoad, false);

            string message = "Load projects referencing selected projects directly complete";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                ProjectFocuserCommandPackage.MessageBoxName,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);           
        }
    }
}
