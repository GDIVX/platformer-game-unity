using Runtime.Inventory;
using Runtime.Player.Movement;
using UnityEngine;
using Utilities;

namespace Runtime.Player
{
    public class PlayerContext : MonoSingleton<PlayerContext>
    {
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private InventoryController _inventory;

        public PlayerMovement PlayerMovement => _playerMovement;
        public InventoryController Inventory => _inventory;
    }
}