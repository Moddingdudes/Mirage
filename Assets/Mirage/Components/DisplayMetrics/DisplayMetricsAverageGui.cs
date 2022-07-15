using Mirage.SocketLayer;
using UnityEngine;

namespace Mirage.DisplayMetrics
{
    /// <summary>
    /// This is an example of how to show metrics, It only shows some of the values inside <see cref="Mirage.SocketLayer.Metrics"/>
    /// <para>If you want to show more of the values then create a copy of this class and add values to DrawAverage</para>
    /// </summary>
    public class DisplayMetricsAverageGui : MonoBehaviour
    {
        public Metrics Metrics { get; set; }

        public Rect offset = new Rect(10, 10, 400, 800);
        public Color background;
        private GUIStyle style;
        private Texture2D tex;


        private void Start()
        {
            this.style = new GUIStyle();
            this.tex = new Texture2D(1, 1);
            this.tex.SetPixel(0, 0, this.background);
            this.tex.Apply();
            this.style.normal.background = this.tex;
        }
        private void OnDestroy()
        {
            if (this.tex != null)
            {
                Destroy(this.tex);
            }
        }

        private void OnValidate()
        {
            if (this.tex != null)
            {
                this.tex.SetPixel(0, 0, this.background);
                this.tex.Apply();
            }
        }

        private void OnGUI()
        {
            if (this.Metrics == null) { return; }

            using (new GUILayout.AreaScope(this.offset, GUIContent.none, this.style))
            {
                this.DrawAverage();
            }
        }

        private void DrawAverage()
        {
            double connectionCount = 0;

            double sendCount = 0;
            double sendBytes = 0;

            double receiveCount = 0;
            double receiveBytes = 0;

            var array = this.Metrics.buffer;
            var count = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (!array[i].init)
                {
                    continue;
                }

                count++;
                connectionCount += array[i].connectionCount;

                sendCount += array[i].sendCount;
                sendBytes += array[i].sendBytes;

                receiveCount += array[i].receiveCount;
                receiveBytes += array[i].receiveBytes;
            }

            GUILayout.Label($"connectionCount: {connectionCount / count:0.0}");
            GUILayout.Space(8);
            GUILayout.Label($"sendCount: {sendCount / count:0.0}");
            GUILayout.Label($"sendBytes: {sendBytes / count:0.00}");
            GUILayout.Space(8);
            GUILayout.Label($"receiveCount: {receiveCount / count:0.0}");
            GUILayout.Label($"receiveBytes: {receiveBytes / count:0.00}");
        }
    }
}
