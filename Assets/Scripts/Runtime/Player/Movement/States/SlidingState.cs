using UnityEngine;

namespace Runtime.Player.Movement.States
{
    public class SlidingState : PlayerMovementStateBase
    {
        public SlidingState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void HandleInput()
        {
            if (!Context.IsGrounded)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            if (Context.MoveInput != Vector2.zero)
            {
                StateMachine.ChangeState<GroundedState>();
                return;
            }

            if (Context.JumpBufferTimer > 0f)
            {
                if (Context.JumpReleasedDuringBuffer)
                {
                    Context.FastFallReleaseSpeed = Context.VerticalVelocity;
                }

                Context.InitiateJump(1);
                StateMachine.ChangeState<JumpingState>();
                return;
            }

            if (!Context.ShouldSlide())
            {
                StateMachine.ChangeState<GroundedState>();
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
