namespace Runtime.Player.Inventory.Crafting
{
    using System.Collections.Generic;
    using System.Linq;

    namespace Runtime.Player.Crafting
    {
        public class CraftingService
        {
            private readonly InventoryController _inventory;
            private readonly List<CraftingRecipe> _allRecipes;

            public CraftingService(
                InventoryController inventory,
                IEnumerable<CraftingRecipe> recipes)
            {
                _inventory = inventory;
                _allRecipes = recipes.ToList();
            }

            /// <summary>
            /// Returns true if the player has everything needed for the recipe.
            /// </summary>
            public bool CanCraft(CraftingRecipe recipe)
            {
                return _inventory.CanMeetRequirements(recipe.Requirements);
            }

            /// <summary>
            /// Attempts to craft the recipe.
            /// </summary>
            public bool TryCraft(CraftingRecipe recipe)
            {
                if (!CanCraft(recipe))
                    return false;

                // spend materials
                if (!_inventory.TryRemoveRequirements(recipe.Requirements))
                    return false;

                // add output
                _inventory.TryAddItem(recipe.Output, recipe.OutputAmount);
                return true;
            }

            /// <summary>
            /// Returns all recipes. If showAvailableOnly = true,
            /// returns only those that can currently be crafted.
            /// </summary>
            public IEnumerable<CraftingRecipe> GetRecipes(bool showAvailableOnly)
            {
                return showAvailableOnly ? _allRecipes.Where(CanCraft) : _allRecipes;
            }

            /// <summary>
            /// Returns all requirements, flattened and compressed.
            /// Useful for UI display.
            /// </summary>
            public IEnumerable<InventoryController.ItemRequirement> GetCompressedRequirements(CraftingRecipe recipe)
            {
                return InventoryController.ItemRequirement.Compress(recipe.Requirements);
            }

            /// <summary>
            /// For UI: returns whether the player meets this specific requirement.
            /// </summary>
            public bool MeetsRequirement(InventoryController.ItemRequirement requirement)
            {
                if (requirement.Item == null || requirement.Amount <= 0)
                    return false;

                int available = _inventory.GetAvailableAmount(requirement.Item);
                return available >= requirement.Amount;
            }

            public void AddRecipe(CraftingRecipe recipe)
            {
                if (_allRecipes.Contains(recipe)) return;
                _allRecipes.Add(recipe);
            }

            public IEnumerable<RequirementViewModel> BuildRequirementModels(CraftingRecipe recipe)
            {
                var compressed = GetCompressedRequirements(recipe);

                foreach (var req in compressed)
                {
                    int available = _inventory.GetAvailableAmount(req.Item);
                    yield return new RequirementViewModel(req.Item, req.Amount, available);
                }
            }
        }
    }
}