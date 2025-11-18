using UnityEngine;

namespace Runtime.Player.Inventory
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

            var component = GetComponent<Collider2D>();
            if (component != null)
            {
                component.isTrigger = true;
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
