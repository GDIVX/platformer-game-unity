using UnityEngine;

namespace Runtime.Player.Inventory
{
    /// <summary>
    /// Abstract base class for item interactions such as equipping the item or consuming it
    /// </summary>
    public abstract class ItemAction : ScriptableObject
    {
        public string ActionName;
        public abstract void Execute();
    }
}