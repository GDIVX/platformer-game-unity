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
            UnityEvent onLand,
            UnityEvent onFall,
            UnityEvent<bool> onTurn,
            UnityEvent<Vector2> onMovement)
        {
            Stats = stats;
            Rigidbody = rigidbody;
            FeetCollider = feetCollider;
            BodyCollider = bodyCollider;
            Transform = transform;
            OnJumpEvent = onJump;
            OnLandEvent = onLand;
            OnFallEvent = onFall;
            OnTurnEvent = onTurn;
            OnMovementEvent = onMovement;

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
        public UnityEvent<Vector2> OnMovementEvent { get; }

        public Vector2 Velocity { get; set; }
        public Vector2 TargetVelocity { get; private set; }
        public bool IsFacingRight { get; private set; }

        public float VerticalVelocity { get; set; }
        public bool IsJumping { get; set; }
        public bool IsFalling { get; set; }
        public bool IsFastFalling { get; set; }
        public int JumpsCount { get; set; }
        public float FastFallTime { get; set; }
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

        public Vector2 MoveInput { get; private set; }
        public bool RunHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpReleased { get; private set; }

        public void SetInput(Vector2 moveInput, bool runHeld, bool jumpPressed, bool jumpHeld, bool jumpReleased)
        {
            MoveInput = moveInput;
            RunHeld = runHeld;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
            JumpReleased = jumpReleased;

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

            if (IsGrounded)
            {
                CoyoteTimer = Stats.JumpCoyoteTime;
            }
            else
            {
                CoyoteTimer = Mathf.Max(0f, CoyoteTimer - deltaTime);
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
            if (MoveInput != Vector2.zero)
            {
                TurnCheck(MoveInput);

                var desired = new Vector2(MoveInput.x, 0f);
                float maxSpeed = RunHeld ? Stats.MaxRunSpeed : Stats.MaxWalkSpeed;
                TargetVelocity = desired * maxSpeed;

                Velocity = Vector2.Lerp(Velocity, TargetVelocity, acceleration * Time.fixedDeltaTime);
                OnMovementEvent?.Invoke(Velocity);
            }
            else
            {
                Velocity = Vector2.Lerp(Velocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
                if (Mathf.Abs(Velocity.x) <= Stats.MinSpeedThreshold)
                {
                    Velocity = new Vector2(0f, Velocity.y);
                }
            }

            Rigidbody.linearVelocity = new Vector2(Velocity.x, Rigidbody.linearVelocityY);
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
            IsJumping = false;
            IsFalling = false;
            IsFastFalling = false;
            FastFallTime = 0f;
            IsPastApexThreshold = false;
            JumpsCount = 0;
            VerticalVelocity = Physics2D.gravity.y;

            Velocity = new Vector2(Mathf.Lerp(Velocity.x, 0f, Stats.StickinessOnLanding), Velocity.y);
            Rigidbody.linearVelocity = new Vector2(Velocity.x, Rigidbody.linearVelocityY);

            OnLandEvent?.Invoke();
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
