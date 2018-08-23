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
    internal sealed class AddSelectedProjectsAndReferencesCommand : CommandBase
    {
        public override int CommandId => 0x0400;

        private AddSelectedProjectsAndReferencesCommand(Package package) : base(package)
        {
            AddCommandToMenu(this.MenuItemCallback);
        }

        public static AddSelectedProjectsAndReferencesCommand Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new AddSelectedProjectsAndReferencesCommand(package);
        }

        public void MenuItemCallback(object sender, EventArgs e)
        {
            if (ShowErrorMessageAndReturnTrueIfNoSolutionOpen())
            {
                return;
            }

            DteUtil.EnsureSelectedProjReferencesAreLoadedCommand(ServiceProvider, false);
        }
    }
}
