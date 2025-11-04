using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Runtime.Player.Movement.Tools
{
    [ExecuteAlways]
    [AddComponentMenu("Player Tools/Jump Arc Gizmo Pro")]
    public class MovementVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovementStats _movementStats;

        [Header("Base Visualization")]
        [SerializeField] private bool _showWalkJumpArc = false;
        [SerializeField] private bool _showRunJumpArc = false;
        [SerializeField] private bool _stopOnCollision = false;
        [SerializeField] private bool _drawRight = true;
        [SerializeField, Range(5, 100)] private int _arcResolution = 20;
        [SerializeField, Range(10, 500)] private int _visualizationSteps = 90;

        [Header("Freefall Visualization")]
        [SerializeField] private bool _showFreefall = false;
        [SerializeField, Range(0.1f, 1f)] private float _airInfluence = 0.4f;
        [SerializeField] private Color _freefallColor = new(0.3f, 0.7f, 1f, 0.5f);

        [Header("Wall Jump Visualization")]
        [SerializeField] private bool _showWallJumps = false;
        [SerializeField] private bool _showLongWallJump = true;
        [SerializeField] private Color _wallJumpColor = new(0.4f, 0.9f, 1f, 0.8f);
        [SerializeField] private Color _longWallJumpColor = new(0.2f, 1f, 1f, 0.6f);
        [SerializeField, Range(1, 2)] private int _wallSides = 2;

        [Header("Design Feedback")]
        [SerializeField] private bool _showLandingMarker = true;
        [SerializeField] private float _landingMarkerSize = 0.25f;
        [SerializeField] private bool _fadeArcWithDistance = true;
        [SerializeField] private float _fadeDistance = 10f;
        [SerializeField] private bool _showReachEnvelope = false;

        private static readonly Color WalkColor = new(1f, 1f, 1f, 0.8f);
        private static readonly Color RunColor = new(1f, 0.3f, 0.3f, 0.8f);
        private static readonly Color LandingColor = new(0.2f, 1f, 0.2f, 1f);

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (_movementStats == null) return;

            // --- Basic Jumps ---
            if (_showWalkJumpArc) DrawJumpArc(false, WalkColor);
            if (_showRunJumpArc) DrawJumpArc(true, RunColor);

            // --- Freefall ---
            if (_showFreefall) DrawFreefallArcs();

            // --- Wall jumps ---
            if (_showWallJumps) DrawWallJumpArcs();

            // --- Reach envelope ---
            if (_showReachEnvelope) DrawReachEnvelope();
#endif
        }

        private void DrawJumpArc(bool runHeld, Color baseColor)
        {
            var simulator = new JumpArcSimulator(_movementStats);

            Vector2 startPosition = transform.position;
            float horizontalInput = _drawRight ? 1f : -1f;

            var settings = new JumpArcSimulator.SimulationSettings
            {
                StartPosition = startPosition,
                HorizontalInput = horizontalInput,
                RunHeld = runHeld,
                MaxSteps = Mathf.Max(1, _visualizationSteps),
                StopOnCollision = _stopOnCollision,
                CollisionMask = _movementStats.GroundLayer
            };

            var result = simulator.Simulate(settings);
            DrawArc(result, startPosition, baseColor, runHeld ? "Run Jump" : "Walk Jump");
        }

        private void DrawFreefallArcs()
        {
            var simulator = new JumpArcSimulator(_movementStats);
            Vector2 startPosition = transform.position;

            // Neutral fall
            DrawFreefall(simulator, startPosition, 0f, _freefallColor, "Freefall");

            // Air drift left/right
            if (_airInfluence > 0.01f)
            {
                DrawFreefall(simulator, startPosition, -_airInfluence, _freefallColor * 0.9f, "Air Drift ←");
                DrawFreefall(simulator, startPosition, _airInfluence, _freefallColor * 0.9f, "Air Drift →");
            }
        }

        private void DrawFreefall(JumpArcSimulator simulator, Vector2 startPos, float horizontalInput, Color color, string label)
        {
            var settings = new JumpArcSimulator.SimulationSettings
            {
                StartPosition = startPos,
                HorizontalInput = horizontalInput,
                RunHeld = false,
                MaxSteps = Mathf.Max(1, _visualizationSteps),
                StopOnCollision = true,
                CollisionMask = _movementStats.GroundLayer
            };

            var result = simulator.Simulate(settings);
            DrawArc(result, startPos, color, label);
        }

        private void DrawWallJumpArcs()
        {
            if (_movementStats.WallSlide == null) return;

            var settings = _movementStats.WallSlide;
            var simulator = new JumpArcSimulator(_movementStats);
            Vector2 start = transform.position;

            for (int dir = -1; dir <= 1; dir += 2)
            {
                if (_wallSides < 2 && ((_drawRight && dir < 0) || (!_drawRight && dir > 0)))
                    continue;

                // --- Short wall jump ---
                var shortSettings = new JumpArcSimulator.SimulationSettings
                {
                    StartPosition = start,
                    HorizontalInput = dir,
                    RunHeld = true,
                    MaxSteps = _visualizationSteps,
                    StopOnCollision = true,
                    CollisionMask = _movementStats.GroundLayer,
                    InitialHorizontalVelocity = dir * settings.WallJumpHorizontalPush
                };
                var shortResult = simulator.Simulate(shortSettings);
                DrawArc(shortResult, start, _wallJumpColor, $"Wall Jump ({(dir > 0 ? "Right" : "Left")})");

                // --- Long wall jump ---
                if (_showLongWallJump)
                {
                    var longSettings = shortSettings;
                    longSettings.InitialHorizontalVelocity = dir * settings.WallJumpHorizontalPush * settings.LongWallJumpHorizontalMultiplier;
                    var longResult = simulator.Simulate(longSettings);
                    DrawArc(longResult, start, _longWallJumpColor, $"Long Wall Jump ({(dir > 0 ? "Right" : "Left")})");
                }
            }
        }

#if UNITY_EDITOR
        private void DrawArc(JumpArcSimulator.SimulationResult result, Vector2 origin, Color baseColor, string label)
        {
            var points = result.Points;
            if (points.Count < 2) return;

            for (int i = 1; i < points.Count; i++)
            {
                float t = _fadeArcWithDistance
                    ? Mathf.Clamp01(1f - Vector2.Distance(origin, points[i]) / _fadeDistance)
                    : 1f;
                Handles.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * t);
                Handles.DrawLine(points[i - 1], points[i]);

                if (result.CollisionIndex.HasValue && i >= result.CollisionIndex.Value)
                    break;
            }

            if (_showLandingMarker)
            {
                Vector2 landing = points[^1];
                Gizmos.color = LandingColor;
                Gizmos.DrawSphere(landing, _landingMarkerSize);
                Handles.Label(landing + Vector2.up * 0.25f, $"{label}\n({landing.x:F2}, {landing.y:F2})", EditorStyles.miniBoldLabel);
            }
        }

        private void DrawReachEnvelope()
        {
            var simulator = new JumpArcSimulator(_movementStats);
            Vector2 start = transform.position;
            var arcs = new[]
            {
                simulator.Simulate(new JumpArcSimulator.SimulationSettings{StartPosition=start,HorizontalInput=1,RunHeld=false,MaxSteps=100}),
                simulator.Simulate(new JumpArcSimulator.SimulationSettings{StartPosition=start,HorizontalInput=-1,RunHeld=false,MaxSteps=100}),
                simulator.Simulate(new JumpArcSimulator.SimulationSettings{StartPosition=start,HorizontalInput=1,RunHeld=true,MaxSteps=100}),
                simulator.Simulate(new JumpArcSimulator.SimulationSettings{StartPosition=start,HorizontalInput=-1,RunHeld=true,MaxSteps=100})
            };

            Handles.color = new Color(1f, 1f, 0f, 0.1f);
            foreach (var arc in arcs)
            {
                var pts = arc.Points;
                for (int i = 1; i < pts.Count; i++)
                {
                    Handles.DrawLine(pts[i - 1], pts[i]);
                }
            }
        }
#endif
    }
}
