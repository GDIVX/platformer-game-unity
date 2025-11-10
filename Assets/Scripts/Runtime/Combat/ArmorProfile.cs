using UnityEngine;

namespace Runtime.Combat
{
    public enum DamageRelation
    {
        Normal,
        Resistant,
        Immune,
        Vulnerable
    }

    [CreateAssetMenu(menuName = "Combat/Armor Profile", fileName = "ArmorProfile")]
    public class ArmorProfile : ScriptableObject
    {
        [Header("Damage Relations")]
        public DamageRelation Raw = DamageRelation.Normal;
        public DamageRelation Sharp = DamageRelation.Normal;
        public DamageRelation Blunt = DamageRelation.Normal;
        public DamageRelation Ballistic = DamageRelation.Normal;
        public DamageRelation Fire = DamageRelation.Normal;
        public DamageRelation Energy = DamageRelation.Normal;

        private float ApplyRelation(DamageRelation relation, float baseDamage)
        {
            return relation switch
            {
                DamageRelation.Normal => baseDamage,
                DamageRelation.Resistant => baseDamage * 0.75f,
                DamageRelation.Vulnerable => baseDamage * 2f,
                DamageRelation.Immune => 0f,
                _ => baseDamage
            };
        }

        public float CalculateEffectiveDamage(DamageProfile damage)
        {
            float sum = 0f;
            sum += ApplyRelation(Raw, damage.Raw);
            sum += ApplyRelation(Sharp, damage.Sharp);
            sum += ApplyRelation(Blunt, damage.Blunt);
            sum += ApplyRelation(Ballistic, damage.Ballistic);
            sum += ApplyRelation(Fire, damage.Fire);
            sum += ApplyRelation(Energy, damage.Energy);
            return sum;
        }
    }
}