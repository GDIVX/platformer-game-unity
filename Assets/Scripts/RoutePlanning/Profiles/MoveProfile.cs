using System;
using System.Collections.Generic;
using Runtime.Player.Movement;
using Runtime.Player.Movement.Math;
using UnityEngine;

namespace RoutePlanning.Profiles
{
    public abstract class MoveProfile : ScriptableObject
    {
        [Header("Profile Settings")]
        [SerializeField] private PlayerMovementStats _movementStats;
        [SerializeField, Min(0f)] private float _staminaCost = 0f;
        [SerializeField, Min(2)] private int _trajectorySamples = 12;
        [SerializeField] private Color _debugColor = Color.cyan;

        public PlayerMovementStats MovementStats => _movementStats;
        public float StaminaCost => Mathf.Max(0f, _staminaCost);
        public int TrajectorySamples => Mathf.Max(2, _trajectorySamples);
        public Color DebugColor => _debugColor;

        public bool TryEvaluate(Vector3 startPosition, Vector3 endPosition, PlayerStateSnapshot startState,
            out MoveEvaluation evaluation, out string error)
        {
            evaluation = default;
            if (_movementStats == null)
            {
                error = $"Move profile '{name}' is missing a movement stats reference.";
                return false;
            }

            if (!CanExecute(startState, out error))
            {
                return false;
            }

            var workingState = MovementMathUtility.SpendStamina(startState, StaminaCost);

            if (!TryEvaluateInternal(startPosition, endPosition, workingState, out evaluation, out error))
            {
                return false;
            }

            if (!MovementMathUtility.ValidateState(evaluation.EndState))
            {
                error = $"Move profile '{name}' produced an invalid state.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        protected virtual bool CanExecute(PlayerStateSnapshot state, out string error)
        {
            if (!MovementMathUtility.HasSufficientStamina(state, StaminaCost))
            {
                error = "Insufficient stamina for profile.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        protected abstract bool TryEvaluateInternal(Vector3 startPosition, Vector3 endPosition,
            PlayerStateSnapshot workingState, out MoveEvaluation evaluation, out string error);
    }

    [Serializable]
    public readonly struct MoveEvaluation
    {
        public MoveEvaluation(PlayerStateSnapshot endState, IReadOnlyList<Vector3> trajectory,
            float estimatedDuration, int? collisionIndex = null)
        {
            EndState = endState;
            Trajectory = trajectory;
            EstimatedDuration = Mathf.Max(0f, estimatedDuration);
            CollisionIndex = collisionIndex;
        }

        public PlayerStateSnapshot EndState { get; }
        public IReadOnlyList<Vector3> Trajectory { get; }
        public float EstimatedDuration { get; }
        public int? CollisionIndex { get; }
    }
}
