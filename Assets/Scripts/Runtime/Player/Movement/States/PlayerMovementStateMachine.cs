using System;
using System.Collections.Generic;

namespace Runtime.Player.Movement.States
{
    public class PlayerMovementStateMachine
    {
        private readonly Dictionary<Type, IPlayerMovementState> _states;

        public PlayerMovementStateMachine(PlayerMovementContext context)
        {
            _states = new Dictionary<Type, IPlayerMovementState>
            {
                { typeof(GroundedState), new GroundedState(context, this) },
                { typeof(SlidingState), new SlidingState(context, this) },
                { typeof(JumpingState), new JumpingState(context, this) },
                { typeof(FallingState), new FallingState(context, this) },
                { typeof(FastFallingState), new FastFallingState(context, this) }
            };
        }

        public IPlayerMovementState CurrentState { get; private set; }
        public IPlayerMovementState PreviousState { get; private set; }

        public void Initialize<TState>() where TState : IPlayerMovementState
        {
            CurrentState = GetState<TState>();
            CurrentState?.OnEnter();
        }

        public void ChangeState<TState>() where TState : IPlayerMovementState
        {
            var nextState = GetState<TState>();
            if (nextState == null || ReferenceEquals(nextState, CurrentState))
            {
                return;
            }

            CurrentState?.OnExit();
            PreviousState = CurrentState;
            CurrentState = nextState;
            CurrentState.OnEnter();
        }

        public void HandleInput()
        {
            CurrentState?.HandleInput();
        }

        public void Tick()
        {
            CurrentState?.Tick();
        }

        public void FixedTick()
        {
            CurrentState?.FixedTick();
        }

        public TState GetState<TState>() where TState : class, IPlayerMovementState
        {
            return _states.TryGetValue(typeof(TState), out var state) ? state as TState : null;
        }
    }
}
