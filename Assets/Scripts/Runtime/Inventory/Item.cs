using System.Collections.Generic;
using Runtime.Inventory.UI;
using UnityEngine;

namespace Runtime.Inventory
{
    [CreateAssetMenu(fileName = "Items ")]
    public class Item : ScriptableObject
    {
        public Sprite Sprite;
        public int MaxStack = 100;
        public InventoryLayer Layer = InventoryLayer.Everything;

        public List<ItemAction> Actions;
    }
}