using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Player.Movement.Tools
{
    /// <summary>
    /// Predicts and visualizes player jump trajectories for use by level designers.
    /// Focuses on consistent, intuitive results over perfect physics parity.
    /// </summary>
    [System.Serializable]
    public class JumpArcSimulator
    {
        [Header("Designer Tweaks")]
        [Tooltip("Applies a mild horizontal damping each step to emulate air resistance or input drift.")]
        [Range(0f, 0.5f)] public float SimulatedAirDrag = 0.05f;

        [Tooltip("Multiplier on player max speed. Values above 1.0 slightly exaggerate jump reach for safety margins.")]
        [Range(1f, 1.3f)] public float SimulatedSpeedBoost = 1.1f;

        [Tooltip("Small tolerance above ground level to avoid premature landing detection.")]
        [Range(0f, 0.1f)] public float GroundTolerance = 0.05f;

        public struct SimulationSettings
        {
            public Vector2 StartPosition;
            public float HorizontalInput;
            public bool RunHeld;
            public float InitialHorizontalVelocity;
            public int MaxSteps;
            public bool StopOnCollision;
            public LayerMask CollisionMask;
        }

        public readonly struct SimulationResult
        {
            public SimulationResult(List<Vector2> points, int? collisionIndex)
            {
                Points = points;
                CollisionIndex = collisionIndex;
            }

            public IReadOnlyList<Vector2> Points { get; }
            public int? CollisionIndex { get; }
        }

        private readonly PlayerMovementStats _stats;

        public JumpArcSimulator(PlayerMovementStats stats)
        {
            _stats = stats;
        }

        /// <summary>
        /// Simulates a jump arc from the given settings and returns a sequence of points.
        /// </summary>
        public SimulationResult Simulate(SimulationSettings settings)
        {
            var points = new List<Vector2> { settings.StartPosition };

            Vector2 position = settings.StartPosition;
            Vector2 velocity = new Vector2(settings.InitialHorizontalVelocity, _stats.InitialJumpVelocity);

            bool isPastApexThreshold = false;
            float timePastApexThreshold = 0f;
            bool appliedReleaseGravity = false;

            float startHeight = settings.StartPosition.y;
            float horizontalInput = settings.HorizontalInput;
            float deltaTime = Time.fixedDeltaTime;
            int stepsLimit = Mathf.Max(1, settings.MaxSteps);
            int? collisionIndex = null;

            for (int step = 0; step < stepsLimit; step++)
            {
                // --------------------------------------------------------
                // HORIZONTAL — mirrors controller feel, not raw physics
                // --------------------------------------------------------
                bool hasHorizontalInput = !Mathf.Approximately(horizontalInput, 0f);
                float baseSpeed = settings.RunHeld ? _stats.MaxRunSpeed : _stats.MaxWalkSpeed;
                float maxSpeed = baseSpeed * SimulatedSpeedBoost;

                if (hasHorizontalInput)
                {
                    float targetSpeed = horizontalInput * maxSpeed;
                    velocity.x = Mathf.Lerp(velocity.x, targetSpeed, _stats.AirAcceleration * deltaTime);
                }
                else
                {
                    velocity.x = Mathf.Lerp(velocity.x, 0f, _stats.AirDeceleration * deltaTime);
                    if (Mathf.Abs(velocity.x) <= _stats.MinSpeedThreshold)
                        velocity.x = 0f;
                }

                // Mild air drag for realism
                velocity.x = Mathf.Lerp(velocity.x, 0f, SimulatedAirDrag * deltaTime);
                velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);

                // --------------------------------------------------------
                // VERTICAL — mirrors HandleJumpAscent and fall logic
                // --------------------------------------------------------
                if (velocity.y >= 0f)
                {
                    float apexPoint = Mathf.InverseLerp(_stats.InitialJumpVelocity, 0f, velocity.y);
                    if (apexPoint > _stats.ApexThreshold)
                    {
                        if (!isPastApexThreshold)
                        {
                            isPastApexThreshold = true;
                            timePastApexThreshold = 0f;
                        }

                        timePastApexThreshold += deltaTime;
                        if (timePastApexThreshold < _stats.ApexHangTime)
                        {
                            velocity.y = 0f;
                        }
                        else
                        {
                            velocity.y = -0.01f;
                        }
                    }
                    else
                    {
                        velocity.y += _stats.Gravity * deltaTime;
                        isPastApexThreshold = false;
                    }
                }
                else
                {
                    if (!appliedReleaseGravity)
                    {
                        velocity.y += _stats.Gravity * _stats.GravityOnReleaseMultiplier * deltaTime;
                        appliedReleaseGravity = true;
                    }
                    else
                    {
                        velocity.y += _stats.Gravity * deltaTime;
                    }
                }

                velocity.y = Mathf.Clamp(velocity.y, -_stats.MaxFallSpeed, _stats.MaxRiseSpeed);

                // --------------------------------------------------------
                // APPLY DISPLACEMENT
                // --------------------------------------------------------
                Vector2 previousPosition = position;
                Vector2 displacement = velocity * deltaTime;
                Vector2 proposedPosition = previousPosition + displacement;

                // Stop on collision
                if (settings.StopOnCollision && settings.CollisionMask != 0)
                {
                    RaycastHit2D hit = Physics2D.Raycast(previousPosition, displacement.normalized, displacement.magnitude, settings.CollisionMask);
                    if (hit.collider != null)
                    {
                        points.Add(hit.point);
                        collisionIndex = points.Count - 1;
                        break;
                    }
                }

                // Landing detection — when crossing starting height (with tolerance)
                bool crossedStartHeight =
                    previousPosition.y >= startHeight + GroundTolerance &&
                    proposedPosition.y <= startHeight + GroundTolerance;

                if (crossedStartHeight)
                {
                    float travelY = previousPosition.y - proposedPosition.y;
                    float t = Mathf.Approximately(travelY, 0f)
                        ? 1f
                        : (previousPosition.y - startHeight) / travelY;
                    t = Mathf.Clamp01(t);

                    Vector2 landingPoint = Vector2.Lerp(previousPosition, proposedPosition, t);
                    points.Add(landingPoint);
                    break;
                }

                position = proposedPosition;
                points.Add(position);

                if (position.y <= startHeight - GroundTolerance)
                    break;
            }

            return new SimulationResult(points, collisionIndex);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draws a simple arc gizmo in the Scene view for visual debugging.
        /// </summary>
        public static void DrawGizmo(JumpArcSimulator.SimulationResult result, Color color)
        {
            if (result.Points == null || result.Points.Count < 2)
                return;

            UnityEditor.Handles.color = color;
            for (int i = 0; i < result.Points.Count - 1; i++)
            {
                UnityEditor.Handles.DrawLine(result.Points[i], result.Points[i + 1]);
            }

            if (result.CollisionIndex.HasValue)
            {
                Vector2 hit = result.Points[result.CollisionIndex.Value];
                UnityEditor.Handles.color = Color.red;
                UnityEditor.Handles.DrawSolidDisc(hit, Vector3.forward, 0.05f);
            }
        }
#endif
    }
}
