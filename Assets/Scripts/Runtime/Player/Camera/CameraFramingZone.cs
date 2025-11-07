using System;
using System.Collections.Generic;
using DG.Tweening;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;

namespace Runtime.Player.Camera
{
    /// <summary>
    /// A trigger zone that smoothly adds/removes a transform from a CinemachineTargetGroup.
    /// Uses DOTween to fade weight/radius in and out, so the camera eases toward this zone.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Camera/Camera Framing Zone (Tweened)")]
    public class CameraFramingZone : MonoBehaviour
    {
        [Header("Target Settings")] [SerializeField]
        private Transform _targetTransform;

        [SerializeField, UnityEngine.Range(0f, 20f)]
        private float _weight = 1f;

        [SerializeField, UnityEngine.Range(0f, 20f)]
        private float _radius = 2f;

        [SerializeField] private CinemachineTargetGroup.PositionModes _positionMode;
        [SerializeField] private CinemachineTargetGroup.RotationModes _rotationMode;
        [SerializeField] private CinemachineTargetGroup.UpdateMethods _updateMethod;

        [Header("Tween Settings")] [SerializeField, UnityEngine.Range(0f, 5f)]
        private float _fadeDuration = 0.25f;

        [SerializeField] private Ease _ease = Ease.OutQuad;

        private CinemachineTargetGroup _targetGroup;
        private Tween _weightTween;
        private Tween _radiusTween;
        private bool _isActive;

        private void Awake()
        {
            if (_targetTransform == null)
                _targetTransform = transform;


            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Start()
        {
            _targetGroup = FindFirstObjectByType<CinemachineTargetGroup>();
            if (_targetGroup != null) return;
            Debug.LogWarning($"[{nameof(CameraFramingZone)}] No CinemachineTargetGroup found. Disabling.");
            enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_targetGroup) return;
            if (!other.CompareTag("Player")) return;
            if (_isActive) return;

            _isActive = true;

            // Add the member if it isn't already in the group
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
            if (!_targetGroup) return;
            if (!other.CompareTag("Player")) return;
            if (!_isActive) return;

            _isActive = false;

            //clear other targets 
            var toRemove = new List<CinemachineTargetGroup.Target>();
            _targetGroup.Targets.ForEach(target =>
            {
                var obj = target.Object;
                if (obj == _targetTransform) return;
                if (!obj.CompareTag("CameraTarget")) return;
                if (obj.CompareTag("Player")) return;

                toRemove.Add(target);
            });
            foreach (var target in toRemove)
            {
                _targetGroup.RemoveMember(target.Object);
            }

            TweenTo(0f, 0f, () =>
            {
                // Only remove if it’s still there
                int idx = FindTargetIndex();
                if (idx != -1)
                    _targetGroup.RemoveMember(_targetTransform);
            });
        }

        /// <summary>
        /// Tween weight+radius on the target group member.
        /// </summary>
        private void TweenTo(float targetWeight, float targetRadius, TweenCallback onComplete)
        {
            int idx = FindTargetIndex();
            if (idx == -1)
                return;

            _weightTween?.Kill();
            _radiusTween?.Kill();

            _weightTween = DOTween.To(
                    () => GetMemberWeight(idx),
                    w => SetMemberWeight(idx, w),
                    targetWeight,
                    _fadeDuration
                )
                .SetEase(_ease);

            _radiusTween = DOTween.To(
                    () => GetMemberRadius(idx),
                    r => SetMemberRadius(idx, r),
                    targetRadius,
                    _fadeDuration
                )
                .SetEase(_ease)
                .OnComplete(onComplete);
        }

        /// <summary>
        /// Find the index of our transform in the target group.
        /// Returns -1 if not found.
        /// </summary>
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

        // --- Helpers to read/write the struct in the list ---

        private float GetMemberWeight(int index)
        {
            var t = _targetGroup.Targets[index];
            return t.Weight;
        }

        private void SetMemberWeight(int index, float weight)
        {
            var t = _targetGroup.Targets[index];
            t.Weight = weight;
            _targetGroup.Targets[index] = t;
        }

        private float GetMemberRadius(int index)
        {
            var t = _targetGroup.Targets[index];
            return t.Radius;
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