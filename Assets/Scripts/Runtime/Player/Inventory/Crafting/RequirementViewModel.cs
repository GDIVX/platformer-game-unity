namespace Runtime.Player.Inventory.Crafting
{
    public struct RequirementViewModel
    {
        public Item Item;
        public int Required;
        public int Available;
        public bool IsMet;

        public RequirementViewModel(
            Item item,
            int required,
            int available)
        {
            Item = item;
            Required = required;
            Available = available;
            IsMet = available >= required;
        }
    }
}