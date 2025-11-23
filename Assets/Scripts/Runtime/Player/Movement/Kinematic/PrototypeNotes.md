# Kinematic controller prototype

The prototype replaces `Rigidbody2D.Slide` with a kinematic sweep that moves the player by casting the body collider along each axis and trimming displacement when a hit occurs. It reuses the existing ground/head/wall raycasts so behaviour tweaks apply to both controllers.

## Observed behaviour
- Horizontal sweeps and per-axis resolution visibly reduce corner jitter when approaching thin edges; wall slide contact sticks more consistently on the playtest blockouts.
- Upward casts stop jump arcs cleanly under low ceilings while still letting the head nudge logic run, cutting down on the “rubber band” bounce seen with the rigidbody.
- Dash sweeps remain deterministic even on moving platforms because they aren’t affected by solver sub-steps.

## Tuning notes
- Slide skin width (`_skinWidth`) and the grounded box height are the two most sensitive values; tightening them too much causes micro-penetrations that have to be corrected by the vertical sweep.
- Air acceleration matches the ScriptableObject values, but reaching top speed in the air now depends heavily on the collider size because more frequent wall contacts cancel velocity.
- Wall slide gravity uses the existing derived value; reducing `WallSlide.MaxSlideSpeed` helped avoid instant stop/start when brushing sloped tiles.

## Performance
- The kinematic path removes the rigidbody solver cost and replaces it with four BoxCast calls per physics tick. In the profiler the total time per frame stayed within the existing budget and looked stable even when dash spammed against walls.

## Next steps
- Keep both controllers wired to the toggle to gather more edge-case footage (stairs, moving one-way platforms).
- If level feedback stays positive, migrate dash and wall-slide from the prototype back into the rigidbody path or retire the rigidbody entirely.
