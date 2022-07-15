using Mirage.Logging;
using UnityEditor;
using UnityEngine;

namespace Mirage.EditorScripts.Logging
{
    [CustomEditor(typeof(LogSettingsSO))]
    public class LogSettingsSOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            this.CurrentScriptField();
            EditorGUILayout.Space();
            LogLevelsGUI.DrawSettings(this.target as LogSettingsSO);
        }

        public void CurrentScriptField()
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(this.serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;
        }
    }
}
