using UnityEngine;

namespace Runtime.Combat
{
    /// <summary>
    /// Represents a collider that can receive damage from a HitBox.
    /// Manages invulnerability frames and forwards valid damage to the Health component.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class HurtBox : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The Health component that this HurtBox will affect.")]
        [SerializeField] private UnitHealth health;

        [Header("Invulnerability")]
        [Tooltip("How long this hurt box remains invulnerable after taking a hit.")]
        [SerializeField] private float invulnerabilityTime = 0.5f;

        private bool _isInvulnerable;
        private float _invulnTimer;

        private void Awake()
        {
            if (health == null)
                health = GetComponentInParent<UnitHealth>();

            // Make sure collider is a trigger
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            if (!_isInvulnerable) return;
            _invulnTimer -= Time.deltaTime;
            if (_invulnTimer <= 0f)
                SetInvulnerable(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!enabled || _isInvulnerable) return;

            var hitBox = other.GetComponent<HitBox>();
            if (hitBox == null) return;

            // Layer filtering: make sure it’s a valid matchup (Player vs Enemy)
            if (!IsValidHit(hitBox.gameObject.layer))
                return;

            HandleDamage(hitBox);
        }

        /// <summary>
        /// Apply damage from a HitBox, with invulnerability window.
        /// </summary>
        private void HandleDamage(HitBox hitBox)
        {
            if (health == null) return;

            health.ModifyHealth(-hitBox.Damage);
            SetInvulnerable(true);
        }

        /// <summary>
        /// Activates or deactivates invulnerability state.
        /// </summary>
        private void SetInvulnerable(bool state)
        {
            _isInvulnerable = state;
            if (state)
                _invulnTimer = invulnerabilityTime;
        }

        /// <summary>
        /// Layer-based filtering: separates PlayerHurt vs EnemyHurt.
        /// </summary>
        private bool IsValidHit(int hitLayer)
        {
            // Example convention:
            // "PlayerHurt" can only be hit by "EnemyHit"
            // "EnemyHurt" can only be hit by "PlayerHit"
            var thisLayer = LayerMask.LayerToName(gameObject.layer);
            var otherLayer = LayerMask.LayerToName(hitLayer);

            if (thisLayer.Contains("Player") && otherLayer.Contains("Enemy"))
                return true;

            return thisLayer.Contains("Enemy") && otherLayer.Contains("Player");
        }
    }
}
