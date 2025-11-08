using System;
using Runtime.Player.Movement;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Player.Movement.Controllers
{
    [Serializable]
    public class HorizontalMovementController
    {
        private readonly PlayerMovementRuntimeData _data;
        private readonly PlayerMovementStats _stats;
        private readonly Rigidbody2D _rigidbody;
        private readonly Collider2D _feetCollider;
        private readonly Transform _transform;
        private readonly UnityEvent _onMoveStartEvent;
        private readonly UnityEvent _onMoveStoppedEvent;
        private readonly UnityEvent _onMoveFullyStoppedEvent;
        private readonly UnityEvent<bool> _onTurnEvent;
        private readonly JumpController _jumpController;

        private bool _hasHorizontalInput;
        private bool _isFullyStopped = true;

        public HorizontalMovementController(
            PlayerMovementRuntimeData data,
            PlayerMovementStats stats,
            Rigidbody2D rigidbody,
            Collider2D feetCollider,
            Transform transform,
            UnityEvent onMoveStart,
            UnityEvent onMoveStopped,
            UnityEvent onMoveFullyStopped,
            UnityEvent<bool> onTurn,
            JumpController jumpController)
        {
            _data = data;
            _stats = stats;
            _rigidbody = rigidbody;
            _feetCollider = feetCollider;
            _transform = transform;
            _onMoveStartEvent = onMoveStart;
            _onMoveStoppedEvent = onMoveStopped;
            _onMoveFullyStoppedEvent = onMoveFullyStopped;
            _onTurnEvent = onTurn;
            _jumpController = jumpController;
        }

        public void SetMovementInput(Vector2 moveInput, bool runHeld)
        {
            bool wasMovingHorizontally = _hasHorizontalInput;

            _data.MoveInput = moveInput;
            _data.RunHeld = runHeld;

            _hasHorizontalInput = !Mathf.Approximately(moveInput.x, 0f);

            if (!wasMovingHorizontally && _hasHorizontalInput)
            {
                _isFullyStopped = false;
                _onMoveStartEvent?.Invoke();
            }
            else if (wasMovingHorizontally && !_hasHorizontalInput)
            {
                _onMoveStoppedEvent?.Invoke();
            }
        }

        public void ApplyMovement(float acceleration, float deceleration, float deltaTime)
        {
            bool wasFullyStopped = _isFullyStopped;

            if (_hasHorizontalInput)
            {
                TurnCheck(_data.MoveInput);

                if (_data.IsTouchingWall && !_data.IsGrounded)
                {
                    if (_data.JumpBufferTimer > 0f)
                    {
                        _jumpController.PerformWallJump(true);
                    }

                    return;
                }

                Vector2 desired = new(_data.MoveInput.x, 0f);
                float maxSpeed = _data.RunHeld ? _stats.MaxRunSpeed : _stats.MaxWalkSpeed;
                _data.TargetVelocity = desired * maxSpeed;

                _data.Velocity = Vector2.Lerp(_data.Velocity, _data.TargetVelocity, acceleration * deltaTime);

                if (IsGoingToCollide(deltaTime) && CanStep())
                {
                    _data.Velocity = new Vector2(_data.Velocity.x, _stats.StepHeight);
                }

                _isFullyStopped = false;
            }
            else
            {
                _data.TargetVelocity = Vector2.zero;
                _data.Velocity = Vector2.Lerp(_data.Velocity, Vector2.zero, deceleration * deltaTime);
                if (Mathf.Abs(_data.Velocity.x) <= _stats.MinSpeedThreshold)
                {
                    _data.Velocity = new Vector2(0f, _data.Velocity.y);
                }
            }

            _rigidbody.linearVelocity = new Vector2(_data.Velocity.x, _rigidbody.linearVelocityY);

            bool isFullyStopped = Mathf.Approximately(_data.Velocity.x, 0f);
            if (!wasFullyStopped && isFullyStopped)
            {
                _onMoveFullyStoppedEvent?.Invoke();
            }

            _isFullyStopped = isFullyStopped;
        }

        public bool ShouldSlide()
        {
            return _data.MoveInput == Vector2.zero && Mathf.Abs(_data.Velocity.x) > _stats.MinSpeedThreshold;
        }

        public void ResetHorizontalVelocity(float lerpFactor)
        {
            _data.Velocity = new Vector2(Mathf.Lerp(_data.Velocity.x, 0f, lerpFactor), _data.Velocity.y);
            _rigidbody.linearVelocity = new Vector2(_data.Velocity.x, _rigidbody.linearVelocityY);
        }

        private void TurnCheck(Vector2 moveInput)
        {
            if (_data.IsFacingRight && moveInput.x < 0f)
            {
                Turn(false);
            }
            else if (!_data.IsFacingRight && moveInput.x > 0f)
            {
                Turn(true);
            }
        }

        private void Turn(bool faceRight)
        {
            if (faceRight == _data.IsFacingRight)
            {
                return;
            }

            _data.IsFacingRight = faceRight;
            _transform.Rotate(0f, faceRight ? 180f : -180f, 0f);
            _onTurnEvent?.Invoke(_data.IsFacingRight);
        }

        private bool CanStep()
        {
            if (_feetCollider == null)
            {
                return false;
            }

            Bounds bounds = _feetCollider.bounds;
            float direction = Mathf.Sign(_data.Velocity.x);

            float stepHeight = _stats.StepHeight;
            float stepCheckDistance = 0.1f;

            Vector2 lowerRayOrigin = new(
                bounds.center.x + direction * (bounds.extents.x + stepCheckDistance),
                bounds.min.y + 0.05f);
            Vector2 upperRayOrigin = lowerRayOrigin + Vector2.up * stepHeight;

            RaycastHit2D lowerHit = Physics2D.Raycast(lowerRayOrigin, Vector2.right * direction, 0.1f, _stats.GroundLayer);
            if (!lowerHit)
            {
                return false;
            }

            RaycastHit2D upperHit = Physics2D.Raycast(upperRayOrigin, Vector2.right * direction, 0.2f, _stats.GroundLayer);
            if (upperHit)
            {
                return false;
            }

            Vector2 stepCheckOrigin = upperRayOrigin + Vector2.right * 0.15f;
            RaycastHit2D stepHit = Physics2D.Raycast(stepCheckOrigin, Vector2.down, stepHeight + 0.1f, _stats.GroundLayer);

            if (!stepHit)
            {
                return false;
            }

            _transform.position += Vector3.up * (stepHit.point.y - bounds.min.y);
            return true;
        }

        private bool IsGoingToCollide(float deltaTime)
        {
            if (Mathf.Approximately(_data.Velocity.x, 0f))
            {
                return false;
            }

            if (_feetCollider == null)
            {
                return false;
            }

            float direction = Mathf.Sign(_data.Velocity.x);
            Vector2 origin = _feetCollider.bounds.center;
            Vector2 size = _feetCollider.bounds.size;

            size.x *= 0.9f;
            size.y *= 0.5f;

            float rayLength = Mathf.Abs(_data.Velocity.x) * deltaTime + 0.05f;

            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.right * direction, rayLength, _stats.GroundLayer);
            return hit.collider != null && hit.normal.y < 0.5f;
        }
    }
}
