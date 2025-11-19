using UnityEngine;
using UnityEngine.Events;
using Runtime.Movement;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif


namespace Runtime.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class HurtBox : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private UnitHealth _health;

        [SerializeField] private ArmorProfile _armorProfile;

        [Header("Invulnerability")] [SerializeField]
        private float _invulnerabilityTime = 0.5f;

        [Header("Events")] public UnityEvent<HitBox> OnHitReceived;
        public UnityEvent<HitBox, int> OnDamageApplied;
        public UnityEvent OnBecameInvulnerable;
        public UnityEvent OnInvulnerabilityEnded;
        public UnityEvent OnKnockbackApplied;

        [ShowInInspector, ReadOnly] private bool _isInvulnerable;
        [ShowInInspector, ReadOnly] private float _invulnTimer;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponentInParent<UnitHealth>();

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Update()
        {
            if (!_isInvulnerable) return;
            _invulnTimer -= Time.deltaTime;
            if (_invulnTimer <= 0f)
                SetInvulnerable(false);
        }

        public bool ApplyHit(HitBox hitBox, bool isCrit)
        {
            if (_isInvulnerable || _health == null || !_health.IsAlive)
                return false;

            OnHitReceived?.Invoke(hitBox);

            DamageProfile data = hitBox.Damage;

            // --- DAMAGE CALCULATION ---
            float baseDamage = _armorProfile != null
                ? _armorProfile.CalculateEffectiveDamage(data)
                : data.TotalBaseDamage;

            if (isCrit)
                baseDamage *= data.CritMultiplier > 0 ? data.CritMultiplier : 2f;

            int finalDamage = Mathf.RoundToInt(baseDamage);
            _health.ModifyHealth(-finalDamage);
            OnDamageApplied?.Invoke(hitBox, finalDamage);

            // --- KNOCKBACK ---
            if (data.KnockbackForce > 0f)
            {
                Vector2 toTarget = ((Vector2)(transform.position - hitBox.transform.position)).normalized;
                Vector2 overrideDir = data.KnockbackDirection.normalized;

                Vector2 dir = data.KnockbackMethod switch
                {
                    DamageProfile.KnockbackMethodEnum.TowardsTarget => toTarget,
                    DamageProfile.KnockbackMethodEnum.OverrideDirection => overrideDir,
                    DamageProfile.KnockbackMethodEnum.Combine =>
                        ((overrideDir * 0.7f) + (toTarget * 0.3f)).normalized,

                    _ => Vector2.zero
                };


                bool knockbackApplied = false;

                IMovementHandler movementHandler = GetComponentInParent<IMovementHandler>();
                if (movementHandler != null)
                {
                    movementHandler.ApplyForce(dir * data.KnockbackForce);
                    knockbackApplied = true;
                }
                else
                {
                    Rigidbody2D rb = GetComponentInParent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.AddForce(dir * data.KnockbackForce, ForceMode2D.Impulse);
                        knockbackApplied = true;
                    }
                }

                if (knockbackApplied)
                {
                    OnKnockbackApplied?.Invoke();
                }
            }

            SetInvulnerable(true);
            return true;
        }

        private void SetInvulnerable(bool state)
        {
            _isInvulnerable = state;
            if (state)
            {
                _invulnTimer = _invulnerabilityTime;
                OnBecameInvulnerable?.Invoke();
            }
            else
            {
                OnInvulnerabilityEnded?.Invoke();
            }
        }

#if UNITY_EDITOR
        [TitleGroup("Debug"), ShowInInspector, ReadOnly, PropertyOrder(100)]
        [ProgressBar(0, nameof(_invulnerabilityTime), ColorGetter = nameof(GetInvulnColor))]
        [LabelText("Invulnerability Timer")]
        private float InvulnerabilityProgress => _isInvulnerable ? _invulnTimer : 0f;

        private Color GetInvulnColor()
        {
            if (!_isInvulnerable)
                return new Color(0.5f, 0.5f, 0.5f);

            float pct = Mathf.Clamp01(_invulnTimer / _invulnerabilityTime);
            // Green when fresh, fades to red as it expires
            return Color.Lerp(Color.red, Color.green, pct);
        }
#endif
    }
}