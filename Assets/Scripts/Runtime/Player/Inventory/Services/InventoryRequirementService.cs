using System;
using System.Collections.Generic;
using System.Linq;
using Runtime.Player.Inventory.UI;

namespace Runtime.Player.Inventory.Services
{
    internal class InventoryRequirementService
    {
        private readonly List<InventorySlot> _slots;
        private readonly InventoryItemService _inventoryItemService;

        public InventoryRequirementService(List<InventorySlot> slots, InventoryItemService inventoryItemService)
        {
            _slots = slots;
            _inventoryItemService = inventoryItemService;
        }

        public int GetAvailableAmount(Item item)
        {
            return item == null ? 0 : GetAvailableAmount(item.DisplayName);
        }

        public int GetAvailableAmount(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName) || _slots == null)
            {
                return 0;
            }

            return (from slot in _slots
                select slot.InventoryItem
                into slotItem
                where slotItem
                where string.Equals(slotItem.Item.DisplayName, itemName, StringComparison.OrdinalIgnoreCase)
                select slotItem.Amount).Sum();
        }

        public bool CanMeetRequirements(IEnumerable<InventoryController.ItemRequirement> requirements)
        {
            if (requirements == null)
            {
                return true;
            }

            return requirements.Where(requirement => requirement.Amount > 0 && requirement.Item).All(requirement =>
                GetAvailableAmount(requirement.Item) >= requirement.Amount);
        }

        public bool TryRemoveRequirements(IEnumerable<InventoryController.ItemRequirement> requirements)
        {
            if (requirements == null)
            {
                return true;
            }

            var compressed = InventoryController.ItemRequirement.Compress(requirements).ToArray();
            if (!CanMeetRequirements(compressed))
            {
                return false;
            }

            foreach (var requirement in compressed)
            {
                if (requirement.Amount <= 0 || requirement.Item == null)
                {
                    continue;
                }

                _inventoryItemService.RemoveItemStacks(requirement.Item.DisplayName, requirement.Amount);
            }

            return true;
        }
    }
}
