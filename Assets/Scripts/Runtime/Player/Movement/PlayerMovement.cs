using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Player.Movement
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private PlayerMovementStats _movementStats;

        [SerializeField] private Collider2D _feetCollider;
        [SerializeField] private Collider2D _bodyCollider;

        private Rigidbody2D _rb;


        //Movement vars
        [ShowInInspector, BoxGroup("movement"), ReadOnly]
        private Vector2 _velocity;


        [ShowInInspector, BoxGroup("movement"), ReadOnly]
        Vector2 _targetVelocity; //outer scope for debugging

        [ShowInInspector, BoxGroup("movement"), ReadOnly]
        private bool _isFacingRight;


        //Jump vars
        [ShowInInspector] [BoxGroup("Jump"), ReadOnly]
        private float _verticalVelocity;

        [ShowInInspector] [BoxGroup("Jump"), ReadOnly]
        private bool _isJumping;

        [ShowInInspector] [BoxGroup("Jump"), ReadOnly]
        private int _jumpsCount;

        [ShowInInspector] [BoxGroup("Jump/Fall"), ReadOnly]
        private bool _isFastFalling;

        [ShowInInspector] [BoxGroup("Jump/Fall"), ReadOnly]
        private bool _isFalling;

        [ShowInInspector] [BoxGroup("Jump/Fall"), ReadOnly]
        private float _fastFallTime;

        [ShowInInspector] [BoxGroup("Jump/Fall"), ReadOnly]
        private float _fastFallReleaseSpeed;

        //Jump Apex vars
        [ShowInInspector] [BoxGroup("Jump/Apex"), ReadOnly]
        private float _apexPoint;

        [ShowInInspector] [BoxGroup("Jump/Apex"), ReadOnly]
        private float _timePastApexThreshold;

        [ShowInInspector] [BoxGroup("Jump/Apex"), ReadOnly]
        private bool _isPastApexThreshold;

        //Jump buffer vars
        [ShowInInspector] [BoxGroup("Jump/Buffer"), ReadOnly]
        private float _jumpBufferTimer;

        [ShowInInspector] [BoxGroup("Jump/Buffer"), ReadOnly]
        private bool _jumpReleasedDuringBuffer;

        //Coyote time vars
        [ShowInInspector, BoxGroup("Jump/Coyote Time"), ReadOnly]
        private float _coyoteTimer;

        //Collision check vars
        [ShowInInspector, BoxGroup("Collision"), ReadOnly]
        private RaycastHit2D _groundHit;

        [ShowInInspector, BoxGroup("Collision"), ReadOnly]
        private RaycastHit2D _headHit;

        [ShowInInspector, BoxGroup("Collision"), ReadOnly]
        private bool _isGrounded;

        [ShowInInspector, BoxGroup("Collision"), ReadOnly]
        private bool _bumpedHead;

        [FoldoutGroup("Events")] public UnityEvent OnJump;
        [FoldoutGroup("Events")] public UnityEvent OnLand;
        [FoldoutGroup("Events")] public UnityEvent OnFall;
        [FoldoutGroup("Events")] public UnityEvent<Vector2> OnMovement;

        private void OnEnable()
        {
            _movementStats.SlideMovement.selectedCollider = _feetCollider;
        }

        private void Awake()
        {
            _isFacingRight = true;
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            CollisionCheck();
            Jump();

            if (_isGrounded)
            {
                Move(_movementStats.GroundAcceleration, _movementStats.GroundDeceleration, InputManager.Movement);
            }
            else
            {
                Move(_movementStats.AirAcceleration, _movementStats.AirDeceleration, InputManager.Movement);
            }

            if (!_isGrounded || _isFalling || _isJumping) return;
            //apply sliding
            _rb.Slide(_velocity, Time.fixedDeltaTime, _movementStats.SlideMovement);
        }

        private void Update()
        {
            CountTimers();
            JumpChecks();
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (_movementStats.ShowWalkJumpArc)
            {
                DrawJumpArc(_movementStats.MaxWalkSpeed, Color.white);
            }

            if (_movementStats.ShowRunJumpArc)
            {
                DrawJumpArc(_movementStats.MaxRunSpeed, Color.red);
            }

#endif
        }

        #region Ground Movement

        private void Move(float acceleration, float deceleration, Vector2 moveInput)
        {
            if (moveInput != Vector2.zero)
            {
                TurnCheck(moveInput);

                _targetVelocity = InputManager.RunHeld
                    ? new Vector2(moveInput.x, 0f) * _movementStats.MaxRunSpeed
                    : new Vector2(moveInput.x, 0f) * _movementStats.MaxWalkSpeed;

                _velocity = Vector2.Lerp(_velocity, _targetVelocity, acceleration * Time.fixedDeltaTime);
                OnMovement?.Invoke(_velocity);
            }
            else
            {
                _velocity = Vector2.Lerp(_velocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
            }

            _rb.linearVelocity = new Vector2(_velocity.x, _rb.linearVelocityY);
        }

        private void TurnCheck(Vector2 moveInput)
        {
            if (_isFacingRight && moveInput.x < 0)
            {
                Turn(true);
            }
            else if (!_isFacingRight && moveInput.x > 0)
            {
                Turn(false);
            }
        }

        private void Turn(bool turnRight)
        {
            if (turnRight)
            {
                _isFacingRight = true;
                transform.Rotate(0f, 180f, 0f);
            }
            else
            {
                _isFacingRight = false;
                transform.Rotate(0f, -180f, 0f);
            }
        }

        #endregion

        #region Collision Checks

        private void CheckIfGrounded()
        {
            Vector2 boxCastOrigin = new Vector2(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
            Vector2 boxCastSize = new Vector2(_feetCollider.bounds.size.x, _movementStats.GroundDetectionRayLength);

            _groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down,
                _movementStats.GroundDetectionRayLength, _movementStats.GroundLayer);
            _isGrounded = _groundHit.collider;

            #region Gizmos

            if (!_movementStats.DebugShowIsGrounded) return;
            Color rayColor = _isGrounded ? Color.green : Color.red;

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y),
                Vector2.down * _movementStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y),
                Vector2.down * _movementStats.GroundDetectionRayLength, rayColor);
            Debug.DrawRay(
                new Vector2(boxCastOrigin.x - boxCastSize.x / 2,
                    boxCastOrigin.y - _movementStats.GroundDetectionRayLength), Vector2.right * boxCastSize.x,
                rayColor);

            #endregion
        }

        private void CheckIfBumpedHead()
        {
            Vector2 boxCastOrigin = new Vector2(_feetCollider.bounds.center.x, _bodyCollider.bounds.max.y);
            Vector2 boxCastSize = new Vector2(_feetCollider.bounds.size.x * _movementStats.HeadWidth,
                _movementStats.HeadDetectionRayLength);

            _headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up,
                _movementStats.HeadDetectionRayLength, _movementStats.GroundLayer);

            _bumpedHead = _headHit.collider;

            #region Gizmos

            Color rayColor = _bumpedHead ? Color.green : Color.red;

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y),
                Vector2.up * _movementStats.HeadDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y),
                Vector2.up * _movementStats.HeadDetectionRayLength, rayColor);
            Debug.DrawRay(
                new Vector2(boxCastOrigin.x - boxCastSize.x / 2,
                    boxCastOrigin.y - _movementStats.HeadDetectionRayLength), Vector2.right * boxCastSize.x,
                rayColor);

            #endregion
        }

        private void CollisionCheck()
        {
            CheckIfGrounded();
            CheckIfBumpedHead();
        }

        #endregion

        #region Jump

        private void JumpChecks()
        {
            //On jump press
            if (InputManager.JumpPressed)
            {
                OnJumpInputPressed();
            }

            //On jump release
            if (InputManager.JumpReleased)
            {
                JumpCut();
            }

            switch (_jumpBufferTimer)
            {
                //Init jump
                case > 0f when !_isJumping && (_isGrounded || _coyoteTimer > 0f):
                {
                    InitiateJump(1);

                    if (_jumpReleasedDuringBuffer)
                    {
                        _isJumping = true;
                        _fastFallReleaseSpeed = _verticalVelocity;
                    }

                    break;
                }
                //Additional jump
                case > 0f when _isJumping && _jumpsCount < _movementStats.NumberOfJumpsAllowed:
                    _isFastFalling = false;
                    InitiateJump(1);
                    break;
                //Air Jump
                case > 0f when _isFalling && _jumpsCount < _movementStats.NumberOfJumpsAllowed - 1:
                    InitiateJump(2);
                    _isFastFalling = false;
                    break;
            }

            //Landing
            if ((_isJumping || _isFalling) && _isGrounded && _verticalVelocity <= 0)
            {
                HandleLanding();
            }
        }

        private void HandleLanding()
        {
            _isJumping = false;
            _isFalling = false;
            _isFastFalling = false;
            _fastFallTime = 0;
            _isPastApexThreshold = false;
            _jumpsCount = 0;

            _verticalVelocity = Physics2D.gravity.y;
            
            //Cut some of the horizontal velocity for stickiness
            _velocity.x = Mathf.Lerp(_velocity.x, 0, _movementStats.StickinessOnLanding);
            
            OnLand?.Invoke();
        }

        private void InitiateJump(int jumps)
        {
            if (!_isJumping)
            {
                _isJumping = true;
            }

            _jumpBufferTimer = 0f;
            _jumpsCount += jumps;
            _verticalVelocity = _movementStats.InitialJumpVelocity;
        }

        private void JumpCut()
        {
            _jumpReleasedDuringBuffer = _jumpBufferTimer > 0f;

            if (!_isJumping || !(_verticalVelocity > 0f)) return;
            _verticalVelocity = 0f;
            _isFastFalling = true;
            if (_isPastApexThreshold)
            {
                _isPastApexThreshold = false;
                _fastFallTime = _movementStats.TimeForUpwardsCancel;
            }
            else
            {
                _fastFallReleaseSpeed = _verticalVelocity;
            }
        }

        private void OnJumpInputPressed()
        {
            _jumpBufferTimer = _movementStats.JumpBufferTime;
            _jumpReleasedDuringBuffer = false;
        }

        private void Jump()
        {
            //Apply gravity
            if (_isJumping)
            {
                HandleJumpGravity();
            }

            //Jump cut
            if (_isFastFalling)
            {
                FastFall();
            }

            // Normal Gravity while falling
            if (!_isGrounded && !_isJumping)
            {
                Fall();
            }

            //Clamp falls speed
            _verticalVelocity =
                Mathf.Clamp(_verticalVelocity, -_movementStats.MaxFallSpeed, _movementStats.MaxRiseSpeed);

            //Apply to RB
            _rb.linearVelocity = new Vector2(_rb.linearVelocityX, _verticalVelocity);
            OnJump?.Invoke();
        }

        private void Fall()
        {
            _isFalling = true;
            _verticalVelocity += _movementStats.Gravity * Time.fixedDeltaTime;
            OnFall?.Invoke();
        }

        private void FastFall()
        {
            if (_fastFallTime >= _movementStats.TimeForUpwardsCancel)
            {
                _verticalVelocity += _movementStats.Gravity * _movementStats.GravityOnReleaseMultiplier *
                                     Time.fixedDeltaTime;
            }
            else if (_fastFallTime < _movementStats.TimeForUpwardsCancel)
            {
                _verticalVelocity = Mathf.Lerp(_fastFallReleaseSpeed, 0f,
                    _fastFallTime / _movementStats.TimeForUpwardsCancel);
            }

            _fastFallTime += Time.fixedDeltaTime;
            OnFall?.Invoke();
        }

        private void HandleJumpGravity()
        {
            // Check for head bumps
            if (_bumpedHead)
            {
                _isFastFalling = true;
            }

            //Gravity on ascend
            if (_verticalVelocity >= 0f)
            {
                OnAscent();
            }

            //Gravity on decent
            else if (!_isFastFalling)
            {
                _verticalVelocity += _movementStats.Gravity * _movementStats.GravityOnReleaseMultiplier *
                                     Time.fixedDeltaTime;
            }
            else if (_verticalVelocity < 0f)
            {
                _isFalling = true;
            }
        }

        private void OnAscent()
        {
            _apexPoint = Mathf.InverseLerp(_movementStats.InitialJumpVelocity, 0f, _verticalVelocity);

            if (_apexPoint > _movementStats.ApexThreshold)
            {
                if (!_isPastApexThreshold)
                {
                    _isPastApexThreshold = true;
                    _timePastApexThreshold = 0f;
                }

                if (!_isPastApexThreshold) return;
                _timePastApexThreshold += Time.fixedDeltaTime;

                if (_timePastApexThreshold < _movementStats.ApexHangTime)
                    _verticalVelocity = 0f;
                else
                    _verticalVelocity = -0.01f;
            }
            //Gravity on decent But not past apex threshold
            else
            {
                _verticalVelocity += _movementStats.Gravity * Time.fixedDeltaTime;
                _isPastApexThreshold = false;
            }
        }

        #region Gizmos

        private void DrawJumpArc(float moveSpeed, Color gizmoColor)
        {
            Vector2 startPosition = new Vector2(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
            Vector2 previousPosition = startPosition;
            float speed = 0f;
            if (_movementStats.DrawnRight)
            {
                speed = moveSpeed;
            }
            else
            {
                speed = -moveSpeed;
            }

            Vector2 velocity = new Vector2(speed, _movementStats.InitialJumpVelocity);

            Gizmos.color = gizmoColor;

            float timeStep =
                2 * _movementStats.TimeToJumpApex / _movementStats.ArcResolution; // time step for the simulation
            //float totalTime = (2 * _movementStats.TimeToJumpApex) + _movementStats.ApexHangTime; // total time of the arc including hang time

            for (int i = 0; i < _movementStats.VisualizationSteps; i++)
            {
                float simulationTime = i * timeStep;
                Vector2 displacement;
                Vector2 drawPoint;

                if (simulationTime < _movementStats.TimeToJumpApex) // Ascending
                {
                    displacement = velocity * simulationTime +
                                   0.5f * new Vector2(0, _movementStats.Gravity) * simulationTime * simulationTime;
                }
                else if (simulationTime < _movementStats.TimeToJumpApex + _movementStats.ApexHangTime) // Apex hang time
                {
                    float apexTime = simulationTime - _movementStats.TimeToJumpApex;
                    displacement = velocity * _movementStats.TimeToJumpApex + 0.5f *
                        new Vector2(0, _movementStats.Gravity) * _movementStats.TimeToJumpApex *
                        _movementStats.TimeToJumpApex;
                    displacement += new Vector2(speed, 0) * apexTime; // No vertical movement during hang time
                }
                else // Descending
                {
                    float descendTime = simulationTime - (_movementStats.TimeToJumpApex + _movementStats.ApexHangTime);
                    displacement = velocity * _movementStats.TimeToJumpApex + 0.5f *
                        new Vector2(0, _movementStats.Gravity) * _movementStats.TimeToJumpApex *
                        _movementStats.TimeToJumpApex;
                    displacement +=
                        new Vector2(speed, 0) * _movementStats.ApexHangTime; // Horizontal movement during hang time
                    displacement += new Vector2(speed, 0) * descendTime +
                                    0.5f * new Vector2(0, _movementStats.Gravity) * descendTime * descendTime;
                }

                drawPoint = startPosition + displacement;

                if (_movementStats.StopOnCollision)
                {
                    RaycastHit2D hit = Physics2D.Raycast(
                        previousPosition,
                        drawPoint - previousPosition,
                        Vector2.Distance(previousPosition, drawPoint),
                        _movementStats.GroundLayer
                    );

                    if (hit.collider != null)
                    {
                        // If a hit is detected, stop drawing the arc at the hit point
                        Gizmos.DrawLine(previousPosition, hit.point);
                        break;
                    }
                }

                Gizmos.DrawLine(previousPosition, drawPoint);
                previousPosition = drawPoint;
            }
        }

        #endregion

        #endregion

        #region Timers

        private void CountTimers()
        {
            _jumpBufferTimer -= Time.deltaTime;

            if (!_isGrounded)
            {
                _coyoteTimer -= Time.deltaTime;
            }
            else
            {
                _coyoteTimer = _movementStats.JumpCoyoteTime;
            }
        }

        #endregion
    }
}