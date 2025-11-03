using System.Reflection;
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
            _context.AirTime = 0f;

            const float deltaTime = 0.037f;
            _context.UpdateTimers(deltaTime);
            _context.UpdateTimers(Time.fixedDeltaTime);

            Assert.That(_context.AirTime, Is.EqualTo(deltaTime + Time.fixedDeltaTime).Within(1e-6f));
        }

        [Test]
        public void UpdateTimers_DirectionBufferTimerUsesProvidedDeltaTime()
        {
            _context.SetInput(Vector2.zero, false, false, false, false);
            SetBackingField("WallDirection", 1);
            SetBackingField("DirectionBufferTimer", 0.05f);
            SetBackingField("WantsToMoveAwayFromWall", true);

            _context.UpdateTimers(0.02f);

            Assert.That(_context.DirectionBufferTimer, Is.EqualTo(0.03f).Within(1e-6f));
            Assert.IsTrue(_context.WantsToMoveAwayFromWall);

            _context.UpdateTimers(0.05f);

            Assert.That(_context.DirectionBufferTimer, Is.EqualTo(-0.02f).Within(1e-6f));
            Assert.IsFalse(_context.WantsToMoveAwayFromWall);
        }

        [Test]
        public void ApplyFastFall_UsesProvidedDeltaTime()
        {
            _context.FastFallTime = _stats.TimeForUpwardsCancel;
            _context.VerticalVelocity = 0f;

            const float deltaTime = 0.02f;
            _context.ApplyFastFall(deltaTime);

            float expectedVelocity = _stats.Gravity * _stats.GravityOnReleaseMultiplier * deltaTime;
            Assert.That(_context.FastFallTime,
                Is.EqualTo(_stats.TimeForUpwardsCancel + deltaTime).Within(1e-6f));
            Assert.That(_context.VerticalVelocity, Is.EqualTo(expectedVelocity).Within(1e-6f));
        }

        [Test]
        public void HandleJumpAscent_AccumulatesProvidedDeltaTime()
        {
            _context.VerticalVelocity = _stats.InitialJumpVelocity * 0.01f;
            _context.IsPastApexThreshold = false;
            _context.TimePastApexThreshold = 0f;

            const float deltaTime = 0.02f;
            _context.HandleJumpAscent(deltaTime);

            Assert.IsTrue(_context.IsPastApexThreshold);
            Assert.That(_context.TimePastApexThreshold, Is.EqualTo(deltaTime).Within(1e-6f));
            Assert.That(_context.VerticalVelocity, Is.EqualTo(0f).Within(1e-6f));
        }

        private void SetBackingField<T>(string propertyName, T value)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = typeof(PlayerMovementContext).GetField($"<{propertyName}>k__BackingField", bindingFlags);
            if (field == null)
            {
                Assert.Fail($"Backing field for {propertyName} was not found");
                return;
            }

            field.SetValue(_context, value);
        }
    }
}
