using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Player.Movement
{
    [CreateAssetMenu(fileName = "PlayerMovementStats", menuName = "Player Movement Stats", order = 1)]
    public class PlayerMovementStats : ScriptableObject
    {
        [Header("Walk")] [Range(1f, 100f)] public float MaxWalkSpeed = 12.5f;
        [Range(0.25f, 50f)] public float GroundAcceleration = 5f;
        [Range(0.25f, 50f)] public float GroundDeceleration = 20f;
        [Range(0.25f, 50f)] public float AirAcceleration = 5f;
        [Range(0.25f, 50f)] public float AirDeceleration = 5f;
        [Range(0, 1)] public float MinSpeedThreshold = 0.01f;
        public float StepHeight = 0.125f;
        public Rigidbody2D.SlideMovement SlideMovement;
        public float DirectionBufferDuration = 0.15f;


        [Header("Flight")]
        [Range(0f, 30f)] public float FlyDuration = 3f;

        [Range(0f, 200f)] public float FlyLift = 35f;

        [Range(0f, 100f)] public float FlyAirAccelerationOverride = 12f;

        [Range(0f, 100f)] public float FlyAirDecelerationOverride = 25f;

        [Range(0f, 3f)] public float FlyExitHangDuration = 0.25f;

        [Range(0f, 30f)] public float FlyRegenerationRate = 2f;


        [Header("Landing")] [Range(0, 1)] public float StickinessOnLanding = 0.1f;

        [Header("Wall Checks")] public float WallDetectionRayLength = 0.1f;
        [Range(0.1f, 1f)] public float WallDetectionHeightScale = 0.9f;
        public bool DebugShowWallChecks = false;

        [Header("Wall Slide")] [SerializeField]
        private WallSlideSettings _wallSlide = new();


        [Header("Run")] [Range(1f, 100f)] public float MaxRunSpeed = 12.5f;

        [Header("Dash")]
        [Range(0f, 200f)]
        public float DashForwardBurstSpeed = 25f;

        [Range(0f, 5f)] public float DashDuration = 0.2f;
        [Range(0f, 5f)] public float DashGroundCooldown = 0.75f;
        [Range(0, 5)] public int DashAirDashLimit = 1;
        [Range(0f, 5f)] public float DashAirDashCooldown = 0.75f;
        [Range(0f, 1f)] public float DashPostDashStopDuration = 0.1f;
        [Range(0f, 1f)] public float DashDoubleTapWindow = 0.2f;

        [Header("Grounded/Collision Checks")] public LayerMask GroundLayer;
        public float GroundDetectionRayLength = 0.02f;
        public float HeadDetectionRayLength = 0.02f;
        [Range(0f, 1f)] public float HeadWidth = 0.75f;

        [Header("Edge Nudging")] public float HeadNudgeDistance = 0.5f;
        public int HeadNudgeSteps = 3;

        [Header("Jump"), OnValueChanged("CalculateValues")]
        public float JumpHeight = 6.5f;

        public float MaxRiseSpeed = 50f;
        [Range(0.1f, 5f)] public float GravityOnReleaseMultiplier = 2f;

        [Range(1f, 1.1f), OnValueChanged("CalculateValues")]
        public float JumpHeightCompensationFactor = 1.054f;

        [OnValueChanged("CalculateValues")] public float TimeToJumpApex = 0.35f;
        public float MaxFallSpeed = 26f;
        [Range(1, 5)] public int NumberOfJumpsAllowed = 2;

        [Header("Jump Cut")] [Range(0.02f, 0.3f)]
        public float TimeForUpwardsCancel = 0.027f;

        [Header("Jump Apex")] [Range(0, 1f)] public float ApexThreshold = 0.97f;

        [Range(0.01f, 1f)] public float ApexHangTime = 0.075f;

        [Header("Apex Buffer"), Range(0f, 1f)] public float JumpBufferTime = 0.125f;

        [Header("Jump Coyote Time"), Range(0, 1f)]
        public float JumpCoyoteTime = 0.1f;

        [Header("Debug")] public bool DebugShowIsGrounded = false;
        public bool DebugShowHeadBumpBox = false;


        [ShowInInspector, ReadOnly] public float Gravity { get; private set; }
        [ShowInInspector, ReadOnly] public float InitialJumpVelocity { get; private set; }
        // [ShowInInspector, ReadOnly] public float AdjustmentFactor { get; private set; }

        public WallSlideSettings WallSlide => _wallSlide;

        private void OnEnable()
        {
            CalculateValues();
        }

        private void CalculateValues()
        {
            Gravity = -(2f * JumpHeight) / Mathf.Pow(TimeToJumpApex, 2f);
            InitialJumpVelocity = 2f * JumpHeight / TimeToJumpApex * JumpHeightCompensationFactor;
            _wallSlide?.CalculateDerivedValues(Gravity);
        }


        [Serializable]
        public class WallSlideSettings
        {
            [FoldoutGroup("Settings"), LabelText("Stick Time"), Range(0f, 1f)]
            public float StickDuration = 0.2f;

            [FoldoutGroup("Settings"), LabelText("Gravity Multiplier"), Range(0f, 5f)]
            public float GravityMultiplier = 0.5f;

            [FoldoutGroup("Settings"), LabelText("Horizontal Friction"), Range(0f, 50f)]
            public float HorizontalFriction = 20f;

            [FoldoutGroup("Settings"), LabelText("Horizontal Acceleration"), Range(0f, 50f)]
            public float HorizontalAcceleration = 15f;

            [FoldoutGroup("Settings"), LabelText("Min Slide Speed"), Range(0f, 50f)]
            public float MinSlideSpeed = 1.5f;

            [FoldoutGroup("Settings"), LabelText("Max Slide Speed"), Range(0f, 50f)]
            public float MaxSlideSpeed = 8f;

            [FoldoutGroup("Jump"), LabelText("Horizontal Push"), Range(0f, 50f)]
            public float WallJumpHorizontalPush = 12f;

            [FoldoutGroup("Jump"), LabelText("Upward Boost"), Range(0f, 50f)]
            public float WallJumpUpwardBoost = 2f;

            [FoldoutGroup("Jump"), LabelText("Long Jump Upward Multiplier"), Range(0.5f, 5f)]
            public float LongWallJumpUpwardMultiplier = 1.25f;

            [FoldoutGroup("Jump"), LabelText("Long Jump Horizontal Multiplier"), Range(0.5f, 5f)]
            public float LongWallJumpHorizontalMultiplier = 1.5f;

            [FoldoutGroup("Detection"), LabelText("Horizontal Distance"), Range(0f, 1f)]
            public float WallDetectionHorizontalDistance = 0.25f;

            [FoldoutGroup("Jump"), LabelText("Wall Jump Downward Cancel Multiplier"), Range(0, 1)]
            public float WallJumpDownwardCancelMultiplier = 0.2f;

            [FoldoutGroup("Detection"), LabelText("Vertical Shrink"), Range(0f, 1f)]
            public float WallDetectionVerticalShrink = 0.1f;


            [ShowInInspector, ReadOnly] public float CalculatedGravity { get; private set; }


            public void CalculateDerivedValues(float baseGravity)
            {
                CalculatedGravity = baseGravity * GravityMultiplier;
            }
        }
    }
}