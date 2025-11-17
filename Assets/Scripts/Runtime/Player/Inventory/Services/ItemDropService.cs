using UnityEngine;

namespace Runtime.Player.Inventory.Services
{
    internal class ItemDropService
    {
        private readonly GameObject _itemDropPrefab;
        private readonly Transform _playerTransform;

        public ItemDropService(GameObject itemDropPrefab, Transform playerTransform)
        {
            _itemDropPrefab = itemDropPrefab;
            _playerTransform = playerTransform;
        }

        public void DropItem(Item item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return;
            }

            if (!_itemDropPrefab)
            {
                Debug.LogWarning("Item drop prefab is not assigned");
                return;
            }

            if (_playerTransform == null)
            {
                Debug.LogWarning("Player transform is not assigned; cannot determine drop position.");
                return;
            }

            var position = (Vector3)(Random.insideUnitCircle * 5f) + _playerTransform.position;
            var dropObject = Object.Instantiate(_itemDropPrefab, position, Quaternion.identity);

            if (!dropObject.TryGetComponent<ItemDrop>(out var itemDrop))
            {
                itemDrop = dropObject.AddComponent<ItemDrop>();
            }

            itemDrop.Initialize(item, amount);
        }
    }
}
