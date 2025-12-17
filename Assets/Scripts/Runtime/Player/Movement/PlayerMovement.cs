using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Runtime.Movement;
using Runtime.Player.Movement.Abilities;
using Runtime.Player.Movement.Events;
using Runtime.Player.Movement.States;

namespace Runtime.Player.Movement
{
    public class PlayerMovement : MonoBehaviour, IMovementHandler
    {
        [Header("References")] [SerializeField]
        private PlayerMovementStats _movementStats;

        [SerializeField] private MovementEventBus _movementEventBus;

        [SerializeField] private Collider2D _feetCollider;
        [SerializeField] private Collider2D _bodyCollider;

        [FoldoutGroup("Events")] public UnityEvent OnJump;
        [FoldoutGroup("Events")] public UnityEvent OnFall;
        [FoldoutGroup("Events")] public UnityEvent OnMoveStart;
        [FoldoutGroup("Events")] public UnityEvent OnMoveStopped;
        [FoldoutGroup("Events")] public UnityEvent OnMoveFullyStopped;
        [FoldoutGroup("Events")] public UnityEvent<bool> OnTurn;
        [FoldoutGroup("Events")] public UnityEvent<float> OnLanded;

        [Header("Abilities")] [SerializeField] private List<MovementAbility> _serializedAbilities;

        [ShowInInspector, ReadOnly] public PlayerMovementContext Context { get; private set; }

        public PlayerMovementStateMachine StateMachine => _stateMachine;

        private readonly List<IMovementAbility> _configuredAbilities = new List<IMovementAbility>();

        private readonly Dictionary<IMovementAbility, AbilityRuntimeData> _abilityRuntimeData =
            new Dictionary<IMovementAbility, AbilityRuntimeData>();

        private Rigidbody2D _rb;
        private PlayerMovementStateMachine _stateMachine;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            if (_movementStats == null)
            {
                return;
            }

            InitializeMovement(_movementStats, _feetCollider, _bodyCollider);
        }

        private void OnEnable()
        {
            if (_movementStats != null)
            {
                _movementStats.SlideMovement.selectedCollider = _feetCollider;
            }

            if (_stateMachine != null && Context != null)
            {
                EnableConfiguredAbilities();
            }
        }

        private void OnDisable()
        {
            DisableAllAbilities();
        }

        public void InitializeMovement(PlayerMovementStats stats, Collider2D feetCollider, Collider2D bodyCollider)
        {
            DisableAllAbilities();

            _movementStats = stats;
            _feetCollider = feetCollider;
            _bodyCollider = bodyCollider;

            if (_movementStats == null)
            {
                Context = null;
                _stateMachine = null;
                return;
            }

            if (_movementStats != null)
            {
                _movementStats.SlideMovement.selectedCollider = _feetCollider;
            }

            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody2D>();
            }

            BuildContext();
            BuildStateMachine();
        }

        [Button]
        public bool EnableAbility(IMovementAbility ability)
        {
            if (ability == null)
            {
                return false;
            }

            if (!_configuredAbilities.Contains(ability))
            {
                _configuredAbilities.Add(ability);
            }

            return EnableAbilityInternal(ability);
        }

        [Button]
        public void DisableAbility(IMovementAbility ability)
        {
            if (ability == null)
            {
                return;
            }

            DisableAbilityInternal(ability);
        }

        public IReadOnlyList<Func<PlayerMovementContext, bool>> GetActivationConditions(IMovementAbility ability)
        {
            if (ability == null)
            {
                return Array.Empty<Func<PlayerMovementContext, bool>>();
            }

            return _abilityRuntimeData.TryGetValue(ability, out var runtimeData)
                ? runtimeData.ActivationConditions
                : Array.Empty<Func<PlayerMovementContext, bool>>();
        }

        private void BuildContext()
        {
            if (_movementStats == null)
            {
                Context = null;
                return;
            }

            Context = new PlayerMovementContext(
                _movementStats,
                _rb,
                _feetCollider,
                _bodyCollider,
                transform,
                OnJump,
                OnFall,
                OnMoveStart,
                OnMoveStopped,
                OnMoveFullyStopped,
                OnTurn,
                OnLanded,
                _movementEventBus);

            CollisionCheck();
        }

        private void BuildStateMachine()
        {
            if (Context == null)
            {
                _stateMachine = null;
                return;
            }

            _stateMachine = new PlayerMovementStateMachine(Context);
            RegisterDefaultStates();
            PrepareConfiguredAbilities();
            EnableConfiguredAbilities();
            _stateMachine.Initialize<GroundedState>();
        }

        private void RegisterDefaultStates()
        {
            if (_stateMachine == null)
            {
                return;
            }

            _stateMachine.RegisterState(new GroundedState(Context, _stateMachine));
            _stateMachine.RegisterState(new SlidingState(Context, _stateMachine));
            _stateMachine.RegisterState(new JumpingState(Context, _stateMachine));
            _stateMachine.RegisterState(new FallingState(Context, _stateMachine));
            _stateMachine.RegisterState(new FastFallingState(Context, _stateMachine));
            _stateMachine.RegisterState(new WallSlideState(Context, _stateMachine));
        }

        private void PrepareConfiguredAbilities()
        {
            _configuredAbilities.Clear();

            foreach (var ability in GetComponents<IMovementAbility>())
            {
                if (ability != null)
                {
                    _configuredAbilities.Add(ability);
                }
            }

            if (_serializedAbilities == null || _serializedAbilities.Count == 0)
            {
                return;
            }

            foreach (var ability in _serializedAbilities)
            {
                if (ability == null)
                {
                    continue;
                }

                if (_configuredAbilities.Contains(ability))
                {
                    continue;
                }

                _configuredAbilities.Add(ability);
            }
        }

        private void EnableConfiguredAbilities()
        {
            if (_stateMachine == null)
            {
                return;
            }

            foreach (var ability in _configuredAbilities)
            {
                EnableAbilityInternal(ability);
            }
        }

        private bool EnableAbilityInternal(IMovementAbility ability)
        {
            if (ability == null || _stateMachine == null)
            {
                return false;
            }

            if (_abilityRuntimeData.ContainsKey(ability))
            {
                return false;
            }

            ability.Initialize(Context, _stateMachine);

            var runtimeData = new AbilityRuntimeData();

            var states = ability.CreateStates(Context, _stateMachine);
            if (states != null)
            {
                foreach (var state in states)
                {
                    if (state == null)
                    {
                        continue;
                    }

                    if (_stateMachine.RegisterState(state))
                    {
                        runtimeData.States.Add(state);
                    }
                }
            }

            var modifiers = ability.CreateModifiers(Context);
            if (modifiers != null)
            {
                foreach (var modifier in modifiers)
                {
                    if (modifier == null)
                    {
                        continue;
                    }

                    modifier.Apply(Context);
                    runtimeData.Modifiers.Add(modifier);
                }
            }

            var activationConditions = ability.CreateActivationConditions(Context);
            if (activationConditions != null)
            {
                foreach (var condition in activationConditions)
                {
                    if (condition == null)
                    {
                        continue;
                    }

                    runtimeData.ActivationConditions.Add(condition);
                }
            }

            ability.OnAbilityEnabled(Context, _stateMachine);
            _abilityRuntimeData[ability] = runtimeData;
            return true;
        }

        private void DisableAbilityInternal(IMovementAbility ability)
        {
            if (ability == null)
            {
                return;
            }

            if (!_abilityRuntimeData.TryGetValue(ability, out var runtimeData))
            {
                return;
            }

            foreach (var modifier in runtimeData.Modifiers)
            {
                modifier?.Remove(Context);
            }

            foreach (var state in runtimeData.States)
            {
                if (state == null)
                {
                    continue;
                }

                var stateMachine = _stateMachine;
                if (stateMachine == null)
                {
                    continue;
                }

                stateMachine.UnregisterState(state.GetType());
            }

            ability.OnAbilityDisabled(Context, _stateMachine);
            _abilityRuntimeData.Remove(ability);

            if (_stateMachine != null && _stateMachine.CurrentState == null)
            {
                RestoreDefaultStateIfNoneActive();
            }
        }

        private void DisableAllAbilities()
        {
            if (_abilityRuntimeData.Count == 0)
            {
                return;
            }

            var abilities = _abilityRuntimeData.Keys.ToArray();
            foreach (var ability in abilities)
            {
                DisableAbilityInternal(ability);
            }
        }

        private void RestoreDefaultStateIfNoneActive()
        {
            var stateMachine = _stateMachine;
            if (stateMachine == null || stateMachine.CurrentState != null)
            {
                return;
            }

            if (stateMachine.GetState<GroundedState>() != null)
            {
                stateMachine.ChangeState<GroundedState>();
                return;
            }

            foreach (var state in stateMachine.RegisteredStates)
            {
                if (state == null)
                {
                    continue;
                }

                if (stateMachine.ChangeState(state.GetType()))
                {
                    return;
                }
            }
        }

        private void Update()
        {
            if (Context == null || _stateMachine == null)
            {
                return;
            }

            ReadInput();
            Context.UpdateTimers(Time.deltaTime);
            _stateMachine.HandleInput();
            _stateMachine.Tick();
        }

        private void FixedUpdate()
        {
            if (Context == null || _stateMachine == null)
            {
                return;
            }

            CollisionCheck();
            _stateMachine.FixedTick();
        }

        private void ReadInput()
        {
            if (Context == null)
            {
                return;
            }

            bool runHeld = ShouldApplyRunInput(InputManager.RunHeld);
            Context.SetInput(
                InputManager.Movement,
                runHeld,
                InputManager.JumpPressed,
                InputManager.JumpHeld,
                InputManager.JumpReleased);
        }

        private bool ShouldApplyRunInput(bool desiredRunHeld)
        {
            if (!desiredRunHeld)
            {
                return false;
            }

            bool dashStateAvailable = _stateMachine?.GetState<DashState>() != null;
            if (!dashStateAvailable)
            {
                return desiredRunHeld;
            }

            var data = Context?.RuntimeData;
            if (data == null)
            {
                return desiredRunHeld;
            }

            if (data.IsDashing || data.DashRequested)
            {
                return false;
            }

            return desiredRunHeld;
        }

        private void CollisionCheck()
        {
            if (_movementStats == null)
            {
                return;
            }

            CheckIfGrounded();
            CheckIfBumpedHead();
            CheckWallContact();
        }

        private void CheckIfGrounded()
        {
            if (_feetCollider == null || _movementStats == null)
            {
                return;
            }

            Vector2 boxCastOrigin = new Vector2(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
            Vector2 boxCastSize = new Vector2(_feetCollider.bounds.size.x, _movementStats.GroundDetectionRayLength);

            RaycastHit2D groundHit = Physics2D.BoxCast(
                boxCastOrigin,
                boxCastSize,
                0f,
                Vector2.down,
                _movementStats.GroundDetectionRayLength,
                _movementStats.GroundLayer);

            Context.SetGroundHit(groundHit);

#if UNITY_EDITOR
            if (_movementStats.DebugShowIsGrounded)
            {
                Color rayColor = Context.RuntimeData.IsGrounded ? Color.green : Color.red;
                Debug.DrawRay(
                    new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y),
                    Vector2.down * _movementStats.GroundDetectionRayLength,
                    rayColor);
                Debug.DrawRay(
                    new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y),
                    Vector2.down * _movementStats.GroundDetectionRayLength,
                    rayColor);
                Debug.DrawRay(
                    new Vector2(
                        boxCastOrigin.x - boxCastSize.x / 2,
                        boxCastOrigin.y - _movementStats.GroundDetectionRayLength),
                    Vector2.right * boxCastSize.x,
                    rayColor);
            }
#endif
        }

        private void CheckIfBumpedHead()
        {
            if (_feetCollider == null || _bodyCollider == null || _movementStats == null)
            {
                return;
            }

            Vector2 boxCastOrigin = new Vector2(_feetCollider.bounds.center.x, _bodyCollider.bounds.max.y);
            Vector2 boxCastSize = new Vector2(
                _feetCollider.bounds.size.x * _movementStats.HeadWidth,
                _movementStats.HeadDetectionRayLength);

            RaycastHit2D headHit = Physics2D.BoxCast(
                boxCastOrigin,
                boxCastSize,
                0f,
                Vector2.up,
                _movementStats.HeadDetectionRayLength,
                _movementStats.GroundLayer);

            Context.WallController.SetHeadHit(headHit);

#if UNITY_EDITOR
            if (_movementStats.DebugShowHeadBumpBox)
            {
                Color rayColor = Context.RuntimeData.BumpedHead ? Color.green : Color.red;
                Debug.DrawRay(
                    new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y),
                    Vector2.up * _movementStats.HeadDetectionRayLength,
                    rayColor);
                Debug.DrawRay(
                    new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y),
                    Vector2.up * _movementStats.HeadDetectionRayLength,
                    rayColor);
                Debug.DrawRay(
                    new Vector2(
                        boxCastOrigin.x - boxCastSize.x / 2,
                        boxCastOrigin.y - _movementStats.HeadDetectionRayLength),
                    Vector2.right * boxCastSize.x,
                    rayColor);
            }
#endif
        }

        private void CheckWallContact()
        {
            if (_movementStats == null)
            {
                return;
            }

            if (_bodyCollider == null)
            {
                Context.WallController.ClearWallHit(true);
                Context.WallController.ClearWallHit(false);
                return;
            }

            var wallSettings = _movementStats.WallSlide;
            float castDistance = wallSettings?.WallDetectionHorizontalDistance ?? _movementStats.WallDetectionRayLength;
            castDistance = Mathf.Max(0f, castDistance);

            float heightScale = Mathf.Clamp01(_movementStats.WallDetectionHeightScale);
            if (wallSettings != null && _bodyCollider != null)
            {
                float verticalShrink = Mathf.Clamp01(wallSettings.WallDetectionVerticalShrink);
                float colliderHeight = _bodyCollider.bounds.size.y * (1f - verticalShrink);
                float colliderCenterY = _bodyCollider.bounds.center.y;

                Vector2 castOriginRight = new Vector2(_bodyCollider.bounds.max.x, colliderCenterY);
                Vector2 castOriginLeft = new Vector2(_bodyCollider.bounds.min.x, colliderCenterY);
                Vector2 castSize = new Vector2(castDistance, colliderHeight * heightScale);

                RaycastHit2D rightHit = Physics2D.BoxCast(
                    castOriginRight,
                    castSize,
                    0f,
                    Vector2.right,
                    castDistance,
                    _movementStats.GroundLayer);

                RaycastHit2D leftHit = Physics2D.BoxCast(
                    castOriginLeft,
                    castSize,
                    0f,
                    Vector2.left,
                    castDistance,
                    _movementStats.GroundLayer);

                Context.WallController.SetWallHit(true, rightHit);
                Context.WallController.SetWallHit(false, leftHit);
            }
            else
            {
                Vector2 castOriginRight = new Vector2(_bodyCollider.bounds.max.x, _bodyCollider.bounds.center.y);
                Vector2 castOriginLeft = new Vector2(_bodyCollider.bounds.min.x, _bodyCollider.bounds.center.y);

                RaycastHit2D rightHit = Physics2D.Raycast(
                    castOriginRight,
                    Vector2.right,
                    castDistance,
                    _movementStats.GroundLayer);
                RaycastHit2D leftHit = Physics2D.Raycast(
                    castOriginLeft,
                    Vector2.left,
                    castDistance,
                    _movementStats.GroundLayer);

                Context.WallController.SetWallHit(true, rightHit);
                Context.WallController.SetWallHit(false, leftHit);
            }
        }

        public Vector2 Velocity
        {
            get
            {
                if (Context?.RuntimeData != null)
                {
                    var data = Context.RuntimeData;
                    return new Vector2(data.Velocity.x, data.VerticalVelocity);
                }

                return _rb != null ? _rb.linearVelocity : Vector2.zero;
            }
        }

        public float VerticalVelocity
        {
            get
            {
                if (Context?.RuntimeData != null)
                {
                    return Context.RuntimeData.VerticalVelocity;
                }

                return _rb != null ? _rb.linearVelocity.y : 0f;
            }
        }

        public void SetVelocity(Vector2 velocity)
        {
            if (Context?.RuntimeData != null)
            {
                var data = Context.RuntimeData;
                data.Velocity = new Vector2(velocity.x, velocity.y);
                data.VerticalVelocity = velocity.y;
                SyncRuntimeVelocityToRigidbody();
                return;
            }

            if (_rb != null)
            {
                _rb.linearVelocity = velocity;
            }
        }

        public void AddVelocity(Vector2 delta)
        {
            if (Context?.RuntimeData != null)
            {
                var data = Context.RuntimeData;
                data.Velocity = new Vector2(data.Velocity.x + delta.x, data.Velocity.y + delta.y);
                data.VerticalVelocity += delta.y;
                SyncRuntimeVelocityToRigidbody();
                return;
            }

            if (_rb != null)
            {
                _rb.linearVelocity += delta;
            }
        }

        public void SetVerticalVelocity(float verticalVelocity)
        {
            if (Context?.RuntimeData != null)
            {
                var data = Context.RuntimeData;
                data.VerticalVelocity = verticalVelocity;
                data.Velocity = new Vector2(data.Velocity.x, verticalVelocity);
                SyncRuntimeVelocityToRigidbody();
                return;
            }

            if (_rb != null)
            {
                Vector2 velocity = _rb.linearVelocity;
                velocity.y = verticalVelocity;
                _rb.linearVelocity = velocity;
            }
        }

        public void AddVerticalVelocity(float delta)
        {
            SetVerticalVelocity(VerticalVelocity + delta);
        }

        private void SyncRuntimeVelocityToRigidbody()
        {
            if (_rb == null || Context?.RuntimeData == null)
            {
                return;
            }

            var data = Context.RuntimeData;
            _rb.linearVelocity = new Vector2(data.Velocity.x, data.VerticalVelocity);
        }

        private class AbilityRuntimeData
        {
            public readonly List<IPlayerMovementState> States = new List<IPlayerMovementState>();
            public readonly List<IPlayerMovementModifier> Modifiers = new List<IPlayerMovementModifier>();

            public readonly List<Func<PlayerMovementContext, bool>> ActivationConditions =
                new List<Func<PlayerMovementContext, bool>>();
        }
    }
}