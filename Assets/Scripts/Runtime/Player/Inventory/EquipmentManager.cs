using Runtime.Player.Inventory.UI;
using UnityEngine;

namespace Runtime.Player.Inventory
{
    public static class EquipmentManager
    {
        public static bool CanEquip(InventoryLayer slotLayer, InventoryLayer itemLayer)
        {
            return slotLayer == InventoryLayer.Everything ||
                   itemLayer == InventoryLayer.Everything ||
                   slotLayer == itemLayer;
        }

        // public static void HandleInvalidSlotAttempt(InventoryLayer slotLayer, Item item)
        // {
        //     // Debug.LogWarning($"Cannot equip {item.ItemName} ({item.Layer}) into {slotLayer} slot.");
        // }

        public static void Equip(InventorySlot slot, InventoryItem item)
        {
            // Debug.Log($"Equipping {item.Item.ItemName} to {slot.name}.");
            // // Future stat modifications can be applied here once equipment effects are defined.
        }
    }
}