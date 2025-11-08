using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Player.Movement.Events
{
    [CreateAssetMenu(menuName = "Player/Movement/Movement Event Bus", fileName = "MovementEventBus")]
    public class MovementEventBus : ScriptableObject
    {
        [SerializeField]
        private UnityEvent _flyStarted = new UnityEvent();

        [SerializeField]
        private UnityEvent _flyEnded = new UnityEvent();

        [SerializeField]
        private UnityEvent _glideStarted = new UnityEvent();

        [SerializeField]
        private UnityEvent _glideEnded = new UnityEvent();

        [SerializeField]
        private UnityEvent _dashStarted = new UnityEvent();

        [SerializeField]
        private UnityEvent _dashEnded = new UnityEvent();

        public UnityEvent FlyStarted => _flyStarted;
        public UnityEvent FlyEnded => _flyEnded;
        public UnityEvent GlideStarted => _glideStarted;
        public UnityEvent GlideEnded => _glideEnded;
        public UnityEvent DashStarted => _dashStarted;
        public UnityEvent DashEnded => _dashEnded;

        public void RaiseFlyStarted()
        {
            _flyStarted?.Invoke();
        }

        public void RaiseFlyEnded()
        {
            _flyEnded?.Invoke();
        }

        public void RaiseGlideStarted()
        {
            _glideStarted?.Invoke();
        }

        public void RaiseGlideEnded()
        {
            _glideEnded?.Invoke();
        }

        public void RaiseDashStarted()
        {
            _dashStarted?.Invoke();
        }

        public void RaiseDashEnded()
        {
            _dashEnded?.Invoke();
        }
    }
}
