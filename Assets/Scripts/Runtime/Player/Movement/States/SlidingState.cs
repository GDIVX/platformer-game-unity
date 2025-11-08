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
            var data = Context.RuntimeData;

            if (!data.IsGrounded)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            if (data.MoveInput != Vector2.zero)
            {
                StateMachine.ChangeState<GroundedState>();
                return;
            }

            if (data.JumpBufferTimer > 0f)
            {
                if (data.JumpReleasedDuringBuffer)
                {
                    data.FastFallReleaseSpeed = data.VerticalVelocity;
                }

                Context.Jump.InitiateJump(1);
                StateMachine.ChangeState<JumpingState>();
                return;
            }

            if (!Context.Horizontal.ShouldSlide())
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }

        public override void FixedTick()
        {
            float fixedDeltaTime = Time.fixedDeltaTime;
            Context.Horizontal.ApplyMovement(
                Context.Stats.GroundAcceleration,
                Context.Stats.GroundDeceleration,
                fixedDeltaTime);
            Context.Jump.ClampVerticalVelocity();
            Context.Jump.ApplyVerticalVelocity();

            Context.Rigidbody.Slide(Context.RuntimeData.Velocity, fixedDeltaTime, Context.Stats.SlideMovement);
        }
    }
}
