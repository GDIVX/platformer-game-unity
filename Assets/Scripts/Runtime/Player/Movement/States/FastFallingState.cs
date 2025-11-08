using UnityEngine;

namespace Runtime.Player.Movement.States
{
    public class FastFallingState : PlayerMovementStateBase
    {
        public FastFallingState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void OnEnter()
        {
            var data = Context.RuntimeData;
            data.IsFastFalling = true;
            data.IsJumping = false;
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

            if (Context.Wall.ShouldStartWallSlide())
            {
                data.IsFastFalling = false;
                StateMachine.ChangeState<WallSlideState>();
                return;
            }

            if (data.JumpHeld &&
                data.JumpsCount >= Context.Stats.NumberOfJumpsAllowed &&
                data.FlightTimeRemaining > 0f &&
                data.FlightHangTimer <= 0f &&
                StateMachine.GetState<FlyState>() != null)
            {
                data.IsFastFalling = false;
                StateMachine.ChangeState<FlyState>();
                return;
            }

            if (data.JumpBufferTimer > 0f && data.JumpsCount < Context.Stats.NumberOfJumpsAllowed)
            {
                data.IsFastFalling = false;
                Context.Jump.InitiateJump(1);
                StateMachine.ChangeState<JumpingState>();
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

            var data = Context.RuntimeData;

            if (Context.Wall.ShouldStartWallSlide())
            {
                data.IsFastFalling = false;
                StateMachine.ChangeState<WallSlideState>();
                return;
            }

            Context.Jump.ApplyFastFall(fixedDeltaTime);
            Context.Jump.ClampVerticalVelocity();
            Context.Jump.ApplyVerticalVelocity();

            if (data.IsGrounded && data.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }
    }
}
