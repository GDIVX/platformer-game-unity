using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Runtime.Player.Movement;
using Runtime.Player.Movement.States;

namespace Tests.EditMode
{
    public class PlayerMovementStateMachineTests
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
            _player = new GameObject("PlayerMovementStateMachineTests");
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
            _stateMachine.Initialize<GroundedState>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stats);
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void GroundedState_BuffersJump_TransitionsToJumping()
        {
            _context.StartJumpBuffer();
            _context.UpdateTimers(-_stats.JumpCoyoteTime);

            _stateMachine.HandleInput();

            Assert.IsInstanceOf<JumpingState>(_stateMachine.CurrentState);
            Assert.AreEqual(1, _context.JumpsCount);
        }

        [Test]
        public void FallingState_BufferedJumpWithoutPreviousJump_ConsumesTwoJumps()
        {
            _stateMachine.ChangeState<FallingState>();
            _context.StartJumpBuffer();
            _context.UpdateTimers(_stats.JumpCoyoteTime + 0.01f);

            _stateMachine.HandleInput();

            Assert.IsInstanceOf<JumpingState>(_stateMachine.CurrentState);
            Assert.AreEqual(Mathf.Min(2, _stats.NumberOfJumpsAllowed), _context.JumpsCount);
        }

        [Test]
        public void FallingState_BufferedJumpDuringCoyoteTime_AllowsInitialJumpWhenSingleJumpConfigured()
        {
            _stats.NumberOfJumpsAllowed = 1;

            _stateMachine.ChangeState<FallingState>();
            _context.StartJumpBuffer();

            _stateMachine.HandleInput();

            Assert.IsInstanceOf<JumpingState>(_stateMachine.CurrentState);
            Assert.AreEqual(1, _context.JumpsCount);
        }

        [Test]
        public void JumpingState_BufferedJump_PerformsDoubleJump()
        {
            _context.InitiateJump(1);
            _stateMachine.ChangeState<JumpingState>();
            _context.StartJumpBuffer();

            _stateMachine.HandleInput();

            Assert.IsInstanceOf<JumpingState>(_stateMachine.CurrentState);
            Assert.AreEqual(Mathf.Min(2, _stats.NumberOfJumpsAllowed), _context.JumpsCount);
        }

        [Test]
        public void ApplyHorizontalMovement_TurnsLeft_InvokesTurnEvent()
        {
            bool eventInvoked = false;
            bool? facingRight = null;
            _context.OnTurnEvent.AddListener(value =>
            {
                eventInvoked = true;
                facingRight = value;
            });

            _context.SetInput(new Vector2(-1f, 0f), false, false, false, false);
            _context.ApplyHorizontalMovement(_stats.GroundAcceleration, _stats.GroundDeceleration);

            Assert.IsTrue(eventInvoked);
            Assert.IsFalse(facingRight ?? true);
        }
    }
}
