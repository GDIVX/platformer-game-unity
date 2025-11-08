namespace Runtime.Player.Movement.States
{
    public abstract class PlayerMovementStateBase : IPlayerMovementState
    {
        protected readonly PlayerMovementContext Context;
        protected readonly PlayerMovementStateMachine StateMachine;

        protected PlayerMovementStateBase(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            Context = context;
            StateMachine = stateMachine;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual void HandleInput()
        {
        }

        public virtual void Tick()
        {
        }

        public virtual void FixedTick()
        {
        }

        protected bool TryEnterDashState()
        {
            var data = Context?.RuntimeData;
            if (data == null || !data.DashRequested)
            {
                return false;
            }

            bool changed = StateMachine?.ChangeState(typeof(DashState)) ?? false;
            if (!changed && data != null)
            {
                data.DashRequested = false;
            }

            return changed;
        }
    }
}
