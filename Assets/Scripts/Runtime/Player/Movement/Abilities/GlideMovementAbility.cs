using System;
using System.Collections.Generic;
using Runtime.Player.Movement.States;
using UnityEngine;

namespace Runtime.Player.Movement.Abilities
{
    [CreateAssetMenu(menuName = "Player/Movement/Abilities/Glide", fileName = "GlideMovementAbility")]
    public class GlideMovementAbility : ScriptableObject, IMovementAbility
    {
        [SerializeField]
        private bool _overrideHorizontalStats = false;

        [SerializeField]
        private float _customGlideAcceleration = 5f;

        [SerializeField]
        private float _customGlideDeceleration = 5f;

        public bool OverrideHorizontalStats
        {
            get => _overrideHorizontalStats;
            set => _overrideHorizontalStats = value;
        }

        public float CustomGlideAcceleration
        {
            get => _customGlideAcceleration;
            set => _customGlideAcceleration = value;
        }

        public float CustomGlideDeceleration
        {
            get => _customGlideDeceleration;
            set => _customGlideDeceleration = value;
        }

        public void Initialize(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
        }

        public IEnumerable<IPlayerMovementState> CreateStates(
            PlayerMovementContext context,
            PlayerMovementStateMachine stateMachine)
        {
            yield return new GlideState(context, stateMachine);
        }

        public IEnumerable<IPlayerMovementModifier> CreateModifiers(PlayerMovementContext context)
        {
            if (_overrideHorizontalStats)
            {
                yield return new GlideHorizontalModifier(
                    Mathf.Max(0f, _customGlideAcceleration),
                    Mathf.Max(0f, _customGlideDeceleration));
            }
        }

        public IEnumerable<Func<PlayerMovementContext, bool>> CreateActivationConditions(
            PlayerMovementContext context)
        {
            return Array.Empty<Func<PlayerMovementContext, bool>>();
        }

        public void OnAbilityEnabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            if (context == null)
            {
                return;
            }

            var runtimeData = context.RuntimeData?.Glide;
            if (runtimeData == null)
            {
                return;
            }

            var glideStats = context.Stats?.Glide;

            if (glideStats != null)
            {
                runtimeData.FallSpeedMultiplier = Mathf.Max(0f, glideStats.FallSpeedMultiplier);
                runtimeData.MaxDuration = glideStats.LimitDuration
                    ? Mathf.Max(0f, glideStats.MaxDuration)
                    : 0f;

                runtimeData.Acceleration = Mathf.Max(0f, glideStats.HorizontalAcceleration);
                runtimeData.Deceleration = Mathf.Max(0f, glideStats.HorizontalDeceleration);
            }
            else if (!_overrideHorizontalStats)
            {
                runtimeData.Acceleration = Mathf.Max(0f, runtimeData.Acceleration);
                runtimeData.Deceleration = Mathf.Max(0f, runtimeData.Deceleration);
                runtimeData.FallSpeedMultiplier = Mathf.Max(0f, runtimeData.FallSpeedMultiplier);
                runtimeData.MaxDuration = 0f;
            }
            else
            {
                runtimeData.FallSpeedMultiplier = Mathf.Max(0f, runtimeData.FallSpeedMultiplier);
                runtimeData.MaxDuration = 0f;
            }

            runtimeData.ElapsedTime = 0f;
            runtimeData.IsGliding = false;
        }

        public void OnAbilityDisabled(PlayerMovementContext context, PlayerMovementStateMachine stateMachine)
        {
            if (context == null)
            {
                return;
            }

            var glideData = context.RuntimeData?.Glide;
            if (glideData == null)
            {
                return;
            }

            if (glideData.IsGliding)
            {
                context.RaiseGlideEnded();
            }

            glideData.Reset();
            glideData.Acceleration = 0f;
            glideData.Deceleration = 0f;
            glideData.MaxDuration = 0f;
            glideData.FallSpeedMultiplier = 0f;
        }

        private class GlideHorizontalModifier : IPlayerMovementModifier
        {
            private readonly float _acceleration;
            private readonly float _deceleration;

            private float _previousAcceleration;
            private float _previousDeceleration;
            private bool _applied;

            public GlideHorizontalModifier(float acceleration, float deceleration)
            {
                _acceleration = acceleration;
                _deceleration = deceleration;
            }

            public void Apply(PlayerMovementContext context)
            {
                if (context == null)
                {
                    return;
                }

                var glideData = context.RuntimeData?.Glide;
                if (glideData == null)
                {
                    return;
                }

                _previousAcceleration = glideData.Acceleration;
                _previousDeceleration = glideData.Deceleration;

                glideData.Acceleration = _acceleration;
                glideData.Deceleration = _deceleration;

                _applied = true;
            }

            public void Remove(PlayerMovementContext context)
            {
                if (!_applied || context == null)
                {
                    return;
                }

                var glideData = context.RuntimeData?.Glide;
                if (glideData == null)
                {
                    return;
                }

                glideData.Acceleration = _previousAcceleration;
                glideData.Deceleration = _previousDeceleration;
            }
        }
    }
}
