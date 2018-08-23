using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using IKoshelev.ProjectFocuser.UI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IKoshelev.ProjectFocuser.Commands
{
    internal sealed class LoadAllProjectsCommand : CommandBase
    {
        public override int CommandId => 0x0200;

        private LoadAllProjectsCommand(Package package) : base(package)
        {
            AddCommandToMenu(this.MenuItemCallback);
        }

        public static LoadAllProjectsCommand Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new LoadAllProjectsCommand(package);
        }

        public void MenuItemCallback(object sender, EventArgs e)
        {
            if(ShowErrorMessageAndReturnTrueIfNoSolutionOpen())
            {
                return;
            }

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            IVsSolution4 solutionService4 = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution4;

            var projects = SlnFileParser.GetProjectNamesToGuidsDict(dte.Solution.FileName);

            foreach (var proj in projects.Values)
            {
                var guid = new Guid(proj);

                var res = solutionService4.ReloadProject(ref guid);
                //ErrorHandler.ThrowOnFailure(res);
            }

            string message = "Load all projects complete";

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
