using UnityEngine;

namespace Runtime.Player.Movement.States
{
    public class GroundedState : PlayerMovementStateBase
    {
        public GroundedState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void OnEnter()
        {
            if (StateMachine.PreviousState != null &&
                StateMachine.PreviousState is not GroundedState and not SlidingState)
            {
                Context.ApplyLanding();
            }

            Context.IsJumping = false;
            Context.IsFalling = false;
            Context.IsFastFalling = false;
        }

        public override void HandleInput()
        {
            if (Context.JumpBufferTimer > 0f && (Context.IsGrounded || Context.CoyoteTimer > 0f))
            {
                if (Context.JumpReleasedDuringBuffer)
                {
                    Context.FastFallReleaseSpeed = Context.VerticalVelocity;
                }

                Context.InitiateJump(1);
                StateMachine.ChangeState<JumpingState>();
                return;
            }

            if (!Context.IsGrounded)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            if (Context.ShouldSlide())
            {
                StateMachine.ChangeState<SlidingState>();
            }
        }

        public override void FixedTick()
        {
            Context.ApplyHorizontalMovement(Context.Stats.GroundAcceleration, Context.Stats.GroundDeceleration);
            Context.ClampVerticalVelocity();
            Context.ApplyVerticalVelocity();
            Context.Rigidbody.Slide(Context.Velocity, Time.fixedDeltaTime, Context.Stats.SlideMovement);
        }
    }
}
