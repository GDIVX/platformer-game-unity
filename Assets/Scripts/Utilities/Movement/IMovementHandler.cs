using UnityEngine;

namespace Runtime.Movement
{
    /// <summary>
    /// Provides a generic interface for modifying the movement state of controllable entities.
    /// </summary>
    public interface IMovementHandler
    {
        /// <summary>
        /// Current velocity represented as a 2D vector.
        /// </summary>
        Vector2 Velocity { get; }

        /// <summary>
        /// Current vertical velocity component.
        /// </summary>
        float VerticalVelocity { get; }

        /// <summary>
        /// Overrides the current velocity with the provided value.
        /// </summary>
        /// <param name="velocity">Velocity to assign.</param>
        void SetVelocity(Vector2 velocity);

        /// <summary>
        /// Adjusts the current velocity by the provided delta.
        /// </summary>
        /// <param name="delta">Change applied to the velocity.</param>
        void AddVelocity(Vector2 delta);

        /// <summary>
        /// Queues a force that will be applied after the current movement step.
        /// </summary>
        /// <param name="force">Force represented as a delta velocity.</param>
        void ApplyForce(Vector2 force);

        /// <summary>
        /// Overrides the vertical velocity component.
        /// </summary>
        /// <param name="verticalVelocity">New vertical velocity value.</param>
        void SetVerticalVelocity(float verticalVelocity);

        /// <summary>
        /// Adjusts the vertical velocity component by the provided delta.
        /// </summary>
        /// <param name="delta">Change applied to the vertical velocity.</param>
        void AddVerticalVelocity(float delta);

        /// <summary>
        /// Prevents any movement for the rest of the current frame.
        /// </summary>
        void FreezeForFrame();
    }
}
