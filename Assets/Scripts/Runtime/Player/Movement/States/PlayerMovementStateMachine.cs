using System;
using System.Collections.Generic;

namespace Runtime.Player.Movement.States
{
    public class PlayerMovementStateMachine
    {
        private readonly List<IPlayerMovementState> _registeredStates = new List<IPlayerMovementState>();
        private readonly Dictionary<Type, IPlayerMovementState> _stateLookup = new Dictionary<Type, IPlayerMovementState>();

        public PlayerMovementStateMachine(PlayerMovementContext context)
        {
            Context = context;
        }

        public IPlayerMovementState CurrentState { get; private set; }
        public IPlayerMovementState PreviousState { get; private set; }
        public PlayerMovementContext Context { get; }

        public IReadOnlyList<IPlayerMovementState> RegisteredStates => _registeredStates;

        public bool RegisterState(IPlayerMovementState state)
        {
            if (state == null)
            {
                return false;
            }

            var type = state.GetType();
            if (_stateLookup.ContainsKey(type))
            {
                return false;
            }

            _registeredStates.Add(state);
            _stateLookup[type] = state;
            return true;
        }

        public bool RegisterStates(IEnumerable<IPlayerMovementState> states)
        {
            if (states == null)
            {
                return false;
            }

            bool anyRegistered = false;
            foreach (var state in states)
            {
                anyRegistered |= RegisterState(state);
            }

            return anyRegistered;
        }

        public bool UnregisterState<TState>() where TState : class, IPlayerMovementState
        {
            return UnregisterState(typeof(TState));
        }

        public bool UnregisterState(Type stateType)
        {
            if (stateType == null)
            {
                return false;
            }

            if (!_stateLookup.TryGetValue(stateType, out var state))
            {
                return false;
            }

            if (ReferenceEquals(CurrentState, state))
            {
                CurrentState.OnExit();
                CurrentState = null;
            }

            if (ReferenceEquals(PreviousState, state))
            {
                PreviousState = null;
            }

            _stateLookup.Remove(stateType);
            _registeredStates.Remove(state);
            return true;
        }

        public void ClearStates()
        {
            _registeredStates.Clear();
            _stateLookup.Clear();
            CurrentState = null;
            PreviousState = null;
        }

        public void Initialize<TState>() where TState : class, IPlayerMovementState
        {
            CurrentState = GetState<TState>();
            CurrentState?.OnEnter();
        }

        public void ChangeState<TState>() where TState : class, IPlayerMovementState
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
            return _stateLookup.TryGetValue(typeof(TState), out var state) ? state as TState : null;
        }
    }
}
