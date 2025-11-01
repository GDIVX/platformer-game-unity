using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Runtime.Player.Movement.States;
using Runtime.Player.Movement.Tools;

namespace Runtime.Player.Movement
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovementStats _movementStats;
        [SerializeField] private Collider2D _feetCollider;
        [SerializeField] private Collider2D _bodyCollider;

        [FoldoutGroup("Events")] public UnityEvent OnJump;
        [FoldoutGroup("Events")] public UnityEvent OnLand;
        [FoldoutGroup("Events")] public UnityEvent OnFall;
        [FoldoutGroup("Events")] public UnityEvent OnMoveStart;
        [FoldoutGroup("Events")] public UnityEvent OnMoveStopped;
        [FoldoutGroup("Events")] public UnityEvent OnMoveFullyStopped;
        [FoldoutGroup("Events")] public UnityEvent<float> OnLanded;
        public PlayerMovementContext Context { get; private set; }

        private Rigidbody2D _rb;
        private PlayerMovementStateMachine _stateMachine;

        private void OnEnable()
        {
            if (_movementStats != null)
            {
                _movementStats.SlideMovement.selectedCollider = _feetCollider;
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
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
                OnLanded);

            CollisionCheck();
            _stateMachine = new PlayerMovementStateMachine(Context);
            _stateMachine.Initialize<GroundedState>();
        }

        private void Update()
        {
            if (Context == null)
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
            if (Context == null)
            {
                return;
            }

            CollisionCheck();
            _stateMachine.FixedTick();
        }

        private void ReadInput()
        {
            Context.SetInput(
                InputManager.Movement,
                InputManager.RunHeld,
                InputManager.JumpPressed,
                InputManager.JumpHeld,
                InputManager.JumpReleased);
        }

        private void CollisionCheck()
        {
            CheckIfGrounded();
            CheckIfBumpedHead();
            CheckWallContact();
        }

        private void CheckIfGrounded()
        {
            if (_feetCollider == null)
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
                Color rayColor = Context.IsGrounded ? Color.green : Color.red;
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
            if (_feetCollider == null || _bodyCollider == null)
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

            Context.SetHeadHit(headHit);

#if UNITY_EDITOR
            if (_movementStats.DebugShowHeadBumpBox)
            {
                Color rayColor = Context.BumpedHead ? Color.green : Color.red;
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
            if (_bodyCollider == null)
            {
                Context.ClearWallHit(true);
                Context.ClearWallHit(false);
                return;
            }

            RaycastHit2D rightHit = CastForWall(Vector2.right);
            if (rightHit.collider != null)
            {
                Context.SetWallHit(true, rightHit);
            }
            else
            {
                Context.ClearWallHit(true);
            }

            RaycastHit2D leftHit = CastForWall(Vector2.left);
            if (leftHit.collider != null)
            {
                Context.SetWallHit(false, leftHit);
            }
            else
            {
                Context.ClearWallHit(false);
            }

#if UNITY_EDITOR
            if (_movementStats.DebugShowWallChecks)
            {
                Bounds bounds = _bodyCollider.bounds;
                float halfHeight = bounds.extents.y * _movementStats.WallDetectionHeightScale;

                Vector2 rightTop = new Vector2(bounds.max.x, bounds.center.y + halfHeight);
                Vector2 rightBottom = new Vector2(bounds.max.x, bounds.center.y - halfHeight);
                Vector2 leftTop = new Vector2(bounds.min.x, bounds.center.y + halfHeight);
                Vector2 leftBottom = new Vector2(bounds.min.x, bounds.center.y - halfHeight);

                Color rightColor = Context.IsTouchingRightWall ? Color.green : Color.red;
                Color leftColor = Context.IsTouchingLeftWall ? Color.green : Color.red;

                Debug.DrawRay(rightTop, Vector2.right * _movementStats.WallDetectionRayLength, rightColor);
                Debug.DrawRay(rightBottom, Vector2.right * _movementStats.WallDetectionRayLength, rightColor);
                Debug.DrawRay(leftTop, Vector2.left * _movementStats.WallDetectionRayLength, leftColor);
                Debug.DrawRay(leftBottom, Vector2.left * _movementStats.WallDetectionRayLength, leftColor);
            }
#endif
        }

        private RaycastHit2D CastForWall(Vector2 direction)
        {
            Bounds bounds = _bodyCollider.bounds;
            float heightScale = Mathf.Clamp01(_movementStats.WallDetectionHeightScale);

            if (_bodyCollider is CapsuleCollider2D capsuleCollider)
            {
                Vector3 lossyScale = capsuleCollider.transform.lossyScale;
                Vector2 capsuleSize = new Vector2(
                    capsuleCollider.size.x * Mathf.Abs(lossyScale.x),
                    capsuleCollider.size.y * Mathf.Abs(lossyScale.y));

                if (capsuleCollider.direction == CapsuleDirection2D.Vertical)
                {
                    capsuleSize.y *= heightScale;
                }
                else
                {
                    capsuleSize.x *= heightScale;
                }

                return Physics2D.CapsuleCast(
                    bounds.center,
                    capsuleSize,
                    capsuleCollider.direction,
                    capsuleCollider.transform.eulerAngles.z,
                    direction,
                    _movementStats.WallDetectionRayLength,
                    _movementStats.GroundLayer);
            }

            Vector2 castSize = new Vector2(bounds.size.x, bounds.size.y * heightScale);
            return Physics2D.BoxCast(
                bounds.center,
                castSize,
                0f,
                direction,
                _movementStats.WallDetectionRayLength,
                _movementStats.GroundLayer);
        }


    }
}
