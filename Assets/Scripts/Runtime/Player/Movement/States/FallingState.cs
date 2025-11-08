using Runtime.Player.Movement;
using UnityEngine;

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
            var data = Context.RuntimeData;
            data.IsJumping = data.JumpsCount > 0;
            Context.Jump.NotifyFallStarted();
            Context.Jump.InvokeFallEvent();
        }

        public override void HandleInput()
        {
            var data = Context.RuntimeData;

            if (TryEnterDashState())
            {
                return;
            }

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

            if (ShouldStartGlide(data))
            {
                StateMachine.ChangeState<GlideState>();
            }
            
            if (data.JumpHeld &&
                data.JumpsCount >= Context.Stats.NumberOfJumpsAllowed &&
                data.FlightTimeRemaining > 0f &&
                data.FlightHangTimer <= 0f &&
                StateMachine.GetState<FlyState>() != null)
            {
                StateMachine.ChangeState<FlyState>();
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
            Context.Horizontal.ApplyMovement(
                Context.Stats.AirAcceleration,
                Context.Stats.AirDeceleration,
                fixedDeltaTime);
            Context.Jump.ApplyFall(fixedDeltaTime);
            Context.Jump.ClampVerticalVelocity();
            Context.Jump.ApplyVerticalVelocity();

            if (Context.Wall.ShouldStartWallSlide())
            {
                StateMachine.ChangeState<WallSlideState>();
                return;
            }

            var data = Context.RuntimeData;

            if (ShouldStartGlide(data))
            {
                StateMachine.ChangeState<GlideState>();
                return;
            }

            if (data.IsGrounded && data.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }

        private bool ShouldStartGlide(PlayerMovementRuntimeData data)
        {
            if (data == null)
            {
                return false;
            }

            if (!data.JumpHeld || data.IsGrounded || data.VerticalVelocity >= 0f || data.IsFastFalling)
            {
                return false;
            }

            var glideData = data.Glide;
            if (glideData == null)
            {
                return false;
            }

            return StateMachine.GetState<GlideState>() != null;
        }
    }
}
