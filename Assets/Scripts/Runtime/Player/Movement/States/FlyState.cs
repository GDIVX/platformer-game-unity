using UnityEngine;

namespace Runtime.Player.Movement.States
{
    public class FlyState : PlayerMovementStateBase
    {
        public FlyState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void OnEnter()
        {
            var data = Context.RuntimeData;
            data.IsFlying = true;
            data.IsFalling = false;
            data.IsFastFalling = false;
            data.FastFallTime = 0f;
            data.FlightHangTimer = 0f;
            if (data.VerticalVelocity < 0f)
            {
                data.VerticalVelocity = 0f;
            }

            Context.RaiseFlyStarted();
        }

        public override void OnExit()
        {
            var data = Context.RuntimeData;
            data.IsFlying = false;
            if (Context.Stats != null)
            {
                data.FlightHangTimer = Context.Stats.FlyExitHangDuration;
            }

            Context.RaiseFlyEnded();
        }

        public override void HandleInput()
        {
            var data = Context.RuntimeData;

            if (!data.JumpHeld || data.FlightTimeRemaining <= 0f)
            {
                StateMachine.ChangeState<FallingState>();
                return;
            }

            if (data.IsGrounded && data.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }

        public override void FixedTick()
        {
            float deltaTime = Time.fixedDeltaTime;
            var stats = Context.Stats;
            var data = Context.RuntimeData;

            float acceleration = stats != null && stats.FlyAirAccelerationOverride > 0f
                ? stats.FlyAirAccelerationOverride
                : stats != null ? stats.AirAcceleration : 0f;

            float deceleration = stats != null && stats.FlyAirDecelerationOverride > 0f
                ? stats.FlyAirDecelerationOverride
                : stats != null ? stats.AirDeceleration : 0f;

            Context.Horizontal.ApplyMovement(acceleration, deceleration, deltaTime);

            float gravity = stats != null ? stats.Gravity : 0f;
            float lift = stats != null ? stats.FlyLift : 0f;

            data.VerticalVelocity += gravity * deltaTime;
            data.VerticalVelocity += lift * deltaTime;

            if (data.BumpedHead && data.VerticalVelocity > 0f)
            {
                data.VerticalVelocity = 0f;
            }
            data.IsFalling = data.VerticalVelocity < 0f;
            data.IsFastFalling = false;

            Context.Jump.ClampVerticalVelocity();
            Context.Jump.ApplyVerticalVelocity();

            if (data.FlightTimeRemaining <= 0f)
            {
                StateMachine.ChangeState<FallingState>();
            }
            else if (data.IsGrounded && data.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }
    }
}
