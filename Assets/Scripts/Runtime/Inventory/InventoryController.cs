using Runtime.Inventory.UI;
using Runtime.Player;
using UnityEngine;
using System;
using UnityEngine.InputSystem;

namespace Runtime.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private InventoryPageView _inventoryPageView;

        private static PlayerInput _playerInput;

        private static InputAction _inventoryOpenInput;
        private static InputAction _inventoryCloseInput;

        private void Start()
        {
            _playerInput = InputManager.PlayerInput;

            _inventoryOpenInput = _playerInput.actions["Open Inventory"];
            _inventoryCloseInput = _playerInput.actions["Close Inventory"];

            _inventoryOpenInput.performed += (_) => OpenInventory();
            _inventoryCloseInput.performed += (_) => CloseInventory();
        }


        private void OpenInventory()
        {
            _inventoryPageView.Show();
            _playerInput.SwitchCurrentActionMap("Inventory");
            
        }
        
        private void CloseInventory()
        {
            _inventoryPageView.Hide();
            _playerInput.SwitchCurrentActionMap("Player");
        }
    }
}