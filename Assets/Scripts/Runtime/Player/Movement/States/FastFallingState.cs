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
            Context.IsFastFalling = true;
            Context.IsJumping = false;
            Context.NotifyFallStarted();
            Context.InvokeFallEvent();
        }

        public override void HandleInput()
        {
            if (Context.ShouldStartWallSlide())
            {
                Context.IsFastFalling = false;
                StateMachine.ChangeState<WallSlideState>();
                return;
            }

            if (Context.JumpBufferTimer > 0f && Context.JumpsCount < Context.Stats.NumberOfJumpsAllowed)
            {
                Context.IsFastFalling = false;
                Context.InitiateJump(1);
                StateMachine.ChangeState<JumpingState>();
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

            if (Context.ShouldStartWallSlide())
            {
                Context.IsFastFalling = false;
                StateMachine.ChangeState<WallSlideState>();
                return;
            }

            Context.ApplyFastFall(Time.fixedDeltaTime);
            Context.ClampVerticalVelocity();
            Context.ApplyVerticalVelocity();

            if (Context.IsGrounded && Context.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }
    }
}
