using UnityEngine;
using Runtime.Combat; // Access UnitHealth

namespace Runtime.Combat.UI
{
    /// <summary>
    /// Displays the values of an attached UnitHealth component through any IHealthDisplay widgets.
    /// Subscribes to UnitHealth UnityEvents to update the UI automatically.
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthView : MonoBehaviour
    {
        [SerializeField] private UnitHealth _unitHealth;
        [SerializeField] private MonoBehaviour[] _displayComponents; // Components implementing IHealthDisplay

        private IHealthDisplay[] _displays;

        private void Awake()
        {
            if (_unitHealth == null)
                _unitHealth = GetComponentInParent<UnitHealth>();

            _displays = new IHealthDisplay[_displayComponents.Length];
            for (int i = 0; i < _displayComponents.Length; i++)
                _displays[i] = _displayComponents[i] as IHealthDisplay;
        }

        private void OnEnable()
        {
            if (_unitHealth == null)
                return;

            // ✅ Subscribe using UnityEvent methods
            _unitHealth.OnHealthChanged.AddListener(HandleHealthChanged);
            _unitHealth.OnDied.AddListener(HandleDeath);

            // Initialize immediately
            UpdateDisplays(_unitHealth.CurrentHealth, _unitHealth.MaxHealth);
            SetAliveState(_unitHealth.IsAlive);
        }

        private void OnDisable()
        {
            if (_unitHealth == null)
                return;

            // ✅ Unsubscribe
            _unitHealth.OnHealthChanged.RemoveListener(HandleHealthChanged);
            _unitHealth.OnDied.RemoveListener(HandleDeath);
        }

        private void HandleHealthChanged(int current, int max)
        {
            UpdateDisplays(current, max);
        }

        private void HandleDeath()
        {
            SetAliveState(false);
        }

        private void UpdateDisplays(int current, int max)
        {
            foreach (var display in _displays)
            {
                if (display == null) continue;
                display.SetHealth(current, max);
            }
        }

        private void SetAliveState(bool isAlive)
        {
            foreach (var display in _displays)
            {
                if (display == null) continue;
                display.SetAliveState(isAlive);
            }
        }
    }
}
