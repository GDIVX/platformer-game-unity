using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

namespace Runtime.Scenes
{
    /// <summary>
    /// Helper for multi-scene loading setups (menus, gameplay, etc).
    /// Works with ScenesHandler.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [Header("Scene Settings")] [SerializeField]
        private string[] _scenesToLoad;

        [SerializeField] bool _unloadThisScene;
        [SerializeField] private string[] _scenesToUnload;
        [SerializeField] private bool _setActiveToLastLoaded = true;

        [Header("Events")] public UnityEvent OnBeforeLoad;
        public UnityEvent OnAfterLoad;

        public void Load()
        {
            OnBeforeLoad?.Invoke();
            StartCoroutine(LoadRoutine());
        }

        private IEnumerator LoadRoutine()
        {
            var handler = ScenesHandler.Instance;
            if (handler == null)
            {
                Debug.LogError($"[{nameof(SceneLoader)}] No ScenesHandler found in the scene!");
                yield break;
            }


            // Unload unwanted scenes
            foreach (string scene in _scenesToUnload)
            {
                if (!string.IsNullOrEmpty(scene))
                {
                    yield return handler.UnloadSceneAsync(scene);
                }
            }

            // Load new ones additively
            for (int i = 0; i < _scenesToLoad.Length; i++)
            {
                string scene = _scenesToLoad[i];
                if (string.IsNullOrEmpty(scene)) continue;

                yield return handler.LoadSceneAsync(scene, LoadSceneMode.Additive, false);

                // Optionally set the last loaded as active
                if (_setActiveToLastLoaded && i == _scenesToLoad.Length - 1)
                    handler.SetActiveScene(scene);
            }

            if (_unloadThisScene)
            {
                var thisScene = gameObject.scene;
                yield return handler.UnloadSceneAsync(thisScene.name);
            }


            OnAfterLoad?.Invoke();
        }
    }
}