using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Player
{
    public class InputManager : MonoBehaviour
    {
        public static PlayerInput PlayerInput;

        // --------------------------- //
        // ──────── EVENTS ─────────── //
        // --------------------------- //
        public static event Action RunPressed;
        public static event Action RunReleased;

        public static event Action DashPressed;

        public static event Action InteractPressed;
        public static event Action InteractReleased;

        public static event Action GadgetPressed;
        public static event Action GadgetReleased;

        public static event Action PrimaryPressed;
        public static event Action PrimaryReleased;
        public static event Action SecondaryPressed;
        public static event Action SecondaryReleased;

        // --------------------------- //
        // ──────── STATES ─────────── //
        // --------------------------- //
        public static Vector2 Movement;

        public static bool JumpPressed;
        public static bool JumpHeld;
        public static bool JumpReleased;

        public static bool RunHeld;
        public static bool InteractHeld;
        public static bool GadgetHeld;
        public static bool PrimaryHeld;
        public static bool SecondaryHeld;

        // --------------------------- //
        // ──────── ACTIONS ────────── //
        // --------------------------- //
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _runAction;
        private InputAction _interactAction;
        private InputAction _gadgetAction;
        private InputAction _primaryAction;
        private InputAction _secondaryAction;

        private void Awake()
        {
            PlayerInput = GetComponent<PlayerInput>();

            _moveAction = PlayerInput.actions["Move"];
            _jumpAction = PlayerInput.actions["Jump"];
            _runAction = PlayerInput.actions["Run"];
            _interactAction = PlayerInput.actions["Interact"];
            _gadgetAction = PlayerInput.actions["Gadget"];
            _primaryAction = PlayerInput.actions["Primary"];
            _secondaryAction = PlayerInput.actions["Secondary"];
        }

        private void Update()
        {
            Movement = _moveAction.ReadValue<Vector2>();

            HandleJump();
            HandleRun();
            HandleInteract();
            HandleGadget();
            HandlePrimary();
            HandleSecondary();
        }

        private void HandleJump()
        {
            JumpPressed = _jumpAction.WasPressedThisFrame();
            JumpHeld = _jumpAction.IsPressed();
            JumpReleased = _jumpAction.WasReleasedThisFrame();
        }

        private void HandleRun()
        {
            if (_runAction == null)
            {
                RunHeld = false;
                return;
            }

            if (_runAction.WasPressedThisFrame())
            {
                RunPressed?.Invoke();
                DashPressed?.Invoke();
            }

            if (_runAction.WasReleasedThisFrame()) RunReleased?.Invoke();
            RunHeld = _runAction.IsPressed();
        }

        private void HandleInteract()
        {
            if (_interactAction == null)
            {
                InteractHeld = false;
                return;
            }

            if (_interactAction.WasPressedThisFrame()) InteractPressed?.Invoke();
            if (_interactAction.WasReleasedThisFrame()) InteractReleased?.Invoke();
            InteractHeld = _interactAction.IsPressed();
        }

        private void HandleGadget()
        {
            if (_gadgetAction == null)
            {
                GadgetHeld = false;
                return;
            }

            if (_gadgetAction.WasPressedThisFrame()) GadgetPressed?.Invoke();
            if (_gadgetAction.WasReleasedThisFrame()) GadgetReleased?.Invoke();
            GadgetHeld = _gadgetAction.IsPressed();
        }

        private void HandlePrimary()
        {
            if (_primaryAction == null)
            {
                PrimaryHeld = false;
                return;
            }

            if (_primaryAction.WasPressedThisFrame()) PrimaryPressed?.Invoke();
            if (_primaryAction.WasReleasedThisFrame()) PrimaryReleased?.Invoke();
            PrimaryHeld = _primaryAction.IsPressed();
        }

        private void HandleSecondary()
        {
            if (_secondaryAction == null)
            {
                SecondaryHeld = false;
                return;
            }

            if (_secondaryAction.WasPressedThisFrame()) SecondaryPressed?.Invoke();
            if (_secondaryAction.WasReleasedThisFrame()) SecondaryReleased?.Invoke();
            SecondaryHeld = _secondaryAction.IsPressed();
        }
    }
}
