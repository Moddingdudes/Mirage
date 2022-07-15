using Mirage.Logging;
using UnityEditor;

namespace Mirage.EditorScripts.Logging
{
    [CustomEditor(typeof(LogSettings)), CanEditMultipleObjects]
    public class LogSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            this.DrawDefaultInspector();

            var target = this.target as LogSettings;

            if (target.settings == null)
            {
                var newSettings = LogLevelsGUI.DrawCreateNewButton();
                if (newSettings != null)
                {
                    var settingsProp = this.serializedObject.FindProperty("settings");
                    settingsProp.objectReferenceValue = newSettings;
                    this.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                LogLevelsGUI.DrawSettings(target.settings);
            }
        }
    }
}
