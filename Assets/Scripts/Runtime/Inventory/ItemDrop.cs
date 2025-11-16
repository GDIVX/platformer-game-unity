using System;
using Runtime.Inventory.UI;
using UnityEngine;

namespace Runtime.Inventory
{
    public class ItemDrop : MonoBehaviour
    {
        [SerializeField] private ItemView _itemView;

        [SerializeField, Min(1)] private int _count;

        public int Count
        {
            get => _count;
            set
            {
                if (value < 1)
                {
                    _count = 1;
                    return;
                }

                _count = value;
            }
        }
    }
}