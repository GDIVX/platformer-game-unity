using UnityEngine;

namespace Runtime.Inventory
{
    [RequireComponent(typeof(Collider2D))]
    public class ItemCollector : MonoBehaviour
    {
        [SerializeField] private InventoryController _inventoryController;

        private void Awake()
        {
            if (_inventoryController == null)
            {
                _inventoryController = GetComponentInParent<InventoryController>();
            }

            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_inventoryController)
            {
                return;
            }

            if (!other.TryGetComponent<ItemDrop>(out var itemDrop))
            {
                return;
            }

            itemDrop.BeginCollection(_inventoryController);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent<ItemDrop>(out var itemDrop))
            {
                return;
            }

            itemDrop.StopCollectRoutine();
        }
    }
}
