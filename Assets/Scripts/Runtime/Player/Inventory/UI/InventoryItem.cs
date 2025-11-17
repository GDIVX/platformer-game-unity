using Runtime.Inventory.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Runtime.Player.Inventory.UI
{
    public class InventoryItem : ItemView, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
    {
        [SerializeField] CanvasGroup _canvasGroup;

        [HideInInspector] public Transform ParentAfterDrag;
        [SerializeField] private InventorySlot _inventorySlot;

        public Item Item { get; private set; }
        public int Amount { get; private set; }


        public InventorySlot InventorySlot
        {
            get => _inventorySlot;
            set => _inventorySlot = value;
        }

        public bool IsFull => Item.MaxStack == Amount;

        private void Awake()
        {
            _image ??= GetComponent<Image>();
            _canvasGroup ??= GetComponent<CanvasGroup>();
        }

        // private void OnEnable()
        // {
        //     InventorySlot ??= transform.parent.GetComponent<InventorySlot>();
        // }


        [Sirenix.OdinInspector.Button]
        public override void SetItem(Item item, int amount)
        {
            Item = item;
            Amount = amount;
            base.SetItem(item, amount);
        }

        public void SetAmount(int amount)
        {
            if (amount <= 0)
            {
                DestroySelf();
                return;
            }

            Amount = amount;
            _countText.text = amount.ToString();
        }

        public void AddAmount(int amount)
        {
            var sum = Amount + amount;
            SetAmount(sum);
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

        public void DestroySelf()
        {
            InventorySlot.InventoryItem = null;
            Item = null;
            Destroy(gameObject);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Keyboard.current.leftShiftKey.isPressed && !Keyboard.current.rightShiftKey.isPressed)
            {
                return;
            }

            if (!InventorySlot)
            {
                return;
            }

            InventorySlot.TryEquipFromClick();
        }
    }
}