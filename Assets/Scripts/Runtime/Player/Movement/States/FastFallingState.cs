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
            Context.IsFalling = true;
        }

        public override void HandleInput()
        {
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
            Context.ApplyFastFall();
            Context.ClampVerticalVelocity();
            Context.ApplyVerticalVelocity();

            if (Context.IsGrounded && Context.VerticalVelocity <= 0f)
            {
                StateMachine.ChangeState<GroundedState>();
            }
        }
    }
}
