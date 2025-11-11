using System;
using System.Collections.Generic;
using Runtime.Player.Movement.Tools;
using UnityEngine;

namespace Runtime.Player.Movement.Math
{
    public static class MovementMathUtility
    {
        private const float DefaultTolerance = 0.001f;

        public static bool HasSufficientStamina(PlayerStateSnapshot state, float cost)
        {
            return state.Stamina >= cost - DefaultTolerance;
        }

        public static PlayerStateSnapshot SpendStamina(PlayerStateSnapshot state, float cost)
        {
            if (cost <= 0f)
            {
                return state;
            }

            return state.ConsumeStamina(cost);
        }

        public static bool ValidateState(PlayerStateSnapshot state)
        {
            return IsFinite(state.Velocity.x) &&
                   IsFinite(state.Velocity.y) &&
                   state.Stamina >= -DefaultTolerance &&
                   state.DashCooldown >= -DefaultTolerance &&
                   state.AirDashCooldown >= -DefaultTolerance &&
                   state.GlideTimeRemaining >= -DefaultTolerance &&
                   state.FlightTimeRemaining >= -DefaultTolerance &&
                   state.AirDashCount >= 0;
        }

        public static float EstimateGroundTravelTime(float distance, float initialVelocity, float maxSpeed,
            float acceleration, out float finalVelocity)
        {
            distance = Mathf.Abs(distance);
            float direction = distance < DefaultTolerance
                ? (Mathf.Sign(initialVelocity) == 0f ? 1f : Mathf.Sign(initialVelocity))
                : 1f;

            float currentSpeed = Mathf.Abs(initialVelocity);
            maxSpeed = Mathf.Max(DefaultTolerance, maxSpeed);
            acceleration = Mathf.Max(DefaultTolerance, acceleration);

            if (distance <= DefaultTolerance)
            {
                finalVelocity = Mathf.Sign(initialVelocity) * currentSpeed;
                return 0f;
            }

            float accelerateTime = Mathf.Max(0f, (maxSpeed - currentSpeed) / acceleration);
            float accelerateDistance = (currentSpeed + maxSpeed) * 0.5f * accelerateTime;

            if (accelerateDistance >= distance)
            {
                float finalSpeedSquared = Mathf.Max(0f, currentSpeed * currentSpeed + 2f * acceleration * distance);
                float finalSpeed = Mathf.Sqrt(finalSpeedSquared);
                finalVelocity = direction * finalSpeed;
                float averageSpeed = (currentSpeed + finalSpeed) * 0.5f;
                averageSpeed = Mathf.Max(DefaultTolerance, averageSpeed);
                return distance / averageSpeed;
            }

            float remaining = Mathf.Max(0f, distance - accelerateDistance);
            float constantSpeedTime = remaining / maxSpeed;
            finalVelocity = direction * maxSpeed;
            return accelerateTime + constantSpeedTime;
        }

        public static IReadOnlyList<Vector3> CreateLinearTrajectory(Vector3 start, Vector3 end, int samples)
        {
            int count = Mathf.Max(2, samples);
            var points = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 1f : (float)i / (count - 1);
                points.Add(Vector3.LerpUnclamped(start, end, t));
            }

            return points;
        }

        public static Vector2 EstimateFinalVelocity(IReadOnlyList<Vector3> trajectory, float stepDuration)
        {
            if (trajectory == null || trajectory.Count < 2 || stepDuration <= 0f)
            {
                return Vector2.zero;
            }

            Vector3 last = trajectory[trajectory.Count - 1];
            Vector3 prev = trajectory[trajectory.Count - 2];
            Vector3 delta = last - prev;
            return new Vector2(delta.x / stepDuration, delta.y / stepDuration);
        }

        public static IReadOnlyList<Vector3> CreateJumpTrajectory(PlayerMovementStats stats, Vector3 startPosition,
            PlayerStateSnapshot state, float horizontalInput, bool runHeld, int steps, LayerMask mask,
            bool stopOnCollision, out Vector2 finalVelocity, out int? collisionIndex)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            var simulator = new JumpArcSimulator(stats);
            var settings = new JumpArcSimulator.SimulationSettings
            {
                StartPosition = startPosition,
                HorizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f),
                RunHeld = runHeld,
                InitialHorizontalVelocity = state.Velocity.x,
                MaxSteps = Mathf.Max(1, steps),
                StopOnCollision = stopOnCollision,
                CollisionMask = mask
            };

            var result = simulator.Simulate(settings);
            var samples = new List<Vector3>(result.Points.Count);
            foreach (var point in result.Points)
            {
                samples.Add(new Vector3(point.x, point.y, startPosition.z));
            }

            finalVelocity = EstimateFinalVelocity(samples, Time.fixedDeltaTime);
            collisionIndex = result.CollisionIndex;
            return samples;
        }

        public static PlayerStateSnapshot ApplyHorizontalVelocity(PlayerStateSnapshot state, float velocityX)
        {
            var velocity = state.Velocity;
            velocity.x = velocityX;
            return state.WithVelocity(velocity);
        }

        public static PlayerStateSnapshot ApplyVerticalVelocity(PlayerStateSnapshot state, float velocityY)
        {
            var velocity = state.Velocity;
            velocity.y = velocityY;
            return state.WithVelocity(velocity);
        }

        public static PlayerStateSnapshot ApplyDash(PlayerStateSnapshot state, PlayerMovementStats stats,
            bool startedGrounded, int direction)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            direction = Mathf.Clamp(direction, -1, 1);
            if (direction == 0)
            {
                direction = 1;
            }

            float dashSpeed = Mathf.Max(0f, stats.DashForwardBurstSpeed);
            var velocity = new Vector2(dashSpeed * direction, 0f);

            float dashCooldown = startedGrounded ? stats.DashGroundCooldown : stats.DashAirDashCooldown;
            float airDashCooldown = startedGrounded
                ? state.AirDashCooldown
                : Mathf.Max(state.AirDashCooldown, stats.DashAirDashCooldown);

            var updated = state.WithVelocity(velocity)
                .WithDashCooldown(dashCooldown)
                .WithAirDashCooldown(airDashCooldown)
                .WithAirDashCount(startedGrounded ? 0 : state.AirDashCount + 1);

            return updated;
        }

        public static IReadOnlyList<Vector3> CreateGlideTrajectory(PlayerMovementStats stats, Vector3 startPosition,
            PlayerStateSnapshot state, float horizontalInput, float duration, int samples, out Vector2 finalVelocity)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            samples = Mathf.Max(2, samples);
            duration = Mathf.Max(0f, duration);
            float step = samples > 1 ? duration / (samples - 1) : duration;

            var points = new List<Vector3>(samples);
            var velocity = state.Velocity;
            var glide = stats.Glide;
            float accel = glide != null ? Mathf.Max(0f, glide.HorizontalAcceleration) : stats.AirAcceleration;
            float decel = glide != null ? Mathf.Max(0f, glide.HorizontalDeceleration) : stats.AirDeceleration;
            float fallMultiplier = glide != null ? Mathf.Max(0f, glide.FallSpeedMultiplier) : 1f;
            Vector3 current = startPosition;

            for (int i = 0; i < samples; i++)
            {
                points.Add(current);
                if (i == samples - 1)
                {
                    break;
                }

                float targetHorizontal = Mathf.Clamp(horizontalInput, -1f, 1f) * stats.MaxRunSpeed;
                if (Mathf.Abs(horizontalInput) > 0.01f)
                {
                    velocity.x = Mathf.MoveTowards(velocity.x, targetHorizontal, accel * step);
                }
                else
                {
                    velocity.x = Mathf.MoveTowards(velocity.x, 0f, decel * step);
                }

                float gravity = stats.Gravity * fallMultiplier;
                velocity.y += gravity * step;
                velocity.y = Mathf.Clamp(velocity.y, -stats.MaxFallSpeed, stats.MaxRiseSpeed);

                current += new Vector3(velocity.x * step, velocity.y * step, 0f);
            }

            finalVelocity = velocity;
            return points;
        }

        public static PlayerStateSnapshot ApplyGlide(PlayerStateSnapshot state, PlayerMovementStats stats, float duration,
            Vector2 finalVelocity)
        {
            var updated = state.WithVelocity(finalVelocity);
            var glide = stats?.Glide;
            if (glide != null && glide.LimitDuration)
            {
                updated = updated.ConsumeGlideTime(duration);
            }

            return updated;
        }

        public static IReadOnlyList<Vector3> CreateFlightTrajectory(PlayerMovementStats stats, Vector3 startPosition,
            PlayerStateSnapshot state, Vector2 input, float duration, int samples, out Vector2 finalVelocity)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            samples = Mathf.Max(2, samples);
            duration = Mathf.Max(0f, duration);
            float step = samples > 1 ? duration / (samples - 1) : duration;

            var points = new List<Vector3>(samples);
            var velocity = state.Velocity;
            float horizontalAccel = stats.FlyAirAccelerationOverride > 0f
                ? stats.FlyAirAccelerationOverride
                : stats.AirAcceleration;
            float horizontalDecel = stats.FlyAirDecelerationOverride > 0f
                ? stats.FlyAirDecelerationOverride
                : stats.AirDeceleration;
            Vector3 current = startPosition;

            for (int i = 0; i < samples; i++)
            {
                points.Add(current);
                if (i == samples - 1)
                {
                    break;
                }

                float targetHorizontal = Mathf.Clamp(input.x, -1f, 1f) * stats.MaxRunSpeed;
                if (Mathf.Abs(input.x) > 0.01f)
                {
                    velocity.x = Mathf.MoveTowards(velocity.x, targetHorizontal, horizontalAccel * step);
                }
                else
                {
                    velocity.x = Mathf.MoveTowards(velocity.x, 0f, horizontalDecel * step);
                }

                float verticalAcceleration = stats.Gravity;
                if (input.y > 0.01f)
                {
                    verticalAcceleration = stats.FlyLift;
                }
                else if (input.y < -0.01f)
                {
                    verticalAcceleration = stats.Gravity * 2f;
                }

                velocity.y += verticalAcceleration * step;
                velocity.y = Mathf.Clamp(velocity.y, -stats.MaxFallSpeed, stats.MaxRiseSpeed);

                current += new Vector3(velocity.x * step, velocity.y * step, 0f);
            }

            finalVelocity = velocity;
            return points;
        }

        public static PlayerStateSnapshot ApplyFlight(PlayerStateSnapshot state, float duration, Vector2 finalVelocity)
        {
            var updated = state.WithVelocity(finalVelocity)
                .ConsumeFlightTime(duration)
                .ConsumeStamina(duration);
            return updated;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
