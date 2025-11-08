using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Runtime.Player.Movement.Events;
using Runtime.Player.Movement;
using Runtime.Player.Movement.Abilities;
using Runtime.Player.Movement.States;
using Object = UnityEngine.Object;

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
        private int _fallEventInvocations;

        [SetUp]
        public void SetUp()
        {
            _stats = ScriptableObject.CreateInstance<PlayerMovementStats>();
            _player = new GameObject("PlayerMovementStateMachineTests");
            _rigidbody = _player.AddComponent<Rigidbody2D>();
            _feetCollider = _player.AddComponent<BoxCollider2D>();
            _bodyCollider = _player.AddComponent<BoxCollider2D>();

            _fallEventInvocations = 0;
            var fallEvent = new UnityEvent();
            fallEvent.AddListener(() => _fallEventInvocations++);

            _context = new PlayerMovementContext(
                _stats,
                _rigidbody,
                _feetCollider,
                _bodyCollider,
                _player.transform,
                new UnityEvent(),
                fallEvent,
                new UnityEvent(),
                new UnityEvent(),
                new UnityEvent(),
                new UnityEvent<bool>(),
                new UnityEvent<float>(),
                null);

            _stateMachine = new PlayerMovementStateMachine(_context);
            RegisterDefaultStates(_stateMachine, _context);
            _stateMachine.Initialize<GroundedState>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stats);
            Object.DestroyImmediate(_player);
        }

        private static void RegisterDefaultStates(PlayerMovementStateMachine stateMachine, PlayerMovementContext context)
        {
            stateMachine.RegisterState(new GroundedState(context, stateMachine));
            stateMachine.RegisterState(new SlidingState(context, stateMachine));
            stateMachine.RegisterState(new JumpingState(context, stateMachine));
            stateMachine.RegisterState(new FallingState(context, stateMachine));
            stateMachine.RegisterState(new FastFallingState(context, stateMachine));
            stateMachine.RegisterState(new WallSlideState(context, stateMachine));
        }

        private PlayerMovement CreatePlayerMovementComponent()
        {
            var movement = _player.AddComponent<PlayerMovement>();
            movement.OnJump = new UnityEvent();
            movement.OnFall = new UnityEvent();
            movement.OnMoveStart = new UnityEvent();
            movement.OnMoveStopped = new UnityEvent();
            movement.OnMoveFullyStopped = new UnityEvent();
            movement.OnTurn = new UnityEvent<bool>();
            movement.OnLanded = new UnityEvent<float>();
            movement.InitializeMovement(_stats, _feetCollider, _bodyCollider);
            return movement;
        }

        private static void SetMovementEventBus(PlayerMovement movement, MovementEventBus eventBus)
        {
            var field = typeof(PlayerMovement).GetField(
                "_movementEventBus",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            field?.SetValue(movement, eventBus);
        }

        // [Test]
        // public void GroundedState_BuffersJump_TransitionsToJumping()
        // {
        //     _context.StartJumpBuffer();
        //     _context.UpdateTimers(-_stats.JumpCoyoteTime);
        //
        //     _stateMachine.HandleInput();
        //
        //     Assert.IsInstanceOf<JumpingState>(_stateMachine.CurrentState);
        //     Assert.AreEqual(1, _context.JumpsCount);
        // }
        //
        // [Test]
        // public void FallingState_BufferedJumpWithoutPreviousJump_ConsumesTwoJumps()
        // {
        //     _stateMachine.ChangeState<FallingState>();
        //     _context.StartJumpBuffer();
        //     _context.UpdateTimers(_stats.JumpCoyoteTime + 0.01f);
        //
        //     _stateMachine.HandleInput();
        //
        //     Assert.IsInstanceOf<JumpingState>(_stateMachine.CurrentState);
        //     Assert.AreEqual(Mathf.Min(2, _stats.NumberOfJumpsAllowed), _context.JumpsCount);
        // }
        //
        // [Test]
        // public void FallingState_BufferedJumpDuringCoyoteTime_AllowsInitialJumpWhenSingleJumpConfigured()
        // {
        //     _stats.NumberOfJumpsAllowed = 1;
        //
        //     _stateMachine.ChangeState<FallingState>();
        //     _context.StartJumpBuffer();
        //
        //     _stateMachine.HandleInput();
        //
        //     Assert.IsInstanceOf<JumpingState>(_stateMachine.CurrentState);
        //     Assert.AreEqual(1, _context.JumpsCount);
        // }
        //
        // [Test]
        // public void JumpingState_BufferedJump_PerformsDoubleJump()
        // {
        //     _context.InitiateJump(1);
        //     _stateMachine.ChangeState<JumpingState>();
        //     _context.StartJumpBuffer();
        //
        //     _stateMachine.HandleInput();
        //
        //     Assert.IsInstanceOf<JumpingState>(_stateMachine.CurrentState);
        //     Assert.AreEqual(Mathf.Min(2, _stats.NumberOfJumpsAllowed), _context.JumpsCount);
        // }
        //
        // [Test]
        // public void ApplyHorizontalMovement_TurnsLeft_InvokesTurnEvent()
        // {
        //     bool eventInvoked = false;
        //     bool? facingRight = null;
        //     _context.OnTurnEvent.AddListener(value =>
        //     {
        //         eventInvoked = true;
        //         facingRight = value;
        //     });
        //
        //     _context.SetInput(new Vector2(-1f, 0f), false, false, false, false);
        //     _context.ApplyHorizontalMovement(_stats.GroundAcceleration, _stats.GroundDeceleration);
        //
        //     Assert.IsTrue(eventInvoked);
        //     Assert.IsFalse(facingRight ?? true);
        // }

        [Test]
        public void FallingState_OnEnter_InvokesFallEventOnce()
        {
            _stateMachine.ChangeState<FallingState>();

            Assert.AreEqual(1, _fallEventInvocations);

            _stateMachine.FixedTick();

            Assert.AreEqual(1, _fallEventInvocations);
        }

        [Test]
        public void FastFallingState_OnEnter_InvokesFallEventOnce()
        {
            _stateMachine.ChangeState<FallingState>();
            _stateMachine.ChangeState<FastFallingState>();

            Assert.AreEqual(2, _fallEventInvocations);

            _stateMachine.FixedTick();

            Assert.AreEqual(2, _fallEventInvocations);
        }

        [Test]
        public void EnableAbility_AddsStateAndAllowsTransition()
        {
            var playerMovement = CreatePlayerMovementComponent();
            var ability = ScriptableObject.CreateInstance<TestMovementAbility>();

            try
            {
                Assert.IsNull(playerMovement.StateMachine.GetState<TestAbilityState>());

                bool enabled = playerMovement.EnableAbility(ability);
                Assert.IsTrue(enabled);

                Assert.IsNotNull(playerMovement.StateMachine.GetState<TestAbilityState>());

                playerMovement.StateMachine.ChangeState<TestAbilityState>();

                Assert.IsInstanceOf<TestAbilityState>(playerMovement.StateMachine.CurrentState);
            }
            finally
            {
                playerMovement.DisableAbility(ability);
                Object.DestroyImmediate(ability);
            }
        }

        [Test]
        public void EnableAbility_RaisesMovementBusEvents()
        {
            var playerMovement = CreatePlayerMovementComponent();
            var ability = ScriptableObject.CreateInstance<TestMovementAbility>();
            var eventBus = ScriptableObject.CreateInstance<MovementEventBus>();

            bool flyStarted = false;
            bool flyEnded = false;
            bool glideStarted = false;
            bool glideEnded = false;
            bool dashStarted = false;
            bool dashEnded = false;

            eventBus.FlyStarted.AddListener(() => flyStarted = true);
            eventBus.FlyEnded.AddListener(() => flyEnded = true);
            eventBus.GlideStarted.AddListener(() => glideStarted = true);
            eventBus.GlideEnded.AddListener(() => glideEnded = true);
            eventBus.DashStarted.AddListener(() => dashStarted = true);
            eventBus.DashEnded.AddListener(() => dashEnded = true);

            try
            {
                SetMovementEventBus(playerMovement, eventBus);
                playerMovement.InitializeMovement(_stats, _feetCollider, _bodyCollider);

                Assert.IsTrue(playerMovement.EnableAbility(ability));

                Assert.IsTrue(flyStarted);
                Assert.IsTrue(flyEnded);
                Assert.IsTrue(glideStarted);
                Assert.IsTrue(glideEnded);
                Assert.IsTrue(dashStarted);
                Assert.IsTrue(dashEnded);
            }
            finally
            {
                playerMovement.DisableAbility(ability);
                Object.DestroyImmediate(ability);
                Object.DestroyImmediate(eventBus);
            }
        }

        [Test]
        public void DisableAbility_RemovesStateAndTransitionsContinueToWork()
        {
            var playerMovement = CreatePlayerMovementComponent();
            var ability = ScriptableObject.CreateInstance<TestMovementAbility>();

            try
            {
                playerMovement.EnableAbility(ability);
                playerMovement.StateMachine.ChangeState<TestAbilityState>();
                Assert.IsInstanceOf<TestAbilityState>(playerMovement.StateMachine.CurrentState);

                playerMovement.DisableAbility(ability);

                Assert.IsInstanceOf<GroundedState>(playerMovement.StateMachine.CurrentState);
                Assert.IsNull(playerMovement.StateMachine.GetState<TestAbilityState>());

                playerMovement.StateMachine.ChangeState<FallingState>();
                Assert.IsInstanceOf<FallingState>(playerMovement.StateMachine.CurrentState);
            }
            finally
            {
                Object.DestroyImmediate(ability);
            }
        }

        private class TestMovementAbility : ScriptableObject, IMovementAbility
        {
            public void Initialize(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            {
            }

            public IEnumerable<IPlayerMovementState> CreateStates(
                PlayerMovementContext context,
                PlayerMovementStateMachine stateMachine)
            {
                yield return new TestAbilityState(context, stateMachine);
            }

            public IEnumerable<IPlayerMovementModifier> CreateModifiers(PlayerMovementContext context)
            {
                return Array.Empty<IPlayerMovementModifier>();
            }

            public IEnumerable<Func<PlayerMovementContext, bool>> CreateActivationConditions(
                PlayerMovementContext context)
            {
                return Array.Empty<Func<PlayerMovementContext, bool>>();
            }

            public void OnAbilityEnabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            {
                context.RaiseFlyStarted();
                context.RaiseFlyEnded();
                context.RaiseGlideStarted();
                context.RaiseGlideEnded();
                context.RaiseDashStarted();
                context.RaiseDashEnded();
            }

            public void OnAbilityDisabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
            {
            }
        }

        private class TestAbilityState : PlayerMovementStateBase
        {
            public TestAbilityState(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
                : base(context, stateMachine)
            {
            }

            public override void OnEnter()
            {
                Context.RuntimeData.IsFalling = false;
            }
        }
    }
}
