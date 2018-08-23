using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using IKoshelev.ProjectFocuser.UI;
using IKoshelev.ProjectFocuser.Util;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IKoshelev.ProjectFocuser.Commands
{
    internal sealed class RestorePresavedSuoCommand : CommandBase
    {
        public override int CommandId => 0x0800;

        private RestorePresavedSuoCommand(Package package) : base(package)
        {
            AddCommandToMenu(this.MenuItemCallback);
        }

        public static RestorePresavedSuoCommand Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new RestorePresavedSuoCommand(package);
        }

        public void MenuItemCallback(object sender, EventArgs e)
        {
            var settingsHelper = new ExtensionSettingsHelper();
            var settings = settingsHelper.GetSettings();

            var documentationControl = new UI.RestoreBackedupSuoDialog();
            documentationControl.DataContext = new RestoreBackedupSuoDialogVM(settings);
            documentationControl.ShowDialog();
        }
    }
}
