using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runtime.Player.Inventory.UI
{
    public class InventorySlot : MonoBehaviour, IDropHandler
    {
        [SerializeField] private Image _image;

        [SerializeField] private Color _selectedColor, _unselectedColor;

        // [SerializeField] private float _colorChangeDuration = 0.2f;
        // [SerializeField] private Ease _easeType = Ease.OutCubic;
        [SerializeField] private InventoryLayer _inventoryLayer;
        [ShowInInspector, ReadOnly] private InventoryItem _inventoryItem;

        public InventoryItem InventoryItem
        {
            get
            {
                if (_inventoryItem != null) return _inventoryItem;

                _inventoryItem = GetComponentInChildren<InventoryItem>();
                return _inventoryItem;
            }
            internal set => _inventoryItem = value;
        }

        private void Awake()
        {
            Deselect();
        }

        public void OnDrop(PointerEventData eventData)
        {
            InventoryItem item = eventData.pointerDrag
                ? eventData.pointerDrag.GetComponent<InventoryItem>()
                : null;

            if (!item)
            {
                Debug.LogWarning("Dropped object is not a valid inventory item.");
                return;
            }

            if (!CanAcceptItem(item))
            {
                // EquipmentManager.HandleInvalidSlotAttempt(_inventoryLayer, item.Item);
                return;
            }

            if (transform.childCount != 0)
            {
                var otherSlot = item.InventorySlot;
                var currItem = transform.GetChild(0).GetComponent<InventoryItem>();

                //Swap the two items
                otherSlot.SetItem(currItem);
            }

            SetItem(item, true);
            EquipItem(item);
        }

        public void Select()
        {
            _image.color = _selectedColor;
        }

        public void Deselect()
        {
            _image.color = _unselectedColor;
        }

        public void TryEquipFromClick()
        {
            if (!InventoryItem)
            {
                return;
            }

            if (!CanAcceptItem(InventoryItem))
            {
                // EquipmentManager.HandleInvalidSlotAttempt(_inventoryLayer, InventoryItem.Item);
                return;
            }

            EquipItem(InventoryItem);
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
        }

        private bool CanAcceptItem(InventoryItem item)
        {
            return item && EquipmentManager.CanEquip(_inventoryLayer, item.Item.Layer);
        }

        private void EquipItem(InventoryItem item)
        {
            EquipmentManager.Equip(this, item);
        }
    }
}