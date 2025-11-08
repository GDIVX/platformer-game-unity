using NUnit.Framework;
using Runtime.Player.Movement;
using Runtime.Player.Movement.Abilities;
using Runtime.Player.Movement.Events;
using Runtime.Player.Movement.States;
using UnityEngine;
using UnityEngine.Events;

namespace Tests.EditMode
{
    public class FlyMovementAbilityTests
    {
        private PlayerMovementStats _stats;
        private GameObject _player;
        private Rigidbody2D _rigidbody;
        private BoxCollider2D _feetCollider;
        private BoxCollider2D _bodyCollider;
        private MovementEventBus _eventBus;
        private PlayerMovementContext _context;
        private PlayerMovementStateMachine _stateMachine;
        private FlyMovementAbility _ability;

        [SetUp]
        public void SetUp()
        {
            _stats = ScriptableObject.CreateInstance<PlayerMovementStats>();
            _stats.FlyDuration = 2f;
            _stats.FlyLift = 50f;
            _stats.FlyAirAccelerationOverride = 15f;
            _stats.FlyAirDecelerationOverride = 20f;
            _stats.FlyExitHangDuration = 0.5f;
            _stats.FlyRegenerationRate = 1f;

            _player = new GameObject("FlyAbilityTestPlayer");
            _rigidbody = _player.AddComponent<Rigidbody2D>();
            _feetCollider = _player.AddComponent<BoxCollider2D>();
            _bodyCollider = _player.AddComponent<BoxCollider2D>();
            _eventBus = ScriptableObject.CreateInstance<MovementEventBus>();

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
                new UnityEvent<float>(),
                _eventBus);

            _stateMachine = new PlayerMovementStateMachine(_context);
            RegisterDefaultStates();

            _ability = new FlyMovementAbility();
            _ability.Initialize(_context, _stateMachine);
            foreach (var state in _ability.CreateStates(_context, _stateMachine))
            {
                _stateMachine.RegisterState(state);
            }

            _ability.OnAbilityEnabled(_context, _stateMachine);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stats);
            Object.DestroyImmediate(_eventBus);
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void FallingStateTransitionsToFlyStateWhenConditionsMet()
        {
            _context.RuntimeData.IsGrounded = false;
            _context.RuntimeData.JumpsCount = _stats.NumberOfJumpsAllowed;
            _context.RuntimeData.FlightTimeRemaining = _context.RuntimeData.FlightTimeMax;
            _context.RuntimeData.FlightHangTimer = 0f;

            _context.SetInput(Vector2.zero, false, false, true, false);
            _stateMachine.Initialize<FallingState>();

            _stateMachine.HandleInput();

            Assert.IsInstanceOf<FlyState>(_stateMachine.CurrentState);
        }

        [Test]
        public void FlyStateConsumesTimerAndRaisesEvents()
        {
            bool flyStarted = false;
            bool flyEnded = false;
            _eventBus.FlyStarted.AddListener(() => flyStarted = true);
            _eventBus.FlyEnded.AddListener(() => flyEnded = true);

            _context.RuntimeData.IsGrounded = false;
            _context.RuntimeData.JumpsCount = _stats.NumberOfJumpsAllowed;
            _context.RuntimeData.FlightTimeRemaining = 1.5f;
            _context.RuntimeData.FlightHangTimer = 0f;
            _context.SetInput(Vector2.zero, false, false, true, false);

            _stateMachine.Initialize<FlyState>();

            Assert.IsTrue(_context.RuntimeData.IsFlying);
            Assert.IsTrue(flyStarted);

            const float deltaTime = 0.25f;
            _context.UpdateTimers(deltaTime);
            Assert.That(
                _context.RuntimeData.FlightTimeRemaining,
                Is.EqualTo(1.5f - deltaTime).Within(1e-6f));

            _context.SetInput(Vector2.zero, false, false, false, true);
            _stateMachine.HandleInput();

            Assert.IsInstanceOf<FallingState>(_stateMachine.CurrentState);
            Assert.IsTrue(flyEnded);
            Assert.IsFalse(_context.RuntimeData.IsFlying);
            Assert.That(
                _context.RuntimeData.FlightHangTimer,
                Is.EqualTo(_stats.FlyExitHangDuration).Within(1e-6f));
        }

        [Test]
        public void FlightTimerRegeneratesAfterHangCompletes()
        {
            _context.RuntimeData.IsFlying = false;
            _context.RuntimeData.FlightTimeRemaining = 0.5f;
            _context.RuntimeData.FlightHangTimer = 0.2f;
            _context.RuntimeData.FlightTimeMax = 2f;

            _context.UpdateTimers(0.1f);
            Assert.That(_context.RuntimeData.FlightTimeRemaining, Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(_context.RuntimeData.FlightHangTimer, Is.EqualTo(0.1f).Within(1e-6f));

            _context.UpdateTimers(0.2f);
            Assert.That(_context.RuntimeData.FlightHangTimer, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(_context.RuntimeData.FlightTimeRemaining, Is.EqualTo(0.5f).Within(1e-6f));

            _context.UpdateTimers(0.5f);
            Assert.That(
                _context.RuntimeData.FlightTimeRemaining,
                Is.EqualTo(1f).Within(1e-6f));
            Assert.That(
                _context.RuntimeData.FlightRegenProgress,
                Is.EqualTo(0.5f).Within(1e-6f));
        }

        private void RegisterDefaultStates()
        {
            _stateMachine.RegisterState(new GroundedState(_context, _stateMachine));
            _stateMachine.RegisterState(new SlidingState(_context, _stateMachine));
            _stateMachine.RegisterState(new JumpingState(_context, _stateMachine));
            _stateMachine.RegisterState(new FallingState(_context, _stateMachine));
            _stateMachine.RegisterState(new FastFallingState(_context, _stateMachine));
            _stateMachine.RegisterState(new WallSlideState(_context, _stateMachine));
        }
    }
}
