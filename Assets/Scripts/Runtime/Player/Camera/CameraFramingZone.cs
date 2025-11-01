using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Runtime.Player.Camera
{
    public class CameraFramingZone : MonoBehaviour
    {
        [SerializeField] private CinemachineTargetGroup _targetGroup;
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private float _weight;
        [SerializeField] private float _radius;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _targetGroup.AddMember(_targetTransform, _weight, _radius);
            }
            
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _targetGroup.RemoveMember(_targetTransform);
            }
        }
    }
}