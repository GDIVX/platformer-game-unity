using System;
using System.Collections.Generic;
using System.Linq;
using Runtime.Inventory.UI;
using Runtime.Player.Inventory.UI;
using UnityEngine;

namespace Runtime.Player.Inventory
{
    public interface IInventorySorter
    {
        void Sort(IList<InventorySlot> slots);
    }

    [Serializable]
    public class DefaultInventorySorter : IInventorySorter
    {
        private const int BagStartIndex = 11;

        public void Sort(IList<InventorySlot> slots)
        {
            if (slots == null || slots.Count <= BagStartIndex)
            {
                return;
            }

            var bagSlots = slots.Skip(BagStartIndex).ToList();
            if (bagSlots.Count == 0)
            {
                return;
            }

            var stackTotals = new Dictionary<string, (Item item, int amount)>(StringComparer.OrdinalIgnoreCase);
            foreach (var slot in bagSlots)
            {
                var slotItem = slot.InventoryItem;
                if (slotItem == null || slotItem.Item == null)
                {
                    continue;
                }

                var itemName = slotItem.Item.ItemName;
                if (stackTotals.TryGetValue(itemName, out var existing))
                {
                    existing.amount += slotItem.Amount;
                    stackTotals[itemName] = existing;
                    continue;
                }

                stackTotals[itemName] = (slotItem.Item, slotItem.Amount);
            }

            var orderedStacks = stackTotals
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var reusableItems = new Queue<InventoryItem>(bagSlots
                .Where(slot => slot.InventoryItem != null)
                .Select(slot => slot.InventoryItem));

            var nextSlotIndex = 0;

            foreach (var entry in orderedStacks)
            {
                var (item, totalAmount) = entry.Value;
                var remaining = totalAmount;

                while (remaining > 0 && nextSlotIndex < bagSlots.Count)
                {
                    var stackAmount = Mathf.Min(remaining, item.MaxStack);
                    var targetSlot = bagSlots[nextSlotIndex++];

                    var inventoryItem = reusableItems.Count > 0 ? reusableItems.Dequeue() : null;
                    if (inventoryItem == null)
                    {
                        break;
                    }

                    inventoryItem.SetItem(item, stackAmount);
                    targetSlot.SetItem(inventoryItem);

                    remaining -= stackAmount;
                }
            }

            while (reusableItems.Count > 0)
            {
                reusableItems.Dequeue().DestroySelf();
            }

            for (; nextSlotIndex < bagSlots.Count; nextSlotIndex++)
            {
                var slot = bagSlots[nextSlotIndex];
                if (slot.InventoryItem == null)
                {
                    continue;
                }

                slot.InventoryItem.DestroySelf();
            }
        }
    }
}
