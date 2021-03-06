﻿using IKoshelev.ProjectFocuser.Util;
using System.IO;
using System.Linq;

namespace IKoshelev.ProjectFocuser
{
    public class SuoBackupHelper
    {
        private string slnPath;

        public SuoBackupHelper(string slnPath)
        {
            SlnFilePath = slnPath;
        }

        public string SlnFilePath { get; set; }
        public string SlnFolderPath => Path.GetDirectoryName(SlnFilePath);
        public string SlnFileName => Path.GetFileName(SlnFilePath);
        public string SlnFileNameNoExtension => Path.GetFileNameWithoutExtension(SlnFilePath);
        public string ExpectedSuoFolderPath => Path.Combine(SlnFolderPath, ".vs", SlnFileNameNoExtension, "v15");
        public string ExpectedSuoFilePath => Path.Combine(ExpectedSuoFolderPath, ".suo");

        public const string BackupFolderName = "IKoshelev.ProjectFocuser.Backup";
        public string ExpectedSuoBackupFolderPath => Path.Combine(ExpectedSuoFolderPath, BackupFolderName);

        public void EnsureBackupFolderExists()
        {
            if (Directory.Exists(ExpectedSuoBackupFolderPath) == false)
            {
                Directory.CreateDirectory(ExpectedSuoBackupFolderPath);
            }
        }

        public const string SuoBackupFileExtension = ".suo.bcp";
        public string[] GetExistingSuoBackupNames()
        {
            if(Directory.Exists(ExpectedSuoBackupFolderPath) == false)
            {
                return new string[0];
            }

            return Directory
                        .GetFiles(ExpectedSuoBackupFolderPath,"*" + SuoBackupFileExtension)
                        .Select(Path.GetFileName)
                        .ToArray();
        }

        public bool SuoFileExists => File.Exists(ExpectedSuoFilePath);

        public bool SuoFileMissing => !SuoFileExists;

        public void BackupCurrentSuo(string backupFileNameWithoutExtensions)
        {
            EnsureBackupFolderExists();
            var backupFileFullPath = Path.Combine(ExpectedSuoBackupFolderPath, backupFileNameWithoutExtensions + SuoBackupFileExtension);
            File.Copy(ExpectedSuoFilePath, backupFileFullPath);

            var settingsHelper = new ExtensionSettingsHelper();
            var settings = settingsHelper.GetSettings();

            settings.SuoBackups.Remove(SlnFilePath);

            var existingBackups = GetExistingSuoBackupNames();
            settings.SuoBackups.Add(SlnFilePath, existingBackups);
            settingsHelper.SaveSettings(settings);
        }

        public void RestoreSuoBackup(string suoBackupFileNameWithExtension)
        {
            var backupFileFullPath = Path.Combine(ExpectedSuoBackupFolderPath, suoBackupFileNameWithExtension);
            File.Copy(backupFileFullPath, ExpectedSuoFilePath, true);
        }
    }
}
