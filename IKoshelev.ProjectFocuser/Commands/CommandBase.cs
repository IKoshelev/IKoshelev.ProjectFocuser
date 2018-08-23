using System;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IKoshelev.ProjectFocuser.Commands
{
    public abstract class CommandBase
    {
        public abstract int CommandId { get; }

        protected CommandBase(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;
        }

        public static readonly Guid CommandSet = new Guid("b97309f8-343e-445a-adaa-16db2957a3b2");

        protected Package package;

        protected IServiceProvider ServiceProvider => this.package;

        protected const uint VSITEMID_ROOT = 0xFFFFFFFE;
        protected void AddCommandToMenu(EventHandler handler)
        {
            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(handler, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        protected bool ShowErrorMessageAndReturnTrueIfNoSolutionOpen()
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            if(dte.Solution.IsOpen)
            {
                return false;
            }

            var message = "Please open solution";

            VsShellUtilities.ShowMessageBox(
                                    this.ServiceProvider,
                                    message,
                                    ProjectFocuserCommandPackage.MessageBoxName,
                                    OLEMSGICON.OLEMSGICON_CRITICAL,
                                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            return true;
        }
    }
}
