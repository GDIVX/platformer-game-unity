using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Player.Inventory.Services
{
    internal class InventoryUiService
    {
        private readonly GameObject _inventoryGroup;
        private readonly PlayerInput _playerInput;
        private readonly bool _freezeTimeOnInventoryOpen;

        public InventoryUiService(GameObject inventoryGroup, PlayerInput playerInput, bool freezeTimeOnInventoryOpen)
        {
            _inventoryGroup = inventoryGroup;
            _playerInput = playerInput;
            _freezeTimeOnInventoryOpen = freezeTimeOnInventoryOpen;
        }

        public void OpenInventory()
        {
            if (_inventoryGroup != null)
            {
                _inventoryGroup.SetActive(true);
            }

            if (_playerInput != null)
            {
                _playerInput.SwitchCurrentActionMap("Inventory");
            }

            if (_freezeTimeOnInventoryOpen)
            {
                Time.timeScale = 0f;
            }
        }

        public void CloseInventory()
        {
            if (_inventoryGroup != null)
            {
                _inventoryGroup.SetActive(false);
            }

            if (_playerInput != null)
            {
                _playerInput.SwitchCurrentActionMap("Player");
            }
        }
    }
}
