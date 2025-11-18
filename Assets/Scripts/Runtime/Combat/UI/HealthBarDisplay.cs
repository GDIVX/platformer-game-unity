using UnityEngine;
using Runtime.Combat;
using Runtime.Combat.UI;
using Utilities.UI;

namespace Runtime.Runtime.Combat.UI
{
    [RequireComponent(typeof(ProgressBar))]
    public class HealthBarDisplay : MonoBehaviour, IHealthDisplay
    {
        private ProgressBar _progressBar;

        private void Awake()
        {
            _progressBar = GetComponent<ProgressBar>();
        }

        public void SetHealth(int current, int max)
        {
            if (_progressBar == null) return;

            _progressBar.minimum = 0;
            _progressBar.maximum = max;
            _progressBar.current = current;
        }

        public void SetAliveState(bool isAlive)
        {
            // Optional visual behavior for dead units
            gameObject.SetActive(isAlive);
        }
    }
}