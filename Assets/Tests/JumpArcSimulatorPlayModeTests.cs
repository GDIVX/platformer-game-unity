using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Runtime.Player.Movement;
using Runtime.Player.Movement.States;
using Runtime.Player.Movement.Tools;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class JumpArcSimulatorPlayModeTests
    {
        private const float DesignTolerance = 1.0f;   // acceptable difference for designer tool
        private const float CriticalFailureThreshold = 3.0f;

        [UnityTest]
        public IEnumerator JumpArcSimulatorProvidesPredictableReach()
        {
            // === Setup ===
            var stats = ScriptableObject.CreateInstance<PlayerMovementStats>();
            stats.GroundLayer = LayerMask.GetMask("Default");
            stats.StopOnCollision = false;
            stats.VisualizationSteps = 100;
            stats.ArcResolution = 20;

            var ground = new GameObject("Ground");
            var groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(10f, 1f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);

            var player = new GameObject("PlayerMovementSimulatorTest")
            {
                transform = { position = new Vector3(0f, 1f, 0f) }
            };
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

            var feet = player.AddComponent<BoxCollider2D>();
            feet.size = new Vector2(0.5f, 1f);
            feet.offset = new Vector2(0f, -0.5f);

            var body = player.AddComponent<BoxCollider2D>();
            body.size = new Vector2(0.5f, 1f);

            var movement = player.AddComponent<PlayerMovement>();
            SetPrivateField(movement, "_movementStats", stats);
            SetPrivateField(movement, "_feetCollider", feet);
            SetPrivateField(movement, "_bodyCollider", body);

            InvokePrivateMethod(movement, "OnEnable");
            InvokePrivateMethod(movement, "Awake");

            var context = movement.Context;
            var stateMachine = GetPrivateField<PlayerMovementStateMachine>(movement, "_stateMachine");

            context.SetGroundHit(new RaycastHit2D());
            context.InitiateJump(1);
            stateMachine.ChangeState<JumpingState>();

            Vector2 startPoint = new Vector2(feet.bounds.center.x, feet.bounds.min.y);

            // === Simulate expected arc ===
            var simulator = new JumpArcSimulator(stats);
            var settings = new JumpArcSimulator.SimulationSettings
            {
                StartPosition = startPoint,
                HorizontalInput = 1f,
                RunHeld = false,
                InitialHorizontalVelocity = 0f,
                MaxSteps = 240,
                StopOnCollision = false,
                CollisionMask = stats.GroundLayer
            };

            var simulation = simulator.Simulate(settings);
            Vector2 expectedLanding = simulation.Points[^1];

            // === Simulate actual movement ===
            var actualPoints = new List<Vector2> { startPoint };
            const float dt = 0.02f;

            for (int i = 0; i < settings.MaxSteps; i++)
            {
                context.UpdateTimers(dt);
                context.SetInput(new Vector2(1f, 0f), false, false, true, false);

                stateMachine.HandleInput();
                InvokePrivateMethod(movement, "CollisionCheck");
                stateMachine.FixedTick();

                Vector2 frameVelocity = new Vector2(context.Velocity.x, context.VerticalVelocity);
                Vector2 newPosition = (Vector2)context.Transform.position + frameVelocity * dt;
                context.Transform.position = newPosition;
                rb.position = newPosition;

                InvokePrivateMethod(movement, "CollisionCheck");

                Vector2 footPoint = new Vector2(feet.bounds.center.x, feet.bounds.min.y);
                actualPoints.Add(footPoint);

                if (footPoint.y <= startPoint.y)
                    break;

                yield return null;
            }

            Vector2 actualLanding = actualPoints[^1];

            // === Report ===
            float deltaX = Mathf.Abs(expectedLanding.x - actualLanding.x);
            float deltaY = Mathf.Abs(expectedLanding.y - actualLanding.y);
            float totalDelta = Vector2.Distance(expectedLanding, actualLanding);
            Vector2 lastVelocity = new Vector2(context.Velocity.x, context.VerticalVelocity);

            string report =
                $"[JumpArc Simulator Validation]\n" +
                $"Expected Landing: {expectedLanding}\n" +
                $"Actual Landing:   {actualLanding}\n" +
                $"ΔX = {deltaX:F4}, ΔY = {deltaY:F4}, ΔTotal = {totalDelta:F4}\n" +
                $"Momentum @ landing: H={Mathf.Abs(lastVelocity.x):F2}, V={Mathf.Abs(lastVelocity.y):F2}";

            switch (totalDelta)
            {
                case > DesignTolerance and <= CriticalFailureThreshold:
                    Debug.LogWarning(report + $"\n⚠️  Outside design tolerance ({DesignTolerance}), but still predictable.");
                    break;
                case > CriticalFailureThreshold:
                    Assert.Fail(report + $"\n❌  Simulation deviated excessively — possible logic regression.");
                    break;
                default:
                    Debug.Log(report + "\n✅  Within expected designer margin.");
                    break;
            }

            // === Cleanup ===
            Object.DestroyImmediate(stats);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(ground);

            yield return null;
        }

        // === Reflection helpers ===
        private static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(instance, value);
        }

        private static T GetPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? (T)field.GetValue(instance) : default;
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(instance, null);
        }
    }
}
