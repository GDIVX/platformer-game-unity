using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Player.Movement
{
    [Serializable]
    public class PlayerMovementRuntimeData
    {
        // ---------------------------- //
        // ────── MOVEMENT DATA ─────── //
        // ---------------------------- //
        [FoldoutGroup("Velocity"), ShowInInspector, ReadOnly]
        public Vector2 Velocity { get; set; }

        [FoldoutGroup("Velocity"), ShowInInspector, ReadOnly]
        public Vector2 TargetVelocity { get; set; }

        [FoldoutGroup("Velocity"), ShowInInspector, ReadOnly]
        public bool IsFacingRight { get; set; }

        [FoldoutGroup("Velocity"), ShowInInspector, ReadOnly]
        public float VerticalVelocity { get; set; }

        // ---------------------------- //
        // ──────── JUMPING ─────────── //
        // ---------------------------- //
        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public bool IsJumping { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public float JumpBufferTimer { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public bool JumpReleasedDuringBuffer { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public float CoyoteTimer { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public int JumpsCount { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public float ApexPoint { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public float TimePastApexThreshold { get; set; }

        [FoldoutGroup("Jumping"), ShowInInspector, ReadOnly]
        public bool IsPastApexThreshold { get; set; }

        // ---------------------------- //
        // ──────── AIR STATE ───────── //
        // ---------------------------- //
        [FoldoutGroup("Air State"), ShowInInspector, ReadOnly]
        public bool IsFalling { get; set; }

        [FoldoutGroup("Air State"), ShowInInspector, ReadOnly]
        public bool IsFastFalling { get; set; }

        [FoldoutGroup("Air State"), ShowInInspector, ReadOnly]
        public float AirTime { get; set; }

        [FoldoutGroup("Air State"), ShowInInspector, ReadOnly]
        public float FastFallTime { get; set; }

        [FoldoutGroup("Air State"), ShowInInspector, ReadOnly]
        public float FastFallReleaseSpeed { get; set; }

        // ---------------------------- //
        // ───────── GROUND ─────────── //
        // ---------------------------- //
        [FoldoutGroup("Ground"), ShowInInspector, ReadOnly]
        public bool IsGrounded { get; set; }

        [FoldoutGroup("Ground"), ShowInInspector, ReadOnly]
        public bool BumpedHead { get; set; }

        [FoldoutGroup("Ground"), ShowInInspector, ReadOnly]
        public RaycastHit2D GroundHit { get; set; }

        [FoldoutGroup("Ground"), ShowInInspector, ReadOnly]
        public RaycastHit2D HeadHit { get; set; }

        // ---------------------------- //
        // ───────── WALLS ──────────── //
        // ---------------------------- //
        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public bool IsTouchingWall { get; set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public bool IsTouchingLeftWall { get; set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public bool IsTouchingRightWall { get; set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public RaycastHit2D LeftWallHit { get; set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public RaycastHit2D RightWallHit { get; set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public RaycastHit2D WallHit { get; set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public int WallDirection { get; set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public float WallStickTimer { get; set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public bool IsWallSliding { get; set; }

        [FoldoutGroup("Walls"), ShowInInspector, ReadOnly]
        public float DirectionBufferTimer { get; set; }

        [ShowInInspector, ReadOnly, FoldoutGroup("Walls")]
        public bool WantsToMoveAwayFromWall { get; set; }

        // ---------------------------- //
        // ───────── INPUTS ─────────── //
        // ---------------------------- //
        [FoldoutGroup("Input"), ShowInInspector, ReadOnly]
        public Vector2 MoveInput { get; set; }

        [FoldoutGroup("Input"), ShowInInspector, ReadOnly]
        public bool RunHeld { get; set; }

        [FoldoutGroup("Input"), ShowInInspector, ReadOnly]
        public bool JumpPressed { get; set; }

        [FoldoutGroup("Input"), ShowInInspector, ReadOnly]
        public bool JumpHeld { get; set; }

        [FoldoutGroup("Input"), ShowInInspector, ReadOnly]
        public bool JumpReleased { get; set; }
    }
}
