using System;
using System.Collections.Generic;
using System.Linq;
using Runtime.Player.Inventory.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace Runtime.Player.Inventory
{
    /// <summary>
    /// Controls the player's inventory: UI visibility, item stacks, requirements,
    /// sorting and item dropping.
    /// </summary>
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

        [SerializeField, FoldoutGroup("Drop")] private Transform _playerTransform;

        [ShowInInspector] private int _selectedSlotIndex = -1;

        private PlayerInput _playerInput;
        private InputAction _inventoryOpenInput;
        private InputAction _inventoryCloseInput;
        private InputAction _hotbarInput;

        // private bool _inventoryOpen;

        #region Unity Events

        private void Start()
        {
            _playerInput = InputManager.PlayerInput;

            _inventoryOpenInput = _playerInput.actions["Open Inventory"];
            _inventoryCloseInput = _playerInput.actions["Close Inventory"];

            _inventoryOpenInput.performed += OnInventoryOpenPerformed;
            _inventoryCloseInput.performed += OnInventoryClosePerformed;

            _hotbarInput = _playerInput.actions["HotbarSelect"];

            _slots ??= new List<InventorySlot>();

            if (_slots.Count > 0)
            {
                SelectedSlotAt(0);
            }

            CloseInventory();

            if (_playerTransform != null) return;
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                _playerTransform = playerGo.transform;
            }
        }

        private void OnDestroy()
        {
            if (_inventoryOpenInput != null)
            {
                _inventoryOpenInput.performed -= OnInventoryOpenPerformed;
            }

            if (_inventoryCloseInput != null)
            {
                _inventoryCloseInput.performed -= OnInventoryClosePerformed;
            }
        }

        private void Update()
        {
            if (_hotbarInput == null || _slots == null || _slots.Count == 0)
            {
                return;
            }

            if (!_hotbarInput.triggered)
            {
                return;
            }

            // Input is [1..N], slots are [0..N-1]
            var read = _hotbarInput.ReadValue<float>();
            var index = Mathf.RoundToInt(read - 1f);
            index = Mathf.Clamp(index, 0, _slots.Count - 1);

            SelectedSlotAt(index);
        }

        #endregion

        #region Inventory UI

        private void OnInventoryOpenPerformed(InputAction.CallbackContext _)
        {
            OpenInventory();
        }

        private void OnInventoryClosePerformed(InputAction.CallbackContext _)
        {
            CloseInventory();
        }

        private void OpenInventory()
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

        private void CloseInventory()
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

        #endregion

        #region Public Interface

        /// <summary>
        /// Tries to add the requested amount of the given item to the inventory.
        /// Returns true if all items fit; false if some or all had to be dropped.
        /// </summary>
        [Button]
        public bool TryAddItem(Item item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return false;
            }

            if (_slots == null || _slots.Count == 0)
            {
                DropItem(item, amount);
                return false;
            }

            var remaining = amount;

            // First: stack into existing slots where possible.
            foreach (var slot in _slots)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var slotItem = slot.InventoryItem;
                if (!slotItem)
                {
                    continue;
                }

                if (!string.Equals(slotItem.Item.ItemName, item.ItemName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (slotItem.IsFull)
                {
                    continue;
                }

                var maxStack = slotItem.Item.MaxStack;
                var space = Mathf.Max(0, maxStack - slotItem.Amount);
                if (space <= 0)
                {
                    continue;
                }

                var toAdd = Mathf.Min(space, remaining);
                slotItem.AddAmount(toAdd);
                remaining -= toAdd;
            }

            // Second: create new stacks in empty slots while we still have room and remaining items.
            foreach (var slot in _slots)
            {
                if (remaining <= 0)
                {
                    break;
                }

                if (slot.InventoryItem)
                {
                    continue;
                }

                var toCreate = Mathf.Min(remaining, item.MaxStack);
                CreateNewItem(item, slot, toCreate);
                remaining -= toCreate;
            }

            // If we still have remaining items, drop them.
            if (remaining > 0)
            {
                DropItem(item, remaining);
                return false;
            }

            return true;
        }

        [Button]
        public void SelectedSlotAt(int slotIndex)
        {
            if (_slots == null || _slots.Count == 0)
            {
                _selectedSlotIndex = -1;
                return;
            }

            slotIndex = Mathf.Clamp(slotIndex, 0, _slots.Count - 1);

            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _slots.Count)
            {
                _slots[_selectedSlotIndex]?.Deselect();
            }

            _slots[slotIndex]?.Select();
            _selectedSlotIndex = slotIndex;
        }

        [Button]
        public Item GetCurrentlySelectedItem()
        {
            if (_slots == null ||
                _selectedSlotIndex < 0 ||
                _selectedSlotIndex >= _slots.Count)
            {
                return null;
            }

            var slot = _slots[_selectedSlotIndex];
            var itemInSlot = slot.InventoryItem;
            return itemInSlot ? itemInSlot.Item : null;
        }

        [Button]
        public ItemRemovalOutcome RemoveItemAt(int slotIndex, int amount)
        {
            if (_slots == null || slotIndex < 0 || slotIndex >= _slots.Count)
            {
                return ItemRemovalOutcome.NoItemToRemove;
            }

            var slot = _slots[slotIndex];
            var itemInSlot = slot.InventoryItem;

            if (!itemInSlot)
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
            if (string.IsNullOrWhiteSpace(itemName) || _slots == null)
            {
                return 0;
            }

            return (from slot in _slots
                select slot.InventoryItem
                into slotItem
                where slotItem
                where string.Equals(slotItem.Item.ItemName, itemName, StringComparison.OrdinalIgnoreCase)
                select slotItem.Amount).Sum();
        }

        public bool CanMeetRequirements(IEnumerable<ItemRequirement> requirements)
        {
            if (requirements == null)
            {
                return true;
            }

            return requirements.Where(requirement => requirement.Amount > 0 && requirement.Item).All(requirement =>
                GetAvailableAmount(requirement.Item) >= requirement.Amount);
        }

        public bool TryRemoveRequirements(IEnumerable<ItemRequirement> requirements)
        {
            if (requirements == null)
            {
                return true;
            }

            var compressed = ItemRequirement.Compress(requirements).ToArray();
            if (!CanMeetRequirements(compressed))
            {
                return false;
            }

            foreach (var requirement in compressed)
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
            if (item == null || slot == null)
            {
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning("Amount of items cannot be less or equal to zero");
                return;
            }

            var newItem = Instantiate(_inventoryItemPrefab);
            newItem.SetItem(item, amount);
            slot.SetItem(newItem);
        }

        private void RemoveItemStacks(string itemName, int amount)
        {
            if (_slots == null || string.IsNullOrWhiteSpace(itemName) || amount <= 0)
            {
                return;
            }

            var remaining = amount;

            foreach (var slot in _slots)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var slotItem = slot.InventoryItem;
                if (!slotItem ||
                    !string.Equals(slotItem.Item.ItemName, itemName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var amountToRemove = Mathf.Min(remaining, slotItem.Amount);
                var newAmount = slotItem.Amount - amountToRemove;
                remaining -= amountToRemove;

                if (newAmount <= 0)
                {
                    slotItem.DestroySelf();
                }
                else
                {
                    slotItem.SetAmount(newAmount);
                }
            }
        }

        [Button]
        private void DropItem(Item item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return;
            }

            if (!_itemDropPrefab)
            {
                Debug.LogWarning("Item drop prefab is not assigned");
                return;
            }

            if (_playerTransform == null)
            {
                Debug.LogWarning("Player transform is not assigned; cannot determine drop position.");
                return;
            }

            var position = (Vector3)(Random.insideUnitCircle * 5f) + _playerTransform.position;
            var dropObject = Instantiate(_itemDropPrefab, position, Quaternion.identity);

            if (!dropObject.TryGetComponent<ItemDrop>(out var itemDrop))
            {
                itemDrop = dropObject.AddComponent<ItemDrop>();
            }

            itemDrop.Initialize(item, amount);
        }

        #endregion

        [Serializable]
        public struct ItemRequirement : IEquatable<ItemRequirement>
        {
            public Item Item;
            public int Amount;

            /// <summary>
            /// Returns a list of requirements where duplicate items are merged,
            /// summing their amounts.
            /// </summary>
            public static List<ItemRequirement> Compress(IEnumerable<ItemRequirement> requirements)
            {
                var result = new List<ItemRequirement>();
                if (requirements == null)
                {
                    return result;
                }

                var accumulator = new Dictionary<Item, int>();

                foreach (var requirement in requirements)
                {
                    if (requirement.Amount <= 0 || requirement.Item == null)
                    {
                        continue;
                    }

                    if (accumulator.TryGetValue(requirement.Item, out var current))
                    {
                        accumulator[requirement.Item] = current + requirement.Amount;
                    }
                    else
                    {
                        accumulator[requirement.Item] = requirement.Amount;
                    }
                }

                result.AddRange(accumulator.Select(kvp => new ItemRequirement { Item = kvp.Key, Amount = kvp.Value }));

                return result;
            }

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