using Runtime.Player.Movement;
using Runtime.Player.Movement.Math;
using UnityEngine;

namespace RoutePlanning.Profiles
{
    [CreateAssetMenu(menuName = "Route Planning/Profiles/Dash Profile", fileName = "DashProfile")]
    public class DashProfile : MoveProfile
    {
        [SerializeField] private bool _assumeGrounded = true;
        [SerializeField] private bool _respectDashCooldown = true;

        protected override bool CanExecute(PlayerStateSnapshot state, out string error)
        {
            if (!base.CanExecute(state, out error))
            {
                return false;
            }

            if (_respectDashCooldown)
            {
                if (state.DashCooldown > 0f)
                {
                    error = "Dash cooldown is active.";
                    return false;
                }

                if (!_assumeGrounded && state.AirDashCooldown > 0f)
                {
                    error = "Air dash cooldown is active.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        protected override bool TryEvaluateInternal(Vector3 startPosition, Vector3 endPosition,
            PlayerStateSnapshot workingState, out MoveEvaluation evaluation, out string error)
        {
            float deltaX = endPosition.x - startPosition.x;
            int direction;
            if (!Mathf.Approximately(deltaX, 0f))
            {
                direction = deltaX > 0f ? 1 : -1;
            }
            else if (!Mathf.Approximately(workingState.Velocity.x, 0f))
            {
                direction = workingState.Velocity.x > 0f ? 1 : -1;
            }
            else
            {
                direction = 1;
            }

            var finalState = MovementMathUtility.ApplyDash(workingState, MovementStats, _assumeGrounded, direction);
            float dashDistance = MovementStats.DashForwardBurstSpeed * MovementStats.DashDuration * direction;
            Vector3 target = Mathf.Approximately(deltaX, 0f)
                ? startPosition + new Vector3(dashDistance, 0f, 0f)
                : endPosition;

            var trajectory = MovementMathUtility.CreateLinearTrajectory(startPosition, target, TrajectorySamples);
            evaluation = new MoveEvaluation(finalState, trajectory, MovementStats.DashDuration);
            error = string.Empty;
            return true;
        }
    }
}
