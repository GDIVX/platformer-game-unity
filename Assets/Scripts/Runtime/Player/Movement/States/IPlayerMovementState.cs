using System;

namespace Runtime.Player.Movement.States
{
    public interface IPlayerMovementState
    {
        void OnEnter();
        void OnExit();
        void HandleInput();
        void Tick();
        void FixedTick();
    }
}
