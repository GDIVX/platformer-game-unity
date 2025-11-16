using System.Collections;
using Runtime.Inventory.UI;
using UnityEngine;

namespace Runtime.Inventory
{
    public class ItemDrop : MonoBehaviour
    {
        [SerializeField] private ItemView _itemView;

        [SerializeField] private Item _item;
        [SerializeField, Min(1)] private int _count;

        private Coroutine _collectRoutine;

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
                UpdateView();
            }
        }

        public Item Item => _item;

        private void OnDisable()
        {
            StopCollectRoutine();
        }

        public void Initialize(Item item, int amount)
        {
            _item = item;
            Count = amount;
            UpdateView();
        }

        private bool TryAddToInventory(InventoryController inventory)
        {
            if (!inventory || !_item)
            {
                return false;
            }

            var success = inventory.TryAddItem(_item, _count);

            if (!success) return false;
            
            StopCollectRoutine();
            Destroy(gameObject);

            return true;
        }

        public void BeginCollection(InventoryController inventory)
        {
            if (_collectRoutine != null || inventory == null || _item == null)
            {
                return;
            }

            _collectRoutine = StartCoroutine(CollectRoutine(inventory));
        }

        public void StopCollectRoutine()
        {
            if (_collectRoutine == null)
            {
                return;
            }

            StopCoroutine(_collectRoutine);
            _collectRoutine = null;
        }

        private IEnumerator CollectRoutine(InventoryController inventory)
        {
            while (!TryAddToInventory(inventory))
            {
                var waitTime = Random.Range(0.1f, 1f);
                yield return new WaitForSeconds(waitTime);
            }

            _collectRoutine = null;
        }

        private void UpdateView()
        {
            if (_item == null || _itemView == null)
            {
                return;
            }

            _itemView.SetItem(_item, _count);
        }
    }
}