using UnityEngine;

namespace Runtime.Player.Movement.States
{
    public class DashState : PlayerMovementStateBase
    {
        private int _dashDirection = 1;
        private bool _stopPhaseStarted;
        private bool _dashFinished;

        public DashState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void OnEnter()
        {
            var data = Context.RuntimeData;

            _dashDirection = data?.DashDirection ?? 0;
            if (_dashDirection == 0)
            {
                _dashDirection = data != null && data.IsFacingRight ? 1 : -1;
            }

            _stopPhaseStarted = false;
            _dashFinished = false;

            if (data != null)
            {
                data.IsDashing = true;
                data.DashRequested = false;
                data.DashTimer = Context.Stats.DashDuration;
                data.DashStopTimer = 0f;
                data.VerticalVelocity = 0f;

                if (data.DashRequestFromGround)
                {
                    data.DashCooldownTimer = Context.Stats.DashGroundCooldown;
                    data.AirDashCount = 0;
                    data.AirDashCooldownTimer = 0f;
                }
                else
                {
                    data.AirDashCount++;
                    data.AirDashCooldownTimer = Context.Stats.DashAirDashCooldown;
                }

                data.DashRequestFromGround = false;
            }

            ApplyDashVelocity();
            Context.RaiseDashStarted();
        }

        public override void OnExit()
        {
            var data = Context.RuntimeData;
            if (data != null)
            {
                data.IsDashing = false;
                data.DashTimer = 0f;
                data.DashStopTimer = 0f;
            }

            Context.RaiseDashEnded();
        }

        public override void Tick()
        {
            var data = Context.RuntimeData;
            if (data == null)
            {
                return;
            }

            if (data.DashTimer > 0f)
            {
                return;
            }

            if (!_stopPhaseStarted)
            {
                data.DashStopTimer = Context.Stats.DashPostDashStopDuration;
                _stopPhaseStarted = true;
            }

            if (_dashFinished || data.DashStopTimer > 0f)
            {
                return;
            }

            _dashFinished = true;
            FinishDash();
        }

        public override void FixedTick()
        {
            var data = Context.RuntimeData;
            if (data == null)
            {
                return;
            }

            if (data.DashTimer > 0f)
            {
                ApplyDashVelocity();
            }
            else
            {
                HaltHorizontalVelocity();
            }
        }

        private void ApplyDashVelocity()
        {
            var rb = Context.Rigidbody;
            if (rb == null)
            {
                return;
            }

            float speed = Context.Stats.DashForwardBurstSpeed * _dashDirection;
            Vector2 velocity = new Vector2(speed, 0f);
            Context.RuntimeData.VerticalVelocity = 0;
            rb.linearVelocity = velocity;

            var data = Context.RuntimeData;
            if (data != null)
            {
                data.Velocity = velocity;
                data.VerticalVelocity = 0f;
            }
        }

        private void HaltHorizontalVelocity()
        {
            var rb = Context.Rigidbody;
            if (rb == null)
            {
                return;
            }

            Vector2 velocity = rb.linearVelocity;
            velocity.x = 0f;
            rb.linearVelocity = velocity;

            var data = Context.RuntimeData;
            if (data != null)
            {
                data.Velocity = new Vector2(0f, data.Velocity.y);
            }
        }

        private void FinishDash()
        {
            HaltHorizontalVelocity();

            var data = Context.RuntimeData;
            if (data == null)
            {
                return;
            }

            if (data.IsGrounded)
            {
                StateMachine.ChangeState<GroundedState>();
                return;
            }

            if (data.IsFastFalling)
            {
                StateMachine.ChangeState<FastFallingState>();
                return;
            }

            if (data.VerticalVelocity > 0f)
            {
                StateMachine.ChangeState<JumpingState>();
                return;
            }

            StateMachine.ChangeState<FallingState>();
        }
    }
}