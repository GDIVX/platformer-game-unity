using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Runtime.Inventory.UI
{
    public class InventoryItemView : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        [SerializeField] CanvasGroup _canvasGroup;

        [HideInInspector] public Transform ParentAfterDrag;
        [SerializeField] private InventorySlotView _inventorySlot;

        public InventorySlotView InventorySlot
        {
            get => _inventorySlot;
            set => _inventorySlot = value;
        }

        private void Awake()
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();
            InventorySlot ??= transform.parent.GetComponent<InventorySlotView>();
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