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
            var data = Context.RuntimeData;

            if (StateMachine.PreviousState != null &&
                StateMachine.PreviousState is not GroundedState and not SlidingState)
            {
                Context.Jump.ApplyLanding();
            }

            data.IsJumping = false;
            data.IsFalling = false;
            data.IsFastFalling = false;
        }

        public override void HandleInput()
        {
            var data = Context.RuntimeData;

            if (data.JumpBufferTimer > 0f && (data.IsGrounded || data.CoyoteTimer > 0f))
            {
                if (data.JumpReleasedDuringBuffer)
                {
                    data.FastFallReleaseSpeed = data.VerticalVelocity;
                }

                Context.Jump.InitiateJump(1);
                StateMachine.ChangeState<JumpingState>();
                return;
            }

            if (!data.IsGrounded)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            if (Context.Horizontal.ShouldSlide())
            {
                StateMachine.ChangeState<SlidingState>();
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

            if (Context.Stats.SlideMovement.maxIterations <= 0)
            {
                Context.Stats.SlideMovement.maxIterations = 50;
            }

            Context.Rigidbody.Slide(Context.RuntimeData.Velocity, fixedDeltaTime, Context.Stats.SlideMovement);
        }
    }
}