using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mirage
{
    [CustomPreview(typeof(GameObject))]
    internal class NetworkInformationPreview : ObjectPreview
    {
        private struct NetworkIdentityInfo
        {
            public GUIContent Name;
            public GUIContent Value;
        }

        private struct NetworkBehaviourInfo
        {
            // This is here just so we can check if it's enabled/disabled
            public NetworkBehaviour Behaviour;
            public GUIContent Name;
        }

        private class Styles
        {
            public GUIStyle LabelStyle = new GUIStyle(EditorStyles.label);
            public GUIStyle ComponentName = new GUIStyle(EditorStyles.boldLabel);
            public GUIStyle DisabledName = new GUIStyle(EditorStyles.miniLabel);

            public Styles()
            {
                var fontColor = new Color(0.7f, 0.7f, 0.7f);
                this.LabelStyle.padding.right += 20;
                this.LabelStyle.normal.textColor = fontColor;
                this.LabelStyle.active.textColor = fontColor;
                this.LabelStyle.focused.textColor = fontColor;
                this.LabelStyle.hover.textColor = fontColor;
                this.LabelStyle.onNormal.textColor = fontColor;
                this.LabelStyle.onActive.textColor = fontColor;
                this.LabelStyle.onFocused.textColor = fontColor;
                this.LabelStyle.onHover.textColor = fontColor;

                this.ComponentName.normal.textColor = fontColor;
                this.ComponentName.active.textColor = fontColor;
                this.ComponentName.focused.textColor = fontColor;
                this.ComponentName.hover.textColor = fontColor;
                this.ComponentName.onNormal.textColor = fontColor;
                this.ComponentName.onActive.textColor = fontColor;
                this.ComponentName.onFocused.textColor = fontColor;
                this.ComponentName.onHover.textColor = fontColor;

                this.DisabledName.normal.textColor = fontColor;
                this.DisabledName.active.textColor = fontColor;
                this.DisabledName.focused.textColor = fontColor;
                this.DisabledName.hover.textColor = fontColor;
                this.DisabledName.onNormal.textColor = fontColor;
                this.DisabledName.onActive.textColor = fontColor;
                this.DisabledName.onFocused.textColor = fontColor;
                this.DisabledName.onHover.textColor = fontColor;
            }
        }

        private GUIContent title;
        private Styles styles = new Styles();

        public override GUIContent GetPreviewTitle()
        {
            if (this.title == null)
            {
                this.title = new GUIContent("Network Information");
            }
            return this.title;
        }

        public override bool HasPreviewGUI()
        {
            // need to check if target is null to stop MissingReferenceException 
            return this.target != null && this.target is GameObject gameObject && gameObject.GetComponent<NetworkIdentity>() != null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (this.target == null)
                return;

            var targetGameObject = this.target as GameObject;

            if (targetGameObject == null)
                return;

            var identity = targetGameObject.GetComponent<NetworkIdentity>();

            if (identity == null)
                return;

            if (this.styles == null)
                this.styles = new Styles();

            // padding
            var previewPadding = new RectOffset(-5, -5, -5, -5);
            var paddedRect = previewPadding.Add(r);

            //Centering
            var initialX = paddedRect.x + 10;
            var Y = paddedRect.y + 10;

            Y = this.DrawNetworkIdentityInfo(identity, initialX, Y);

            Y = this.DrawNetworkBehaviors(identity, initialX, Y);

            Y = this.DrawObservers(identity, initialX, Y);

            _ = this.DrawOwner(identity, initialX, Y);

        }

        private float DrawNetworkIdentityInfo(NetworkIdentity identity, float initialX, float Y)
        {
            var infos = this.GetNetworkIdentityInfo(identity);
            // Get required label size for the names of the information values we're going to show
            // There are two columns, one with label for the name of the info and the next for the value
            var maxNameLabelSize = new Vector2(140, 16);
            var maxValueLabelSize = this.GetMaxNameLabelSize(infos);

            var labelRect = new Rect(initialX, Y, maxNameLabelSize.x, maxNameLabelSize.y);
            var idLabelRect = new Rect(maxNameLabelSize.x, Y, maxValueLabelSize.x, maxValueLabelSize.y);

            foreach (var info in infos)
            {
                GUI.Label(labelRect, info.Name, this.styles.LabelStyle);
                GUI.Label(idLabelRect, info.Value, this.styles.ComponentName);
                labelRect.y += labelRect.height;
                labelRect.x = initialX;
                idLabelRect.y += idLabelRect.height;
            }

            return labelRect.y;
        }

        private float DrawNetworkBehaviors(NetworkIdentity identity, float initialX, float Y)
        {
            var behavioursInfo = this.GetNetworkBehaviorInfo(identity);

            // Show behaviours list in a different way than the name/value pairs above

            var maxBehaviourLabelSize = this.GetMaxBehaviourLabelSize(behavioursInfo);
            var behaviourRect = new Rect(initialX, Y + 10, maxBehaviourLabelSize.x, maxBehaviourLabelSize.y);

            GUI.Label(behaviourRect, new GUIContent("Network Behaviours"), this.styles.LabelStyle);
            // indent names
            behaviourRect.x += 20;
            behaviourRect.y += behaviourRect.height;

            foreach (var info in behavioursInfo)
            {
                if (info.Behaviour == null)
                {

                    // could be the case in the editor after existing play mode.
                    continue;
                }

                GUI.Label(behaviourRect, info.Name, info.Behaviour.enabled ? this.styles.ComponentName : this.styles.DisabledName);
                behaviourRect.y += behaviourRect.height;
                Y = behaviourRect.y;
            }

            return Y;
        }

        private float DrawObservers(NetworkIdentity identity, float initialX, float Y)
        {
            if (identity.observers.Count > 0)
            {
                var observerRect = new Rect(initialX, Y + 10, 200, 20);

                GUI.Label(observerRect, new GUIContent("Network observers"), this.styles.LabelStyle);
                // indent names
                observerRect.x += 20;
                observerRect.y += observerRect.height;

                foreach (var player in identity.observers)
                {
                    GUI.Label(observerRect, player.Connection.EndPoint + ":" + player, this.styles.ComponentName);
                    observerRect.y += observerRect.height;
                    Y = observerRect.y;
                }
            }

            return Y;
        }

        private float DrawOwner(NetworkIdentity identity, float initialX, float Y)
        {
            if (identity.Owner != null)
            {
                var ownerRect = new Rect(initialX, Y + 10, 400, 20);
                GUI.Label(ownerRect, new GUIContent("Client Authority: " + identity.Owner), this.styles.LabelStyle);
                Y += ownerRect.height;
            }
            return Y;
        }

        // Get the maximum size used by the value of information items
        private Vector2 GetMaxNameLabelSize(IEnumerable<NetworkIdentityInfo> infos)
        {
            var maxLabelSize = Vector2.zero;
            foreach (var info in infos)
            {
                var labelSize = this.styles.LabelStyle.CalcSize(info.Value);
                if (maxLabelSize.x < labelSize.x)
                {
                    maxLabelSize.x = labelSize.x;
                }
                if (maxLabelSize.y < labelSize.y)
                {
                    maxLabelSize.y = labelSize.y;
                }
            }
            return maxLabelSize;
        }

        private Vector2 GetMaxBehaviourLabelSize(IEnumerable<NetworkBehaviourInfo> behavioursInfo)
        {
            var maxLabelSize = Vector2.zero;
            foreach (var behaviour in behavioursInfo)
            {
                var labelSize = this.styles.LabelStyle.CalcSize(behaviour.Name);
                if (maxLabelSize.x < labelSize.x)
                {
                    maxLabelSize.x = labelSize.x;
                }
                if (maxLabelSize.y < labelSize.y)
                {
                    maxLabelSize.y = labelSize.y;
                }
            }
            return maxLabelSize;
        }

        private IEnumerable<NetworkIdentityInfo> GetNetworkIdentityInfo(NetworkIdentity identity)
        {
            var infos = new List<NetworkIdentityInfo>
            {
                this.GetAssetId(identity),
                GetString("Scene ID", identity.SceneId.ToString("X"))
            };

            if (Application.isPlaying)
            {
                infos.Add(GetString("Network ID", identity.NetId.ToString()));
                infos.Add(GetBoolean("Is Client", identity.IsClient));
                infos.Add(GetBoolean("Is Server", identity.IsServer));
                if (identity.IsClient)
                {
                    infos.Add(GetBoolean("Has Authority", identity.HasAuthority));
                    infos.Add(GetBoolean("Is Local Player", identity.IsLocalPlayer));
                }
                if (identity.IsServer)
                {
                    infos.Add(GetString("Owner", identity.Owner != null ? identity.Owner.ToString() : "NULL"));
                }
            }
            return infos;
        }

        private IEnumerable<NetworkBehaviourInfo> GetNetworkBehaviorInfo(NetworkIdentity identity)
        {
            var behaviourInfos = new List<NetworkBehaviourInfo>();

            var behaviours = identity.GetComponents<NetworkBehaviour>();
            foreach (var behaviour in behaviours)
            {
                behaviourInfos.Add(new NetworkBehaviourInfo
                {
                    Name = new GUIContent(behaviour.GetType().FullName),
                    Behaviour = behaviour
                });
            }
            return behaviourInfos;
        }

        private NetworkIdentityInfo GetAssetId(NetworkIdentity identity)
        {
            var prefabHash = identity.PrefabHash;

            var value = prefabHash != 0
                ? prefabHash.ToString("X")
                : "<object has no prefab>";

            return GetString("Asset ID", value);
        }

        private static NetworkIdentityInfo GetString(string name, string value)
        {
            return new NetworkIdentityInfo
            {
                Name = new GUIContent(name),
                Value = new GUIContent(value)
            };
        }

        private static NetworkIdentityInfo GetBoolean(string name, bool value)
        {
            return new NetworkIdentityInfo
            {
                Name = new GUIContent(name),
                Value = new GUIContent(value ? "Yes" : "No")
            };
        }
    }
}
