using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Runtime.Inventory.UI
{
    public class InventoryItemView : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] Image _image;

        [HideInInspector] public Transform ParentAfterDrag;
        [SerializeField] private InventorySlotView _inventorySlot;

        public InventoryItem InventoryItem { get; private set; }

        public InventorySlotView InventorySlot
        {
            get => _inventorySlot;
            set => _inventorySlot = value;
        }

        private void Awake()
        {
            _image ??= GetComponent<Image>();
            _canvasGroup ??= GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            InventorySlot ??= transform.parent.GetComponent<InventorySlotView>();
        }

        [Sirenix.OdinInspector.Button]
        public void SetItem(Item item)
        {
            InventoryItem = new InventoryItem(item);
            _image.sprite = item.Sprite;
        }


        public void OnBeginDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.6f;
            ParentAfterDrag = transform.parent;
            transform.SetParent(transform.root);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
            transform.SetParent(ParentAfterDrag);
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = Mouse.current.position.ReadValue();
        }
    }
}