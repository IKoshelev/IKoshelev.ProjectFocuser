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
    internal sealed class SaveCurrentSuoCommand : CommandBase
    {
        public override int CommandId => 0x0700;

        private SaveCurrentSuoCommand(Package package) : base(package)
        {
            AddCommandToMenu(this.MenuItemCallback);
        }

        public static SaveCurrentSuoCommand Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new SaveCurrentSuoCommand(package);
        }

        public void MenuItemCallback(object sender, EventArgs e)
        {
            if (ShowErrorMessageAndReturnTrueIfNoSolutionOpen())
            {
                return;
            }

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            var documentationControl = new UI.SaveCurrentSuoDialog();
            documentationControl.DataContext = new SaveCurrentSuoDialogVM(dte.Solution.FileName);
            documentationControl.ShowDialog();
        }
    }
}
