using UnityEngine;

namespace Runtime.Player.Movement.Tools
{
    public class JumpArcGizmo : MonoBehaviour
    {
        [SerializeField] private PlayerMovementStats _movementStats;
        [SerializeField] private float _initialHorizontalVelocity;

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR


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

            Vector2 startPosition = transform.position;
            float horizontalInput = _movementStats.DrawnRight ? 1f : -1f;

            var settings = new JumpArcSimulator.SimulationSettings
            {
                StartPosition = startPosition,
                HorizontalInput = horizontalInput,
                RunHeld = runHeld,
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
