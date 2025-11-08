using System;
using Runtime.Player.Movement;
using UnityEngine;

namespace Runtime.Player.Movement.Controllers
{
    [Serializable]
    public class WallInteractionController
    {
        private readonly PlayerMovementRuntimeData _data;
        private readonly PlayerMovementStats _stats;
        private readonly Rigidbody2D _rigidbody;
        private readonly Collider2D _feetCollider;
        private readonly Collider2D _bodyCollider;

        public WallInteractionController(
            PlayerMovementRuntimeData data,
            PlayerMovementStats stats,
            Rigidbody2D rigidbody,
            Collider2D feetCollider,
            Collider2D bodyCollider)
        {
            _data = data;
            _stats = stats;
            _rigidbody = rigidbody;
            _feetCollider = feetCollider;
            _bodyCollider = bodyCollider;
        }

        private float WallStickDuration => _stats.WallSlide?.StickDuration ?? 0f;

        public void SetGroundHit(RaycastHit2D hit)
        {
            _data.GroundHit = hit;
            _data.IsGrounded = hit.collider != null;
        }

        public void SetHeadHit(RaycastHit2D hit)
        {
            _data.HeadHit = hit;
            _data.BumpedHead = hit.collider != null;
        }

        public void SetWallHit(bool isRight, RaycastHit2D hit)
        {
            if (isRight)
            {
                _data.RightWallHit = hit;
                _data.IsTouchingRightWall = hit.collider != null;
            }
            else
            {
                _data.LeftWallHit = hit;
                _data.IsTouchingLeftWall = hit.collider != null;
            }

            if (hit.collider != null)
            {
                _data.WallStickTimer = WallStickDuration;
                _data.WallDirection = isRight ? 1 : -1;
                _data.WallHit = hit;
            }

            UpdateWallContactState();
        }

        public void ClearWallHit(bool isRight)
        {
            if (isRight)
            {
                _data.RightWallHit = default;
                _data.IsTouchingRightWall = false;
            }
            else
            {
                _data.LeftWallHit = default;
                _data.IsTouchingLeftWall = false;
            }

            UpdateWallContactState();
        }

        public void ClearWallHit()
        {
            _data.LeftWallHit = default;
            _data.RightWallHit = default;
            _data.IsTouchingLeftWall = false;
            _data.IsTouchingRightWall = false;
            UpdateWallContactState();
            if (_data.WallStickTimer <= 0f)
            {
                _data.WallDirection = 0;
                _data.WallHit = default;
            }
        }

        public void UpdateTimers(float deltaTime)
        {
            if (_data.IsTouchingWall)
            {
                _data.WallStickTimer = WallStickDuration;
            }
            else if (_data.WallStickTimer > 0f)
            {
                _data.WallStickTimer = Mathf.Max(0f, _data.WallStickTimer - deltaTime);
                if (_data.WallStickTimer <= 0f && !_data.IsWallSliding)
                {
                    _data.WallDirection = 0;
                    _data.WallHit = default;
                }
            }

            UpdateDirectionBuffer(deltaTime);
        }

        public void UpdateDirectionBuffer(float deltaTime)
        {
            if (_data.WallDirection == 0)
            {
                _data.WantsToMoveAwayFromWall = false;
                _data.DirectionBufferTimer = 0f;
                return;
            }

            bool movingAway = Mathf.Approximately(Mathf.Sign(_data.MoveInput.x), -_data.WallDirection) &&
                              Mathf.Abs(_data.MoveInput.x) > 0.25f;

            if (movingAway)
            {
                _data.WantsToMoveAwayFromWall = true;
                _data.DirectionBufferTimer = _stats.DirectionBufferDuration;
            }
            else if (_data.DirectionBufferTimer > 0f)
            {
                _data.DirectionBufferTimer = Mathf.Max(0f, _data.DirectionBufferTimer - deltaTime);
                if (_data.DirectionBufferTimer <= 0f)
                {
                    _data.WantsToMoveAwayFromWall = false;
                }
            }
        }

        public bool ShouldStartWallSlide()
        {
            if (_stats.WallSlide == null)
            {
                return false;
            }

            if (_data.IsGrounded || _data.VerticalVelocity > 0f)
            {
                return false;
            }

            if (!_data.IsTouchingWall && _data.WallStickTimer <= 0f)
            {
                return false;
            }

            int inputDirection = Mathf.Approximately(_data.MoveInput.x, 0f)
                ? 0
                : (_data.MoveInput.x > 0f ? 1 : -1);

            if (_data.IsTouchingWall)
            {
                return _data.WallDirection != 0 && (inputDirection == _data.WallDirection || inputDirection == 0);
            }

            return _data.WallDirection != 0 && _data.WallStickTimer > 0f && inputDirection != -_data.WallDirection;
        }

        public bool CanContinueWallSlide()
        {
            if (_stats.WallSlide == null)
            {
                return false;
            }

            if (_data.IsGrounded || _data.VerticalVelocity > 0f)
            {
                return false;
            }

            int inputDirection = Mathf.Approximately(_data.MoveInput.x, 0f)
                ? 0
                : (_data.MoveInput.x > 0f ? 1 : -1);

            if (_data.IsTouchingWall)
            {
                return _data.WallDirection != 0 && (inputDirection == _data.WallDirection || inputDirection == 0);
            }

            return _data.WallDirection != 0 && _data.WallStickTimer > 0f && inputDirection != -_data.WallDirection;
        }

        public void ApplyWallSlideVertical(PlayerMovementStats.WallSlideSettings settings, float deltaTime)
        {
            float gravity = settings.CalculatedGravity != 0f
                ? settings.CalculatedGravity
                : _stats.Gravity * settings.GravityMultiplier;

            _data.VerticalVelocity += gravity * deltaTime;

            float maxDownward = -Mathf.Abs(settings.MaxSlideSpeed);
            float minDownward = -Mathf.Abs(settings.MinSlideSpeed);
            if (minDownward < maxDownward)
            {
                (maxDownward, minDownward) = (minDownward, maxDownward);
            }

            _data.VerticalVelocity = Mathf.Clamp(_data.VerticalVelocity, maxDownward, minDownward);
        }

        public void ApplyWallSlideHorizontal(PlayerMovementStats.WallSlideSettings settings, float deltaTime)
        {
            float inputX = _data.MoveInput.x;
            int inputDirection = Mathf.Approximately(inputX, 0f) ? 0 : (inputX > 0f ? 1 : -1);

            float target = 0f;
            float rate = settings.HorizontalFriction;

            if (_data.WallDirection != 0 && inputDirection == -_data.WallDirection)
            {
                target = inputX * (_data.RunHeld ? _stats.MaxRunSpeed : _stats.MaxWalkSpeed);
                rate = settings.HorizontalAcceleration;
            }

            float nextX = Mathf.Lerp(_data.Velocity.x, target, rate * deltaTime);
            _data.Velocity = new Vector2(nextX, _data.Velocity.y);
            _rigidbody.linearVelocity = new Vector2(_data.Velocity.x, _rigidbody.linearVelocityY);
        }

        private void UpdateWallContactState()
        {
            _data.IsTouchingWall = _data.IsTouchingLeftWall || _data.IsTouchingRightWall;

            if (_data.IsTouchingLeftWall && _data.IsTouchingRightWall)
            {
                if (_data.LeftWallHit.distance <= _data.RightWallHit.distance)
                {
                    _data.WallDirection = -1;
                    _data.WallHit = _data.LeftWallHit;
                }
                else
                {
                    _data.WallDirection = 1;
                    _data.WallHit = _data.RightWallHit;
                }
            }
            else if (_data.IsTouchingRightWall)
            {
                _data.WallDirection = 1;
                _data.WallHit = _data.RightWallHit;
            }
            else if (_data.IsTouchingLeftWall)
            {
                _data.WallDirection = -1;
                _data.WallHit = _data.LeftWallHit;
            }
            else if (_data.WallStickTimer <= 0f && !_data.IsWallSliding)
            {
                _data.WallDirection = 0;
                _data.WallHit = default;
            }
        }
    }
}
