using System;
using Runtime.Player;
using Runtime.Player.Movement.Events;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Player.Movement.Kinematic
{
    [DefaultExecutionOrder(-50)]
    public class KinematicPlayerMovement : MonoBehaviour, IMovementHandler
    {
        [Header("References"), SerializeField]
        private PlayerMovementStats _movementStats;

        [SerializeField] private MovementEventBus _movementEventBus;

        [SerializeField] private Collider2D _feetCollider;
        [SerializeField] private Collider2D _bodyCollider;

        [FoldoutGroup("Events"), SerializeField]
        private UnityEvent _onJump = new();

        [FoldoutGroup("Events"), SerializeField]
        private UnityEvent _onFall = new();

        [FoldoutGroup("Events"), SerializeField]
        private UnityEvent _onMoveStart = new();

        [FoldoutGroup("Events"), SerializeField]
        private UnityEvent _onMoveStopped = new();

        [FoldoutGroup("Events"), SerializeField]
        private UnityEvent _onMoveFullyStopped = new();

        [FoldoutGroup("Events"), SerializeField]
        private UnityEvent<bool> _onTurn = new();

        [FoldoutGroup("Events"), SerializeField]
        private UnityEvent<float> _onLanded = new();

        [Header("Settings"), SerializeField]
        private float _skinWidth = 0.01f;

        private readonly PlayerMovementRuntimeData _runtimeData = new();

        private Vector2 _pendingDisplacement;
        private bool _isInitialized;

        public void Configure(
            PlayerMovementStats stats,
            MovementEventBus movementEventBus,
            Collider2D feetCollider,
            Collider2D bodyCollider,
            Transform sourceTransform,
            UnityEvent onJump,
            UnityEvent onFall,
            UnityEvent onMoveStart,
            UnityEvent onMoveStopped,
            UnityEvent onMoveFullyStopped,
            UnityEvent<bool> onTurn,
            UnityEvent<float> onLanded)
        {
            _movementStats = stats;
            _movementEventBus = movementEventBus;
            _feetCollider = feetCollider;
            _bodyCollider = bodyCollider;

            _onJump = onJump ?? _onJump;
            _onFall = onFall ?? _onFall;
            _onMoveStart = onMoveStart ?? _onMoveStart;
            _onMoveStopped = onMoveStopped ?? _onMoveStopped;
            _onMoveFullyStopped = onMoveFullyStopped ?? _onMoveFullyStopped;
            _onTurn = onTurn ?? _onTurn;
            _onLanded = onLanded ?? _onLanded;

            if (sourceTransform != null)
            {
                transform.position = sourceTransform.position;
                transform.rotation = sourceTransform.rotation;
                transform.localScale = sourceTransform.localScale;
            }

            Initialize();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            InputManager.DashPressed += OnDashPressed;
        }

        private void OnDisable()
        {
            InputManager.DashPressed -= OnDashPressed;
        }

        private void Initialize()
        {
            if (_isInitialized || _movementStats == null || _feetCollider == null || _bodyCollider == null)
            {
                return;
            }

            _runtimeData.VerticalVelocity = _movementStats.Gravity;
            _runtimeData.IsFacingRight = true;
            _runtimeData.Velocity = Vector2.zero;
            _runtimeData.TargetVelocity = Vector2.zero;

            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                Initialize();
                if (!_isInitialized)
                {
                    return;
                }
            }

            ReadInput();
            UpdateTimers(Time.deltaTime);
            HandleStateTransitions();
        }

        private void FixedUpdate()
        {
            if (!_isInitialized)
            {
                return;
            }

            CollisionCheck();
            SimulateKinematicMovement(Time.fixedDeltaTime);
        }

        private void ReadInput()
        {
            bool runHeld = ShouldApplyRunInput(InputManager.RunHeld);
            _runtimeData.MoveInput = InputManager.Movement;
            _runtimeData.RunHeld = runHeld;
            _runtimeData.JumpPressed = InputManager.JumpPressed;
            _runtimeData.JumpHeld = InputManager.JumpHeld;
            _runtimeData.JumpReleased = InputManager.JumpReleased;
        }

        private bool ShouldApplyRunInput(bool desiredRunHeld)
        {
            return desiredRunHeld && !_runtimeData.IsDashing;
        }

        private void UpdateTimers(float deltaTime)
        {
            var data = _runtimeData;
            if (data.JumpBufferTimer > 0f)
            {
                data.JumpBufferTimer = Mathf.Max(0f, data.JumpBufferTimer - deltaTime);
            }

            if (data.CoyoteTimer > 0f)
            {
                data.CoyoteTimer = Mathf.Max(0f, data.CoyoteTimer - deltaTime);
            }

            if (data.DashCooldownTimer > 0f)
            {
                data.DashCooldownTimer = Mathf.Max(0f, data.DashCooldownTimer - deltaTime);
            }

            if (data.AirDashCooldownTimer > 0f)
            {
                data.AirDashCooldownTimer = Mathf.Max(0f, data.AirDashCooldownTimer - deltaTime);
            }

            if (data.DashTimer > 0f)
            {
                data.DashTimer = Mathf.Max(0f, data.DashTimer - deltaTime);
            }

            if (data.DashStopTimer > 0f)
            {
                data.DashStopTimer = Mathf.Max(0f, data.DashStopTimer - deltaTime);
            }

            data.JumpReleasedDuringBuffer |= data.JumpReleased;
        }

        private void HandleStateTransitions()
        {
            var data = _runtimeData;

            if (data.JumpPressed)
            {
                data.JumpBufferTimer = _movementStats.JumpBufferTime;
                data.JumpReleasedDuringBuffer = false;
            }

            if (!data.IsGrounded && data.CoyoteTimer <= 0f && data.GroundHit.collider != null)
            {
                data.CoyoteTimer = _movementStats.JumpCoyoteTime;
            }

            if (data.DashRequested)
            {
                TryBeginDash();
            }

            if (data.JumpBufferTimer > 0f && (data.IsGrounded || data.CoyoteTimer > 0f))
            {
                StartJump();
            }
            else if (!data.IsGrounded && data.VerticalVelocity < 0f && !data.IsDashing)
            {
                data.IsFalling = true;
                _onFall?.Invoke();
            }

            if (data.JumpReleased && data.VerticalVelocity > 0f)
            {
                data.VerticalVelocity *= _movementStats.GravityOnReleaseMultiplier;
            }
        }

        private void StartJump()
        {
            var data = _runtimeData;
            data.JumpBufferTimer = 0f;
            data.CoyoteTimer = 0f;
            data.IsGrounded = false;
            data.IsJumping = true;
            data.IsFalling = false;
            data.IsFastFalling = false;
            data.JumpsCount++;
            data.VerticalVelocity = _movementStats.InitialJumpVelocity;
            _onJump?.Invoke();
        }

        private void TryBeginDash()
        {
            var data = _runtimeData;
            data.DashRequested = false;
            if (data.DashTimer > 0f || data.DashStopTimer > 0f)
            {
                return;
            }

            if (data.IsGrounded && data.DashCooldownTimer > 0f)
            {
                return;
            }

            if (!data.IsGrounded && (data.AirDashCount >= _movementStats.DashAirDashLimit || data.AirDashCooldownTimer > 0f))
            {
                return;
            }

            data.IsDashing = true;
            data.DashTimer = _movementStats.DashDuration;
            data.DashStopTimer = 0f;
            data.DashDirection = data.MoveInput.x == 0f ? (data.IsFacingRight ? 1 : -1) : Math.Sign(data.MoveInput.x);
            data.VerticalVelocity = 0f;

            data.DashRequestFromGround = data.IsGrounded;

            if (data.IsGrounded)
            {
                data.DashCooldownTimer = _movementStats.DashGroundCooldown;
            }
            else
            {
                data.AirDashCount++;
                data.AirDashCooldownTimer = _movementStats.DashAirDashCooldown;
            }

            _movementEventBus?.RaiseDashStarted();
        }

        private void CollisionCheck()
        {
            CheckIfGrounded();
            CheckIfBumpedHead();
            CheckWallContact();
        }

        private void SimulateKinematicMovement(float deltaTime)
        {
            var data = _runtimeData;
            _pendingDisplacement = Vector2.zero;

            if (data.IsDashing)
            {
                HandleDash(deltaTime);
            }
            else
            {
                HandleHorizontalMovement(deltaTime);
                HandleVerticalMovement(deltaTime);
            }

            ApplyDisplacement();

            if (data.IsDashing && data.DashTimer <= 0f)
            {
                data.IsDashing = false;
                data.DashStopTimer = _movementStats.DashPostDashStopDuration;
                _movementEventBus?.RaiseDashEnded();
            }
        }

        private void HandleHorizontalMovement(float deltaTime)
        {
            var data = _runtimeData;
            bool hadInput = Mathf.Abs(data.MoveInput.x) > 0f;

            if (hadInput)
            {
                if (data.IsGrounded && data.IsTouchingWall && !data.IsWallSliding)
                {
                    return;
                }

                TurnCheck(data.MoveInput);
                float maxSpeed = data.RunHeld ? _movementStats.MaxRunSpeed : _movementStats.MaxWalkSpeed;
                data.TargetVelocity = new Vector2(data.MoveInput.x * maxSpeed, data.TargetVelocity.y);
                float acceleration = data.IsGrounded ? _movementStats.GroundAcceleration : _movementStats.AirAcceleration;
                data.Velocity = Vector2.Lerp(data.Velocity, new Vector2(data.TargetVelocity.x, data.Velocity.y), acceleration * deltaTime);

                if (!_runtimeData.IsGrounded)
                {
                    data.DirectionBufferTimer = _movementStats.DirectionBufferDuration;
                }

                if (Mathf.Abs(data.Velocity.x) > 0f)
                {
                    _onMoveStart?.Invoke();
                }
            }
            else
            {
                float deceleration = data.IsGrounded ? _movementStats.GroundDeceleration : _movementStats.AirDeceleration;
                data.Velocity = Vector2.Lerp(data.Velocity, new Vector2(0f, data.Velocity.y), deceleration * deltaTime);
                data.TargetVelocity = new Vector2(0f, data.TargetVelocity.y);

                if (Mathf.Abs(data.Velocity.x) <= _movementStats.MinSpeedThreshold)
                {
                    data.Velocity = new Vector2(0f, data.Velocity.y);
                    _onMoveFullyStopped?.Invoke();
                }
                else
                {
                    _onMoveStopped?.Invoke();
                }
            }

            _pendingDisplacement += Vector2.right * data.Velocity.x * deltaTime;
        }

        private void HandleVerticalMovement(float deltaTime)
        {
            var data = _runtimeData;

            if (data.IsTouchingWall && !data.IsGrounded && data.VerticalVelocity <= 0f)
            {
                data.IsWallSliding = true;
                float targetSlide = Mathf.Clamp(
                    data.VerticalVelocity + _movementStats.WallSlide.CalculatedGravity * deltaTime,
                    -_movementStats.WallSlide.MaxSlideSpeed,
                    -_movementStats.WallSlide.MinSlideSpeed);
                data.VerticalVelocity = targetSlide;
            }
            else
            {
                data.IsWallSliding = false;
                data.VerticalVelocity = Mathf.Max(
                    data.VerticalVelocity + _movementStats.Gravity * deltaTime,
                    -_movementStats.MaxFallSpeed);
            }

            _pendingDisplacement += Vector2.up * data.VerticalVelocity * deltaTime;
        }

        private void HandleDash(float deltaTime)
        {
            var data = _runtimeData;
            float dashSpeed = _movementStats.DashForwardBurstSpeed * data.DashDirection;
            data.Velocity = new Vector2(dashSpeed, 0f);
            data.VerticalVelocity = 0f;

            _pendingDisplacement += data.Velocity * deltaTime;
        }

        private void OnDashPressed()
        {
            _runtimeData.DashRequested = true;
        }

        private void ApplyDisplacement()
        {
            Vector2 remainder = _pendingDisplacement;
            Vector2 position = transform.position;

            if (!Mathf.Approximately(remainder.x, 0f))
            {
                ResolveAxis(ref position, Vector2.right, remainder.x, ref _runtimeData.Velocity.x, out bool hitWall);
                _runtimeData.IsTouchingWall = hitWall;
            }
            else
            {
                _runtimeData.IsTouchingWall = false;
            }

            if (!Mathf.Approximately(remainder.y, 0f))
            {
                ResolveAxis(ref position, Vector2.up, remainder.y, ref _runtimeData.VerticalVelocity, out bool hitVertical);
                if (hitVertical && remainder.y < 0f)
                {
                    _runtimeData.IsGrounded = true;
                    _runtimeData.CoyoteTimer = _movementStats.JumpCoyoteTime;
                    _runtimeData.AirDashCount = 0;
                    _onLanded?.Invoke(Mathf.Abs(_runtimeData.VerticalVelocity));
                }
                else if (hitVertical && remainder.y > 0f)
                {
                    _runtimeData.BumpedHead = true;
                }
                else
                {
                    _runtimeData.IsGrounded = false;
                }
            }

            transform.position = position;
        }

        private void ResolveAxis(
            ref Vector2 position,
            Vector2 axis,
            float distance,
            ref float velocityComponent,
            out bool collided)
        {
            collided = false;
            float direction = Mathf.Sign(distance);
            Vector2 displacement = axis * distance;
            float magnitude = Mathf.Abs(distance);

            Vector2 size = _bodyCollider.bounds.size;
            Vector2 center = _bodyCollider.bounds.center;

            RaycastHit2D hit = Physics2D.BoxCast(
                center,
                size,
                0f,
                axis * direction,
                magnitude + _skinWidth,
                _movementStats.GroundLayer);

            if (hit.collider != null)
            {
                collided = true;
                float adjustedDistance = Mathf.Max(0f, hit.distance - _skinWidth);
                displacement = axis * adjustedDistance * direction;
                velocityComponent = 0f;

                if (axis == Vector2.right)
                {
                    _runtimeData.WallHit = hit;
                    _runtimeData.WallDirection = direction > 0f ? 1 : -1;
                }
                else if (axis == Vector2.up)
                {
                    if (direction < 0f)
                    {
                        _runtimeData.GroundHit = hit;
                    }
                    else
                    {
                        _runtimeData.HeadHit = hit;
                    }
                }
            }

            position += displacement;
        }

        private void CheckIfGrounded()
        {
            Vector2 boxCastOrigin = new(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
            Vector2 boxCastSize = new(_feetCollider.bounds.size.x, _movementStats.GroundDetectionRayLength);

            RaycastHit2D groundHit = Physics2D.BoxCast(
                boxCastOrigin,
                boxCastSize,
                0f,
                Vector2.down,
                _movementStats.GroundDetectionRayLength,
                _movementStats.GroundLayer);

            _runtimeData.GroundHit = groundHit;
            _runtimeData.IsGrounded = groundHit.collider != null;
        }

        private void CheckIfBumpedHead()
        {
            Vector2 headBoxOrigin = new(_bodyCollider.bounds.center.x, _bodyCollider.bounds.max.y);
            Vector2 headBoxSize = new(_bodyCollider.bounds.size.x * _movementStats.HeadWidth, _movementStats.HeadDetectionRayLength);

            RaycastHit2D headHit = Physics2D.BoxCast(
                headBoxOrigin,
                headBoxSize,
                0f,
                Vector2.up,
                _movementStats.HeadDetectionRayLength,
                _movementStats.GroundLayer);

            _runtimeData.HeadHit = headHit;
            _runtimeData.BumpedHead = headHit.collider != null;
        }

        private void CheckWallContact()
        {
            float castDistance = _movementStats.WallSlide.WallDetectionHorizontalDistance;
            Vector2 castOriginRight = new(_bodyCollider.bounds.max.x, _bodyCollider.bounds.center.y);
            Vector2 castOriginLeft = new(_bodyCollider.bounds.min.x, _bodyCollider.bounds.center.y);

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

            _runtimeData.RightWallHit = rightHit;
            _runtimeData.LeftWallHit = leftHit;
            _runtimeData.IsTouchingRightWall = rightHit.collider != null;
            _runtimeData.IsTouchingLeftWall = leftHit.collider != null;
            _runtimeData.IsTouchingWall = _runtimeData.IsTouchingLeftWall || _runtimeData.IsTouchingRightWall;
            _runtimeData.WallDirection = _runtimeData.IsTouchingRightWall ? 1 : _runtimeData.IsTouchingLeftWall ? -1 : 0;
            _runtimeData.WallHit = _runtimeData.IsTouchingRightWall ? rightHit : leftHit;
        }

        private void TurnCheck(Vector2 moveInput)
        {
            if (_runtimeData.IsFacingRight && moveInput.x < 0f)
            {
                Turn(false);
            }
            else if (!_runtimeData.IsFacingRight && moveInput.x > 0f)
            {
                Turn(true);
            }
        }

        private void Turn(bool faceRight)
        {
            if (faceRight == _runtimeData.IsFacingRight)
            {
                return;
            }

            _runtimeData.IsFacingRight = faceRight;
            transform.Rotate(0f, faceRight ? 180f : -180f, 0f);
            _onTurn?.Invoke(_runtimeData.IsFacingRight);
        }

        public Vector2 Velocity => _runtimeData.Velocity;

        public float VerticalVelocity => _runtimeData.VerticalVelocity;

        public void SetVelocity(Vector2 velocity)
        {
            _runtimeData.Velocity = velocity;
            _runtimeData.VerticalVelocity = velocity.y;
        }

        public void AddVelocity(Vector2 delta)
        {
            SetVelocity(new Vector2(_runtimeData.Velocity.x + delta.x, _runtimeData.VerticalVelocity + delta.y));
        }

        public void SetVerticalVelocity(float verticalVelocity)
        {
            _runtimeData.VerticalVelocity = verticalVelocity;
            _runtimeData.Velocity = new Vector2(_runtimeData.Velocity.x, verticalVelocity);
        }

        public void AddVerticalVelocity(float delta)
        {
            SetVerticalVelocity(_runtimeData.VerticalVelocity + delta);
        }
    }
}
