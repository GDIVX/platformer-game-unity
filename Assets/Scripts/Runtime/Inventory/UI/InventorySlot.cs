using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Runtime.Inventory.UI
{
    public class InventorySlot : MonoBehaviour, IDropHandler
    {
        [SerializeField] private InventoryLayer _inventoryLayer;
        [ShowInInspector, ReadOnly] private InventoryItem _inventoryItem;
        public event Action<InventoryItem> OnSetItem;
        public event Action<InventoryItem> OnRemoveItem;

        public InventoryItem InventoryItem
        {
            get
            {
                if (_inventoryItem != null) return _inventoryItem;

                _inventoryItem = GetComponentInChildren<InventoryItem>();
                return _inventoryItem;
            }
            private set => _inventoryItem = value;
        }

        public void OnDrop(PointerEventData eventData)
        {
            InventoryItem item = eventData.pointerDrag.GetComponent<InventoryItem>();
            if (transform.childCount != 0)
            {
                var otherSlot = item.InventorySlot;
                var currItem = transform.GetChild(0).GetComponent<InventoryItem>();

                //Swap the two items
                otherSlot.SetItem(currItem);
            }

            SetItem(item, true);
        }

        public void RemoveItem()
        {
            if (_inventoryItem == null) return;
        }

        public void SetItem(InventoryItem item, bool setParentAfterDrag = false)
        {
            if (item.InventorySlot != this && item.InventorySlot != null)
            {
                var otherSlot = item.InventorySlot;
                otherSlot._inventoryItem = null;
            }

            if (setParentAfterDrag)
            {
                item.ParentAfterDrag = transform;
            }
            else
            {
                item.transform.SetParent(transform);
            }

            item.InventorySlot = this;
            _inventoryItem = item;

            OnSetItem?.Invoke(item);
        }
    }
}