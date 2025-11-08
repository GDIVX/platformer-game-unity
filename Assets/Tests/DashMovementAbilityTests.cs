using NUnit.Framework;
using Runtime.Player.Movement;
using Runtime.Player.Movement.Abilities;
using Runtime.Player.Movement.Events;
using Runtime.Player.Movement.States;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Tests.EditMode
{
    public class DashMovementAbilityTests
    {
        private PlayerMovementStats _stats;
        private DashMovementAbility _ability;
        private GameObject _player;
        private Rigidbody2D _rigidbody;
        private BoxCollider2D _feetCollider;
        private BoxCollider2D _bodyCollider;
        private PlayerMovement _movement;
        private MovementEventBus _eventBus;
        private float _timeCursor;

        [SetUp]
        public void SetUp()
        {
            _stats = ScriptableObject.CreateInstance<PlayerMovementStats>();
            _stats.DashForwardBurstSpeed = 30f;
            _stats.DashDuration = 0.1f;
            _stats.DashGroundCooldown = 0.3f;
            _stats.DashAirDashLimit = 2;
            _stats.DashAirDashCooldown = 0.25f;
            _stats.DashPostDashStopDuration = 0.05f;
            _stats.DashDoubleTapWindow = 0.2f;

            _ability = ScriptableObject.CreateInstance<DashMovementAbility>();
            _player = new GameObject("DashMovementAbilityTests");
            _rigidbody = _player.AddComponent<Rigidbody2D>();
            _feetCollider = _player.AddComponent<BoxCollider2D>();
            _bodyCollider = _player.AddComponent<BoxCollider2D>();

            _movement = _player.AddComponent<PlayerMovement>();
            _movement.OnJump = new UnityEvent();
            _movement.OnFall = new UnityEvent();
            _movement.OnMoveStart = new UnityEvent();
            _movement.OnMoveStopped = new UnityEvent();
            _movement.OnMoveFullyStopped = new UnityEvent();
            _movement.OnTurn = new UnityEvent<bool>();
            _movement.OnLanded = new UnityEvent<float>();

            _eventBus = ScriptableObject.CreateInstance<MovementEventBus>();
            SetMovementEventBus(_movement, _eventBus);

            _movement.InitializeMovement(_stats, _feetCollider, _bodyCollider);
            _timeCursor = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_movement != null && _ability != null)
            {
                _movement.DisableAbility(_ability);
            }

            Object.DestroyImmediate(_eventBus);
            Object.DestroyImmediate(_ability);
            Object.DestroyImmediate(_stats);
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void DashAbility_GroundDashHonorsCooldownAndRaisesEvents()
        {
            EnableDashAbility();

            bool dashStarted = false;
            bool dashEnded = false;
            _eventBus.DashStarted.AddListener(() => dashStarted = true);
            _eventBus.DashEnded.AddListener(() => dashEnded = true);

            var context = _movement.Context;
            context.RuntimeData.IsGrounded = true;
            context.SetInput(new Vector2(1f, 0f), false, false, false, false);

            TriggerDash();
            SimulateFrame(0f);

            Assert.IsInstanceOf<DashState>(_movement.StateMachine.CurrentState);

            float elapsed = 0f;
            while (_movement.StateMachine.CurrentState is DashState && elapsed < 1f)
            {
                SimulateFrame(0.02f);
                elapsed += 0.02f;
                context.RuntimeData.IsGrounded = true;
            }

            Assert.IsInstanceOf<GroundedState>(_movement.StateMachine.CurrentState);
            Assert.IsTrue(dashStarted);
            Assert.IsTrue(dashEnded);
            Assert.Greater(context.RuntimeData.DashCooldownTimer, 0f);

            TriggerDash();
            SimulateFrame(0f);
            Assert.IsInstanceOf<GroundedState>(_movement.StateMachine.CurrentState);

            while (context.RuntimeData.DashCooldownTimer > 0f && elapsed < 2f)
            {
                SimulateFrame(0.02f);
                elapsed += 0.02f;
                context.RuntimeData.IsGrounded = true;
            }

            TriggerDash();
            SimulateFrame(0f);
            Assert.IsInstanceOf<DashState>(_movement.StateMachine.CurrentState);
        }

        [Test]
        public void DashAbility_RespectsAirDashLimit()
        {
            _stats.DashAirDashLimit = 1;
            EnableDashAbility();

            var context = _movement.Context;
            context.RuntimeData.IsGrounded = false;
            context.RuntimeData.IsFastFalling = false;
            context.SetInput(new Vector2(1f, 0f), false, false, false, false);

            _movement.StateMachine.ChangeState<FallingState>();

            TriggerDash();
            SimulateFrame(0f);
            Assert.IsInstanceOf<DashState>(_movement.StateMachine.CurrentState);

            float elapsed = 0f;
            while (_movement.StateMachine.CurrentState is DashState && elapsed < 1f)
            {
                SimulateFrame(0.02f);
                elapsed += 0.02f;
                context.RuntimeData.IsGrounded = false;
            }

            Assert.IsInstanceOf<FallingState>(_movement.StateMachine.CurrentState);
            Assert.AreEqual(1, context.RuntimeData.AirDashCount);

            TriggerDash();
            SimulateFrame(0f);
            Assert.IsInstanceOf<FallingState>(_movement.StateMachine.CurrentState);
        }

        [Test]
        public void DashAbility_RequiresCooldownBeforeNextAirDash()
        {
            _stats.DashAirDashLimit = 2;
            EnableDashAbility();

            var context = _movement.Context;
            context.RuntimeData.IsGrounded = false;
            context.SetInput(new Vector2(1f, 0f), false, false, false, false);

            _movement.StateMachine.ChangeState<FallingState>();

            TriggerDash();
            SimulateFrame(0f);
            Assert.IsInstanceOf<DashState>(_movement.StateMachine.CurrentState);

            float elapsed = 0f;
            while (_movement.StateMachine.CurrentState is DashState && elapsed < 1f)
            {
                SimulateFrame(0.02f);
                elapsed += 0.02f;
                context.RuntimeData.IsGrounded = false;
            }

            Assert.IsInstanceOf<FallingState>(_movement.StateMachine.CurrentState);
            Assert.AreEqual(1, context.RuntimeData.AirDashCount);
            Assert.Greater(context.RuntimeData.AirDashCooldownTimer, 0f);

            TriggerDash();
            SimulateFrame(0f);
            Assert.IsInstanceOf<FallingState>(_movement.StateMachine.CurrentState);

            while (context.RuntimeData.AirDashCooldownTimer > 0f && elapsed < 2f)
            {
                SimulateFrame(0.02f);
                elapsed += 0.02f;
                context.RuntimeData.IsGrounded = false;
            }

            TriggerDash();
            SimulateFrame(0f);
            Assert.IsInstanceOf<DashState>(_movement.StateMachine.CurrentState);
            Assert.AreEqual(2, context.RuntimeData.AirDashCount);
        }

        [Test]
        public void DashAbility_AppliesForwardBurst()
        {
            EnableDashAbility();

            var context = _movement.Context;
            context.RuntimeData.IsGrounded = true;
            context.SetInput(new Vector2(1f, 0f), false, false, false, false);

            TriggerDash();
            SimulateFrame(0f);

            Assert.IsInstanceOf<DashState>(_movement.StateMachine.CurrentState);
            _movement.StateMachine.FixedTick();

            float expected = _stats.DashForwardBurstSpeed;
            Assert.AreEqual(expected, _rigidbody.linearVelocity.x, 0.001f);
        }

        private void TriggerDash()
        {
            float firstTap = _timeCursor;
            float secondTap = _timeCursor + 0.1f;
            _timeCursor += 0.1f;
        }

        private void SimulateFrame(float deltaTime)
        {
            var context = _movement.Context;
            context.UpdateTimers(deltaTime);
            _movement.StateMachine.HandleInput();
            _movement.StateMachine.Tick();
            _movement.StateMachine.FixedTick();
        }

        private void EnableDashAbility()
        {
            bool enabled = _movement.EnableAbility(_ability);
            Assert.IsTrue(enabled);
        }

        private static void SetMovementEventBus(PlayerMovement movement, MovementEventBus eventBus)
        {
            var field = typeof(PlayerMovement).GetField(
                "_movementEventBus",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            field?.SetValue(movement, eventBus);
        }
    }
}
