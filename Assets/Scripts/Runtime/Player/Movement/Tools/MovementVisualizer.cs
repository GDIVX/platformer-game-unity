#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Runtime.Player.Movement.Tools
{
    [ExecuteAlways]
    [AddComponentMenu("Player Tools/Jump Arc Gizmo Pro")]
    public class MovementVisualizer : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private PlayerMovementStats _movementStats;

        [Header("Base Visualization")] [SerializeField]
        private bool _showWalkJumpArc = true;

        [SerializeField] private bool _showRunJumpArc = true;
        [SerializeField] private bool _stopOnCollision = true;
        [SerializeField] private bool _drawRight = true;
        [SerializeField, Range(5, 100)] private int _arcResolution = 20;
        [SerializeField, Range(10, 500)] private int _visualizationSteps = 90;

        [Header("Freefall Visualization")] [SerializeField]
        private bool _showFreefall = true;

        [SerializeField, Range(0.1f, 1f)] private float _airInfluence = 0.4f;
        [SerializeField] private Color _freefallColor = new(0.3f, 0.7f, 1f, 0.5f);

        [Header("Wall Jump Visualization")] [SerializeField]
        private bool _showWallJumps;

        [SerializeField] private bool _showLongWallJump = true;
        [SerializeField] private Color _wallJumpColor = new(0.4f, 0.9f, 1f, 0.8f);
        [SerializeField] private Color _longWallJumpColor = new(0.2f, 1f, 1f, 0.6f);

        [Header("Design Feedback")] [SerializeField]
        private bool _showLandingMarker = true;

        [SerializeField] private float _landingMarkerSize = 0.25f;
        [SerializeField] private bool _fadeArcWithDistance = true;
        [SerializeField] private float _fadeDistance = 10f;

        private static readonly Color WalkColor = new(1f, 1f, 1f, 0.8f);
        private static readonly Color RunColor = new(1f, 0.3f, 0.3f, 0.8f);
        private static readonly Color LandingColor = new(0.2f, 1f, 0.2f, 1f);

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (_movementStats == null) return;

            if (_showWalkJumpArc) DrawJumpArc(false, WalkColor);
            if (_showRunJumpArc) DrawJumpArc(true, RunColor);
            if (_showFreefall) DrawFreefallArc();
            if (_showWallJumps) DrawWallJumpArcs();
#endif
        }

        private void DrawJumpArc(bool runHeld, Color color)
        {
            var simulator = new JumpArcSimulator(_movementStats);
            Vector2 start = transform.position;

            var settings = new JumpArcSimulator.SimulationSettings
            {
                StartPosition = start,
                HorizontalInput = _drawRight ? 1f : -1f,
                RunHeld = runHeld,
                MaxSteps = _visualizationSteps,
                StopOnCollision = _stopOnCollision,
                CollisionMask = _movementStats.GroundLayer
            };

            var result = simulator.Simulate(settings);
            DrawArc(result, color, runHeld ? "Run Jump" : "Walk Jump");
        }

        private void DrawFreefallArc()
        {
            var simulator = new JumpArcSimulator(_movementStats);
            Vector2 start = transform.position;

            foreach (float dir in new[] { 0f, -_airInfluence, _airInfluence })
            {
                var result = simulator.Simulate(new JumpArcSimulator.SimulationSettings
                {
                    StartPosition = start,
                    HorizontalInput = dir,
                    RunHeld = false,
                    MaxSteps = _visualizationSteps,
                    StopOnCollision = true,
                    CollisionMask = _movementStats.GroundLayer
                });
                DrawArc(result, _freefallColor, dir == 0 ? "Freefall" : dir < 0 ? "← Drift" : "→ Drift");
            }
        }

        private void DrawWallJumpArcs()
        {
            if (_movementStats.WallSlide == null) return;
            var settings = _movementStats.WallSlide;
            var simulator = new JumpArcSimulator(_movementStats);
            Vector2 start = transform.position;

            foreach (int dir in new[] { -1, 1 })
            {
                var shortJump = simulator.Simulate(new JumpArcSimulator.SimulationSettings
                {
                    StartPosition = start,
                    HorizontalInput = dir,
                    RunHeld = true,
                    MaxSteps = _visualizationSteps,
                    StopOnCollision = true,
                    CollisionMask = _movementStats.GroundLayer,
                    InitialHorizontalVelocity = dir * settings.WallJumpHorizontalPush
                });
                DrawArc(shortJump, _wallJumpColor, $"Wall Jump {(dir > 0 ? "Right" : "Left")}");

                if (!_showLongWallJump) continue;
                var longJump = simulator.Simulate(new JumpArcSimulator.SimulationSettings
                {
                    StartPosition = start,
                    HorizontalInput = dir,
                    RunHeld = true,
                    MaxSteps = _visualizationSteps,
                    StopOnCollision = true,
                    CollisionMask = _movementStats.GroundLayer,
                    InitialHorizontalVelocity = dir * settings.WallJumpHorizontalPush *
                                                settings.LongWallJumpHorizontalMultiplier
                });
                DrawArc(longJump, _longWallJumpColor, $"Long Wall Jump {(dir > 0 ? "Right" : "Left")}");
            }
        }

#if UNITY_EDITOR
        private void DrawArc(JumpArcSimulator.SimulationResult result, Color color, string label)
        {
            var points = result.Points;
            if (points.Count < 2) return;

            Vector2 origin = points[0];
            for (int i = 1; i < points.Count; i++)
            {
                float t = _fadeArcWithDistance
                    ? Mathf.Clamp01(1f - Vector2.Distance(origin, points[i]) / _fadeDistance)
                    : 1f;
                Handles.color = new Color(color.r, color.g, color.b, color.a * t);
                Handles.DrawLine(points[i - 1], points[i]);

                if (result.CollisionIndex.HasValue && i >= result.CollisionIndex.Value)
                    break;
            }

            if (!_showLandingMarker) return;
            Vector2 landing = points[^1];
            Gizmos.color = LandingColor;
            Gizmos.DrawSphere(landing, _landingMarkerSize);
            Handles.Label(landing + Vector2.up * 0.25f, $"{label}\n({landing.x:F2}, {landing.y:F2})",
                EditorStyles.miniBoldLabel);
        }
#endif
    }
}