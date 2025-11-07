using System.Reflection;
using NUnit.Framework;
using Runtime.Player.Movement;
using Runtime.Player.Movement.States;
using UnityEngine;
using UnityEngine.Events;

namespace Tests.EditMode
{
    public class WallSlideStateTests
    {
        private PlayerMovementStats _stats;
        private GameObject _player;
        private Rigidbody2D _rigidbody;
        private BoxCollider2D _feetCollider;
        private BoxCollider2D _bodyCollider;
        private PlayerMovementContext _context;
        private PlayerMovementStateMachine _stateMachine;

        [SetUp]
        public void SetUp()
        {
            _stats = ScriptableObject.CreateInstance<PlayerMovementStats>();
            _player = new GameObject("WallSlideStateTests");
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

            _stateMachine = new PlayerMovementStateMachine(_context);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stats);
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void HandleInput_LongWallJumpWhenMovingAwayWithinDirectionBuffer()
        {
            _context.SetInput(Vector2.zero, false, false, false, false);
            SetBackingField("IsTouchingWall", true);
            SetBackingField("WallDirection", 1);
            SetBackingField("WallStickTimer", _stats.WallSlide.StickDuration);

            _stateMachine.Initialize<WallSlideState>();

            _context.StartJumpBuffer();
            _stateMachine.HandleInput();

            Assert.That(_stateMachine.CurrentState, Is.EqualTo(_stateMachine.GetState<WallSlideState>()));

            float deltaTime = Mathf.Min(_stats.DirectionBufferDuration * 0.5f, _stats.JumpBufferTime * 0.5f);
            _context.SetInput(Vector2.left, false, false, false, false);
            _context.UpdateTimers(deltaTime);

            _stateMachine.HandleInput();

            Assert.That(_stateMachine.CurrentState, Is.EqualTo(_stateMachine.GetState<JumpingState>()));

            float expectedHorizontal = _stats.WallSlide.WallJumpHorizontalPush *
                                       _stats.WallSlide.LongWallJumpHorizontalMultiplier;
            Assert.That(Mathf.Abs(_context.Velocity.x), Is.EqualTo(expectedHorizontal).Within(1e-5f));
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
