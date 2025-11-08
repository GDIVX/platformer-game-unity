using System;
using System.Collections.Generic;
using Runtime.Player;
using Runtime.Player.Movement.States;
using UnityEngine;

namespace Runtime.Player.Movement.Abilities
{
    [CreateAssetMenu(menuName = "Player/Movement/Abilities/Dash", fileName = "DashMovementAbility")]
    public class DashMovementAbility : ScriptableObject, IMovementAbility
    {
        private PlayerMovementContext _context;
        private PlayerMovementStateMachine _stateMachine;
        private float _lastRunPressedTime = float.MinValue;
        private int _runTapCount;
        private bool _inputSubscribed;

        public void Initialize(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            _context = context;
            _stateMachine = stateMachine;
        }

        public IEnumerable<IPlayerMovementState> CreateStates(
            PlayerMovementContext context,
            PlayerMovementStateMachine stateMachine)
        {
            yield return new DashState(context, stateMachine);
        }

        public IEnumerable<IPlayerMovementModifier> CreateModifiers(PlayerMovementContext context)
        {
            return Array.Empty<IPlayerMovementModifier>();
        }

        public IEnumerable<Func<PlayerMovementContext, bool>> CreateActivationConditions(
            PlayerMovementContext context)
        {
            return Array.Empty<Func<PlayerMovementContext, bool>>();
        }

        public void OnAbilityEnabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            SubscribeToInput();
            ResetTapTracking();
        }

        public void OnAbilityDisabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            UnsubscribeFromInput();
            ResetRuntimeFlags();
        }

        public void EvaluateRunTap(float currentTime)
        {
            if (_context == null)
            {
                return;
            }

            float window = Mathf.Max(0f, _context.Stats?.DashDoubleTapWindow ?? 0f);
            if (currentTime - _lastRunPressedTime <= window)
            {
                _runTapCount++;
            }
            else
            {
                _runTapCount = 1;
            }

            _lastRunPressedTime = currentTime;

            if (_runTapCount < 2)
            {
                return;
            }

            _runTapCount = 0;
            TryRequestDash();
        }

        private void TryRequestDash()
        {
            if (_context == null || _stateMachine == null)
            {
                return;
            }

            if (_stateMachine.GetState<DashState>() == null)
            {
                return;
            }

            var data = _context.RuntimeData;
            if (!CanDash(data))
            {
                return;
            }

            data.DashRequested = true;
            data.DashRequestFromGround = data.IsGrounded;
            data.DashDirection = DetermineDashDirection(data);
        }

        private static int DetermineDashDirection(PlayerMovementRuntimeData data)
        {
            if (data == null)
            {
                return 0;
            }

            if (Mathf.Abs(data.MoveInput.x) > 0.01f)
            {
                return (int)Mathf.Sign(data.MoveInput.x);
            }

            return data.IsFacingRight ? 1 : -1;
        }

        private bool CanDash(PlayerMovementRuntimeData data)
        {
            if (data == null)
            {
                return false;
            }

            if (data.IsDashing || data.DashStopTimer > 0f)
            {
                return false;
            }

            if (data.DashCooldownTimer > 0f)
            {
                return false;
            }

            if (data.IsGrounded)
            {
                return true;
            }

            if (_context?.Stats == null)
            {
                return false;
            }

            if (_context.Stats.DashAirDashLimit <= 0)
            {
                return false;
            }

            if (data.AirDashCount >= _context.Stats.DashAirDashLimit)
            {
                return false;
            }

            return data.AirDashCooldownTimer <= 0f;
        }

        private void SubscribeToInput()
        {
            if (_inputSubscribed)
            {
                return;
            }

            InputManager.RunPressed += HandleRunPressed;
            InputManager.RunReleased += HandleRunReleased;
            _inputSubscribed = true;
        }

        private void UnsubscribeFromInput()
        {
            if (!_inputSubscribed)
            {
                return;
            }

            InputManager.RunPressed -= HandleRunPressed;
            InputManager.RunReleased -= HandleRunReleased;
            _inputSubscribed = false;
        }

        private void HandleRunPressed()
        {
            EvaluateRunTap(Time.time);
        }

        private void HandleRunReleased()
        {
            _runTapCount = Mathf.Clamp(_runTapCount, 0, 1);
        }

        private void ResetTapTracking()
        {
            _lastRunPressedTime = float.MinValue;
            _runTapCount = 0;
        }

        private void ResetRuntimeFlags()
        {
            var data = _context?.RuntimeData;
            if (data == null)
            {
                return;
            }

            data.DashRequested = false;
            data.DashRequestFromGround = false;
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
        }
    }
}
