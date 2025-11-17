using System;
using System.Collections.Generic;
using System.Linq;
using Runtime.Inventory.UI;
using Runtime.Player.Inventory.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace Runtime.Player.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField, FoldoutGroup("UI")] private GameObject _inventoryGroup;
        [SerializeField, FoldoutGroup("UI")] private InventoryItem _inventoryItemPrefab;
        [SerializeField, FoldoutGroup("UI")] private bool _freezeTimeOnInventoryOpen;

        [SerializeReference, FoldoutGroup("Content")]
        private IInventorySorter _inventorySorter = new DefaultInventorySorter();


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
        private static GameObject _player;


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

            _player = GameObject.FindWithTag("Player");
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

                if (!emptySlot)
                {
                    DropItem(item, amount);
                    return false;
                }

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

        public int GetAvailableAmount(Item item)
        {
            return item == null ? 0 : GetAvailableAmount(item.ItemName);
        }

        public int GetAvailableAmount(string itemName)
        {
            var totalAmount = 0;

            foreach (var slot in _slots)
            {
                var slotItem = slot.InventoryItem;
                if (!slotItem) continue;

                if (!string.Equals(slotItem.Item.ItemName, itemName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                totalAmount += slotItem.Amount;
            }

            return totalAmount;
        }

        public bool CanMeetRequirements(IEnumerable<ItemRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                if (requirement.Amount <= 0 || !requirement.Item)
                {
                    continue;
                }

                if (GetAvailableAmount(requirement.Item) < requirement.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryRemoveRequirements(IEnumerable<ItemRequirement> requirements)
        {
            requirements = ItemRequirement.Compress(requirements);
            IEnumerable<ItemRequirement> itemRequirements = requirements.ToArray();
            if (!CanMeetRequirements(itemRequirements))
            {
                return false;
            }

            foreach (var requirement in itemRequirements)
            {
                if (requirement.Amount <= 0 || requirement.Item == null)
                {
                    continue;
                }

                RemoveItemStacks(requirement.Item.ItemName, requirement.Amount);
            }

            return true;
        }

        [Button]
        public void SortInventory()
        {
            _inventorySorter ??= new DefaultInventorySorter();
            if (_slots == null)
            {
                return;
            }

            _inventorySorter.Sort(_slots);
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

        private void RemoveItemStacks(string itemName, int amount)
        {
            foreach (var slot in _slots)
            {
                if (amount <= 0)
                {
                    break;
                }

                var slotItem = slot.InventoryItem;
                if (!slotItem || !string.Equals(slotItem.Item.ItemName, itemName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var amountToRemove = Mathf.Min(amount, slotItem.Amount);
                var newAmount = slotItem.Amount - amountToRemove;
                amount -= amountToRemove;

                if (newAmount <= 0)
                {
                    slotItem.DestroySelf();
                    continue;
                }

                slotItem.SetAmount(newAmount);
            }
        }

        [Button]
        private void DropItem(Item item, int amount)
        {
            if (!_itemDropPrefab)
            {
                Debug.LogWarning("Item drop prefab is not assigned");
                return;
            }

            var position = (Vector3)(Random.insideUnitCircle * 5) + _player.transform.position;

            var dropObject = Instantiate(_itemDropPrefab, position,
                Quaternion.identity);
            if (!dropObject.TryGetComponent<ItemDrop>(out var itemDrop))
            {
                itemDrop = dropObject.AddComponent<ItemDrop>();
            }

            itemDrop.Initialize(item, amount);
        }

        [Serializable]
        public struct ItemRequirement : IEquatable<ItemRequirement>
        {
            public Item Item;
            public int Amount;

            /// <summary>
            /// Return an organized list of requirements where duplicate items are merged into the same requirements
            /// </summary>
            /// <param name="requirements"></param>
            /// <returns></returns>
            public static List<ItemRequirement> Compress(IEnumerable<ItemRequirement> requirements)
            {
                var newList = new List<ItemRequirement>();

                foreach (var requirement in requirements)
                {
                    if (requirement.Amount <= 0 || !requirement.Item)
                    {
                        continue;
                    }

                    if (newList.Contains(requirement))
                    {
                        var item = newList.First((r) => Equals(r.Item, requirement.Item));
                        item.Amount += requirement.Amount;
                        continue;
                    }

                    newList.Add(requirement);
                }

                return newList;
            }

            #endregion

            public bool Equals(ItemRequirement other)
            {
                return Equals(Item, other.Item) && Amount == other.Amount;
            }

            public override bool Equals(object obj)
            {
                return obj is ItemRequirement other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Item, Amount);
            }
        }
    }
}