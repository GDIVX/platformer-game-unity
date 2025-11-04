using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Player.Movement.Tools
{
    public class JumpArcSimulator
    {
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

        public SimulationResult Simulate(SimulationSettings settings)
        {
            var points = new List<Vector2> { settings.StartPosition };
            float horizontalVelocity = settings.InitialHorizontalVelocity;
            float verticalVelocity = _stats.InitialJumpVelocity;
            float deltaTime = Time.fixedDeltaTime;
            int? collisionIndex = null;

            for (int step = 0; step < settings.MaxSteps; step++)
            {
                // Horizontal movement
                float maxSpeed = settings.RunHeld ? _stats.MaxRunSpeed : _stats.MaxWalkSpeed;
                float accel = _stats.AirAcceleration;
                horizontalVelocity = Mathf.Lerp(horizontalVelocity, settings.HorizontalInput * maxSpeed,
                    accel * deltaTime);

                // Vertical movement
                verticalVelocity += _stats.Gravity * deltaTime;
                verticalVelocity = Mathf.Clamp(verticalVelocity, -_stats.MaxFallSpeed, _stats.MaxRiseSpeed);

                Vector2 previous = points[^1];
                Vector2 displacement = new Vector2(horizontalVelocity, verticalVelocity) * deltaTime;
                Vector2 next = previous + displacement;

                // --- collision handling ---
                if (settings.StopOnCollision && settings.CollisionMask != 0)
                {
                    RaycastHit2D hit = Physics2D.Raycast(previous, displacement.normalized, displacement.magnitude,
                        settings.CollisionMask);
                    if (hit.collider != null)
                    {
                        points.Add(hit.point);
                        collisionIndex = points.Count - 1;
                        break;
                    }
                }

                points.Add(next);
            }

            return new SimulationResult(points, collisionIndex);
        }
    }
}