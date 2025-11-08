using NUnit.Framework;
using Runtime.Player.Movement;
using Runtime.Player.Movement.States;
using UnityEngine;
using UnityEngine.Events;

namespace Tests.EditMode
{
    public class PlayerMovementContextTimerTests
    {
        private PlayerMovementStats _stats;
        private GameObject _player;
        private Rigidbody2D _rigidbody;
        private BoxCollider2D _feetCollider;
        private BoxCollider2D _bodyCollider;
        private PlayerMovementContext _context;
        private float _originalFixedDeltaTime;

        [SetUp]
        public void SetUp()
        {
            _originalFixedDeltaTime = Time.fixedDeltaTime;
            _stats = ScriptableObject.CreateInstance<PlayerMovementStats>();
            _player = new GameObject("PlayerMovementContextTimerTests");
            _rigidbody = _player.AddComponent<Rigidbody2D>();
            _feetCollider = _player.AddComponent<BoxCollider2D>();
            _bodyCollider = _player.AddComponent<BoxCollider2D>();

            _context = new PlayerMovementContext(
                _stats,
                _rigidbody,
                _feetCollider,
                _bodyCollider,
                _player.transform,
                new UnityEvent(),
                new UnityEvent(),
                new UnityEvent(),
                new UnityEvent(),
                new UnityEvent(),
                new UnityEvent<bool>(),
                new UnityEvent<float>());
        }

        [TearDown]
        public void TearDown()
        {
            Time.fixedDeltaTime = _originalFixedDeltaTime;
            Object.DestroyImmediate(_stats);
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void UpdateTimers_UsesProvidedDeltaTimeForAirTime()
        {
            Time.fixedDeltaTime = 0.005f;
            var data = _context.RuntimeData;
            data.AirTime = 0f;

            const float deltaTime = 0.037f;
            _context.UpdateTimers(deltaTime);
            _context.UpdateTimers(Time.fixedDeltaTime);

            Assert.That(data.AirTime, Is.EqualTo(deltaTime + Time.fixedDeltaTime).Within(1e-6f));
        }

        [Test]
        public void UpdateTimers_DirectionBufferTimerUsesProvidedDeltaTime()
        {
            _context.SetInput(Vector2.zero, false, false, false, false);
            var data = _context.RuntimeData;
            data.WallDirection = 1;
            data.DirectionBufferTimer = 0.05f;
            data.WantsToMoveAwayFromWall = true;

            _context.UpdateTimers(0.02f);

            Assert.That(data.DirectionBufferTimer, Is.EqualTo(0.03f).Within(1e-6f));
            Assert.IsTrue(data.WantsToMoveAwayFromWall);

            _context.UpdateTimers(0.05f);

            Assert.That(data.DirectionBufferTimer, Is.EqualTo(-0.02f).Within(1e-6f));
            Assert.IsFalse(data.WantsToMoveAwayFromWall);
        }

        [Test]
        public void ApplyFastFall_UsesProvidedDeltaTime()
        {
            var data = _context.RuntimeData;
            data.FastFallTime = _stats.TimeForUpwardsCancel;
            data.VerticalVelocity = 0f;

            const float deltaTime = 0.02f;
            _context.Jump.ApplyFastFall(deltaTime);

            float expectedVelocity = _stats.Gravity * _stats.GravityOnReleaseMultiplier * deltaTime;
            Assert.That(data.FastFallTime,
                Is.EqualTo(_stats.TimeForUpwardsCancel + deltaTime).Within(1e-6f));
            Assert.That(data.VerticalVelocity, Is.EqualTo(expectedVelocity).Within(1e-6f));
        }

        [TestCase(0.016f, 0.016f)]
        [TestCase(0.02f, 0.005f)]
        [TestCase(0.008f, 0.02f)]
        public void ApplyFall_IntegratesGravityUsingProvidedDeltaTime(float deltaTime, float fixedDeltaTime)
        {
            Time.fixedDeltaTime = fixedDeltaTime;
            var data = _context.RuntimeData;
            data.VerticalVelocity = 0f;
            data.IsFalling = false;

            _context.Jump.ApplyFall(deltaTime);

            float expectedVelocity = _stats.Gravity * deltaTime;
            Assert.IsTrue(data.IsFalling);
            Assert.That(data.VerticalVelocity, Is.EqualTo(expectedVelocity).Within(1e-6f));
        }

        [Test]
        public void HandleJumpAscent_AccumulatesProvidedDeltaTime()
        {
            var data = _context.RuntimeData;
            data.VerticalVelocity = _stats.InitialJumpVelocity * 0.01f;
            data.IsPastApexThreshold = false;
            data.TimePastApexThreshold = 0f;

            const float deltaTime = 0.02f;
            _context.Jump.HandleJumpAscent(deltaTime);

            Assert.IsTrue(data.IsPastApexThreshold);
            Assert.That(data.TimePastApexThreshold, Is.EqualTo(deltaTime).Within(1e-6f));
            Assert.That(data.VerticalVelocity, Is.EqualTo(0f).Within(1e-6f));
        }
    }
}
