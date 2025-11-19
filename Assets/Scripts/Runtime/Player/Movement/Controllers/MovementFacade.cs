using System;
using UnityEngine;
using Runtime.Player.Movement;

namespace Runtime.Player.Movement.Controllers
{
    [Serializable]
    public class MovementFacade
    {
        private readonly PlayerMovementRuntimeData _data;
        private readonly Rigidbody2D _rigidbody;

        private Vector2 _queuedForce;
        private bool _freezeRequested;

        public MovementFacade(PlayerMovementRuntimeData data, Rigidbody2D rigidbody)
        {
            _data = data;
            _rigidbody = rigidbody;
        }

        public void SetVelocity(Vector2 velocity)
        {
            if (_data != null)
            {
                _data.Velocity = velocity;
                _data.VerticalVelocity = velocity.y;
            }

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = velocity;
            }
        }

        public void SetVerticalVelocity(float verticalVelocity)
        {
            if (_data != null)
            {
                _data.VerticalVelocity = verticalVelocity;
                _data.Velocity = new Vector2(_data.Velocity.x, verticalVelocity);
            }

            if (_rigidbody != null)
            {
                Vector2 velocity = _rigidbody.linearVelocity;
                velocity.y = verticalVelocity;
                _rigidbody.linearVelocity = velocity;
            }
        }

        public void AddVerticalVelocity(float delta)
        {
            float current = 0f;
            if (_data != null)
            {
                current = _data.VerticalVelocity;
            }
            else if (_rigidbody != null)
            {
                current = _rigidbody.linearVelocity.y;
            }

            SetVerticalVelocity(current + delta);
        }

        public void ApplyForce(Vector2 force)
        {
            _queuedForce += force;
        }

        public void Freeze()
        {
            _freezeRequested = true;
        }

        public void CommitFrame()
        {
            if (_rigidbody == null)
            {
                _queuedForce = Vector2.zero;
                _freezeRequested = false;
                return;
            }

            if (_freezeRequested)
            {
                _rigidbody.linearVelocity = Vector2.zero;

                if (_data != null)
                {
                    _data.Velocity = Vector2.zero;
                    _data.VerticalVelocity = 0f;
                }

                _queuedForce = Vector2.zero;
                _freezeRequested = false;
                return;
            }

            if (_queuedForce == Vector2.zero)
            {
                return;
            }

            Vector2 velocity = _rigidbody.linearVelocity + _queuedForce;
            _rigidbody.linearVelocity = velocity;

            if (_data != null)
            {
                _data.Velocity = velocity;
                _data.VerticalVelocity = velocity.y;
            }

            _queuedForce = Vector2.zero;
        }
    }
}
