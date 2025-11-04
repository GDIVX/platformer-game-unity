using UnityEngine;
using UnityEngine.Events;

namespace Runtime
{
    public class Bootstrap : MonoBehaviour
    {
        public UnityEvent OnSceneLoaded;

        void Start()
        {
            DontDestroyOnLoad(this);
            OnSceneLoaded?.Invoke();
        }
    }
}