using System;
using Runtime.Player.Movement;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Player.Movement.Controllers
{
    [Serializable]
    public class JumpController
    {
        private readonly PlayerMovementRuntimeData _data;
        private readonly PlayerMovementStats _stats;
        private readonly Rigidbody2D _rigidbody;
        private readonly Transform _transform;
        private readonly Collider2D _bodyCollider;
        private readonly UnityEvent _onJumpEvent;
        private readonly UnityEvent _onFallEvent;
        private readonly UnityEvent<float> _onLandedEvent;

        private HorizontalMovementController _horizontalController;
        private WallInteractionController _wallController;

        public JumpController(
            PlayerMovementRuntimeData data,
            PlayerMovementStats stats,
            Rigidbody2D rigidbody,
            Transform transform,
            Collider2D bodyCollider,
            UnityEvent onJump,
            UnityEvent onFall,
            UnityEvent<float> onLanded)
        {
            _data = data;
            _stats = stats;
            _rigidbody = rigidbody;
            _transform = transform;
            _bodyCollider = bodyCollider;
            _onJumpEvent = onJump;
            _onFallEvent = onFall;
            _onLandedEvent = onLanded;
        }

        public void ConfigureDependencies(
            WallInteractionController wallController,
            HorizontalMovementController horizontalController)
        {
            _wallController = wallController;
            _horizontalController = horizontalController;
        }

        public void SetJumpInput(bool jumpPressed, bool jumpHeld, bool jumpReleased)
        {
            _data.JumpPressed = jumpPressed;
            _data.JumpHeld = jumpHeld;
            _data.JumpReleased = jumpReleased;

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
            _data.JumpBufferTimer = Mathf.Max(0f, _data.JumpBufferTimer - deltaTime);

            if (!_data.IsGrounded)
            {
                _data.AirTime += deltaTime;
            }

            _data.CoyoteTimer = _data.IsGrounded
                ? _stats.JumpCoyoteTime
                : Mathf.Max(0f, _data.CoyoteTimer - deltaTime);
        }

        public void ApplyVerticalVelocity()
        {
            _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocityX, _data.VerticalVelocity);
        }

        public void ClampVerticalVelocity()
        {
            _data.VerticalVelocity = Mathf.Clamp(_data.VerticalVelocity, -_stats.MaxFallSpeed, _stats.MaxRiseSpeed);
        }

        public void ApplyLanding()
        {
            float landingForce = _data.VerticalVelocity;
            _onLandedEvent?.Invoke(landingForce);

            _data.AirTime = 0f;
            _data.VerticalVelocity = _stats.Gravity;
            _horizontalController?.ResetHorizontalVelocity(_stats.StickinessOnLanding);

            _data.IsJumping = false;
            _data.IsFalling = false;
            _data.IsFastFalling = false;
            _data.FastFallTime = 0f;
            _data.IsPastApexThreshold = false;
            _data.JumpsCount = 0;
            _data.IsWallSliding = false;
            _data.WallStickTimer = 0f;
            _data.WallDirection = 0;
            _wallController?.ClearWallHit();
        }

        public void InitiateJump(int jumpIncrements, float? initialVerticalVelocityOverride = null, bool countJumps = true)
        {
            if (!_data.IsJumping)
            {
                _data.IsJumping = true;
            }

            if (countJumps)
            {
                _data.JumpsCount += jumpIncrements;
            }

            ConsumeJumpBuffer();
            _data.JumpReleasedDuringBuffer = false;
            float targetVertical = initialVerticalVelocityOverride ?? _stats.InitialJumpVelocity;
            _data.VerticalVelocity = targetVertical;
            _data.FastFallTime = 0f;
            _data.IsFastFalling = false;
            _data.IsFalling = false;
            _data.FastFallReleaseSpeed = _data.VerticalVelocity;

            _onJumpEvent?.Invoke();
        }

        public void PerformWallJump(bool isLong = false)
        {
            if (_data.WallDirection == 0)
            {
                return;
            }

            var settings = _stats.WallSlide;
            if (settings == null)
            {
                return;
            }

            int pushDirection = -_data.WallDirection;

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
            float preservedVertical = _data.VerticalVelocity < 0f
                ? _data.VerticalVelocity * cancelMultiplier
                : _data.VerticalVelocity;
            float baseVertical = Mathf.Max(0f, preservedVertical);
            float targetVerticalVelocity = Mathf.Max(_stats.InitialJumpVelocity, baseVertical + upwardBoost);

            float currentAwaySpeed = Mathf.Max(0f, _data.Velocity.x * pushDirection);
            float finalHorizontalSpeed = Mathf.Max(horizontalPush, currentAwaySpeed) * pushDirection;
            _data.Velocity = new Vector2(finalHorizontalSpeed, _data.Velocity.y);

            InitiateJump(1, targetVerticalVelocity, false);

            _rigidbody.linearVelocity = new Vector2(_data.Velocity.x, _data.VerticalVelocity);

            _data.IsWallSliding = false;
            _data.WallStickTimer = 0f;
            _data.WallDirection = 0;
            _wallController?.ClearWallHit();
        }

        public void AttemptJumpCut()
        {
            if (!_data.IsJumping || _data.VerticalVelocity <= 0f)
            {
                return;
            }

            float releaseSpeed = _data.VerticalVelocity;
            _data.FastFallReleaseSpeed = releaseSpeed;

            _data.VerticalVelocity = 0f;
            _data.IsFastFalling = true;

            if (_data.IsPastApexThreshold)
            {
                _data.IsPastApexThreshold = false;
                _data.FastFallTime = _stats.TimeForUpwardsCancel;
            }
            else
            {
                _data.FastFallTime = 0f;
            }
        }

        public void ApplyFastFall(float deltaTime)
        {
            if (_data.FastFallTime >= _stats.TimeForUpwardsCancel)
            {
                _data.VerticalVelocity += _stats.Gravity * _stats.GravityOnReleaseMultiplier * deltaTime;
            }
            else
            {
                _data.VerticalVelocity = Mathf.Lerp(
                    _data.FastFallReleaseSpeed,
                    0f,
                    _data.FastFallTime / _stats.TimeForUpwardsCancel);
            }

            _onFallEvent?.Invoke();
            _data.FastFallTime += deltaTime;
        }

        public void ApplyFall(float deltaTime)
        {
            _data.IsFalling = true;
            _data.VerticalVelocity += _stats.Gravity * deltaTime;
        }

        public void NotifyFallStarted()
        {
            _data.IsFalling = true;
        }

        public void InvokeFallEvent()
        {
            _onFallEvent?.Invoke();
        }

        public void HandleJumpAscent(float deltaTime)
        {
            _data.ApexPoint = Mathf.InverseLerp(_stats.InitialJumpVelocity, 0f, _data.VerticalVelocity);

            if (_data.ApexPoint > _stats.ApexThreshold)
            {
                if (!_data.IsPastApexThreshold)
                {
                    _data.IsPastApexThreshold = true;
                    _data.TimePastApexThreshold = 0f;
                }

                _data.TimePastApexThreshold += deltaTime;
                if (_data.TimePastApexThreshold < _stats.ApexHangTime)
                {
                    _data.VerticalVelocity = 0f;
                }
                else
                {
                    _data.VerticalVelocity = -0.01f;
                }
            }
            else
            {
                _data.VerticalVelocity += _stats.Gravity * deltaTime;
                _data.IsPastApexThreshold = false;
            }
        }

        public bool TryEdgeNudge()
        {
            float maxNudge = _stats.HeadNudgeDistance;
            LayerMask groundMask = _stats.GroundLayer;

            Bounds bodyBounds = _bodyCollider.bounds;

            Vector2 clearanceSize = new(bodyBounds.size.x, _stats.HeadDetectionRayLength);
            Vector2 clearanceOriginBase = new(bodyBounds.center.x, bodyBounds.max.y);

            Vector2[] directions = _data.IsFacingRight
                ? new[] { Vector2.right, Vector2.left }
                : new[] { Vector2.left, Vector2.right };

            int steps = _stats.HeadNudgeSteps;
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

                    _transform.position += (Vector3)offset;
                    return true;
                }
            }

            return false;
        }

        private void StartJumpBuffer()
        {
            _data.JumpBufferTimer = _stats.JumpBufferTime;
            _data.JumpReleasedDuringBuffer = false;
        }

        private void RegisterJumpRelease()
        {
            _data.JumpReleasedDuringBuffer = _data.JumpBufferTimer > 0f;
        }

        private void ConsumeJumpBuffer()
        {
            _data.JumpBufferTimer = 0f;
        }
    }
}
