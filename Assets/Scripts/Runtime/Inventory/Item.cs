using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Inventory
{
    [CreateAssetMenu(fileName = "Items / Basic ")]
    public class Item : ScriptableObject
    {
        public Sprite Sprite;
        public int MaxStack = 100;

        public List<ItemAction> Actions;
    }
}