using System.Collections.Generic;
using Runtime.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Inventory.UI
{
    public class InventoryContextMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform _optionContainer;
        [SerializeField] private Button _optionButtonPrefab;
        [SerializeField] private InventoryController _inventoryController;

        private readonly List<Button> _spawnedOptions = new();
        private InventoryItem _currentItem;

        private void Awake()
        {
            if (_inventoryController == null)
            {
                _inventoryController = FindObjectOfType<InventoryController>();
            }

            gameObject.SetActive(false);

            if (_optionButtonPrefab != null)
            {
                _optionButtonPrefab.gameObject.SetActive(false);
            }
        }

        public void Show(InventoryItem item, Vector2 screenPosition)
        {
            if (item == null) return;

            _currentItem = item;
            ClearOptions();

            AddOption("Drop", HandleDrop);            

            if (item.Item.Actions != null)
            {
                foreach (var action in item.Item.Actions)
                {
                    if (action == null) continue;
                    AddOption(action.ActionName, () => HandleAction(action));
                }
            }

            var rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.position = screenPosition;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            ClearOptions();
        }

        private void AddOption(string label, UnityEngine.Events.UnityAction onClick)
        {
            if (_optionButtonPrefab == null || _optionContainer == null) return;

            var option = Instantiate(_optionButtonPrefab, _optionContainer);
            option.gameObject.SetActive(true);
            option.onClick.AddListener(() =>
            {
                onClick?.Invoke();
                Hide();
            });

            var text = option.GetComponentInChildren<TMPro.TMP_Text>();
            if (text != null)
            {
                text.text = label;
            }
            else
            {
                var legacyText = option.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.text = label;
                }
            }

            _spawnedOptions.Add(option);
        }

        private void ClearOptions()
        {
            foreach (var option in _spawnedOptions)
            {
                if (option != null)
                {
                    Destroy(option.gameObject);
                }
            }

            _spawnedOptions.Clear();
        }

        private void HandleDrop()
        {
            if (_currentItem == null) return;
            _inventoryController?.DropItem(_currentItem);
        }

        private void HandleAction(ItemAction action)
        {
            if (_currentItem == null || action == null) return;
            _inventoryController?.PerformItemAction(_currentItem, action);
        }
    }
}
