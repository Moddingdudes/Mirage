using System;
using Unity.CodeEditor;
using UnityEditor;
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
                Type syncVS = typeof(CodeEditor).Assembly.GetType("UnityEditor.SyncVS");
                System.Reflection.MethodInfo syncSln = syncVS.GetMethod("SyncSolution", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
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
                //var vsEditor = new VisualStudioEditor();
                //vsEditor.SyncAll();

                var projectGeneration = new Microsoft.Unity.VisualStudio.Editor.ProjectGeneration();
                AssetDatabase.Refresh();
                projectGeneration.Sync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                Debug.Log($"CodeEditor.Editor {CodeEditor.Editor}");
                Debug.Log($"CodeEditor.Editor typeof {CodeEditor.Editor.GetType()}");
                Debug.Log($"CodeEditor.CurrentEditor {CodeEditor.CurrentEditor}");
                Debug.Log($"CodeEditor.Editor.CurrentCodeEditor {CodeEditor.Editor.CurrentCodeEditor}");
                if (CodeEditor.Editor.CurrentCodeEditor.Installations == null)
                {
                    Debug.Log($"CodeEditor.Editor.CurrentCodeEditor.Installations NULL");
                }
                else
                {
                    Debug.Log($"CodeEditor.Editor.CurrentCodeEditor.Installations.Length {CodeEditor.Editor.CurrentCodeEditor.Installations.Length}");
                    for (int i = 0; i < CodeEditor.Editor.CurrentCodeEditor.Installations.Length; i++)
                    {
                        Debug.Log($"CodeEditor.Editor.CurrentCodeEditor.Installations[{i}]: {CodeEditor.Editor.CurrentCodeEditor.Installations[i]}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Debug.Log($"CreateSolution End");
        }
    }
}
