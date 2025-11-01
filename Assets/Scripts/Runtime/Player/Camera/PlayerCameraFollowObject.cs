using System;
using System.Collections;
using DG.Tweening;
using Runtime.Player.Movement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Player.Camera
{
    public class PlayerCameraFollowObject : MonoBehaviour
    {

        [SerializeField]private PlayerMovement _playerMovement;
        [SerializeField, BoxGroup("Flip Rotation")]
        private float _flipYRotationTime;
        [SerializeField, BoxGroup("Flip Rotation")]
        private Ease _flipYRotationEase;
        
        private bool _isFacingRight;

        private void Start()
        {
            _isFacingRight = _playerMovement.Context.IsFacingRight;
        }

        private void Update()
        {
            transform.position = _playerMovement.transform.position;
        }

        public void CallTurn()
        {
            var rotation = new Vector3(0, DetermineEndRotation(), 0);
            _playerMovement.transform.DORotate(rotation , _flipYRotationTime, RotateMode.Fast).SetEase(_flipYRotationEase);
        }



        private float DetermineEndRotation()
        {
            _isFacingRight = !_isFacingRight;

            return _isFacingRight ? 180f : 0f;
        }
    }
}
