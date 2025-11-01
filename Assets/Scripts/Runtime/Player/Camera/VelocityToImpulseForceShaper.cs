using Runtime.Player.Movement;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Player.Camera
{
    public class VelocityToImpulseForceShaper : MonoBehaviour
    {
        public UnityEvent<float> OnShapedForce;
        [SerializeField] AnimationCurve _curve;
        [SerializeField] private float _maxAirTime = 5f;
        [SerializeField] PlayerMovementStats _stats;
        [SerializeField] private PlayerMovement _playerMovement;

        public void ShapedForce()
        {
            var airTime = _playerMovement.Context.AirTime;
            var relation = Mathf.InverseLerp(0, _maxAirTime, airTime);
            var relativeForce = _curve.Evaluate(relation);
            OnShapedForce.Invoke(relativeForce);
            Debug.Log(relation);
        }
    }
}