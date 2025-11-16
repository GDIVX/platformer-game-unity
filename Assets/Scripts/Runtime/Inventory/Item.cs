using System.Collections.Generic;
using Runtime.Inventory.UI;
using Sirenix.Utilities;
using UnityEngine;

namespace Runtime.Inventory
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