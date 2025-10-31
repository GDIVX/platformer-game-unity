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
            Context.IsJumping = true;
            Context.IsFalling = false;
        }

        public override void HandleInput()
        {
            if (Context.JumpReleased)
            {
                Context.AttemptJumpCut();
            }

            if (Context.JumpBufferTimer > 0f && Context.JumpsCount < Context.Stats.NumberOfJumpsAllowed)
            {
                Context.IsFastFalling = false;
                Context.InitiateJump(1);
                return;
            }

            if (Context.IsFastFalling)
            {
                StateMachine.ChangeState<FastFallingState>();
                return;
            }

            if (Context.VerticalVelocity < 0f)
            {
                StateMachine.ChangeState<FallingState>();
            }
        }

        public override void Tick()
        {
            if (Context.BumpedHead)
            {
                if (!Context.TryEdgeNudge())
                {
                    Context.IsFastFalling = true;
                }
            }
        }

        public override void FixedTick()
        {
            Context.ApplyHorizontalMovement(Context.Stats.AirAcceleration, Context.Stats.AirDeceleration);

            if (Context.VerticalVelocity >= 0f)
            {
                Context.HandleJumpAscent();
            }
            else if (!Context.IsFastFalling)
            {
                Context.VerticalVelocity += Context.Stats.Gravity * Context.Stats.GravityOnReleaseMultiplier *
                                            Time.fixedDeltaTime;
            }
            else if (Context.VerticalVelocity < 0f)
            {
                Context.IsFalling = true;
            }

            Context.ClampVerticalVelocity();
            Context.ApplyVerticalVelocity();

            if (Context.IsGrounded && Context.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
                return;
            }

            if (Context.IsFastFalling)
            {
                StateMachine.ChangeState<FastFallingState>();
            }
            else if (Context.VerticalVelocity < 0f)
            {
                StateMachine.ChangeState<FallingState>();
            }
        }
    }
}
