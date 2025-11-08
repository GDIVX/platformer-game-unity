using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Player
{
    public class InputManager : MonoBehaviour
    {
        public static PlayerInput PlayerInput;

        public static event Action RunPressed;
        public static event Action RunReleased;

        public static Vector2 Movement;
        public static bool JumpPressed;
        public static bool JumpHeld;
        public static bool JumpReleased;
        public static bool RunHeld;
        public static float RunPressedTime { get; private set; }
        public static float RunReleasedTime { get; private set; }
        
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

            if (_runAction == null)
            {
                RunHeld = false;
                return;
            }

            bool runPressed = _runAction.WasPressedThisFrame();
            if (runPressed)
            {
                RunPressedTime = Time.time;
                RunPressed?.Invoke();
            }

            bool runReleased = _runAction.WasReleasedThisFrame();
            if (runReleased)
            {
                RunReleasedTime = Time.time;
                RunReleased?.Invoke();
            }

            RunHeld = _runAction.IsPressed();
        }
    }
}
