using System;
using System.Collections;
using DG.Tweening;
using Runtime.Inventory.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Player.Inventory
{
    public class ItemDrop : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private ItemView _itemView;

        [Header("Data")]
        [SerializeField] private Item _item;
        [SerializeField, Min(1)] private int _count;

        [Header("Movement")]
        [SerializeField] private float _pickupDistance = 0.6f;
        [SerializeField] private float _baseSpeed = 3f;
        [SerializeField] private float _distanceAccel = 6f;
        [SerializeField] private float _maxMagnetDistance = 6f;

        [Header("Events")]
        public UnityEvent OnMovementStart;
        public UnityEvent OnPickUp;

        private Coroutine _collectRoutine;
        private bool _movementStarted;
        
        private static GameObject _player;

        public int Count
        {
            set
            {
                _count = Mathf.Max(1, value);
                UpdateView();
            }
        }

        private void OnDisable()
        {
            StopCollectRoutine();
        }

        private void Start()
        {
            _player ??= GameObject.FindGameObjectWithTag("Player");
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
                return false;

            var success = inventory.TryAddItem(_item, _count);
            if (!success)
                return false;

            StopCollectRoutine();
            OnPickUp?.Invoke();
            Destroy(gameObject);
            return true;
        }

        public void BeginCollection(InventoryController inventory)
        {
            if (_collectRoutine != null || inventory == null)
                return;

            _collectRoutine = StartCoroutine(CollectRoutine(inventory));
        }

        public void StopCollectRoutine()
        {
            if (_collectRoutine == null) return;

            StopCoroutine(_collectRoutine);
            _collectRoutine = null;
            _movementStarted = false;
        }

        private IEnumerator CollectRoutine(InventoryController inventory)
        {
            var playerTr = _player.transform;

            while (true)
            {
                if (inventory == null || playerTr == null)
                    yield break;

                var pos = transform.position;
                var target = playerTr.position;

                var toPlayer = target - pos;
                var dist = toPlayer.magnitude;

                if (!_movementStarted)
                {
                    _movementStarted = true;
                    OnMovementStart?.Invoke();
                }

                if (dist <= _pickupDistance)
                {
                    if (TryAddToInventory(inventory))
                        yield break;
                }

                if (dist > Mathf.Epsilon)
                {
                    // BASE DIRECTION — locked toward the player
                    Vector3 dir = toPlayer.normalized;

                    // ------- SHAPING (safe, additive, never flips direction) -------

                    // Gravity bias: if we're moving DOWN, accelerate slightly
                    if (dir.y < 0f)
                    {
                        dir.y -= 0.4f;   // stronger downward pull
                    }
                    else
                    {
                        // Jump assist: gently encourage sideways drift
                        // but never enough to overpower the main direction
                        float sideWiggle = UnityEngine.Random.Range(-0.15f, 0.15f);
                        dir.x += sideWiggle;
                    }

                    // Renormalize so we never wander off
                    dir = dir.normalized;

                    // Distance-based speed curve
                    float t = Mathf.Clamp01(1f - dist / _maxMagnetDistance);
                    float speed = _baseSpeed + t * _distanceAccel;

                    transform.position += dir * (speed * Time.deltaTime);
                }

                yield return null;
            }
        }

        private void UpdateView()
        {
            if (_itemView != null && _item != null)
                _itemView.SetItem(_item, _count);
        }
    }
}
