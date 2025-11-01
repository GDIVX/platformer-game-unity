using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Player.Movement.Debug
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
            bool isPastApexThreshold = false;
            float timePastApexThreshold = 0f;
            bool appliedReleaseGravity = false;

            float startHeight = settings.StartPosition.y;
            float horizontalInput = settings.HorizontalInput;
            float acceleration = _stats.AirAcceleration;
            float deceleration = _stats.AirDeceleration;
            float deltaTime = Time.fixedDeltaTime;

            int? collisionIndex = null;
            int stepsLimit = Mathf.Max(1, settings.MaxSteps);

            for (int step = 0; step < stepsLimit; step++)
            {
                bool hasHorizontalInput = !Mathf.Approximately(horizontalInput, 0f);
                if (hasHorizontalInput)
                {
                    float targetSpeed = horizontalInput * (settings.RunHeld ? _stats.MaxRunSpeed : _stats.MaxWalkSpeed);
                    float lerpFactor = Mathf.Clamp01(acceleration * deltaTime);
                    horizontalVelocity = Mathf.Lerp(horizontalVelocity, targetSpeed, lerpFactor);
                }
                else
                {
                    float lerpFactor = Mathf.Clamp01(deceleration * deltaTime);
                    horizontalVelocity = Mathf.Lerp(horizontalVelocity, 0f, lerpFactor);

                    if (Mathf.Abs(horizontalVelocity) <= _stats.MinSpeedThreshold)
                    {
                        horizontalVelocity = 0f;
                    }
                }

                if (verticalVelocity >= 0f)
                {
                    float apexPoint = Mathf.InverseLerp(_stats.InitialJumpVelocity, 0f, verticalVelocity);
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
                            verticalVelocity = 0f;
                        }
                        else
                        {
                            verticalVelocity = -0.01f;
                        }
                    }
                    else
                    {
                        verticalVelocity += _stats.Gravity * deltaTime;
                        isPastApexThreshold = false;
                    }
                }
                else
                {
                    if (!appliedReleaseGravity)
                    {
                        verticalVelocity += _stats.Gravity * _stats.GravityOnReleaseMultiplier * deltaTime;
                        appliedReleaseGravity = true;
                    }
                    else
                    {
                        verticalVelocity += _stats.Gravity * deltaTime;
                    }
                }

                verticalVelocity = Mathf.Clamp(verticalVelocity, -_stats.MaxFallSpeed, _stats.MaxRiseSpeed);

                Vector2 previousPosition = points[points.Count - 1];
                Vector2 displacement = new Vector2(horizontalVelocity * deltaTime, verticalVelocity * deltaTime);
                Vector2 proposedPosition = previousPosition + displacement;

                if (settings.StopOnCollision && settings.CollisionMask != 0)
                {
                    Vector2 direction = displacement.normalized;
                    float distance = displacement.magnitude;
                    RaycastHit2D hit = Physics2D.Raycast(previousPosition, direction, distance, settings.CollisionMask);

                    if (hit.collider != null)
                    {
                        points.Add(hit.point);
                        collisionIndex = points.Count - 1;
                        break;
                    }
                }

                if (previousPosition.y >= startHeight && proposedPosition.y < startHeight)
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

                points.Add(proposedPosition);

                if (proposedPosition.y <= startHeight)
                {
                    break;
                }
            }

            return new SimulationResult(points, collisionIndex);
        }
    }
}
