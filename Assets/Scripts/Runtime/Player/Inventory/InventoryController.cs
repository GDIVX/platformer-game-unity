using System;
using System.Collections.Generic;
using System.Linq;
using Runtime.Player.Inventory.Services;
using Runtime.Player.Inventory.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

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

        private InventorySelectionService _selectionService;
        private InventoryItemService _itemService;
        private InventoryRequirementService _requirementService;
        private InventoryUiService _uiService;
        private ItemDropService _itemDropService;

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

            if (_playerTransform == null)
            {
                var playerGo = GameObject.FindWithTag("Player");
                if (playerGo != null)
                {
                    _playerTransform = playerGo.transform;
                }
            }

            _itemDropService = new ItemDropService(_itemDropPrefab, _playerTransform);
            _selectionService = new InventorySelectionService(_slots);
            _itemService = new InventoryItemService(_slots, _inventoryItemPrefab, _itemDropService);
            _requirementService = new InventoryRequirementService(_slots, _itemService);
            _uiService = new InventoryUiService(_inventoryGroup, _playerInput, _freezeTimeOnInventoryOpen);

            if (_slots.Count > 0)
            {
                SelectedSlotAt(0);
            }

            CloseInventory();
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
            _uiService?.OpenInventory();
        }

        private void CloseInventory()
        {
            _uiService?.CloseInventory();
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
            return _itemService != null && _itemService.TryAddItem(item, amount);
        }

        [Button]
        public void SelectedSlotAt(int slotIndex)
        {
            _selectionService?.SelectSlot(slotIndex);
            _selectedSlotIndex = _selectionService?.SelectedSlotIndex ?? -1;
        }

        [Button]
        public Item GetCurrentlySelectedItem()
        {
            return _selectionService?.GetCurrentlySelectedItem();
        }

        [Button]
        public ItemRemovalOutcome RemoveItemAt(int slotIndex, int amount)
        {
            return _itemService == null ? ItemRemovalOutcome.NoItemToRemove : _itemService.RemoveItemAt(slotIndex, amount);
        }

        [Button]
        public ItemRemovalOutcome RemoveCurrentlySelectedItem(int amount)
        {
            return RemoveItemAt(_selectedSlotIndex, amount);
        }

        public int GetAvailableAmount(Item item)
        {
            return _requirementService?.GetAvailableAmount(item) ?? 0;
        }

        public int GetAvailableAmount(string itemName)
        {
            return _requirementService?.GetAvailableAmount(itemName) ?? 0;
        }

        public bool CanMeetRequirements(IEnumerable<ItemRequirement> requirements)
        {
            return _requirementService?.CanMeetRequirements(requirements) ?? requirements == null;
        }

        public bool TryRemoveRequirements(IEnumerable<ItemRequirement> requirements)
        {
            return _requirementService?.TryRemoveRequirements(requirements) ?? true;
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