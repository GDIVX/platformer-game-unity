using UnityEngine;

namespace Runtime.Player.Movement.States
{
    public class WallSlideState : PlayerMovementStateBase
    {
        private const float JumpBufferIncreaseTolerance = 0.0001f;

        private bool _hasPendingWallJump;
        private float _pendingWallJumpWaitTimer;
        private float _lastJumpBufferTime;

        public WallSlideState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void OnEnter()
        {
            Context.IsWallSliding = true;
            Context.IsFalling = true;
            Context.IsFastFalling = false;
            Context.IsJumping = false;

            ResetPendingWallJump();
        }

        public override void OnExit()
        {
            Context.IsWallSliding = false;

            ResetPendingWallJump();
        }

        public override void HandleInput()
        {
            if (TryHandleBufferedWallJump())
            {
                return;
            }


            // 🔽 If can’t keep sliding, transition out
            if (!Context.CanContinueWallSlide())
            {
                if (Context.IsFastFalling)
                    StateMachine.ChangeState<FastFallingState>();
                else
                    StateMachine.ChangeState<FallingState>();
                return;
            }

            // 🟩 Ground contact check
            if (Context.IsGrounded)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }

        public override void FixedTick()
        {
            // 🔁 If can’t keep sliding, bail out early
            if (!Context.CanContinueWallSlide())
            {
                if (Context.IsGrounded)
                    StateMachine.ChangeState<GroundedState>();
                else if (Context.IsFastFalling)
                    StateMachine.ChangeState<FastFallingState>();
                else
                    StateMachine.ChangeState<FallingState>();

                return;
            }

            // 📉 Sliding logic
            var settings = Context.Stats.WallSlide;
            if (settings == null)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            float fixedDeltaTime = Time.fixedDeltaTime;
            Context.ApplyWallSlideHorizontal(settings, fixedDeltaTime);
            Context.ApplyWallSlideVertical(settings, fixedDeltaTime);
            Context.ApplyVerticalVelocity();
        }

        private bool TryHandleBufferedWallJump()
        {
            bool hasWall = Context.WallDirection != 0;
            bool jumpBuffered = Context.JumpBufferTimer > 0f;
            bool awayIntent = Context.WantsToMoveAwayFromWall || Context.DirectionBufferTimer > 0f;

            if (!hasWall)
            {
                ResetPendingWallJump();
                return false;
            }

            if (jumpBuffered && awayIntent)
            {
                PerformBufferedWallJump(true);
                return true;
            }

            if (!_hasPendingWallJump)
            {
                if (!jumpBuffered)
                {
                    return false;
                }

                StartPendingWallJump();
            }
            else if (jumpBuffered && Context.JumpBufferTimer > _lastJumpBufferTime + JumpBufferIncreaseTolerance)
            {
                StartPendingWallJump();
            }

            if (awayIntent)
            {
                PerformBufferedWallJump(true);
                return true;
            }

            float consumedBuffer = Mathf.Max(0f, _lastJumpBufferTime - Context.JumpBufferTimer);
            if (consumedBuffer > 0f || !jumpBuffered)
            {
                _pendingWallJumpWaitTimer = Mathf.Max(0f, _pendingWallJumpWaitTimer - consumedBuffer);
            }

            _lastJumpBufferTime = Context.JumpBufferTimer;

            if (_pendingWallJumpWaitTimer <= 0f || !jumpBuffered)
            {
                PerformBufferedWallJump(false);
                return true;
            }

            return false;
        }

        private void StartPendingWallJump()
        {
            _hasPendingWallJump = true;
            _lastJumpBufferTime = Context.JumpBufferTimer;

            float waitWindow = Mathf.Max(0f, Context.Stats.DirectionBufferDuration);
            _pendingWallJumpWaitTimer = Mathf.Min(waitWindow, Context.JumpBufferTimer);
        }

        private void PerformBufferedWallJump(bool isLong)
        {
            ResetPendingWallJump();
            Context.PerformWallJump(isLong);
            StateMachine.ChangeState<JumpingState>();
        }

        private void ResetPendingWallJump()
        {
            _hasPendingWallJump = false;
            _pendingWallJumpWaitTimer = 0f;
            _lastJumpBufferTime = 0f;
        }
    }
}
