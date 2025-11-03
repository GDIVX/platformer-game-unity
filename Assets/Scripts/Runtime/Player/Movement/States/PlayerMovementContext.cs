using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Player.Movement.States
{
    [Serializable]
    public class PlayerMovementContext
    {
        public PlayerMovementContext(
            PlayerMovementStats stats,
            Rigidbody2D rigidbody,
            Collider2D feetCollider,
            Collider2D bodyCollider,
            Transform transform,
            UnityEvent onJump,
            UnityEvent onFall,
            UnityEvent onMoveStart,
            UnityEvent onMoveStopped,
            UnityEvent onMoveFullyStopped,
            UnityEvent<bool> onTurn,
            UnityEvent<float> onLanded)
        {
            Stats = stats;
            Rigidbody = rigidbody;
            FeetCollider = feetCollider;
            BodyCollider = bodyCollider;
            Transform = transform;
            OnJumpEvent = onJump;
            OnFallEvent = onFall;
            OnMoveStartEvent = onMoveStart;
            OnMoveStoppedEvent = onMoveStopped;
            OnMoveFullyStoppedEvent = onMoveFullyStopped;
            OnTurnEvent = onTurn;
            OnLandedEvent = onLanded;

            CoyoteTimer = stats.JumpCoyoteTime;
            VerticalVelocity = Stats.Gravity;
            IsFacingRight = true;
        }

        public PlayerMovementStats Stats { get; }
        public Rigidbody2D Rigidbody { get; }
        public Collider2D FeetCollider { get; }
        public Collider2D BodyCollider { get; }
        public Transform Transform { get; }
        public UnityEvent OnJumpEvent { get; }
        public UnityEvent<bool> OnTurnEvent { get; }
        public UnityEvent OnFallEvent { get; }
        public UnityEvent OnMoveStartEvent { get; }
        public UnityEvent OnMoveStoppedEvent { get; }
        public UnityEvent OnMoveFullyStoppedEvent { get; }
        public UnityEvent<float> OnLandedEvent { get; }

        // ---------------------------- //
        // ────── MOVEMENT DATA ─────── //
        // ---------------------------- //
        [FoldoutGroup("Velocity"), ShowInInspector, ReadOnly]
        public Vector2 Velocity { get; set; }

        [FoldoutGroup("Velocity"), ShowInInspector, ReadOnly]
        public Vector2 TargetVelocity { get; private set; }

        [FoldoutGroup("Velocity"), ShowInInspector, ReadOnly]
        public bool IsFacingRight { get; private set; }

        [FoldoutGroup("Velocity"), ShowInInspector, ReadOnly]
        public float VerticalVelocity { get; set; }

        // ---------------------------- //
        // ──────── JUMPING ─────────── //
        // ---------------------------- //
        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public bool IsJumping { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public float JumpBufferTimer { get; private set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public bool JumpReleasedDuringBuffer { get; private set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public float CoyoteTimer { get; private set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public int JumpsCount { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public float ApexPoint { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public float TimePastApexThreshold { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public bool IsPastApexThreshold { get; set; }

        // ---------------------------- //
        // ──────── AIR STATE ───────── //
        // ---------------------------- //
        [FoldoutGroup("Air State"), ShowInInspector, ReadOnly]
        public bool IsFalling { get; set; }

        [FoldoutGroup("Air State"), ShowInInspector, ReadOnly]
        public bool IsFastFalling { get; set; }

        [FoldoutGroup("Air State"), ShowInInspector, ReadOnly]
        public float AirTime { get; set; }

        [FoldoutGroup("Air State"), ShowInInspector, ReadOnly]
        public float FastFallTime { get; set; }

        [FoldoutGroup("Air State"), ShowInInspector, ReadOnly]
        public float FastFallReleaseSpeed { get; set; }

        // ---------------------------- //
        // ───────── GROUND ─────────── //
        // ---------------------------- //
        [FoldoutGroup("Ground"), ShowInInspector, ReadOnly]
        public bool IsGrounded { get; private set; }

        [FoldoutGroup("Ground"), ShowInInspector, ReadOnly]
        public bool BumpedHead { get; private set; }

        [FoldoutGroup("Ground"), ShowInInspector, ReadOnly]
        public RaycastHit2D GroundHit { get; private set; }

        [FoldoutGroup("Ground"), ShowInInspector, ReadOnly]
        public RaycastHit2D HeadHit { get; private set; }

        // ---------------------------- //
        // ───────── WALLS ──────────── //
        // ---------------------------- //
        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public bool IsTouchingWall { get; private set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public bool IsTouchingLeftWall { get; private set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public bool IsTouchingRightWall { get; private set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public RaycastHit2D LeftWallHit { get; private set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public RaycastHit2D RightWallHit { get; private set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public RaycastHit2D WallHit { get; private set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public int WallDirection { get; private set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public float WallStickTimer { get; private set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public bool IsWallSliding { get; set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public float DirectionBufferTimer { get; private set; }
        [ShowInInspector, ReadOnly , FoldoutGroup("Walls")]public bool WantsToMoveAwayFromWall { get; private set; }


        // ---------------------------- //
        // ───────── INPUTS ─────────── //
        // ---------------------------- //
        [FoldoutGroup("Input"), ShowInInspector, ReadOnly]
        public Vector2 MoveInput { get; private set; }

        [FoldoutGroup("Input"), ShowInInspector, ReadOnly]
        public bool RunHeld { get; private set; }

        [FoldoutGroup("Input"), ShowInInspector, ReadOnly]
        public bool JumpPressed { get; private set; }

        [FoldoutGroup("Input"), ShowInInspector, ReadOnly]
        public bool JumpHeld { get; private set; }

        [FoldoutGroup("Input"), ShowInInspector, ReadOnly]
        public bool JumpReleased { get; private set; }

        // ---------------------------- //
        // ──────── INTERNALS ───────── //
        // ---------------------------- //
        [FoldoutGroup("Internals"), ShowInInspector, ReadOnly]
        private bool _hasHorizontalInput;

        [FoldoutGroup("Internals"), ShowInInspector, ReadOnly]
        private bool _isFullyStopped = true;

        // ---------------------------- //
        // ───────── HELPERS ────────── //
        // ---------------------------- //
        [FoldoutGroup("Helpers"), ShowInInspector, ReadOnly]
        private float WallStickDuration => Stats.WallSlide?.StickDuration ?? 0f;

        public void SetInput(Vector2 moveInput, bool runHeld, bool jumpPressed, bool jumpHeld, bool jumpReleased)
        {
            bool wasMovingHorizontally = _hasHorizontalInput;
            MoveInput = moveInput;
            RunHeld = runHeld;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
            JumpReleased = jumpReleased;

            _hasHorizontalInput = !Mathf.Approximately(MoveInput.x, 0f);

            if (!wasMovingHorizontally && _hasHorizontalInput)
            {
                _isFullyStopped = false;
                OnMoveStartEvent?.Invoke();
            }
            else if (wasMovingHorizontally && !_hasHorizontalInput)
            {
                OnMoveStoppedEvent?.Invoke();
            }

            if (jumpPressed)
            {
                StartJumpBuffer();
            }

            if (jumpReleased)
            {
                RegisterJumpRelease();
            }
        }

        public void UpdateTimers(float deltaTime)
        {
            JumpBufferTimer = Mathf.Max(0f, JumpBufferTimer - deltaTime);

            if (!IsGrounded)
            {
                AirTime += deltaTime;
            }

            CoyoteTimer = IsGrounded ? Stats.JumpCoyoteTime : Mathf.Max(0f, CoyoteTimer - deltaTime);

            if (IsTouchingWall)
            {
                WallStickTimer = WallStickDuration;
            }
            else if (WallStickTimer > 0f)
            {
                WallStickTimer = Mathf.Max(0f, WallStickTimer - deltaTime);
                if (WallStickTimer <= 0f && !IsWallSliding)
                {
                    WallDirection = 0;
                    WallHit = default;
                }
            }

            UpdateDirectionBuffer(deltaTime);
        }



        public void UpdateDirectionBuffer(float deltaTime)
        {
            if (WallDirection == 0)
            {
                WantsToMoveAwayFromWall = false;
                DirectionBufferTimer = 0;
                return;
            }

            // Player is holding opposite direction of wall
            bool movingAway = Mathf.Approximately(Mathf.Sign(MoveInput.x), -WallDirection) && Mathf.Abs(MoveInput.x) > 0.25f;

            if (movingAway)
            {
                WantsToMoveAwayFromWall = true;
                DirectionBufferTimer = Stats.DirectionBufferDuration;
            }
            else if (DirectionBufferTimer > 0)
            {
                DirectionBufferTimer = Mathf.Max(0f, DirectionBufferTimer - deltaTime);
                if (DirectionBufferTimer <= 0)
                    WantsToMoveAwayFromWall = false;
            }
        }


        public void SetGroundHit(RaycastHit2D hit)
        {
            GroundHit = hit;
            IsGrounded = hit.collider != null;
        }

        public void SetHeadHit(RaycastHit2D hit)
        {
            HeadHit = hit;
            BumpedHead = hit.collider != null;
        }

        public void SetWallHit(bool isRight, RaycastHit2D hit)
        {
            if (isRight)
            {
                RightWallHit = hit;
                IsTouchingRightWall = hit.collider != null;
            }
            else
            {
                LeftWallHit = hit;
                IsTouchingLeftWall = hit.collider != null;
            }

            if (hit.collider != null)
            {
                WallStickTimer = WallStickDuration;
                WallDirection = isRight ? 1 : -1;
                WallHit = hit;
            }

            UpdateWallContactState();
        }

        public void ClearWallHit(bool isRight)
        {
            if (isRight)
            {
                RightWallHit = default;
                IsTouchingRightWall = false;
            }
            else
            {
                LeftWallHit = default;
                IsTouchingLeftWall = false;
            }

            UpdateWallContactState();
        }

        public void ClearWallHit()
        {
            LeftWallHit = default;
            RightWallHit = default;
            IsTouchingLeftWall = false;
            IsTouchingRightWall = false;
            UpdateWallContactState();
            if (WallStickTimer <= 0f)
            {
                WallDirection = 0;
                WallHit = default;
            }
        }

        private void UpdateWallContactState()
        {
            IsTouchingWall = IsTouchingLeftWall || IsTouchingRightWall;

            if (IsTouchingLeftWall && IsTouchingRightWall)
            {
                if (LeftWallHit.distance <= RightWallHit.distance)
                {
                    WallDirection = -1;
                    WallHit = LeftWallHit;
                }
                else
                {
                    WallDirection = 1;
                    WallHit = RightWallHit;
                }
            }
            else if (IsTouchingRightWall)
            {
                WallDirection = 1;
                WallHit = RightWallHit;
            }
            else if (IsTouchingLeftWall)
            {
                WallDirection = -1;
                WallHit = LeftWallHit;
            }
            else if (WallStickTimer <= 0f && !IsWallSliding)
            {
                WallDirection = 0;
                WallHit = default;
            }
        }

        public void StartJumpBuffer()
        {
            JumpBufferTimer = Stats.JumpBufferTime;
            JumpReleasedDuringBuffer = false;
        }

        public void RegisterJumpRelease()
        {
            JumpReleasedDuringBuffer = JumpBufferTimer > 0f;
        }

        public void ConsumeJumpBuffer()
        {
            JumpBufferTimer = 0f;
        }

        public void ApplyHorizontalMovement(float acceleration, float deceleration)
        {
            bool wasFullyStopped = _isFullyStopped;

            if (_hasHorizontalInput)
            {
                TurnCheck(MoveInput);

                var desired = new Vector2(MoveInput.x, 0f);
                float maxSpeed = RunHeld ? Stats.MaxRunSpeed : Stats.MaxWalkSpeed;
                TargetVelocity = desired * maxSpeed;

                Velocity = Vector2.Lerp(Velocity, TargetVelocity, acceleration * Time.fixedDeltaTime);
                _isFullyStopped = false;
            }
            else
            {
                TargetVelocity = Vector2.zero;
                Velocity = Vector2.Lerp(Velocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
                if (Mathf.Abs(Velocity.x) <= Stats.MinSpeedThreshold)
                {
                    Velocity = new Vector2(0f, Velocity.y);
                }
            }

            Rigidbody.linearVelocity = new Vector2(Velocity.x, Rigidbody.linearVelocityY);

            bool isFullyStopped = Mathf.Approximately(Velocity.x, 0f);
            if (!wasFullyStopped && isFullyStopped)
            {
                OnMoveFullyStoppedEvent?.Invoke();
            }

            _isFullyStopped = isFullyStopped;
        }

        public void ApplyVerticalVelocity()
        {
            Rigidbody.linearVelocity = new Vector2(Rigidbody.linearVelocityX, VerticalVelocity);
        }

        public void ClampVerticalVelocity()
        {
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, -Stats.MaxFallSpeed, Stats.MaxRiseSpeed);
        }

        public void ApplyLanding()
        {
            float landingForce = VerticalVelocity;
            OnLandedEvent?.Invoke(landingForce);

            AirTime = 0f;
            VerticalVelocity = Stats.Gravity;
            Velocity = new Vector2(Mathf.Lerp(Velocity.x, 0f, Stats.StickinessOnLanding), Velocity.y);
            Rigidbody.linearVelocity = new Vector2(Velocity.x, Rigidbody.linearVelocityY);

            IsJumping = false;
            IsFalling = false;
            IsFastFalling = false;
            FastFallTime = 0f;
            IsPastApexThreshold = false;
            JumpsCount = 0;
            IsWallSliding = false;
            WallStickTimer = 0f;
            WallDirection = 0;
            ClearWallHit();
        }

        public void InitiateJump(int jumpIncrements, float? initialVerticalVelocityOverride = null)
        {
            if (!IsJumping)
            {
                IsJumping = true;
            }

            ConsumeJumpBuffer();
            JumpReleasedDuringBuffer = false;
            JumpsCount += jumpIncrements;
            float targetVertical = initialVerticalVelocityOverride ?? Stats.InitialJumpVelocity;
            VerticalVelocity = targetVertical;
            FastFallTime = 0f;
            IsFastFalling = false;
            IsFalling = false;
            FastFallReleaseSpeed = VerticalVelocity;

            OnJumpEvent?.Invoke();
        }

        public void PerformWallJump(bool isLong = false)
        {
            if (WallDirection == 0)
                return;

            var settings = Stats.WallSlide;
            if (settings == null)
                return;

            int pushDirection = -WallDirection;

            float upwardBoost = Mathf.Max(0f, settings.WallJumpUpwardBoost);
            float horizontalPush = Mathf.Max(0f, settings.WallJumpHorizontalPush);

            if (isLong)
            {
                float verticalMultiplier = Mathf.Max(1f, settings.LongWallJumpUpwardMultiplier);
                float horizontalMultiplier = Mathf.Max(1f, settings.LongWallJumpHorizontalMultiplier);
                upwardBoost *= verticalMultiplier;
                horizontalPush *= horizontalMultiplier;
            }

            float cancelMultiplier = Mathf.Clamp01(settings.WallJumpDownwardCancelMultiplier);
            float preservedVertical = VerticalVelocity < 0f
                ? VerticalVelocity * cancelMultiplier
                : VerticalVelocity;
            float baseVertical = Mathf.Max(0f, preservedVertical);
            float targetVerticalVelocity = Mathf.Max(Stats.InitialJumpVelocity, baseVertical + upwardBoost);

            float currentAwaySpeed = Mathf.Max(0f, Velocity.x * pushDirection);
            float finalHorizontalSpeed = Mathf.Max(horizontalPush, currentAwaySpeed) * pushDirection;
            Velocity = new Vector2(finalHorizontalSpeed, Velocity.y);

            InitiateJump(1, targetVerticalVelocity);

            Rigidbody.linearVelocity = new Vector2(Velocity.x, VerticalVelocity);

            // --- Reset wall-related state ---
            IsWallSliding = false;
            WallStickTimer = 0f;
            WallDirection = 0;
            ClearWallHit();
        }


        public void AttemptJumpCut()
        {
            if (!IsJumping || VerticalVelocity <= 0f)
            {
                return;
            }

            float releaseSpeed = VerticalVelocity;
            FastFallReleaseSpeed = releaseSpeed;

            VerticalVelocity = 0f;
            IsFastFalling = true;

            if (IsPastApexThreshold)
            {
                IsPastApexThreshold = false;
                FastFallTime = Stats.TimeForUpwardsCancel;
            }
            else
            {
                FastFallTime = 0f;
            }
        }

        public void ApplyFastFall(float deltaTime)
        {
            if (FastFallTime >= Stats.TimeForUpwardsCancel)
            {
                VerticalVelocity += Stats.Gravity * Stats.GravityOnReleaseMultiplier * deltaTime;
            }
            else
            {
                VerticalVelocity = Mathf.Lerp(FastFallReleaseSpeed, 0f,
                    FastFallTime / Stats.TimeForUpwardsCancel);
            }

            OnFallEvent?.Invoke();
            FastFallTime += deltaTime;
        }

        public void ApplyFall(float deltaTime)
        {
            IsFalling = true;
            VerticalVelocity += Stats.Gravity * deltaTime;
            VerticalVelocity += Stats.Gravity * Time.fixedDeltaTime;
        }

        public void NotifyFallStarted()
        {
            IsFalling = true;
        }

        public void InvokeFallEvent()
        {
            OnFallEvent?.Invoke();
        }

        public void ApplyWallSlideVertical(PlayerMovementStats.WallSlideSettings settings, float deltaTime)
        {
            float gravity = settings.CalculatedGravity != 0f
                ? settings.CalculatedGravity
                : Stats.Gravity * settings.GravityMultiplier;

            VerticalVelocity += gravity * deltaTime;

            float maxDownward = -Mathf.Abs(settings.MaxSlideSpeed);
            float minDownward = -Mathf.Abs(settings.MinSlideSpeed);
            if (minDownward < maxDownward)
            {
                (maxDownward, minDownward) = (minDownward, maxDownward);
            }

            VerticalVelocity = Mathf.Clamp(VerticalVelocity, maxDownward, minDownward);
        }

        public void ApplyWallSlideHorizontal(PlayerMovementStats.WallSlideSettings settings, float deltaTime)
        {
            float inputX = MoveInput.x;
            int inputDirection = Mathf.Approximately(inputX, 0f) ? 0 : (inputX > 0f ? 1 : -1);

            float target = 0f;
            float rate = settings.HorizontalFriction;

            if (WallDirection != 0 && inputDirection == -WallDirection)
            {
                target = inputX * (RunHeld ? Stats.MaxRunSpeed : Stats.MaxWalkSpeed);
                rate = settings.HorizontalAcceleration;
            }

            float nextX = Mathf.Lerp(Velocity.x, target, rate * deltaTime);
            Velocity = new Vector2(nextX, Velocity.y);
            Rigidbody.linearVelocity = new Vector2(Velocity.x, Rigidbody.linearVelocityY);
        }

        public void HandleJumpAscent(float deltaTime)
        {
            ApexPoint = Mathf.InverseLerp(Stats.InitialJumpVelocity, 0f, VerticalVelocity);

            if (ApexPoint > Stats.ApexThreshold)
            {
                if (!IsPastApexThreshold)
                {
                    IsPastApexThreshold = true;
                    TimePastApexThreshold = 0f;
                }

                TimePastApexThreshold += deltaTime;
                if (TimePastApexThreshold < Stats.ApexHangTime)
                {
                    VerticalVelocity = 0f;
                }
                else
                {
                    VerticalVelocity = -0.01f;
                }
            }
            else
            {
                VerticalVelocity += Stats.Gravity * deltaTime;
                IsPastApexThreshold = false;
            }
        }

        public bool TryEdgeNudge()
        {
            float maxNudge = Stats.HeadNudgeDistance;
            LayerMask groundMask = Stats.GroundLayer;

            Bounds bodyBounds = BodyCollider.bounds;

            Vector2 clearanceSize = new Vector2(bodyBounds.size.x, Stats.HeadDetectionRayLength);
            Vector2 clearanceOriginBase = new Vector2(bodyBounds.center.x, bodyBounds.max.y);

            Vector2[] directions = IsFacingRight
                ? new[] { Vector2.right, Vector2.left }
                : new[] { Vector2.left, Vector2.right };

            int steps = Stats.HeadNudgeSteps;
            float stepSize = steps > 0 ? maxNudge / steps : maxNudge;

            foreach (var dir in directions)
            {
                for (int i = 1; i <= Mathf.Max(1, steps); i++)
                {
                    float dist = stepSize * i;
                    Vector2 offset = dir * dist;

                    bool blockedSide = Physics2D.BoxCast(
                        bodyBounds.center,
                        bodyBounds.size,
                        0f,
                        dir,
                        dist,
                        groundMask);

                    if (blockedSide)
                    {
                        continue;
                    }

                    Vector2 clearanceOrigin = clearanceOriginBase + offset;
                    bool blockedAbove = Physics2D.OverlapBox(
                        clearanceOrigin,
                        clearanceSize,
                        0f,
                        groundMask);

                    if (blockedAbove)
                    {
                        continue;
                    }

                    Transform.position += (Vector3)offset;
                    return true;
                }
            }

            return false;
        }

        public bool ShouldStartWallSlide()
        {
            if (Stats.WallSlide == null)
            {
                return false;
            }

            if (IsGrounded || VerticalVelocity > 0f)
            {
                return false;
            }

            if (!IsTouchingWall && WallStickTimer <= 0f)
            {
                return false;
            }

            int inputDirection = Mathf.Approximately(MoveInput.x, 0f) ? 0 : (MoveInput.x > 0f ? 1 : -1);

            if (IsTouchingWall)
            {
                return WallDirection != 0 && (inputDirection == WallDirection || inputDirection == 0);
            }

            return WallDirection != 0 && WallStickTimer > 0f && inputDirection != -WallDirection;
        }

        public bool CanContinueWallSlide()
        {
            if (Stats.WallSlide == null)
            {
                return false;
            }

            if (IsGrounded || VerticalVelocity > 0f)
            {
                return false;
            }

            int inputDirection = Mathf.Approximately(MoveInput.x, 0f) ? 0 : (MoveInput.x > 0f ? 1 : -1);

            if (IsTouchingWall)
            {
                return WallDirection != 0 && (inputDirection == WallDirection || inputDirection == 0);
            }

            return WallDirection != 0 && WallStickTimer > 0f && inputDirection != -WallDirection;
        }

        public bool ShouldSlide()
        {
            return MoveInput == Vector2.zero && Mathf.Abs(Velocity.x) > Stats.MinSpeedThreshold;
        }

        private void TurnCheck(Vector2 moveInput)
        {
            switch (IsFacingRight)
            {
                case true when moveInput.x < 0f:
                    Turn(false);
                    break;
                case false when moveInput.x > 0f:
                    Turn(true);
                    break;
            }
        }

        private void Turn(bool faceRight)
        {
            if (faceRight == IsFacingRight)
            {
                return;
            }

            IsFacingRight = faceRight;
            Transform.Rotate(0f, faceRight ? 180f : -180f, 0f);
            OnTurnEvent?.Invoke(IsFacingRight);
        }
    }
}