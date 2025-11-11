using Runtime.Player.Movement;
using Runtime.Player.Movement.Math;
using UnityEngine;

namespace RoutePlanning.Profiles
{
    [CreateAssetMenu(menuName = "Route Planning/Profiles/Glide Profile", fileName = "GlideProfile")]
    public class GlideProfile : MoveProfile
    {
        [SerializeField, Min(0f)] private float _duration = 0.75f;
        [SerializeField, Range(-1f, 1f)] private float _horizontalInput = 0f;

        protected override bool CanExecute(PlayerStateSnapshot state, out string error)
        {
            if (!base.CanExecute(state, out error))
            {
                return false;
            }

            var glide = MovementStats.Glide;
            if (glide != null && glide.LimitDuration && state.GlideTimeRemaining < _duration)
            {
                error = "Not enough glide time remaining.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        protected override bool TryEvaluateInternal(Vector3 startPosition, Vector3 endPosition,
            PlayerStateSnapshot workingState, out MoveEvaluation evaluation, out string error)
        {
            var trajectory = MovementMathUtility.CreateGlideTrajectory(MovementStats, startPosition, workingState,
                _horizontalInput, _duration, TrajectorySamples, out var finalVelocity);

            var finalState = MovementMathUtility.ApplyGlide(workingState, MovementStats, _duration, finalVelocity);
            evaluation = new MoveEvaluation(finalState, trajectory, _duration);
            error = string.Empty;
            return true;
        }
    }
}
