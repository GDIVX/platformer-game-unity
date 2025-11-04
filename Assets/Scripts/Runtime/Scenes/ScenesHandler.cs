using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace Runtime.Scenes
{
    /// <summary>
    /// Central place to load/unload scenes (single or additive) with optional fading.
    /// Requires a MonoSingleton
    /// base in your project.
    /// </summary>
    public class ScenesHandler : MonoSingleton<ScenesHandler>
    {
        #region Public API

        /// <summary>
        /// Load a scene normally (single). Replaces current scene.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Single, setActiveAfterLoad: true));
        }

        /// <summary>
        /// Reload current scene.
        /// </summary>
        public void ReloadCurrentScene()
        {
            string current = SceneManager.GetActiveScene().name;
            LoadScene(current);
        }

        /// <summary>
        /// Load a scene additively and optionally make it the active scene.
        /// </summary>
        public void LoadSceneAdditive(string sceneName, bool setActive = false)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, LoadSceneMode.Additive, setActive));
        }

        /// <summary>
        /// Unload an additive scene by name.
        /// </summary>
        public void UnloadScene(string sceneName)
        {
            StartCoroutine(UnloadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Set which loaded scene is the active one (for lighting, Instantiate, etc).
        /// </summary>
        public void SetActiveScene(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.SetActiveScene(scene);
            else
                Debug.LogWarning($"[ScenesHandler] Cannot set active scene. Scene '{sceneName}' is not loaded.");
        }

        #endregion


        private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode, bool setActiveAfterLoad)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, mode);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                yield return null;

            op.allowSceneActivation = true;
            yield return new WaitUntil(() => op.isDone);

            if (setActiveAfterLoad)
                SetActiveScene(sceneName);
        }

        private IEnumerator UnloadSceneRoutine(string sceneName)
        {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                Debug.LogWarning($"[ScenesHandler] Tried to unload '{sceneName}' but it's not loaded.");
            }
            else
            {
                AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
                if (op != null)
                    yield return op;
            }
        }

        public IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode, bool setActiveAfterLoad)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, mode);
            while (!op.isDone) yield return null;
            if (setActiveAfterLoad) SetActiveScene(sceneName);
        }

        public IEnumerator UnloadSceneAsync(string sceneName)
        {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                yield break;
            AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
            while (op is { isDone: false }) yield return null;
        }
    }
}