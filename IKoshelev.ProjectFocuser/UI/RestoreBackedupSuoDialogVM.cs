using IKoshelev.ProjectFocuser.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IKoshelev.ProjectFocuser.UI
{
    public class RestoreBackedupSuoDialogVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private SuoBackup[] availableSuoBackups;

        public SuoBackup[] AvailableSuoBackups
        {
            get
            {
                return availableSuoBackups;
            }
            set
            {
                availableSuoBackups = value;
                OnPropertyChanged();
            }
        }

        private ICommand restoreBackedupSuoCommand;
        public ICommand RestoreBackedupSuoCommand
        {
            get
            {
                if (restoreBackedupSuoCommand == null)
                {
                    restoreBackedupSuoCommand = new RelayCommand(
                       p => true,
                       p =>
                       {
                           var backup = p as SuoBackup;
                           if(p is null)
                           {
                               return;
                           }
                           var helper = new SuoBackupHelper(backup.SlnPath);
                           helper.RestoreSuoBackup(backup.BackupFileNameWithExtension);
                           MessageBox.Show("Restore done", "Done", MessageBoxButton.OK, MessageBoxImage.None);
                       });
                }
                return restoreBackedupSuoCommand;
            }
        }

        public RestoreBackedupSuoDialogVM(ProjectFocuserSettings settings)
        {
            AvailableSuoBackups = settings
                                    .SuoBackups
                                    .SelectMany(kvp => kvp.Value.Select(val => new SuoBackup(kvp.Key, val)))
                                    .ToArray();
        }

        public class SuoBackup
        {
            public SuoBackup(string slnPath, string backupFileNameWithExtension)
            {
                SlnPath = slnPath;
                BackupFileNameWithExtension = backupFileNameWithExtension;
            }

            public string SlnPath { get; set; }
            public string BackupFileNameWithExtension { get; set; }
        }
    }
}
