using Runtime.Player.Movement;
using Runtime.Player.Movement.Math;
using UnityEngine;

namespace RoutePlanning.Profiles
{
    [CreateAssetMenu(menuName = "Route Planning/Profiles/Flight Profile", fileName = "FlightProfile")]
    public class FlightProfile : MoveProfile
    {
        [SerializeField, Min(0.1f)] private float _duration = 1f;
        [SerializeField] private Vector2 _input = new Vector2(0f, 1f);

        protected override bool CanExecute(PlayerStateSnapshot state, out string error)
        {
            if (!base.CanExecute(state, out error))
            {
                return false;
            }

            if (state.FlightTimeRemaining <= 0f)
            {
                error = "No flight time remaining.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        protected override bool TryEvaluateInternal(Vector3 startPosition, Vector3 endPosition,
            PlayerStateSnapshot workingState, out MoveEvaluation evaluation, out string error)
        {
            float duration = Mathf.Min(_duration, Mathf.Max(0f, workingState.FlightTimeRemaining));
            var trajectory = MovementMathUtility.CreateFlightTrajectory(MovementStats, startPosition, workingState,
                _input, duration, TrajectorySamples, out var finalVelocity);

            var finalState = MovementMathUtility.ApplyFlight(workingState, duration, finalVelocity);
            evaluation = new MoveEvaluation(finalState, trajectory, duration);
            error = string.Empty;
            return true;
        }
    }
}
