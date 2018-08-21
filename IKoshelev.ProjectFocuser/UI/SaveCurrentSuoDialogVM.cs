using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IKoshelev.ProjectFocuser.UI
{
    public class SaveCurrentSuoDialogVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private SolutionPathsHelper slnPathsHelper;

        public SolutionPathsHelper SlnPathsHelper
        {
            get
            {
                return slnPathsHelper;
            }
            set
            {
                slnPathsHelper = value;
                OnPropertyChanged();
            }
        }

        private string userMessage;

        public string UserMessage
        {
            get
            {
                return userMessage;
            }
            set
            {
                userMessage = value;
                OnPropertyChanged();
            }
        }

        private string suoBackupFileNameWithoutExtension;

        public string SuoBackupFileNameWithoutExtension
        {
            get
            {
                return suoBackupFileNameWithoutExtension;
            }
            set
            {
                suoBackupFileNameWithoutExtension = value;
                OnPropertyChanged();
            }
        }

        public SaveCurrentSuoDialogVM(string solutionPath)
        {
            SlnPathsHelper = new SolutionPathsHelper(solutionPath);
            var existingSuoBackups = SlnPathsHelper.GetExistingSuoBackupNames();
            userMessage = "Existing .suo backups: " +
                               (existingSuoBackups.Any()
                                    ? " \r\n" + string.Join("\r\n", existingSuoBackups)
                                    : "none");

            PopulateInitialSuoBackupFileNameWithoutExtension(existingSuoBackups);

            if (slnPathsHelper.SuoFileExists == false)
            {
                userMessage += "\r\n" + GetSuoNotExistsMessage();
            }
        }

        private void PopulateInitialSuoBackupFileNameWithoutExtension(string[] existingSuoBackups)
        {
            var initialBackupFileNameCandidate = "";
            var counter = 1;
            do
            {
                initialBackupFileNameCandidate = "backup" + counter;
                counter += 1;
            }
            while (existingSuoBackups.Any(x => x.StartsWith(initialBackupFileNameCandidate)));

            SuoBackupFileNameWithoutExtension = initialBackupFileNameCandidate;
        }

        private string GetSuoNotExistsMessage()
        {
            return ".suo file not found at expected path " + slnPathsHelper.ExpectedSuoFilePath;
        }

        private ICommand openSolutionFolderCommand;
        public ICommand OpenSolutionFolderCommand
        {
            get
            {
                if (openSolutionFolderCommand == null)
                {
                    openSolutionFolderCommand = new RelayCommand(
                       p => true,
                       p => this.OpenSolutionFolder());
                }
                return openSolutionFolderCommand;
            }
        }

        private ICommand backupCurrentSuoFileCommand;
        public ICommand BackupCurrentSuoFileCommand
        {
            get
            {
                if (backupCurrentSuoFileCommand == null)
                {
                    backupCurrentSuoFileCommand = new RelayCommand(
                       p => string.IsNullOrWhiteSpace(SuoBackupFileNameWithoutExtension) == false && slnPathsHelper.SuoFileExists,
                       p => this.BackupCurrentSuoFile());
                }
                return backupCurrentSuoFileCommand;
            }
        }

        private ICommand openExpectedSuoFolderCommand;
        public ICommand OpenExpectedSuoFolderCommand
        {
            get
            {
                if (openExpectedSuoFolderCommand == null)
                {
                    openExpectedSuoFolderCommand = new RelayCommand(
                       p => true,
                       p => this.OpenExpectedSuoFolder());
                }
                return openExpectedSuoFolderCommand;
            }
        }     

        public void OpenSolutionFolder()
        {
            DoWithUserMessage(() => 
            {
                System.Diagnostics.Process.Start(SlnPathsHelper.SlnFolderPath);             
            });
        }

        public void OpenExpectedSuoFolder()
        {
            DoWithUserMessage(() =>
            {
                System.Diagnostics.Process.Start(SlnPathsHelper.ExpectedSuoFolderPath);
            });
        }

        public void ClearUserMessage()
        {
            this.UserMessage = string.Empty;
        }

        public void SetUserMessage(string message)
        {
            this.UserMessage = message;
        }

        public void DoWithUserMessage(Action action, string successMessage = "")
        {
            try
            {
                ClearUserMessage();
                action();
                SetUserMessage(successMessage);

            }
            catch (Exception ex)
            {
                SetUserMessage(ex.Message);
            }
        }

        public void BackupCurrentSuoFile()
        {
            if(slnPathsHelper.SuoFileExists == false)
            {
                SetUserMessage(GetSuoNotExistsMessage());
                return; 
            }

            DoWithUserMessage(() => slnPathsHelper.BackupCurrentSuo(SuoBackupFileNameWithoutExtension), 
                $"{SuoBackupFileNameWithoutExtension + SolutionPathsHelper.SuoBackupFileExtension} backed up.");

        } 
    }
}
