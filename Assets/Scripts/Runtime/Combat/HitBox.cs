using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class HitBox : MonoBehaviour
    {
        public bool IsActive = true;

        [Header("Damage Settings")] [SerializeField]
        private DamageProfile _damageProfile;

        [SerializeField] private LayerMask _targetMask = ~0;

        [Header("Ownership")] [SerializeField] private GameObject _owner;

        [Header("Events")] public UnityEvent<HurtBox, bool> OnHit;
        public UnityEvent OnActivated;
        public UnityEvent OnDeactivated;

        private Collider2D _collider;


        // --- Crit tracking (kept identical) ---
        private float _critMomentum;
        [SerializeField] private float _biasGainOnCrit = 0.25f;
        [SerializeField] private float _biasLossOnMiss = 0.15f;

        // ──────────────────────────────────────────────
        //  LIFECYCLE
        // ──────────────────────────────────────────────
        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
        }


        // ──────────────────────────────────────────────
        //  CORE COLLISION
        // ──────────────────────────────────────────────
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsActive) return;
            if (_owner != null && other.gameObject == _owner) return;
            if ((_targetMask.value & (1 << other.gameObject.layer)) == 0) return;
            if (!other.TryGetComponent(out HurtBox hurtBox)) return;

            bool isCrit = RollCrit(_damageProfile);

            if (hurtBox.ApplyHit(this, isCrit))
            {
                OnHit?.Invoke(hurtBox, isCrit);
            }
        }

        private bool RollCrit(DamageProfile profile)
        {
            float chance = profile.CritChance + _critMomentum;
            if (profile.CritChanceCurve != null && profile.CritChanceCurve.length > 0)
                chance = Mathf.Clamp01(profile.CritChanceCurve.Evaluate(chance));

            bool isCrit = Random.value <= Mathf.Clamp01(chance);
            _critMomentum = Mathf.Clamp(
                _critMomentum + (isCrit ? _biasGainOnCrit : -_biasLossOnMiss),
                -1f, 1f
            );
            return isCrit;
        }

        // ──────────────────────────────────────────────
        //  PUBLIC API 
        // ──────────────────────────────────────────────
        public DamageProfile Damage => _damageProfile;
        public void SetDamage(DamageProfile newProfile) => _damageProfile = newProfile;
        public void SetOwner(GameObject newOwner) => _owner = newOwner;
    }
}