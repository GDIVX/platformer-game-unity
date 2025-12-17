using System;
using UnityEngine;

namespace Runtime.Player.Movement.Controllers
{
    /// <summary>
    /// Manages all player interactions with walls, including:
    /// detection, stick timing, direction buffers, and
    /// wall-slide motion shaping (vertical + horizontal).
    /// 
    /// IMPORTANT:
    /// - Wall slide only continues while TOUCHING the wall.
    /// - Wall-stick timer is preserved for wall-jumps ONLY.
    /// - No more ghost-sliding midair.
    /// </summary>
    [Serializable]
    public class WallInteractionController
    {
        private readonly PlayerMovementRuntimeData _data;
        private readonly PlayerMovementStats _stats;
        private readonly Rigidbody2D _rigidbody;

        /// <summary>
        /// Creates a new instance of the WallInteractionController.
        /// </summary>
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
        }

        /// <summary>
        /// Duration for which wall stick persists (wall-coyote).
        /// </summary>
        private float WallStickDuration => _stats.WallSlide?.StickDuration ?? 0f;

        // ---------------------------------------------------------------------
        // HIT SETUP
        // ---------------------------------------------------------------------



        /// <summary>
        /// Updates the head bump state from the given raycast hit.
        /// </summary>
        public void SetHeadHit(RaycastHit2D hit)
        {
            _data.HeadHit = hit;
            _data.BumpedHead = hit.collider;
        }

        /// <summary>
        /// Updates left/right wall contact and resets wall-stick timer.
        /// </summary>
        public void SetWallHit(bool isRight, RaycastHit2D hit)
        {
            if (isRight)
            {
                _data.RightWallHit = hit;
                _data.IsTouchingRightWall = hit.collider;
            }
            else
            {
                _data.LeftWallHit = hit;
                _data.IsTouchingLeftWall = hit.collider;
            }

            if (hit.collider)
            {
                _data.WallStickTimer = WallStickDuration;
                _data.WallDirection = isRight ? 1 : -1;
                _data.WallHit = hit;
            }

            UpdateWallContactState();
        }

        /// <summary>
        /// Clears wall contact for one side.
        /// </summary>
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

        /// <summary>
        /// Clears all wall contact information.
        /// </summary>
        public void ClearWallHit()
        {
            _data.LeftWallHit = default;
            _data.RightWallHit = default;
            _data.IsTouchingLeftWall = false;
            _data.IsTouchingRightWall = false;

            UpdateWallContactState();
        }

        // ---------------------------------------------------------------------
        // TIMERS
        // ---------------------------------------------------------------------

        /// <summary>
        /// Updates wall-stick, coyote logic, and direction buffer timers.
        /// </summary>
        public void UpdateTimers(float deltaTime)
        {
            // Refresh stick time if touching any wall.
            if (_data.IsTouchingWall)
            {
                _data.WallStickTimer = WallStickDuration;
            }
            else
            {
                // Count down wall-coyote for WALL JUMPS ONLY.
                if (_data.WallStickTimer > 0f)
                {
                    _data.WallStickTimer = Mathf.Max(0f, _data.WallStickTimer - deltaTime);
                }
            }

            UpdateDirectionBuffer(deltaTime);
        }

        /// <summary>
        /// Tracks whether the player is pushing away from the wall,
        /// giving them a small buffer window.
        /// </summary>
        private void UpdateDirectionBuffer(float deltaTime)
        {
            if (_data.WallDirection == 0)
            {
                _data.WantsToMoveAwayFromWall = false;
                _data.DirectionBufferTimer = 0f;
                return;
            }

            int inputDir = GetInputDirection();

            bool movingAway =
                inputDir != 0 &&
                inputDir == -_data.WallDirection &&
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

        // ---------------------------------------------------------------------
        // WALL SLIDE CONDITIONS
        // ---------------------------------------------------------------------

        /// <summary>
        /// Whether wall slide should begin this frame.
        /// Requires:
        /// - Falling
        /// - Touching the wall
        /// - Wall stick active
        /// - Facing the wall (or neutral)
        /// </summary>
        public bool ShouldStartWallSlide()
        {
            if (_stats.WallSlide == null)
                return false;

            if (_data.IsGrounded)
                return false;

            if (_data.VerticalVelocity > 0f)
                return false;

            if (!_data.IsTouchingWall)
                return false;

            if (_data.WallStickTimer <= 0f)
                return false;

            int inputDir = GetInputDirection();

            return _data.WallDirection != 0 &&
                   (inputDir == _data.WallDirection || inputDir == 0);
        }

        /// <summary>
        /// Whether a currently running wall slide may continue.
        /// 
        /// FIXED:
        /// - NO CONTINUING WHILE NOT TOUCHING WALL.
        /// - Wall-coyote no longer fakes slide in midair.
        /// </summary>
        public bool CanContinueWallSlide()
        {
            if (_stats.WallSlide == null)
                return false;

            if (_data.IsGrounded)
                return false;

            if (_data.VerticalVelocity > 0f)
                return false;

            // THE FIX:
            // Wall slide stops instantly if not touching the wall.
            if (!_data.IsTouchingWall)
                return false;

            int inputDir = GetInputDirection();

            return _data.WallDirection != 0 &&
                   (inputDir == _data.WallDirection || inputDir == 0);
        }

        // ---------------------------------------------------------------------
        // WALL SLIDE MOTION
        // ---------------------------------------------------------------------

        /// <summary>
        /// Applies vertical shaping for the slide (gravity + clamping).
        /// </summary>
        public void ApplyWallSlideVertical(PlayerMovementStats.WallSlideSettings settings, float deltaTime)
        {
            float gravity = settings.CalculatedGravity != 0f
                ? settings.CalculatedGravity
                : _stats.Gravity * settings.GravityMultiplier;

            _data.VerticalVelocity += gravity * deltaTime;

            float maxDown = -Mathf.Abs(settings.MaxSlideSpeed);
            float minDown = -Mathf.Abs(settings.MinSlideSpeed);

            if (minDown < maxDown)
                (maxDown, minDown) = (minDown, maxDown);

            _data.VerticalVelocity = Mathf.Clamp(_data.VerticalVelocity, maxDown, minDown);
        }

        /// <summary>
        /// Applies horizontal friction/acceleration shaping during slide.
        /// </summary>
        public void ApplyWallSlideHorizontal(PlayerMovementStats.WallSlideSettings settings, float deltaTime)
        {
            float inputX = _data.MoveInput.x;
            int inputDir = GetInputDirection();

            float target = 0f;
            float rate = settings.HorizontalFriction;

            // Pushing away → accelerate
            if (_data.WallDirection != 0 && inputDir == -_data.WallDirection)
            {
                float maxSpeed = _data.RunHeld ? _stats.MaxRunSpeed : _stats.MaxWalkSpeed;
                target = inputX * maxSpeed;
                rate = settings.HorizontalAcceleration;
            }

            float nextX = Mathf.Lerp(_data.Velocity.x, target, rate * deltaTime);

            _data.Velocity = new Vector2(nextX, _data.Velocity.y);

            // Using your custom linearVelocity API
            _rigidbody.linearVelocity = new Vector2(_data.Velocity.x, _rigidbody.linearVelocityY);
        }

        // ---------------------------------------------------------------------
        // INTERNAL HELPERS
        // ---------------------------------------------------------------------

        /// <summary>
        /// Updates which wall side is active and wall direction.
        /// </summary>
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

                return;
            }

            if (_data.IsTouchingRightWall)
            {
                _data.WallDirection = 1;
                _data.WallHit = _data.RightWallHit;
                return;
            }

            if (_data.IsTouchingLeftWall)
            {
                _data.WallDirection = -1;
                _data.WallHit = _data.LeftWallHit;
                return;
            }

            // No wall contact → direction only resets when stick expires.
            if (!(_data.WallStickTimer <= 0f) || _data.IsWallSliding) return;
            _data.WallDirection = 0;
            _data.WallHit = default;
        }

        /// <summary>
        /// Converts analog input into -1, 0, or 1.
        /// </summary>
        private int GetInputDirection()
        {
            if (Mathf.Approximately(_data.MoveInput.x, 0f))
                return 0;

            return _data.MoveInput.x > 0f ? 1 : -1;
        }
    }
}