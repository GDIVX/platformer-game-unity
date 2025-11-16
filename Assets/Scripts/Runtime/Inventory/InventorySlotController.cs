using System;
using Runtime.Inventory.UI;
using UnityEngine;

namespace Runtime.Inventory
{
    public class InventorySlotController : MonoBehaviour
    {
        [SerializeField] private InventorySlotView _view;

        private void Awake()
        {
            _view ??= GetComponent<InventorySlotView>();
            _view.OnItemDropped += ViewOnOnItemDropped;
        }

        private void ViewOnOnItemDropped(InventoryItemView ItemView)
        {
        }
    }
}