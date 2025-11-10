using UnityEngine;
using UnityEngine.Events;
using Runtime.Movement;

namespace Runtime.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class HurtBox : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnitHealth _health;
        [SerializeField] private ArmorProfile _armorProfile;

        [Header("Invulnerability")]
        [SerializeField] private float _invulnerabilityTime = 0.5f;

        [Header("Events")]
        public UnityEvent<HitBox> OnHitReceived;
        public UnityEvent<HitBox, int> OnDamageApplied;
        public UnityEvent OnBecameInvulnerable;
        public UnityEvent OnInvulnerabilityEnded;
        public UnityEvent OnKnockbackApplied;

        private bool _isInvulnerable;
        private float _invulnTimer;

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
                Vector2 dir = ((Vector2)(transform.position - hitBox.transform.position)).normalized;
                bool knockbackApplied = false;

                IMovementHandler movementHandler = GetComponentInParent<IMovementHandler>();
                if (movementHandler != null)
                {
                    movementHandler.AddVelocity(dir * data.KnockbackForce);
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
    }
}
