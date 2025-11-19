using System;
using System.Collections.Generic;
using Runtime.Player.Inventory.UI;
using UnityEngine;

namespace Runtime.Player.Inventory.Services
{
    internal class InventoryItemService
    {
        private readonly List<InventorySlot> _slots;
        private readonly InventoryItem _inventoryItemPrefab;
        private readonly ItemDropService _itemDropService;

        public InventoryItemService(List<InventorySlot> slots, InventoryItem inventoryItemPrefab,
            ItemDropService itemDropService)
        {
            _slots = slots;
            _inventoryItemPrefab = inventoryItemPrefab;
            _itemDropService = itemDropService;
        }

        public bool TryAddItem(Item item, int amount)
        {
            if (!item || amount <= 0)
            {
                return false;
            }

            if (_slots == null || _slots.Count == 0)
            {
                _itemDropService.DropItem(item, amount);
                return false;
            }

            var remaining = amount;

            foreach (var slot in _slots)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var slotItem = slot.InventoryItem;
                if (!slotItem)
                {
                    continue;
                }

                if (!string.Equals(slotItem.Item.ItemName, item.ItemName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (slotItem.IsFull)
                {
                    continue;
                }

                var maxStack = slotItem.Item.MaxStack;
                var space = Mathf.Max(0, maxStack - slotItem.Amount);
                if (space <= 0)
                {
                    continue;
                }

                var toAdd = Mathf.Min(space, remaining);
                slotItem.AddAmount(toAdd);
                remaining -= toAdd;
            }

            foreach (var slot in _slots)
            {
                if (remaining <= 0)
                {
                    break;
                }

                if (slot.InventoryItem)
                {
                    continue;
                }

                var toCreate = Mathf.Min(remaining, item.MaxStack);
                CreateNewItem(item, slot, toCreate);
                remaining -= toCreate;
            }

            if (remaining <= 0) return true;
            _itemDropService.DropItem(item, remaining);
            return false;
        }

        public InventoryController.ItemRemovalOutcome RemoveItemAt(int slotIndex, int amount)
        {
            if (_slots == null || slotIndex < 0 || slotIndex >= _slots.Count)
            {
                return InventoryController.ItemRemovalOutcome.NoItemToRemove;
            }

            var slot = _slots[slotIndex];
            var itemInSlot = slot.InventoryItem;

            if (!itemInSlot)
            {
                return InventoryController.ItemRemovalOutcome.NoItemToRemove;
            }

            var newAmount = Mathf.Clamp(itemInSlot.Amount - amount, 0, itemInSlot.Item.MaxStack);
            if (newAmount <= 0)
            {
                itemInSlot.DestroySelf();
                return InventoryController.ItemRemovalOutcome.ItemDestroyed;
            }

            itemInSlot.SetAmount(newAmount);
            return InventoryController.ItemRemovalOutcome.ChangedStackAmount;
        }

        public void RemoveItemStacks(string itemName, int amount)
        {
            if (_slots == null || string.IsNullOrWhiteSpace(itemName) || amount <= 0)
            {
                return;
            }

            var remaining = amount;

            foreach (var slot in _slots)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var slotItem = slot.InventoryItem;
                if (!slotItem ||
                    !string.Equals(slotItem.Item.ItemName, itemName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var amountToRemove = Mathf.Min(remaining, slotItem.Amount);
                var newAmount = slotItem.Amount - amountToRemove;
                remaining -= amountToRemove;

                if (newAmount <= 0)
                {
                    slotItem.DestroySelf();
                }
                else
                {
                    slotItem.SetAmount(newAmount);
                }
            }
        }

        private void CreateNewItem(Item item, InventorySlot slot, int amount)
        {
            if (!item)
            {
                Debug.LogWarning("Item cannot be null");
                return;
            }

            if (!_inventoryItemPrefab)
            {
                Debug.LogError("Inventory item prefab is not assigned");
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning("Amount of items cannot be less or equal to zero. Setting to 1");
                amount = 1;
            }

            var newItem = UnityEngine.Object.Instantiate(_inventoryItemPrefab);
            newItem.SetItem(item, amount);
            slot.SetItem(newItem);
        }
    }
}