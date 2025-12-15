using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Player.Inventory.Crafting
{
    [CreateAssetMenu(menuName = "Inventory/Crafting Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        public Item Output;
        public int OutputAmount = 1;
        public List<InventoryController.ItemRequirement> Requirements;
        public Sprite Icon => Output.Sprite;
        public string DisplayName => Output.DisplayName;
        [TextArea] public string Description;
    }
}