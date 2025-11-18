using Runtime.Player.Inventory;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.World
{
    public class ResourceNode : MonoBehaviour
    {
        [Header("Drops")] [SerializeField] private Item _item;
        [SerializeField] private ItemDrop _dropPrefab;

        [SerializeField] private int _minDropAmount = 1;
        [SerializeField] private int _maxDropAmount = 3;
        [SerializeField] private AnimationCurve _dropRate = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Scatter")] [SerializeField] private float _scatterRadius = 1f;

        [Header("Pickup Delay")] [SerializeField]
        private float _pickupDelay = 0.35f;

        [Button]
        public void Drop()
        {
            float sample = _dropRate.Evaluate(Random.value);
            int dropCount = Mathf.RoundToInt(Mathf.Lerp(_minDropAmount, _maxDropAmount, sample));

            for (int i = 0; i < dropCount; i++)
                SpawnDrop();
        }

        private void SpawnDrop()
        {
            Vector2 offset = Random.insideUnitCircle * _scatterRadius;
            Vector3 pos = transform.position + (Vector3)offset;

            ItemDrop drop = Instantiate(_dropPrefab, pos, Quaternion.identity);

            // Initialize pickup content
            drop.Initialize(_item, 1);

            drop.SetPickupDelay(_pickupDelay);
        }
    }
}