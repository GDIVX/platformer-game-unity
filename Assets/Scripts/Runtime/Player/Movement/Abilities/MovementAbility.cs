using System;
using System.Collections.Generic;
using Runtime.Player.Movement.States;
using UnityEngine;

namespace Runtime.Player.Movement.Abilities
{
    public abstract class MovementAbility : ScriptableObject, IMovementAbility
    {
        public abstract void Initialize(PlayerMovementContext context, PlayerMovementStateMachine stateMachine);

        public abstract IEnumerable<IPlayerMovementState> CreateStates(PlayerMovementContext context,
            PlayerMovementStateMachine stateMachine);

        public abstract IEnumerable<IPlayerMovementModifier> CreateModifiers(PlayerMovementContext context);

        public abstract IEnumerable<Func<PlayerMovementContext, bool>> CreateActivationConditions(
            PlayerMovementContext context);

        public abstract void OnAbilityEnabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine);
        public abstract void OnAbilityDisabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine);
    }
}