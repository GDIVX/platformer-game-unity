using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class HitBox : MonoBehaviour
    {
        [Header("Damage Settings")]
        [SerializeField] private DamageProfile _damageProfile;

        [Tooltip("Layers that can be damaged by this HitBox.")]
        [SerializeField] private LayerMask _targetMask = ~0;

        [SerializeField] private bool _canHitMultipleTargets = false;
        [SerializeField] private bool _deactivateAfterHit = false;

        [Header("Ownership")]
        [SerializeField] private GameObject _owner;

        [Header("Events")]
        public UnityEvent<HurtBox, bool> OnHit; // bool = isCrit
        public UnityEvent OnActivated;
        public UnityEvent OnDeactivated;

        private bool _hasHit;
        private Collider2D _collider;

        // --- Crit tracking ---
        private float _critMomentum;
        [SerializeField] private float _biasGainOnCrit = 0.25f;
        [SerializeField] private float _biasLossOnMiss = 0.15f;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
        }

        private void OnEnable()
        {
            _hasHit = false;
            OnActivated?.Invoke();
        }

        private void OnDisable()
        {
            OnDeactivated?.Invoke();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasHit && !_canHitMultipleTargets) return;
            if (_owner != null && other.gameObject == _owner) return;

            // Layer filtering
            if ((_targetMask.value & (1 << other.gameObject.layer)) == 0)
                return;

            if (!other.TryGetComponent(out HurtBox hurtBox))
                return;

            // --- Roll crit ---
            bool isCrit = RollCrit(_damageProfile);

            // --- Apply damage ---
            if (hurtBox.ApplyHit(this, isCrit))
            {
                OnHit?.Invoke(hurtBox, isCrit);

                if (_deactivateAfterHit)
                    gameObject.SetActive(false);

                if (!_canHitMultipleTargets)
                    _hasHit = true;
            }
        }

        private bool RollCrit(DamageProfile profile)
        {
            float chance = profile.CritChance + _critMomentum;

            if (profile.CritChanceCurve != null && profile.CritChanceCurve.length > 0)
                chance = Mathf.Clamp01(profile.CritChanceCurve.Evaluate(chance));

            bool isCrit = Random.value <= Mathf.Clamp01(chance);

            // Adjust momentum depending on outcome
            _critMomentum = Mathf.Clamp(
                _critMomentum + (isCrit ? _biasGainOnCrit : -_biasLossOnMiss),
                -1f, 1f
            );

            return isCrit;
        }

        public DamageProfile Damage => _damageProfile;
        public void SetDamage(DamageProfile newProfile) => _damageProfile = newProfile;
        public void SetOwner(GameObject newOwner) => _owner = newOwner;
    }
}
