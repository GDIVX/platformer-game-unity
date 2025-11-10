using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Combat
{
    /// <summary>
    /// Manages health values, clamps inputs, and raises events when health changes or death occurs.
    /// Pure state container — no visuals or gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitHealth : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        //  HEALTH DATA
        // ──────────────────────────────────────────────

        [Title("Health Configuration", bold: true)]
        [Tooltip("Maximum health points this unit can have.")]
        [MinValue(1)]
        [OnValueChanged(nameof(OnMaxHealthChanged))]
        [BoxGroup("Health", centerLabel: true)]
        [LabelWidth(100)]
        [SerializeField] private int _maxHealth = 100;

        [BoxGroup("Health"), LabelWidth(100)]
        [ShowInInspector, ReadOnly, PropertyOrder(1)]
        [ProgressBar(0, nameof(_maxHealth), ColorGetter = nameof(GetHealthBarColor))]
        [LabelText("Current Health")]
        [SerializeField] private int _currentHealth;

        [BoxGroup("Health"), LabelWidth(100)]
        [ReadOnly, ShowInInspector, GUIColor(nameof(GetAliveColor))]
        [PropertyOrder(2)]
        [SerializeField, LabelText("Is Alive")]
        private bool _isAlive = true;

        public int MaxHealth => _maxHealth;
        public int CurrentHealth => _currentHealth;
        public bool IsAlive => _isAlive;

        // ──────────────────────────────────────────────
        //  EVENTS
        // ──────────────────────────────────────────────

        [TitleGroup("Events"), LabelWidth(120)]
        public UnityEvent<int, int> OnHealthChanged;

        [TitleGroup("Events"), LabelWidth(120)]
        public UnityEvent OnDied;

        [TitleGroup("Events"), LabelWidth(120)]
        public UnityEvent OnRevived;

        // ──────────────────────────────────────────────
        //  INITIALIZATION
        // ──────────────────────────────────────────────

        private void Awake()
        {
            _currentHealth = _maxHealth;
            _isAlive = true;
        }

        // ──────────────────────────────────────────────
        //  DESIGNER TOOLS
        // ──────────────────────────────────────────────

        [BoxGroup("Debug Modify", showLabel: true)]
        [LabelText("Δ Health"), LabelWidth(80)]
        [Tooltip("Positive = Heal, Negative = Damage")]
        [PropertyOrder(90)]
        [SerializeField] private int _modifyValue = -10;

        [Button(ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        [PropertyOrder(91)]
        private void ApplyModify()
        {
            ModifyHealth(_modifyValue);
        }

        // ──────────────────────────────────────────────
        //  CORE RUNTIME LOGIC
        // ──────────────────────────────────────────────

        public void ModifyHealth(int delta)
        {
            if (!_isAlive) return;

            ApplyHealthChange(_currentHealth + delta);
        }

        [ButtonGroup("Buttons")]
        [GUIColor(0.4f, 1f, 0.4f)]
        public void RestoreFull()
        {
            if (!_isAlive || _currentHealth == _maxHealth) return;

            ApplyHealthChange(_maxHealth);
        }

        [ButtonGroup("Buttons")]
        [GUIColor(1f, 0.3f, 0.3f)]
        public void Kill()
        {
            if (!_isAlive) return;

            ApplyHealthChange(0, true);
        }

        [ButtonGroup("Buttons")]
        [GUIColor(0.3f, 0.8f, 1f)]
        public void Revive()
        {
            if (_isAlive) return;

            ApplyHealthChange(_maxHealth, true, true);
        }

        // ──────────────────────────────────────────────
        //  INTERNAL HELPERS
        // ──────────────────────────────────────────────

        public void SetMaxHealth(int newMax)
        {
            InternalSetMaxHealth(newMax, newMax != _maxHealth);
        }

        private void OnMaxHealthChanged()
        {
            InternalSetMaxHealth(_maxHealth, true);
        }

        private void InternalSetMaxHealth(int requestedMax, bool forceBroadcast)
        {
            int sanitizedMax = Mathf.Max(1, requestedMax);
            bool sanitizedChanged = sanitizedMax != requestedMax;
            bool maxChanged = sanitizedMax != _maxHealth;

            _maxHealth = sanitizedMax;

            int clampedHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
            bool healthClamped = clampedHealth != _currentHealth;

            ApplyHealthChange(
                clampedHealth,
                forceBroadcast || sanitizedChanged || maxChanged || healthClamped);
        }

        private void ApplyHealthChange(int targetHealth, bool forceInvokeHealthChanged = false, bool allowReviveEvent = false)
        {
            int clamped = Mathf.Clamp(targetHealth, 0, _maxHealth);
            int previousHealth = _currentHealth;
            bool wasAlive = _isAlive;

            _currentHealth = clamped;
            bool shouldBeAlive = _currentHealth > 0;
            _isAlive = shouldBeAlive;

            if (forceInvokeHealthChanged || previousHealth != _currentHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            }

            if (wasAlive && !shouldBeAlive)
            {
                OnDied?.Invoke();
            }
            else if (!wasAlive && shouldBeAlive && allowReviveEvent)
            {
                OnRevived?.Invoke();
            }
        }

        private Color GetHealthBarColor()
        {
            float pct = (float)_currentHealth / _maxHealth;
            return Color.Lerp(Color.red, Color.green, pct);
        }

        private Color GetAliveColor()
        {
            return _isAlive ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.4f, 0.4f);
        }
    }
}
