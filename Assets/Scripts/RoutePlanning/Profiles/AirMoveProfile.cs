using Runtime.Player.Movement;
using Runtime.Player.Movement.Math;
using UnityEngine;

namespace RoutePlanning.Profiles
{
    [CreateAssetMenu(menuName = "Route Planning/Profiles/Air Move Profile", fileName = "AirMoveProfile")]
    public class AirMoveProfile : MoveProfile
    {
        [SerializeField] private bool _useRunSpeed = true;
        [SerializeField, Range(-1f, 1f)] private float _horizontalInput = 1f;
        [SerializeField] private bool _preserveVerticalVelocity = false;

        protected override bool TryEvaluateInternal(Vector3 startPosition, Vector3 endPosition,
            PlayerStateSnapshot workingState, out MoveEvaluation evaluation, out string error)
        {
            float deltaX = endPosition.x - startPosition.x;
            float targetDirection = Mathf.Approximately(deltaX, 0f) ? Mathf.Sign(_horizontalInput) : Mathf.Sign(deltaX);
            float maxSpeed = _useRunSpeed ? MovementStats.MaxRunSpeed : MovementStats.MaxWalkSpeed;
            float acceleration = MovementStats.AirAcceleration;

            float finalVelocityX;
            float duration = MovementMathUtility.EstimateGroundTravelTime(Mathf.Abs(deltaX), workingState.Velocity.x,
                maxSpeed, acceleration, out finalVelocityX);
            finalVelocityX = Mathf.Abs(finalVelocityX) * (targetDirection == 0f ? 1f : targetDirection);

            Vector2 finalVelocity = workingState.Velocity;
            finalVelocity.x = Mathf.Lerp(finalVelocity.x, finalVelocityX, 0.75f);

            if (!_preserveVerticalVelocity)
            {
                float gravityStep = MovementStats.Gravity * duration;
                finalVelocity.y = Mathf.Clamp(workingState.Velocity.y + gravityStep, -MovementStats.MaxFallSpeed,
                    MovementStats.MaxRiseSpeed);
            }

            var finalState = workingState.WithVelocity(finalVelocity);
            var trajectory = MovementMathUtility.CreateLinearTrajectory(startPosition, endPosition, TrajectorySamples);

            evaluation = new MoveEvaluation(finalState, trajectory, duration);
            error = string.Empty;
            return true;
        }
    }
}
