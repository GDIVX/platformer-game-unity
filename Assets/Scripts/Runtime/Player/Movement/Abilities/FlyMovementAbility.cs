using System;
using System.Collections.Generic;
using Runtime.Player.Movement.States;
using UnityEngine;

namespace Runtime.Player.Movement.Abilities
{
    [CreateAssetMenu(menuName = "Player/Movement/Abilities/Fly", fileName = "FlyMovementAbility")]

    public class FlyMovementAbility : MovementAbility
    {
        private FlyState _flyState;

        public override void Initialize(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
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

        public override IEnumerable<IPlayerMovementState> CreateStates(
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
            var data = context?.RuntimeData;
            if (data == null)
            {
                return;
            }

            context.SetFlightTimeRemaining(data.FlightTimeRemaining);
            data.FlightHangTimer = 0f;
        }

        public override void OnAbilityDisabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
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
