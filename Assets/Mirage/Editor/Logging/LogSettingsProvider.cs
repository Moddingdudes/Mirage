using System.Collections.Generic;
using Mirage.Logging;
using UnityEditor;

namespace Mirage.EditorScripts.Logging
{
    public class LogSettingsProvider : SettingsProvider
    {
        private LogSettingsSO settings;

        public LogSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new LogSettingsProvider("Mirage/Logging", SettingsScope.Project) { label = "Logging" };
        }

        public override void OnGUI(string searchContext)
        {
            // look for existing settings first
            if (this.settings == null)
            {
                this.settings = EditorLogSettingsLoader.FindLogSettings();
            }

            // then draw field
            this.settings = (LogSettingsSO)EditorGUILayout.ObjectField("Settings", this.settings, typeof(LogSettingsSO), false);

            // then draw rest of ui
            if (this.settings == null)
                this.settings = LogLevelsGUI.DrawCreateNewButton();
            else
                LogLevelsGUI.DrawSettings(this.settings);
        }
    }
}
