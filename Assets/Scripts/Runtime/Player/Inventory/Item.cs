using System.Collections.Generic;
using Runtime.Inventory;
using Runtime.Inventory.UI;
using Runtime.Player.Inventory.UI;
using Sirenix.Utilities;
using UnityEngine;

namespace Runtime.Player.Inventory
{
    [CreateAssetMenu(fileName = "Items ")]
    public class Item : ScriptableObject
    {
        public string ItemName;
        public Sprite Sprite;
        public int MaxStack = 100;
        public InventoryLayer Layer = InventoryLayer.Everything;

        public List<ItemAction> Actions;

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (ItemName.IsNullOrWhitespace())
            {
                ItemName = name;
            }
        }
#endif
    }
}