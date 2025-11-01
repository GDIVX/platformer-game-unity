using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Runtime.Player.Movement;
using Runtime.Player.Movement.Debug;
using Runtime.Player.Movement.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class JumpArcSimulatorPlayModeTests
    {
        private const float Tolerance = 0.05f;

        [UnityTest]
        public IEnumerator JumpArcSimulatorAlignsWithPlayerMovementLanding()
        {
            var stats = ScriptableObject.CreateInstance<PlayerMovementStats>();
            stats.GroundLayer = LayerMask.GetMask("Default");
            stats.StopOnCollision = false;

            var ground = new GameObject("Ground");
            var groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(10f, 1f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);

            var player = new GameObject("PlayerMovementSimulatorTest");
            player.transform.position = new Vector3(0f, 1f, 0f);
            var rigidbody = player.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.isKinematic = true;

            var feetCollider = player.AddComponent<BoxCollider2D>();
            feetCollider.size = new Vector2(0.5f, 1f);
            feetCollider.offset = new Vector2(0f, -0.5f);

            var bodyCollider = player.AddComponent<BoxCollider2D>();
            bodyCollider.size = new Vector2(0.5f, 1f);

            var movement = player.AddComponent<PlayerMovement>();
            SetPrivateField(movement, "_movementStats", stats);
            SetPrivateField(movement, "_feetCollider", feetCollider);
            SetPrivateField(movement, "_bodyCollider", bodyCollider);

            InvokePrivateMethod(movement, "OnEnable");
            InvokePrivateMethod(movement, "Awake");

            var context = movement.Context;
            var stateMachine = GetPrivateField<PlayerMovementStateMachine>(movement, "_stateMachine");
            InvokePrivateMethod(movement, "CollisionCheck");

            context.SetGroundHit(new RaycastHit2D());
            context.InitiateJump(1);
            stateMachine.ChangeState<JumpingState>();

            Vector2 startPoint = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y);

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

            JumpArcSimulator.SimulationResult simulation = simulator.Simulate(settings);
            IReadOnlyList<Vector2> simulatedPoints = simulation.Points;
            Vector2 expectedLanding = simulatedPoints[simulatedPoints.Count - 1];

            var actualPoints = new List<Vector2> { startPoint };

            for (int i = 0; i < settings.MaxSteps; i++)
            {
                context.UpdateTimers(Time.fixedDeltaTime);
                context.SetInput(new Vector2(1f, 0f), false, false, true, false);

                stateMachine.HandleInput();
                InvokePrivateMethod(movement, "CollisionCheck");
                stateMachine.FixedTick();

                Vector2 frameVelocity = new Vector2(context.Velocity.x, context.VerticalVelocity);
                Vector2 newPosition = (Vector2)context.Transform.position + frameVelocity * Time.fixedDeltaTime;
                context.Transform.position = newPosition;
                rigidbody.position = newPosition;

                InvokePrivateMethod(movement, "CollisionCheck");

                Vector2 footPoint = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y);
                actualPoints.Add(footPoint);

                if (footPoint.y <= startPoint.y)
                {
                    break;
                }

                yield return null;
            }

            Vector2 actualLanding = actualPoints[actualPoints.Count - 1];

            Assert.That(Vector2.Distance(expectedLanding, actualLanding), Is.LessThan(Tolerance));
            Assert.That(Mathf.Abs(expectedLanding.x - actualLanding.x), Is.LessThan(Tolerance));

            Object.DestroyImmediate(stats);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(ground);

            yield return null;
        }

        private static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(instance, value);
        }

        private static T GetPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)field.GetValue(instance);
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(instance, null);
        }
    }
}
