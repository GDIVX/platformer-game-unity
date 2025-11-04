using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Combat
{
    /// <summary>
    /// A simple container for CurrentHealth and MaxHealth.
    /// Handles basic value management and broadcasts changes.
    /// Does not handle UI, damage reception, or death behavior.
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        public int MaxHealth
        {
            get => maxHealth;
            private set => maxHealth = Mathf.Max(1, value);
        }

        [SerializeField] private int currentHealth;
        public int CurrentHealth
        {
            get => currentHealth;
            private set
            {
                int newValue = Mathf.Clamp(value, 0, MaxHealth);
                if (newValue == currentHealth) return;

                currentHealth = newValue;
                OnHealthChanged?.Invoke(currentHealth, MaxHealth);

                if (currentHealth <= 0 && IsAlive)
                {
                    IsAlive = false;
                    OnDied?.Invoke();
                }
            }
        }

        public bool IsAlive { get; private set; } = true;

        [Header("Events")]
        public UnityEvent<int, int> OnHealthChanged;
        public UnityEvent OnDied;

        private void Awake()
        {
            CurrentHealth = MaxHealth;
            IsAlive = true;
        }

        /// <summary>
        /// Sets a new max health value. Clamps current health if necessary.
        /// </summary>
        public void SetMaxHealth(int newMaxHealth)
        {
            MaxHealth = newMaxHealth;
            CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// Applies a delta to current health. Positive = heal, negative = damage.
        /// </summary>
        public void ModifyHealth(int amount)
        {
            if (!IsAlive) return;
            CurrentHealth += amount;
        }

        /// <summary>
        /// Fully restores health to maximum.
        /// </summary>
        public void RestoreFull()
        {
            if (!IsAlive) return;
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// Instantly kills this entity.
        /// </summary>
        public void Kill()
        {
            if (!IsAlive) return;
            CurrentHealth = 0;
        }

        public void Revive()
        {
            IsAlive = true;
            RestoreFull();
        }
    }
}
