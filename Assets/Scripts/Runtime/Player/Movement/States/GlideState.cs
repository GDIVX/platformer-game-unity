using Runtime.Player.Movement;
using UnityEngine;

namespace Runtime.Player.Movement.States
{
    public class GlideState : PlayerMovementStateBase
    {
        public GlideState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void OnEnter()
        {
            var data = Context.RuntimeData;
            var glideData = data.Glide;
            glideData.IsGliding = true;
            glideData.ElapsedTime = 0f;
            data.IsFalling = true;
            Context.RaiseGlideStarted();
        }

        public override void OnExit()
        {
            var data = Context.RuntimeData;
            var glideData = data.Glide;

            if (!glideData.IsGliding)
            {
                return;
            }

            glideData.Reset();
            Context.RaiseGlideEnded();
        }

        public override void HandleInput()
        {
            var data = Context.RuntimeData;

            if (data.JumpBufferTimer > 0f)
            {
                if (data.CoyoteTimer > 0f && data.JumpsCount == 0)
                {
                    if (data.JumpReleasedDuringBuffer)
                    {
                        data.FastFallReleaseSpeed = data.VerticalVelocity;
                    }

                    data.IsFastFalling = false;
                    Context.Jump.InitiateJump(1);
                    StateMachine.ChangeState<JumpingState>();
                    return;
                }

                if (data.JumpsCount == 0 && Context.Stats.NumberOfJumpsAllowed >= 2)
                {
                    data.IsFastFalling = false;
                    Context.Jump.InitiateJump(2);
                    StateMachine.ChangeState<JumpingState>();
                    return;
                }

                if (data.JumpsCount > 0 && data.JumpsCount < Context.Stats.NumberOfJumpsAllowed)
                {
                    data.IsFastFalling = false;
                    Context.Jump.InitiateJump(1);
                    StateMachine.ChangeState<JumpingState>();
                    return;
                }
            }

            if (Context.Wall.ShouldStartWallSlide())
            {
                StateMachine.ChangeState<WallSlideState>();
                return;
            }

            if (!data.JumpHeld)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            if (data.IsFastFalling)
            {
                StateMachine.ChangeState<FastFallingState>();
                return;
            }

            if (data.IsGrounded && data.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }

        public override void FixedTick()
        {
            float fixedDeltaTime = Time.fixedDeltaTime;
            var data = Context.RuntimeData;
            var glideData = data.Glide;

            float acceleration = glideData.Acceleration > 0f
                ? glideData.Acceleration
                : Context.Stats.AirAcceleration;
            float deceleration = glideData.Deceleration > 0f
                ? glideData.Deceleration
                : Context.Stats.AirDeceleration;

            Context.Horizontal.ApplyMovement(
                acceleration,
                deceleration,
                fixedDeltaTime);

            Context.Jump.ApplyFall(fixedDeltaTime);

            float multiplier = glideData.FallSpeedMultiplier > 0f
                ? glideData.FallSpeedMultiplier
                : 1f;
            float maxFallSpeed = Context.Stats.MaxFallSpeed * multiplier;
            float maxRiseSpeed = Context.Stats.MaxRiseSpeed;
            data.VerticalVelocity = Mathf.Clamp(data.VerticalVelocity, -maxFallSpeed, maxRiseSpeed);
            Context.Jump.ApplyVerticalVelocity();

            glideData.ElapsedTime += fixedDeltaTime;

            if (Context.Wall.ShouldStartWallSlide())
            {
                StateMachine.ChangeState<WallSlideState>();
                return;
            }

            if (glideData.MaxDuration > 0f && glideData.ElapsedTime >= glideData.MaxDuration)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            if (!data.JumpHeld)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            if (data.IsFastFalling)
            {
                StateMachine.ChangeState<FastFallingState>();
                return;
            }

            if (data.IsGrounded && data.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }
    }
}
