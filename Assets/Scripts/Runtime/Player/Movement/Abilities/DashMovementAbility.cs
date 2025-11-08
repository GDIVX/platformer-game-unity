using System;
using System.Collections.Generic;
using Runtime.Player;
using Runtime.Player.Movement.States;
using UnityEngine;

namespace Runtime.Player.Movement.Abilities
{
    [CreateAssetMenu(menuName = "Player/Movement/Abilities/Dash", fileName = "DashMovementAbility")]
    public class DashMovementAbility : MovementAbility
    {
        private PlayerMovementContext _context;
        private PlayerMovementStateMachine _stateMachine;
        private bool _inputSubscribed;

        public override void Initialize(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            _context = context;
            _stateMachine = stateMachine;
        }

        public override IEnumerable<IPlayerMovementState> CreateStates(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            yield return new DashState(context, stateMachine);
        }

        public override IEnumerable<IPlayerMovementModifier> CreateModifiers(PlayerMovementContext context)
        {
            return Array.Empty<IPlayerMovementModifier>();
        }

        public override IEnumerable<Func<PlayerMovementContext, bool>> CreateActivationConditions(PlayerMovementContext context)
        {
            return Array.Empty<Func<PlayerMovementContext, bool>>();
        }

        public override void OnAbilityEnabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            SubscribeToInput();
        }

        public override void OnAbilityDisabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            UnsubscribeFromInput();
            ResetRuntimeFlags();
        }

        private void SubscribeToInput()
        {
            if (_inputSubscribed)
                return;

            InputManager.DashPressed += HandleDashPressed;
            _inputSubscribed = true;
        }

        private void UnsubscribeFromInput()
        {
            if (!_inputSubscribed)
                return;

            InputManager.DashPressed -= HandleDashPressed;
            _inputSubscribed = false;
        }

        private void HandleDashPressed()
        {
            if (_context == null || _stateMachine == null)
                return;

            var data = _context.RuntimeData;
            if (!CanDash(data))
                return;

            data.DashRequested = true;
            data.DashRequestFromGround = data.IsGrounded;
            data.DashDirection = DetermineDashDirection(data);
        }

        private static int DetermineDashDirection(PlayerMovementRuntimeData data)
        {
            if (Mathf.Abs(data.MoveInput.x) > 0.01f)
                return (int)Mathf.Sign(data.MoveInput.x);

            return data.IsFacingRight ? 1 : -1;
        }

        private bool CanDash(PlayerMovementRuntimeData data)
        {
            if (data == null)
                return false;

            if (data.IsDashing || data.DashStopTimer > 0f || data.DashCooldownTimer > 0f)
                return false;

            if (data.IsGrounded)
                return true;

            if (!_context?.Stats || _context.Stats.DashAirDashLimit <= 0)
                return false;

            return data.AirDashCount < _context.Stats.DashAirDashLimit && data.AirDashCooldownTimer <= 0f;
        }

        private void ResetRuntimeFlags()
        {
            var data = _context?.RuntimeData;
            if (data == null)
                return;

            data.DashRequested = false;
            data.DashRequestFromGround = false;
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
        }
    }
}
