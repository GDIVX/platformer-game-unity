using UnityEngine;

namespace Runtime.Combat
{
    /// <summary>
    /// Pure data definition for an attack’s damage breakdown and crit parameters.
    /// </summary>
    [System.Serializable]
    public struct DamageProfile
    {
        [Header("Damage Composition")]
        public float Raw;
        public float Sharp;
        public float Blunt;
        public float Ballistic;
        public float Fire;
        public float Energy;

        [Header("Impact")]
        public float KnockbackForce; 

        [Header("Critical")]
        [Range(0f, 1f)] public float CritChance;
        public AnimationCurve CritChanceCurve;
        public float CritMultiplier; // e.g., 1.5×

        public float TotalBaseDamage =>
            Raw + Sharp + Blunt + Ballistic + Fire + Energy;
    }
}