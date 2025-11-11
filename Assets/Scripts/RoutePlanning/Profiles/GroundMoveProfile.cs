using Runtime.Player.Movement;
using Runtime.Player.Movement.Math;
using UnityEngine;

namespace RoutePlanning.Profiles
{
    [CreateAssetMenu(menuName = "Route Planning/Profiles/Ground Move Profile", fileName = "GroundMoveProfile")]
    public class GroundMoveProfile : MoveProfile
    {
        [SerializeField] private bool _useRunSpeed = true;

        protected override bool TryEvaluateInternal(Vector3 startPosition, Vector3 endPosition,
            PlayerStateSnapshot workingState, out MoveEvaluation evaluation, out string error)
        {
            float deltaX = endPosition.x - startPosition.x;
            float maxSpeed = _useRunSpeed ? MovementStats.MaxRunSpeed : MovementStats.MaxWalkSpeed;
            float acceleration = MovementStats.GroundAcceleration;

            float finalVelocityX;
            float duration = MovementMathUtility.EstimateGroundTravelTime(Mathf.Abs(deltaX), workingState.Velocity.x,
                maxSpeed, acceleration, out finalVelocityX);
            finalVelocityX *= Mathf.Sign(deltaX == 0f ? workingState.Velocity.x : deltaX);

            var finalState = MovementMathUtility.ApplyHorizontalVelocity(workingState, finalVelocityX);
            var trajectory = MovementMathUtility.CreateLinearTrajectory(startPosition, endPosition, TrajectorySamples);

            evaluation = new MoveEvaluation(finalState, trajectory, duration);
            error = string.Empty;
            return true;
        }
    }
}
