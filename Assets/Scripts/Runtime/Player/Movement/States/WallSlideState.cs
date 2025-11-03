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
            Context.AirTime = 0;
        }

        public override void HandleInput()
        {
            if (Context.JumpBufferTimer > 0f && Context.WallDirection != 0)
            {
                bool longJump = Context.WantsToMoveAwayFromWall;
                Context.PerformWallJump(longJump);
                StateMachine.ChangeState<JumpingState>();
                return;
            }


            // 🔽 If can’t keep sliding, transition out
            if (!Context.CanContinueWallSlide())
            {
                if (Context.IsFastFalling)
                    StateMachine.ChangeState<FastFallingState>();
                else
                    StateMachine.ChangeState<FallingState>();
                return;
            }

            // 🟩 Ground contact check
            if (Context.IsGrounded)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }

        public override void FixedTick()
        {
            // 🔁 If can’t keep sliding, bail out early
            if (!Context.CanContinueWallSlide())
            {
                if (Context.IsGrounded)
                    StateMachine.ChangeState<GroundedState>();
                else if (Context.IsFastFalling)
                    StateMachine.ChangeState<FastFallingState>();
                else
                    StateMachine.ChangeState<FallingState>();

                return;
            }

            // 📉 Sliding logic
            var settings = Context.Stats.WallSlide;
            if (settings == null)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            float fixedDeltaTime = Time.fixedDeltaTime;
            Context.ApplyWallSlideHorizontal(settings, fixedDeltaTime);
            Context.ApplyWallSlideVertical(settings, fixedDeltaTime);
            Context.ApplyVerticalVelocity();
        }
    }
}
