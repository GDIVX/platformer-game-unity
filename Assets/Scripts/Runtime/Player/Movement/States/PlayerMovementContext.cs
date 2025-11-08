using System;
using Runtime.Player.Movement;
using Runtime.Player.Movement.Controllers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Player.Movement.States
{
    [Serializable]
    public class PlayerMovementContext
    {
        public PlayerMovementContext(
            PlayerMovementStats stats,
            Rigidbody2D rigidbody,
            Collider2D feetCollider,
            Collider2D bodyCollider,
            Transform transform,
            UnityEvent onJump,
            UnityEvent onFall,
            UnityEvent onMoveStart,
            UnityEvent onMoveStopped,
            UnityEvent onMoveFullyStopped,
            UnityEvent<bool> onTurn,
            UnityEvent<float> onLanded)
        {
            Stats = stats;
            Rigidbody = rigidbody;
            FeetCollider = feetCollider;
            BodyCollider = bodyCollider;
            Transform = transform;
            OnJumpEvent = onJump;
            OnFallEvent = onFall;
            OnMoveStartEvent = onMoveStart;
            OnMoveStoppedEvent = onMoveStopped;
            OnMoveFullyStoppedEvent = onMoveFullyStopped;
            OnTurnEvent = onTurn;
            OnLandedEvent = onLanded;

            RuntimeData = new PlayerMovementRuntimeData
            {
                VerticalVelocity = Stats.Gravity,
                IsFacingRight = true
            };

            Jump = new JumpController(
                RuntimeData,
                Stats,
                Rigidbody,
                Transform,
                BodyCollider,
                OnJumpEvent,
                OnFallEvent,
                OnLandedEvent);

            Wall = new WallInteractionController(
                RuntimeData,
                Stats,
                Rigidbody,
                FeetCollider,
                BodyCollider);

            Horizontal = new HorizontalMovementController(
                RuntimeData,
                Stats,
                Rigidbody,
                FeetCollider,
                Transform,
                OnMoveStartEvent,
                OnMoveStoppedEvent,
                OnMoveFullyStoppedEvent,
                OnTurnEvent,
                Jump);

            Jump.ConfigureDependencies(Wall, Horizontal);
        }

        public PlayerMovementStats Stats { get; }
        public Rigidbody2D Rigidbody { get; }
        public Collider2D FeetCollider { get; }
        public Collider2D BodyCollider { get; }
        public Transform Transform { get; }
        public UnityEvent OnJumpEvent { get; }
        public UnityEvent<bool> OnTurnEvent { get; }
        public UnityEvent OnFallEvent { get; }
        public UnityEvent OnMoveStartEvent { get; }
        public UnityEvent OnMoveStoppedEvent { get; }
        public UnityEvent OnMoveFullyStoppedEvent { get; }
        public UnityEvent<float> OnLandedEvent { get; }

        [ShowInInspector, ReadOnly]
        public PlayerMovementRuntimeData RuntimeData { get; }

        [ShowInInspector, ReadOnly]
        public HorizontalMovementController Horizontal { get; }

        [ShowInInspector, ReadOnly]
        public JumpController Jump { get; }

        [ShowInInspector, ReadOnly]
        public WallInteractionController Wall { get; }

        public void SetInput(Vector2 moveInput, bool runHeld, bool jumpPressed, bool jumpHeld, bool jumpReleased)
        {
            Horizontal.SetMovementInput(moveInput, runHeld);
            Jump.SetJumpInput(jumpPressed, jumpHeld, jumpReleased);
        }

        public void UpdateTimers(float deltaTime)
        {
            Jump.UpdateTimers(deltaTime);
            Wall.UpdateTimers(deltaTime);
        }
    }
}
