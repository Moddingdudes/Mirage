using System;
using Microsoft.Unity.VisualStudio.Editor;
using Unity.CodeEditor;
using UnityEngine;

namespace Mirage.EditorScripts
{
    /// <summary>
    /// Creates .sln and .csproj files for CI
    /// </summary>
    public static class GenerateSolutionFiles
    {
        public static void CreateSolution()
        {
            Debug.Log($"CreateSolution Start");
            try
            {
                Debug.Log($"UnityEditor.SyncVS.SyncSolution");
                var syncVS = typeof(CodeEditor).Assembly.GetType("UnityEditor.SyncVS");
                var syncSln = syncVS.GetMethod("SyncSolution", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                syncSln.Invoke(null, null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            CodeEditor.Editor.CurrentCodeEditor.SyncAll();
            CodeEditor.CurrentEditor.SyncAll();


            //try
            //{
            //    Debug.Log($"UnityEditor.CodeEditorProjectSync");
            //    AssetDatabase.Refresh();
            //    CodeEditor.Editor.CurrentCodeEditor.SyncAll();
            //    CodeEditor.Editor.CurrentCodeEditor.OpenProject("", -1, -1);
            //}
            //catch (Exception e)
            //{
            //    Debug.LogException(e);
            //}


            try
            {
                Debug.Log($"VisualStudioEditor.SyncAll");
                var vsEditor = new VisualStudioEditor();
                vsEditor.SyncAll();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Debug.Log($"CodeEditor.Editor {CodeEditor.Editor}");
            Debug.Log($"CodeEditor.Editor typeof {CodeEditor.Editor.GetType()}");
            Debug.Log($"CodeEditor.CurrentEditor {CodeEditor.CurrentEditor}");
            Debug.Log($"CodeEditor.Editor.CurrentCodeEditor {CodeEditor.Editor.CurrentCodeEditor}");
            Debug.Log($"CodeEditor.Editor.CurrentCodeEditor.Installations.Length {CodeEditor.Editor.CurrentCodeEditor.Installations.Length}");
            for (int i = 0; i < CodeEditor.Editor.CurrentCodeEditor.Installations.Length; i++)
            {
                Debug.Log($"CodeEditor.Editor.CurrentCodeEditor.Installations[{i}]: {CodeEditor.Editor.CurrentCodeEditor.Installations[i]}");
            }

            Debug.Log($"CreateSolution End");
        }

    }
}
