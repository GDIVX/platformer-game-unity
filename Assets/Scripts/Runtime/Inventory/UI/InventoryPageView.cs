using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Inventory.UI
{
    public class InventoryPageView : MonoBehaviour
    {
        [SerializeField] private InventoryItemView _inventoryItemPrefab;
        [SerializeField] private RectTransform _contentPanel;

        [ShowInInspector, ReadOnly] private List<InventoryItemView> _inventoryItems = new();

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
    }
}