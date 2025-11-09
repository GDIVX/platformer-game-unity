using UnityEngine;

namespace Runtime.Combat.UI
{
    /// <summary>
    /// Generic interface for displaying health values in the UI.
    /// </summary>
    public interface IHealthDisplay
    {
        void SetHealth(int currentHealth, int maxHealth);
        void SetAliveState(bool isAlive);
    }
}