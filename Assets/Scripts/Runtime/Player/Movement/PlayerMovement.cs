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
            CheckForWall();
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

        private void CheckForWall()
        {
            if (_movementStats == null || _bodyCollider == null)
            {
                Context.ClearWallHit();
                return;
            }

            var wallSettings = _movementStats.WallSlide;
            if (wallSettings == null)
            {
                Context.ClearWallHit();
                return;
            }

            Bounds bodyBounds = _bodyCollider.bounds;
            float shrink = Mathf.Clamp(wallSettings.WallDetectionVerticalShrink, 0f, bodyBounds.size.y * 0.95f);
            Vector2 castSize = new Vector2(bodyBounds.size.x, Mathf.Max(0.05f, bodyBounds.size.y - shrink));
            Vector2 castOrigin = bodyBounds.center;

            RaycastHit2D leftHit = Physics2D.BoxCast(
                castOrigin,
                castSize,
                0f,
                Vector2.left,
                wallSettings.WallDetectionHorizontalDistance,
                _movementStats.GroundLayer);

            RaycastHit2D rightHit = Physics2D.BoxCast(
                castOrigin,
                castSize,
                0f,
                Vector2.right,
                wallSettings.WallDetectionHorizontalDistance,
                _movementStats.GroundLayer);

            bool hasLeft = leftHit.collider != null && leftHit.collider != _bodyCollider && leftHit.collider != _feetCollider;
            bool hasRight = rightHit.collider != null && rightHit.collider != _bodyCollider && rightHit.collider != _feetCollider;

            if (hasLeft && hasRight)
            {
                if (leftHit.distance <= rightHit.distance)
                {
                    Context.SetWallHit(leftHit, -1);
                }
                else
                {
                    Context.SetWallHit(rightHit, 1);
                }
            }
            else if (hasLeft)
            {
                Context.SetWallHit(leftHit, -1);
            }
            else if (hasRight)
            {
                Context.SetWallHit(rightHit, 1);
            }
            else
            {
                Context.ClearWallHit();
            }
        }


    }
}
