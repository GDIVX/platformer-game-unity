using System;
using System.Collections.Generic;
using Runtime.Player.Movement.States;

namespace Runtime.Player.Movement.Abilities
{
    public interface IMovementAbility
    {
        void Initialize(PlayerMovementContext context, PlayerMovementStateMachine stateMachine);
        IEnumerable<IPlayerMovementState> CreateStates(PlayerMovementContext context, PlayerMovementStateMachine stateMachine);
        IEnumerable<IPlayerMovementModifier> CreateModifiers(PlayerMovementContext context);
        IEnumerable<Func<PlayerMovementContext, bool>> CreateActivationConditions(PlayerMovementContext context);
        void OnAbilityEnabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine);
        void OnAbilityDisabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine);
    }
}
