using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Runtime.Player.Movement.DebugTools;
using Runtime.Player.Movement.States;

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

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (_movementStats == null || _feetCollider == null)
            {
                return;
            }

            if (_movementStats.ShowWalkJumpArc)
            {
                DrawJumpArc(false, Color.white);
            }

            if (_movementStats.ShowRunJumpArc)
            {
                DrawJumpArc(true, Color.red);
            }
#endif
        }

        private void DrawJumpArc(bool runHeld, Color gizmoColor)
        {
            var simulator = new JumpArcSimulator(_movementStats);

            Vector2 startPosition = new Vector2(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
            float horizontalInput = _movementStats.DrawnRight ? 1f : -1f;
            float initialHorizontalVelocity = Context != null ? Context.Velocity.x : 0f;

            var settings = new JumpArcSimulator.SimulationSettings
            {
                StartPosition = startPosition,
                HorizontalInput = horizontalInput,
                RunHeld = runHeld,
                InitialHorizontalVelocity = initialHorizontalVelocity,
                MaxSteps = Mathf.Max(1, _movementStats.VisualizationSteps),
                StopOnCollision = _movementStats.StopOnCollision,
                CollisionMask = _movementStats.GroundLayer
            };

            JumpArcSimulator.SimulationResult result = simulator.Simulate(settings);

            Gizmos.color = gizmoColor;
            var points = result.Points;

            for (int i = 1; i < points.Count; i++)
            {
                Gizmos.DrawLine(points[i - 1], points[i]);

                if (result.CollisionIndex.HasValue && i >= result.CollisionIndex.Value)
                {
                    break;
                }
            }
        }
    }
}
