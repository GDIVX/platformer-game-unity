using UnityEngine;

namespace Runtime.Combat
{
    /// <summary>
    /// Represents an attack area that can deal damage to valid HurtBoxes.
    /// Should be attached to a GameObject with a Trigger Collider2D.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class HitBox : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("The amount of damage this hitbox will deal.")]
        [SerializeField] private int damage = 10;

        [Tooltip("If true, this hitbox will deactivate itself after a successful hit.")]
        [SerializeField] private bool deactivateAfterHit = false;

        [Tooltip("If true, the hitbox can hit multiple targets before deactivation.")]
        [SerializeField] private bool canHitMultipleTargets = false;

        [Header("Ownership")]
        [Tooltip("Optional reference to the GameObject that owns this hitbox (used to prevent self-hits).")]
        [SerializeField] private GameObject owner;

        private bool _hasHit;

        /// <summary>
        /// The damage value this hitbox inflicts.
        /// </summary>
        public int Damage => damage;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnEnable()
        {
            _hasHit = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasHit && !canHitMultipleTargets) return;

            // Don’t hit our own colliders
            if (owner != null && other.gameObject == owner)
                return;

            var hurtBox = other.GetComponent<HurtBox>();
            if (hurtBox == null) return;

            // Validate layer matchups (handled by HurtBox’s IsValidHit internally)
            hurtBox.SendMessage("HandleDamage", this, SendMessageOptions.DontRequireReceiver);

            if (deactivateAfterHit)
                gameObject.SetActive(false);

            if (!canHitMultipleTargets)
                _hasHit = true;
        }

        /// <summary>
        /// Assigns who owns this hitbox (e.g., player, enemy).
        /// Used to avoid self-hits.
        /// </summary>
        /// <param name="newOwner">The GameObject that spawned or triggered this hitbox.</param>
        public void SetOwner(GameObject newOwner)
        {
            owner = newOwner;
        }

        /// <summary>
        /// Allows setting damage dynamically (e.g., for scaling attacks or combo hits).
        /// </summary>
        public void SetDamage(int newDamage)
        {
            damage = newDamage;
        }
    }
}
