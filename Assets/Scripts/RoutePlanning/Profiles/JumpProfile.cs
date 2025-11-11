using Runtime.Player.Movement;
using Runtime.Player.Movement.Math;
using UnityEngine;

namespace RoutePlanning.Profiles
{
    [CreateAssetMenu(menuName = "Route Planning/Profiles/Jump Profile", fileName = "JumpProfile")]
    public class JumpProfile : MoveProfile
    {
        [SerializeField, Range(-1f, 1f)] private float _horizontalInput = 0f;
        [SerializeField] private bool _runHeld = false;
        [SerializeField] private bool _stopOnCollision = true;
        [SerializeField] private LayerMask _collisionMask;
        [SerializeField, Min(1)] private int _maxSimulationSteps = 30;

        protected override bool TryEvaluateInternal(Vector3 startPosition, Vector3 endPosition,
            PlayerStateSnapshot workingState, out MoveEvaluation evaluation, out string error)
        {
            var trajectory = MovementMathUtility.CreateJumpTrajectory(MovementStats, startPosition, workingState,
                _horizontalInput, _runHeld, Mathf.Max(_maxSimulationSteps, TrajectorySamples), _collisionMask,
                _stopOnCollision, out var finalVelocity, out var collisionIndex);

            Vector2 adjustedVelocity = finalVelocity;
            if (_stopOnCollision && collisionIndex.HasValue)
            {
                adjustedVelocity = Vector2.zero;
            }

            var finalState = workingState.WithVelocity(adjustedVelocity);
            float duration = Time.fixedDeltaTime * Mathf.Max(0, trajectory.Count - 1);
            evaluation = new MoveEvaluation(finalState, trajectory, duration, collisionIndex);
            error = string.Empty;
            return true;
        }
    }
}
