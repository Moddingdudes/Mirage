using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Mirage.Tests.Performance.Runtime
{
    [Category("Performance")]
    [Category("Benchmark")]
    public class BenchmarkPerformanceLight
    {
        private const string ScenePath = "Assets/Tests/Performance/Runtime/10KL/Scenes/Scene.unity";

        private NetworkManager benchmarker;

        [UnitySetUp]
        public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
        {
#if UNITY_EDITOR
            await EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive });
#else
            throw new System.NotSupportedException("Test not supported in player");
#endif

            var scene = SceneManager.GetSceneByPath(ScenePath);
            SceneManager.SetActiveScene(scene);

            // load host
            this.benchmarker = Object.FindObjectOfType<NetworkManager>();

            this.benchmarker.Server.StartServer(this.benchmarker.Client);

        });

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // shutdown
            this.benchmarker.Server.Stop();
            yield return null;

            // unload scene
            var scene = SceneManager.GetSceneByPath(ScenePath);
            yield return SceneManager.UnloadSceneAsync(scene);

            GameObject.Destroy(this.benchmarker.gameObject);
        }

        [UnityTest]
        [Performance]
        public IEnumerator Benchmark10KLight()
        {
            yield return Measure.Frames().MeasurementCount(240).WarmupCount(50).Run();
        }

    }
}

