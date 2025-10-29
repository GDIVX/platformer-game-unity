using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Player
{
    public class InputManager : MonoBehaviour
    {
        public static PlayerInput PlayerInput;

        public static Vector2 Movement;
        public static bool JumpPressed;
        public static bool JumpHeld;
        public static bool JumpReleased;
        public static bool RunHeld;
        
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _runAction;

        private void Awake()
        {
            PlayerInput = GetComponent<PlayerInput>();
            _moveAction = PlayerInput.actions["Move"];
            _jumpAction = PlayerInput.actions["Jump"];
            _runAction = PlayerInput.actions["Run"];
        }

        private void Update()
        {
            Movement = _moveAction.ReadValue<Vector2>();

            JumpPressed = _jumpAction.WasPressedThisFrame();
            JumpHeld = _jumpAction.IsPressed();
            JumpReleased = _jumpAction.WasReleasedThisFrame();
            
            RunHeld = _runAction.IsPressed();
        }
    }
}
