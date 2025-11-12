using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Combat
{
    /// <summary>
    /// Pure data definition for an attack’s damage breakdown and crit parameters.
    /// </summary>
    [CreateAssetMenu(menuName = "Combat/Damage Profile")]
    public class DamageProfile : ScriptableObject
    {
        [Header("Damage Composition")]
        public float Raw;
        public float Sharp;
        public float Blunt;
        public float Ballistic;
        public float Fire;
        public float Energy;

        [BoxGroup("Impact")]
        public float KnockbackForce;

        [BoxGroup("Impact")]
        public KnockbackMethodEnum KnockbackMethod;

        [BoxGroup("Impact"), ShowIf("@KnockbackMethod != KnockbackMethodEnum.TowardsTarget")]
        public Vector2 KnockbackDirection;

        [Header("Critical")]
        [Range(0f, 1f)] public float CritChance;
        public AnimationCurve CritChanceCurve;
        public float CritMultiplier; // e.g., 1.5×

        public float TotalBaseDamage =>
            Raw + Sharp + Blunt + Ballistic + Fire + Energy;

        public enum KnockbackMethodEnum
        {
            TowardsTarget,
            OverrideDirection,
            Combine
        }
    }
}