using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Runtime.Inventory.UI
{
    public class InventorySlotView : MonoBehaviour, IDropHandler
    {
        public event Action<InventoryItemView> OnItemDropped;

        public void OnDrop(PointerEventData eventData)
        {
            InventoryItemView itemView = eventData.pointerDrag.GetComponent<InventoryItemView>();
            if (transform.childCount != 0)
            {
                var otherSlot = itemView.InventorySlot;
                var currItem = transform.GetChild(0).GetComponent<InventoryItemView>();

                //Swap the two items
                otherSlot.SetItem(currItem);
            }

            SetItem(itemView, true);
        }

        private void SetItem(InventoryItemView itemView, bool setParentAfterDrag = false)
        {
            if (setParentAfterDrag)
            {
                itemView.ParentAfterDrag = transform;
            }
            else
            {
                itemView.transform.SetParent(transform);
            }

            itemView.InventorySlot = this;
            OnItemDropped?.Invoke(itemView);
        }
    }
}