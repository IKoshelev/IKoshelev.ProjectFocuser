using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IKoshelev.ProjectFocuser.Util
{
    public class ExtensionSettingsHelper
    {
        public static string SettingsFolderPaths => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Visual Studio 2017\Settings\IKoshelev.ProjectFocuser\");

        public static string SettingsFileNameWithExtensions => "IKoshelev.ProjectFocuser.Settings.json";

        public static string SettingsFilePath => Path.Combine(SettingsFolderPaths, SettingsFileNameWithExtensions);

        public ProjectFocuserSettings GetSettings()
        {
            if (File.Exists(SettingsFilePath) == false)
            {
                return new ProjectFocuserSettings();
            }

            var settingsJson = File.ReadAllText(SettingsFilePath);

            var settings = JsonConvert.DeserializeObject<ProjectFocuserSettings>(settingsJson);

            return settings;
        }

        //todo threadsafety? 
        public void SaveSettings(ProjectFocuserSettings settings)
        {
            if (File.Exists(SettingsFolderPaths) == false)
            {
                Directory.CreateDirectory(SettingsFolderPaths);
            }

            var settingsJson = JsonConvert.SerializeObject(settings);

            File.WriteAllText(SettingsFilePath, settingsJson);
        }
    }

    public class ProjectFocuserSettings
    {
        public Dictionary<string, string[]> SuoBackups = new Dictionary<string, string[]>();
    }
}
