using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

namespace Runtime.Player.Camera
{
    /// <summary>
    /// A trigger zone that smoothly adds/removes a transform from a CinemachineTargetGroup.
    /// Uses DOTween to fade weight/radius in and out for smooth camera blending.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Camera/Camera Framing Zone (Tweened)")]
    public class CameraFramingZone : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform _targetTransform;
        [SerializeField, Range(0f, 20f)] private float _weight = 1f;
        [SerializeField, Range(0f, 20f)] private float _radius = 2f;

        [SerializeField] private CinemachineTargetGroup.PositionModes _positionMode;
        [SerializeField] private CinemachineTargetGroup.RotationModes _rotationMode;
        [SerializeField] private CinemachineTargetGroup.UpdateMethods _updateMethod;

        [Header("Tween Settings")]
        [SerializeField, Range(0f, 5f)] private float _fadeDuration = 0.25f;
        [SerializeField] private Ease _ease = Ease.OutQuad;

        private CinemachineTargetGroup _targetGroup;
        private Tween _weightTween;
        private Tween _radiusTween;
        private bool _isActive;

        // Allow injection for testing
        public void Initialize(CinemachineTargetGroup group)
        {
            _targetGroup = group;
        }

        private void Awake()
        {
            if (_targetTransform == null)
                _targetTransform = transform;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Start()
        {
            if (_targetGroup == null)
                _targetGroup = FindFirstObjectByType<CinemachineTargetGroup>();

            if (_targetGroup == null)
            {
                Debug.LogWarning($"[{nameof(CameraFramingZone)}] No CinemachineTargetGroup found. Disabling.");
                enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_targetGroup || !other.CompareTag("Player") || _isActive)
                return;

            _isActive = true;

            if (FindTargetIndex() == -1)
            {
                _targetGroup.AddMember(_targetTransform, 0f, 0f);
                _targetGroup.PositionMode = _positionMode;
                _targetGroup.RotationMode = _rotationMode;
                _targetGroup.UpdateMethod = _updateMethod;
            }

            TweenTo(_weight, _radius, null);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_targetGroup || !other.CompareTag("Player") || !_isActive)
                return;

            _isActive = false;
            RemoveNonPlayerCameraTargets();
            TweenTo(0f, 0f, () =>
            {
                int idx = FindTargetIndex();
                if (idx != -1)
                    _targetGroup.RemoveMember(_targetTransform);
            });
        }

        private void RemoveNonPlayerCameraTargets()
        {
            var toRemove = new List<CinemachineTargetGroup.Target>();
            foreach (var target in _targetGroup.Targets)
            {
                var obj = target.Object;
                if (obj == null) continue;
                if (obj == _targetTransform) continue;
                if (obj.CompareTag("Player")) continue;
                if (obj.CompareTag("CameraTarget"))
                    toRemove.Add(target);
            }

            foreach (var t in toRemove)
                _targetGroup.RemoveMember(t.Object);
        }

        private void TweenTo(float targetWeight, float targetRadius, TweenCallback onComplete)
        {
            int idx = FindTargetIndex();
            if (idx == -1) return;

            _weightTween?.Kill(false);
            _radiusTween?.Kill(false);

            _weightTween = DOTween.To(
                () => GetMemberWeight(idx),
                w => SetMemberWeight(idx, w),
                targetWeight,
                _fadeDuration
            ).SetEase(_ease);

            _radiusTween = DOTween.To(
                () => GetMemberRadius(idx),
                r => SetMemberRadius(idx, r),
                targetRadius,
                _fadeDuration
            ).SetEase(_ease)
             .OnComplete(onComplete);
        }

        private int FindTargetIndex()
        {
            var targets = _targetGroup.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Object == _targetTransform)
                    return i;
            }
            return -1;
        }

        private float GetMemberWeight(int index)
        {
            return _targetGroup.Targets[index].Weight;
        }

        private void SetMemberWeight(int index, float weight)
        {
            var t = _targetGroup.Targets[index];
            t.Weight = weight;
            _targetGroup.Targets[index] = t;
        }

        private float GetMemberRadius(int index)
        {
            return _targetGroup.Targets[index].Radius;
        }

        private void SetMemberRadius(int index, float radius)
        {
            var t = _targetGroup.Targets[index];
            t.Radius = radius;
            _targetGroup.Targets[index] = t;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            if (TryGetComponent(out Collider2D col))
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);

            if (_targetTransform != null)
            {
                Gizmos.color = new Color(0.4f, 1f, 1f, 0.4f);
                Gizmos.DrawWireSphere(_targetTransform.position, _radius);
            }
        }
#endif
    }
}
