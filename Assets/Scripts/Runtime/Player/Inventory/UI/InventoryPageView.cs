using System.Collections.Generic;
using Runtime.Inventory;
using Runtime.Player.Inventory;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Inventory.UI
{
    public class InventoryPageView : MonoBehaviour
    {
        [SerializeField] private InventoryItem _inventoryItemPrefab;
        [SerializeField] private RectTransform _contentPanel;
        [SerializeField] private InventoryController _inventoryController;

        [ShowInInspector, ReadOnly] private List<InventoryItem> _inventoryItems = new();

        public void Initialize(int inventorySize)
        {
            for (int i = 0; i < inventorySize; i++)
            {
                var inventoryItemView = Instantiate(_inventoryItemPrefab, _contentPanel);
                _inventoryItems.Add(inventoryItemView);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SortInventory()
        {
            _inventoryController?.SortInventory();
        }
    }
}