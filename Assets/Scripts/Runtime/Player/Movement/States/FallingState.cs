namespace Runtime.Player.Movement.States
{
    public class FallingState : PlayerMovementStateBase
    {
        public FallingState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void OnEnter()
        {
            Context.IsFalling = true;
            Context.IsJumping = Context.JumpsCount > 0;
        }

        public override void HandleInput()
        {
            if (Context.JumpBufferTimer > 0f)
            {
                if (Context.CoyoteTimer > 0f && Context.JumpsCount == 0)
                {
                    if (Context.JumpReleasedDuringBuffer)
                    {
                        Context.FastFallReleaseSpeed = Context.VerticalVelocity;
                    }

                    Context.IsFastFalling = false;
                    Context.InitiateJump(1);
                    StateMachine.ChangeState<JumpingState>();
                    return;
                }

                if (Context.JumpsCount == 0 && Context.Stats.NumberOfJumpsAllowed >= 2)
                {
                    Context.IsFastFalling = false;
                    Context.InitiateJump(2);
                    StateMachine.ChangeState<JumpingState>();
                    return;
                }

                if (Context.JumpsCount > 0 && Context.JumpsCount < Context.Stats.NumberOfJumpsAllowed)
                {
                    Context.IsFastFalling = false;
                    Context.InitiateJump(1);
                    StateMachine.ChangeState<JumpingState>();
                    return;
                }
            }

            if (Context.ShouldStartWallSlide())
            {
                StateMachine.ChangeState<WallSlideState>();
                return;
            }

            if (Context.IsFastFalling)
            {
                StateMachine.ChangeState<FastFallingState>();
                return;
            }

            if (Context.IsGrounded && Context.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }

        public override void FixedTick()
        {
            Context.ApplyHorizontalMovement(Context.Stats.AirAcceleration, Context.Stats.AirDeceleration);
            Context.ApplyFall(Time.fixedDeltaTime);
            Context.ClampVerticalVelocity();
            Context.ApplyVerticalVelocity();

            if (Context.ShouldStartWallSlide())
            {
                StateMachine.ChangeState<WallSlideState>();
                return;
            }

            if (Context.IsGrounded && Context.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }
    }
}
