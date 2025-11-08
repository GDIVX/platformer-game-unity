using UnityEngine;

namespace Runtime.Player.Movement.States
{
    public class JumpingState : PlayerMovementStateBase
    {
        public JumpingState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            : base(context, stateMachine)
        {
        }

        public override void OnEnter()
        {
            var data = Context.RuntimeData;
            data.IsJumping = true;
            data.IsFalling = false;
        }

        public override void HandleInput()
        {
            var data = Context.RuntimeData;

            if (TryEnterDashState())
            {
                return;
            }

            if (data.JumpReleased)
            {
                Context.Jump.AttemptJumpCut();
            }

            if (data.JumpBufferTimer > 0f && data.JumpsCount < Context.Stats.NumberOfJumpsAllowed)
            {
                data.IsFastFalling = false;
                Context.Jump.InitiateJump(1);
                return;
            }

            if (data.IsFastFalling)
            {
                StateMachine.ChangeState<FastFallingState>();
                return;
            }

            if (data.VerticalVelocity < 0f)
            {
                StateMachine.ChangeState<FallingState>();
            }
        }

        public override void Tick()
        {
            var data = Context.RuntimeData;

            if (data.BumpedHead)
            {
                if (!Context.Jump.TryEdgeNudge())
                {
                    data.IsFastFalling = true;
                }
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

            if (data.VerticalVelocity >= 0f)
            {
                Context.Jump.HandleJumpAscent(fixedDeltaTime);
            }
            else if (!data.IsFastFalling)
            {
                data.VerticalVelocity += Context.Stats.Gravity * Context.Stats.GravityOnReleaseMultiplier *
                                         fixedDeltaTime;
            }
            else if (data.VerticalVelocity < 0f)
            {
                data.IsFalling = true;
            }

            Context.Jump.ClampVerticalVelocity();
            Context.Jump.ApplyVerticalVelocity();

            if (data.IsGrounded && data.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
                return;
            }

            if (data.IsFastFalling)
            {
                StateMachine.ChangeState<FastFallingState>();
            }
            else if (data.VerticalVelocity < 0f)
            {
                StateMachine.ChangeState<FallingState>();
            }
        }
    }
}
