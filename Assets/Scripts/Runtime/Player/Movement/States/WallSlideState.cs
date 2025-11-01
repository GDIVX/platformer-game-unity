namespace Runtime.Player.Movement.States
{
    public class WallSlideState : PlayerMovementStateBase
    {
        public WallSlideState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void OnEnter()
        {
            Context.IsWallSliding = true;
            Context.IsFalling = true;
            Context.IsFastFalling = false;
            Context.IsJumping = false;
        }

        public override void OnExit()
        {
            Context.IsWallSliding = false;
        }

        public override void HandleInput()
        {
            if (Context.JumpBufferTimer > 0f && Context.WallDirection != 0)
            {
                Context.PerformWallJump();
                StateMachine.ChangeState<JumpingState>();
                return;
            }

            if (!Context.CanContinueWallSlide())
            {
                if (Context.IsFastFalling)
                {
                    StateMachine.ChangeState<FastFallingState>();
                }
                else
                {
                    StateMachine.ChangeState<FallingState>();
                }

                return;
            }

            if (Context.IsGrounded)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }

        public override void FixedTick()
        {
            if (!Context.CanContinueWallSlide())
            {
                if (Context.IsGrounded)
                {
                    StateMachine.ChangeState<GroundedState>();
                }
                else if (Context.IsFastFalling)
                {
                    StateMachine.ChangeState<FastFallingState>();
                }
                else
                {
                    StateMachine.ChangeState<FallingState>();
                }

                return;
            }

            var settings = Context.Stats.WallSlide;
            if (settings == null)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            Context.ApplyWallSlideHorizontal(settings);
            Context.ApplyWallSlideVertical(settings);
            Context.ApplyVerticalVelocity();
        }
    }
}
