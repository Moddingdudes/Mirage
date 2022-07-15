using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mirage.Collections;
using Mirage.Logging;
using UnityEditor;
using UnityEngine;

namespace Mirage
{
    [CustomEditor(typeof(NetworkBehaviour), true)]
    [CanEditMultipleObjects]
    public class NetworkBehaviourInspector : Editor
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkBehaviourInspector));

        /// <summary>
        /// List of all visible syncVars in target class
        /// </summary>
        protected List<string> syncVarNames = new List<string>();
        private bool syncsAnything;
        private SyncListDrawer syncListDrawer;

        // does this type sync anything? otherwise we don't need to show syncInterval
        private bool SyncsAnything(Type scriptClass)
        {
            // check for all SyncVar fields, they don't have to be visible
            foreach (var field in InspectorHelper.GetAllFields(scriptClass, typeof(NetworkBehaviour)))
            {
                if (field.IsSyncVar())
                {
                    return true;
                }
            }

            // has OnSerialize that is not in NetworkBehaviour?
            // then it either has a syncvar or custom OnSerialize. either way
            // this means we have something to sync.
            var method = scriptClass.GetMethod("OnSerialize");
            if (method != null && method.DeclaringType != typeof(NetworkBehaviour))
            {
                return true;
            }

            // SyncObjects are serialized in NetworkBehaviour.OnSerialize, which
            // is always there even if we don't use SyncObjects. so we need to
            // search for SyncObjects manually.
            // Any SyncObject should be added to syncObjects when unity creates an
            // object so we can cheeck length of list so see if sync objects exists
            var syncObjectsField = scriptClass.GetField("syncObjects", BindingFlags.NonPublic | BindingFlags.Instance);
            var syncObjects = (List<ISyncObject>)syncObjectsField.GetValue(this.serializedObject.targetObject);

            return syncObjects.Count > 0;
        }

        private void OnEnable()
        {
            if (this.target == null) { logger.LogWarning("NetworkBehaviourInspector had no target object"); return; }

            // If target's base class is changed from NetworkBehaviour to MonoBehaviour
            // then Unity temporarily keep using this Inspector causing things to break
            if (!(this.target is NetworkBehaviour)) { return; }

            var scriptClass = this.target.GetType();

            this.syncVarNames = new List<string>();
            foreach (var field in InspectorHelper.GetAllFields(scriptClass, typeof(NetworkBehaviour)))
            {
                if (field.IsSyncVar() && field.IsVisibleField())
                {
                    this.syncVarNames.Add(field.Name);
                }
            }

            this.syncListDrawer = new SyncListDrawer(this.serializedObject.targetObject);

            this.syncsAnything = this.SyncsAnything(scriptClass);
        }

        public override void OnInspectorGUI()
        {
            this.DrawDefaultInspector();
            this.DrawDefaultSyncLists();
            this.DrawDefaultSyncSettings();
        }

        /// <summary>
        /// Draws Sync Objects that are IEnumerable
        /// </summary>
        protected void DrawDefaultSyncLists()
        {
            // Need this check incase OnEnable returns early
            if (this.syncListDrawer == null) { return; }

            this.syncListDrawer.Draw();
        }

        /// <summary>
        /// Draws SyncSettings if the NetworkBehaviour has anything to sync
        /// </summary>
        protected void DrawDefaultSyncSettings()
        {
            // does it sync anything? then show extra properties
            // (no need to show it if the class only has Cmds/Rpcs and no sync)
            if (!this.syncsAnything)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sync Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(this.serializedObject.FindProperty("syncMode"));
            EditorGUILayout.PropertyField(this.serializedObject.FindProperty("syncInterval"));

            // apply
            this.serializedObject.ApplyModifiedProperties();
        }
    }
    public class SyncListDrawer
    {
        private readonly UnityEngine.Object targetObject;
        private readonly List<SyncListField> syncListFields;

        public SyncListDrawer(UnityEngine.Object targetObject)
        {
            this.targetObject = targetObject;
            this.syncListFields = new List<SyncListField>();
            foreach (var field in InspectorHelper.GetAllFields(targetObject.GetType(), typeof(NetworkBehaviour)))
            {
                if (field.IsSyncObject() && field.IsVisibleSyncObject())
                {
                    this.syncListFields.Add(new SyncListField(field));
                }
            }
        }

        public void Draw()
        {
            if (this.syncListFields.Count == 0) { return; }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sync Lists", EditorStyles.boldLabel);

            for (var i = 0; i < this.syncListFields.Count; i++)
            {
                this.DrawSyncList(this.syncListFields[i]);
            }
        }

        private void DrawSyncList(SyncListField syncListField)
        {
            syncListField.visible = EditorGUILayout.Foldout(syncListField.visible, syncListField.label);
            if (syncListField.visible)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var fieldValue = syncListField.field.GetValue(this.targetObject);
                    if (fieldValue is IEnumerable synclist)
                    {
                        var index = 0;
                        foreach (var item in synclist)
                        {
                            var itemValue = item != null ? item.ToString() : "NULL";
                            var itemLabel = "Element " + index;
                            EditorGUILayout.LabelField(itemLabel, itemValue);

                            index++;
                        }
                    }
                }
            }
        }

        private class SyncListField
        {
            public bool visible;
            public readonly FieldInfo field;
            public readonly string label;

            public SyncListField(FieldInfo field)
            {
                this.field = field;
                this.visible = false;
                this.label = field.Name + "  [" + field.FieldType.Name + "]";
            }
        }
    }
}
