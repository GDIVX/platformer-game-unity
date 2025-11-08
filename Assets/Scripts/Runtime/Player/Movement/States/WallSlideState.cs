using UnityEngine;

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
            var data = Context.RuntimeData;
            data.IsWallSliding = true;
            data.IsFalling = true;
            data.IsFastFalling = false;
            data.IsJumping = false;
        }

        public override void OnExit()
        {
            Context.RuntimeData.IsWallSliding = false;
        }

        public override void HandleInput()
        {
            var data = Context.RuntimeData;

            if (data.JumpBufferTimer > 0f && data.WallDirection != 0)
            {
                bool longJump = data.WantsToMoveAwayFromWall;
                Context.Jump.PerformWallJump(longJump);
                StateMachine.ChangeState<JumpingState>();
                return;
            }

            if (!Context.Wall.CanContinueWallSlide())
            {
                if (data.IsFastFalling)
                    StateMachine.ChangeState<FastFallingState>();
                else
                    StateMachine.ChangeState<FallingState>();
                return;
            }

            if (data.IsGrounded)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }

        public override void FixedTick()
        {
            var data = Context.RuntimeData;

            if (!Context.Wall.CanContinueWallSlide())
            {
                if (data.IsGrounded)
                    StateMachine.ChangeState<GroundedState>();
                else if (data.IsFastFalling)
                    StateMachine.ChangeState<FastFallingState>();
                else
                    StateMachine.ChangeState<FallingState>();

                return;
            }

            var settings = Context.Stats.WallSlide;
            if (settings == null)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            float fixedDeltaTime = Time.fixedDeltaTime;
            Context.Wall.ApplyWallSlideHorizontal(settings, fixedDeltaTime);
            Context.Wall.ApplyWallSlideVertical(settings, fixedDeltaTime);
            Context.Jump.ApplyVerticalVelocity();
        }
    }
}
