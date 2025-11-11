using System;
using UnityEngine;

namespace Runtime.Player.Movement
{
    [Serializable]
    public struct PlayerStateSnapshot
    {
        [SerializeField] private Vector2 _velocity;
        [SerializeField] private float _stamina;
        [SerializeField] private float _dashCooldown;
        [SerializeField] private float _airDashCooldown;
        [SerializeField] private float _glideTimeRemaining;
        [SerializeField] private float _flightTimeRemaining;
        [SerializeField] private int _airDashCount;

        public PlayerStateSnapshot(
            Vector2 velocity,
            float stamina,
            float dashCooldown,
            float airDashCooldown,
            float glideTimeRemaining,
            float flightTimeRemaining,
            int airDashCount)
        {
            _velocity = velocity;
            _stamina = Mathf.Max(0f, stamina);
            _dashCooldown = Mathf.Max(0f, dashCooldown);
            _airDashCooldown = Mathf.Max(0f, airDashCooldown);
            _glideTimeRemaining = Mathf.Max(0f, glideTimeRemaining);
            _flightTimeRemaining = Mathf.Max(0f, flightTimeRemaining);
            _airDashCount = Mathf.Max(0, airDashCount);
        }

        public Vector2 Velocity => _velocity;
        public float Stamina => _stamina;
        public float DashCooldown => _dashCooldown;
        public float AirDashCooldown => _airDashCooldown;
        public float GlideTimeRemaining => _glideTimeRemaining;
        public float FlightTimeRemaining => _flightTimeRemaining;
        public int AirDashCount => _airDashCount;

        public PlayerStateSnapshot WithVelocity(Vector2 velocity)
        {
            return new PlayerStateSnapshot(
                velocity,
                _stamina,
                _dashCooldown,
                _airDashCooldown,
                _glideTimeRemaining,
                _flightTimeRemaining,
                _airDashCount);
        }

        public PlayerStateSnapshot WithStamina(float stamina)
        {
            return new PlayerStateSnapshot(
                _velocity,
                Mathf.Max(0f, stamina),
                _dashCooldown,
                _airDashCooldown,
                _glideTimeRemaining,
                _flightTimeRemaining,
                _airDashCount);
        }

        public PlayerStateSnapshot WithDashCooldown(float dashCooldown)
        {
            return new PlayerStateSnapshot(
                _velocity,
                _stamina,
                Mathf.Max(0f, dashCooldown),
                _airDashCooldown,
                _glideTimeRemaining,
                _flightTimeRemaining,
                _airDashCount);
        }

        public PlayerStateSnapshot WithAirDashCooldown(float airDashCooldown)
        {
            return new PlayerStateSnapshot(
                _velocity,
                _stamina,
                _dashCooldown,
                Mathf.Max(0f, airDashCooldown),
                _glideTimeRemaining,
                _flightTimeRemaining,
                _airDashCount);
        }

        public PlayerStateSnapshot WithGlideTime(float glideTimeRemaining)
        {
            return new PlayerStateSnapshot(
                _velocity,
                _stamina,
                _dashCooldown,
                _airDashCooldown,
                Mathf.Max(0f, glideTimeRemaining),
                _flightTimeRemaining,
                _airDashCount);
        }

        public PlayerStateSnapshot WithFlightTime(float flightTimeRemaining)
        {
            return new PlayerStateSnapshot(
                _velocity,
                _stamina,
                _dashCooldown,
                _airDashCooldown,
                _glideTimeRemaining,
                Mathf.Max(0f, flightTimeRemaining),
                _airDashCount);
        }

        public PlayerStateSnapshot WithAirDashCount(int airDashCount)
        {
            return new PlayerStateSnapshot(
                _velocity,
                _stamina,
                _dashCooldown,
                _airDashCooldown,
                _glideTimeRemaining,
                _flightTimeRemaining,
                Mathf.Max(0, airDashCount));
        }

        public PlayerStateSnapshot ConsumeStamina(float amount)
        {
            return WithStamina(_stamina - Mathf.Max(0f, amount));
        }

        public PlayerStateSnapshot ConsumeGlideTime(float amount)
        {
            return WithGlideTime(_glideTimeRemaining - Mathf.Max(0f, amount));
        }

        public PlayerStateSnapshot ConsumeFlightTime(float amount)
        {
            return WithFlightTime(_flightTimeRemaining - Mathf.Max(0f, amount));
        }

        public PlayerStateSnapshot ConsumeDashCooldown(float duration)
        {
            return WithDashCooldown(Mathf.Max(0f, _dashCooldown - Mathf.Max(0f, duration)));
        }

        public PlayerStateSnapshot ConsumeAirDashCooldown(float duration)
        {
            return WithAirDashCooldown(Mathf.Max(0f, _airDashCooldown - Mathf.Max(0f, duration)));
        }

        public bool ApproximatelyEquals(PlayerStateSnapshot other, float tolerance = 0.05f,
            float velocityTolerance = 0.05f)
        {
            return Vector2.Distance(Velocity, other.Velocity) <= velocityTolerance &&
                   Mathf.Abs(Stamina - other.Stamina) <= tolerance &&
                   Mathf.Abs(DashCooldown - other.DashCooldown) <= tolerance &&
                   Mathf.Abs(AirDashCooldown - other.AirDashCooldown) <= tolerance &&
                   Mathf.Abs(GlideTimeRemaining - other.GlideTimeRemaining) <= tolerance &&
                   Mathf.Abs(FlightTimeRemaining - other.FlightTimeRemaining) <= tolerance &&
                   AirDashCount == other.AirDashCount;
        }

        public override string ToString()
        {
            return $"Velocity={_velocity}, Stamina={_stamina:F2}, DashCd={_dashCooldown:F2}, " +
                   $"AirDashCd={_airDashCooldown:F2}, Glide={_glideTimeRemaining:F2}, Flight={_flightTimeRemaining:F2}, " +
                   $"AirDashCount={_airDashCount}";
        }

        public static PlayerStateSnapshot FromRuntime(PlayerMovementRuntimeData runtimeData)
        {
            if (runtimeData == null)
            {
                return default;
            }

            float glideRemaining = 0f;
            if (runtimeData.Glide != null)
            {
                float maxDuration = Mathf.Max(0f, runtimeData.Glide.MaxDuration);
                glideRemaining = Mathf.Max(0f, maxDuration - runtimeData.Glide.ElapsedTime);
            }

            return new PlayerStateSnapshot(
                runtimeData.Velocity,
                Mathf.Max(0f, runtimeData.FlightTimeRemaining),
                Mathf.Max(0f, runtimeData.DashCooldownTimer),
                Mathf.Max(0f, runtimeData.AirDashCooldownTimer),
                glideRemaining,
                Mathf.Max(0f, runtimeData.FlightTimeRemaining),
                Mathf.Max(0, runtimeData.AirDashCount));
        }
    }
}
