using System;
using System.Collections.Generic;
using Runtime.Player.Movement.States;
using UnityEngine;

namespace Runtime.Player.Movement.Abilities
{
    [Serializable]
    public class FlyMovementAbility : IMovementAbility
    {
        private FlyState _flyState;

        public void Initialize(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            if (context == null)
            {
                return;
            }

            var data = context.RuntimeData;
            if (data == null)
            {
                return;
            }

            context.RefillFlightTime();
            data.FlightHangTimer = 0f;
            data.IsFlying = false;
        }

        public IEnumerable<IPlayerMovementState> CreateStates(
            PlayerMovementContext context,
            PlayerMovementStateMachine stateMachine)
        {
            if (context == null || stateMachine == null)
            {
                _flyState = null;
                return Array.Empty<IPlayerMovementState>();
            }

            _flyState = new FlyState(context, stateMachine);
            return new[] { _flyState };
        }

        public IEnumerable<IPlayerMovementModifier> CreateModifiers(PlayerMovementContext context)
        {
            return Array.Empty<IPlayerMovementModifier>();
        }

        public IEnumerable<Func<PlayerMovementContext, bool>> CreateActivationConditions(PlayerMovementContext context)
        {
            return Array.Empty<Func<PlayerMovementContext, bool>>();
        }

        public void OnAbilityEnabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            if (context == null)
            {
                return;
            }

            var data = context.RuntimeData;
            if (data == null)
            {
                return;
            }

            context.SetFlightTimeRemaining(data.FlightTimeRemaining);
            data.FlightHangTimer = 0f;
        }

        public void OnAbilityDisabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            if (context == null)
            {
                return;
            }

            var data = context.RuntimeData;
            if (data == null)
            {
                return;
            }

            if (data.IsFlying && stateMachine != null)
            {
                if (ReferenceEquals(stateMachine.CurrentState, _flyState))
                {
                    stateMachine.ChangeState<FallingState>();
                }
            }

            data.IsFlying = false;
            if (context.Stats != null)
            {
                data.FlightHangTimer = context.Stats.FlyExitHangDuration;
            }
        }
    }
}
