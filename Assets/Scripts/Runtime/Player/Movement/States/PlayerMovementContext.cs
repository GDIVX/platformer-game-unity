using System;
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
            OnLandedEvent = onLanded;

            CoyoteTimer = stats.JumpCoyoteTime;
            VerticalVelocity = Physics2D.gravity.y;
            IsFacingRight = true;
        }

        public PlayerMovementStats Stats { get; }
        public Rigidbody2D Rigidbody { get; }
        public Collider2D FeetCollider { get; }
        public Collider2D BodyCollider { get; }
        public Transform Transform { get; }
        public UnityEvent OnJumpEvent { get; }
        public UnityEvent OnLandEvent { get; }
        public UnityEvent<bool> OnTurnEvent { get; }
        public UnityEvent OnFallEvent { get; }
        public UnityEvent OnMoveStartEvent { get; }
        public UnityEvent OnMoveStoppedEvent { get; }
        public UnityEvent OnMoveFullyStoppedEvent { get; }
        public UnityEvent<float> OnLandedEvent { get; }

        public Vector2 Velocity { get; set; }
        public Vector2 TargetVelocity { get; private set; }
        public bool IsFacingRight { get; private set; }

        public float VerticalVelocity { get; set; }
        public bool IsJumping { get; set; }
        public bool IsFalling { get; set; }
        public bool IsFastFalling { get; set; }
        public int JumpsCount { get; set; }
        public float FastFallTime { get; set; }
        public float AirTime { get; set; }
        public float FastFallReleaseSpeed { get; set; }

        public float ApexPoint { get; set; }
        public float TimePastApexThreshold { get; set; }
        public bool IsPastApexThreshold { get; set; }

        public float JumpBufferTimer { get; private set; }
        public bool JumpReleasedDuringBuffer { get; private set; }
        public float CoyoteTimer { get; private set; }

        public RaycastHit2D GroundHit { get; private set; }
        public RaycastHit2D HeadHit { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool BumpedHead { get; private set; }

        public RaycastHit2D LeftWallHit { get; private set; }
        public RaycastHit2D RightWallHit { get; private set; }
        public RaycastHit2D WallHit { get; private set; }
        public bool IsTouchingWall { get; private set; }
        public bool IsTouchingLeftWall { get; private set; }
        public bool IsTouchingRightWall { get; private set; }
        public int WallDirection { get; private set; }
        public float WallStickTimer { get; private set; }
        public bool IsWallSliding { get; set; }

        public Vector2 MoveInput { get; private set; }
        public bool RunHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpReleased { get; private set; }

        private bool _hasHorizontalInput;
        private bool _isFullyStopped = true;

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
            AirTime += Time.fixedDeltaTime;

            if (IsGrounded)
            {
                CoyoteTimer = Stats.JumpCoyoteTime;
            }
            else
            {
                CoyoteTimer = Mathf.Max(0f, CoyoteTimer - deltaTime);
            }

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
            VerticalVelocity = Physics2D.gravity.y;
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

        public void InitiateJump(int jumpIncrements)
        {
            if (!IsJumping)
            {
                IsJumping = true;
            }

            ConsumeJumpBuffer();
            JumpReleasedDuringBuffer = false;
            JumpsCount += jumpIncrements;
            VerticalVelocity = Stats.InitialJumpVelocity;
            FastFallTime = 0f;
            IsFastFalling = false;
            IsFalling = false;
            FastFallReleaseSpeed = VerticalVelocity;

            OnJumpEvent?.Invoke();
        }

        public void PerformWallJump()
        {
            if (WallDirection == 0)
            {
                return;
            }

            var settings = Stats.WallSlide;
            if (settings == null)
            {
                return;
            }

            int pushDirection = -WallDirection;

            InitiateJump(1);

            if (settings.WallJumpUpwardBoost > 0f)
            {
                VerticalVelocity += settings.WallJumpUpwardBoost;
            }

            float horizontalPush = settings.WallJumpHorizontalPush * pushDirection;
            Velocity = new Vector2(horizontalPush, Velocity.y);
            Rigidbody.linearVelocity = new Vector2(Velocity.x, Rigidbody.linearVelocityY);

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

            VerticalVelocity = 0f;
            IsFastFalling = true;

            if (IsPastApexThreshold)
            {
                IsPastApexThreshold = false;
                FastFallTime = Stats.TimeForUpwardsCancel;
            }
            else
            {
                FastFallReleaseSpeed = VerticalVelocity;
            }
        }

        public void ApplyFastFall()
        {
            if (FastFallTime >= Stats.TimeForUpwardsCancel)
            {
                VerticalVelocity += Stats.Gravity * Stats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else
            {
                VerticalVelocity = Mathf.Lerp(FastFallReleaseSpeed, 0f,
                    FastFallTime / Stats.TimeForUpwardsCancel);
            }

            FastFallTime += Time.fixedDeltaTime;
            OnFallEvent?.Invoke();
        }

        public void ApplyFall()
        {
            IsFalling = true;
            VerticalVelocity += Stats.Gravity * Time.fixedDeltaTime;
            OnFallEvent?.Invoke();
        }

        public void ApplyWallSlideVertical(PlayerMovementStats.WallSlideSettings settings)
        {
            float gravity = settings.CalculatedGravity != 0f
                ? settings.CalculatedGravity
                : Stats.Gravity * settings.GravityMultiplier;

            VerticalVelocity += gravity * Time.fixedDeltaTime;

            float maxDownward = -Mathf.Abs(settings.MaxSlideSpeed);
            float minDownward = -Mathf.Abs(settings.MinSlideSpeed);
            if (minDownward < maxDownward)
            {
                float temp = maxDownward;
                maxDownward = minDownward;
                minDownward = temp;
            }
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, maxDownward, minDownward);
        }

        public void ApplyWallSlideHorizontal(PlayerMovementStats.WallSlideSettings settings)
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

            float nextX = Mathf.Lerp(Velocity.x, target, rate * Time.fixedDeltaTime);
            Velocity = new Vector2(nextX, Velocity.y);
            Rigidbody.linearVelocity = new Vector2(Velocity.x, Rigidbody.linearVelocityY);
        }

        public void HandleJumpAscent()
        {
            ApexPoint = Mathf.InverseLerp(Stats.InitialJumpVelocity, 0f, VerticalVelocity);

            if (ApexPoint > Stats.ApexThreshold)
            {
                if (!IsPastApexThreshold)
                {
                    IsPastApexThreshold = true;
                    TimePastApexThreshold = 0f;
                }

                TimePastApexThreshold += Time.fixedDeltaTime;
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
                VerticalVelocity += Stats.Gravity * Time.fixedDeltaTime;
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
