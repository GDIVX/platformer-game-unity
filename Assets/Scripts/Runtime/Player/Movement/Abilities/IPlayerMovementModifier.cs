using Runtime.Player.Movement.States;

namespace Runtime.Player.Movement.Abilities
{
    public interface IPlayerMovementModifier
    {
        void Apply(PlayerMovementContext context);
        void Remove(PlayerMovementContext context);
    }
}
