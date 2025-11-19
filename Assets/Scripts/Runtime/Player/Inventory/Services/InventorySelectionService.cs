using System.Collections.Generic;
using Runtime.Player.Inventory.UI;

namespace Runtime.Player.Inventory.Services
{
    internal class InventorySelectionService
    {
        private readonly List<InventorySlot> _slots;

        public int SelectedSlotIndex { get; private set; } = -1;

        public InventorySelectionService(List<InventorySlot> slots)
        {
            _slots = slots;
        }

        public void SelectSlot(int slotIndex)
        {
            if (_slots == null || _slots.Count == 0)
            {
                SelectedSlotIndex = -1;
                return;
            }

            slotIndex = UnityEngine.Mathf.Clamp(slotIndex, 0, _slots.Count - 1);

            if (SelectedSlotIndex >= 0 && SelectedSlotIndex < _slots.Count)
            {
                _slots[SelectedSlotIndex]?.Deselect();
            }

            _slots[slotIndex]?.Select();
            SelectedSlotIndex = slotIndex;
        }

        public Item GetCurrentlySelectedItem()
        {
            if (_slots == null || SelectedSlotIndex < 0 || SelectedSlotIndex >= _slots.Count)
            {
                return null;
            }

            var slot = _slots[SelectedSlotIndex];
            var itemInSlot = slot.InventoryItem;
            return itemInSlot ? itemInSlot.Item : null;
        }
    }
}
