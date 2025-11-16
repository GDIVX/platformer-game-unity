using System;
using Runtime.Inventory.UI;
using Runtime.Player;
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;

namespace Runtime.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField, FoldoutGroup("UI")] private GameObject _inventoryGroup;
        [SerializeField, FoldoutGroup("UI")] private InventoryItem _inventoryItemPrefab;
        [SerializeField, FoldoutGroup("UI")] private bool _freezeTimeOnInventoryOpen;


        [SerializeField, FoldoutGroup("Content")]
        private List<InventorySlot> _slots;

        [SerializeField, FoldoutGroup("Drop")] private GameObject _itemDropPrefab;

        [ShowInInspector] private int _selectedSlotIndex = -1;

        private static PlayerInput _playerInput;
        private static InputAction _inventoryOpenInput;
        private static InputAction _inventoryCloseInput;
        private InputAction _hotbarInput;
        private InputAction _moveInput;

        private bool _inventoryOpen;


        #region Unity Events

        private void Start()
        {
            _playerInput = InputManager.PlayerInput;

            _inventoryOpenInput = _playerInput.actions["Open Inventory"];
            _inventoryCloseInput = _playerInput.actions["Close Inventory"];

            _inventoryOpenInput.performed += (_) => OpenInventory();
            _inventoryCloseInput.performed += (_) => CloseInventory();

            _moveInput = _playerInput.actions["UIMove"];
            _hotbarInput = _playerInput.actions["HotbarSelect"];

            SelectedSlotAt(0);
            CloseInventory();
        }


        private void Update()
        {
            //TODO: Fix
            // if (_inventoryOpen)
            // {
            //     var movement = _moveInput.ReadValue<Vector2>();
            //     Debug.Log($"Movement: {movement.x}, {movement.y}");
            //     //There are 10 columns
            //     var slotIndexDelta = Mathf.FloorToInt(movement.x + 10 * movement.y);
            //     Debug.Log($"Slot Index Delta: {slotIndexDelta}");
            //     var slotIndex = Mathf.Clamp(slotIndexDelta + _selectedSlotIndex % _slots.Count - 1, 0,
            //         _slots.Count - 1);
            //     Debug.Log($"Slot Index: {slotIndex}");
            //     SelectedSlotAt(slotIndex);
            // }

            if (!_hotbarInput.triggered) return;
            var read = _hotbarInput.ReadValue<float>();
            var index = Mathf.Clamp(Mathf.RoundToInt(read - 1), 0, _slots.Count - 1);

            SelectedSlotAt(Mathf.RoundToInt(index));
        }

        #endregion


        #region Show Inventory Page

        private void OpenInventory()
        {
            _inventoryGroup.SetActive(true);
            _playerInput.SwitchCurrentActionMap("Inventory");

            if (_freezeTimeOnInventoryOpen)
            {
                Time.timeScale = 0;
            }

            _inventoryOpen = true;
        }

        private void CloseInventory()
        {
            _inventoryGroup.SetActive(false);
            _playerInput.SwitchCurrentActionMap("Player");

            Time.timeScale = 1;
            _inventoryOpen = false;
        }

        #endregion

        #region Public Interface

        [Button]
        public bool TryAddItem(Item item, int amount)
        {
            while (true)
            {
                if (amount <= 0)
                {
                    return false;
                }

                //early split check
                if (amount > item.MaxStack)
                {
                    //we need to split before we continue,
                    //first add a full stack first
                    TryAddItem(item, item.MaxStack);
                    //now add whatever left
                    var remains = amount - item.MaxStack;
                    amount = remains;
                    continue;
                }

                InventorySlot emptySlot = null;

                //Search for a stack opportunity
                foreach (InventorySlot slot in _slots)
                {
                    var itemInSlot = slot.InventoryItem;
                    //is this empty?
                    if (!itemInSlot)
                    {
                        if (emptySlot) continue;
                        //Save this for later if we need to look for an empty slot
                        emptySlot = slot;
                        continue;
                    }

                    //is this the same item?
                    if (itemInSlot.Item.ItemName != item.ItemName)
                    {
                        continue;
                    }

                    //Can we stack it?
                    if (itemInSlot.IsFull) continue;

                    //Do we need to split?
                    var sum = amount + itemInSlot.Amount;
                    if (sum > itemInSlot.Item.MaxStack)
                    {
                        //split
                        itemInSlot.SetAmount(itemInSlot.Item.MaxStack);
                        var diff = sum - itemInSlot.Item.MaxStack;
                        return TryAddItem(item, diff);
                    }

                    //We found an item of the same type and that can accept stacking
                    itemInSlot.AddAmount(amount);
                    return true;
                }

                if (!emptySlot) return false;
                CreateNewItem(item, emptySlot, amount);
                return true;
            }
        }

        [Button]
        public void SelectedSlotAt(int slotIndex)
        {
            if (_selectedSlotIndex >= 0)
            {
                _slots[_selectedSlotIndex].Deselect();
            }

            _slots[slotIndex].Select();
            _selectedSlotIndex = slotIndex;
        }

        [Button]
        public Item GetCurrentlySelectedItem()
        {
            var slot = _slots[_selectedSlotIndex];
            var itemInSlot = slot.InventoryItem;
            return itemInSlot ? itemInSlot.Item : null;
        }

        [Button]
        public ItemRemovalOutcome RemoveItemAt(int slotIndex, int amount)
        {
            var slot = _slots[slotIndex];
            var itemInSlot = slot.InventoryItem;

            if (itemInSlot == null)
            {
                return ItemRemovalOutcome.NoItemToRemove;
            }

            var newAmount = Mathf.Clamp(itemInSlot.Amount - amount, 0, itemInSlot.Item.MaxStack);
            if (newAmount <= 0)
            {
                itemInSlot.DestroySelf();
                return ItemRemovalOutcome.ItemDestroyed;
            }

            itemInSlot.SetAmount(newAmount);
            return ItemRemovalOutcome.ChangedStackAmount;
        }

        [Button]
        public ItemRemovalOutcome RemoveCurrentlySelectedItem(int amount)
        {
            return RemoveItemAt(_selectedSlotIndex, amount);
        }

        public enum ItemRemovalOutcome
        {
            NoItemToRemove,
            ChangedStackAmount,
            ItemDestroyed
        }

        #endregion

        #region Helpers

        private void CreateNewItem(Item item, InventorySlot slot, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning("Amount of items cannot be less or equal to zero");
                return;
            }

            InventoryItem newItem = Instantiate(_inventoryItemPrefab);
            newItem.SetItem(item, amount);
            slot.SetItem(newItem);
        }

        #endregion
    }
}