# Godzilla Test Matrix

Allowed statuses: `NOT_RUN` `RUNNING` `PASS` `FAIL` `BLOCKED` `READY_FOR_DEVICE` `WAIVED` (needs reason + approval).
Device rows may only become PASS after a run on Xin's Quest 3 with image evidence under `docs/validation/evidence/<Test ID>/`.

| Test ID | Test | Required | Status | Procedure / command | Evidence / measurement | Last run / environment |
|---|---|---:|---|---|---|---|
| T-MVP-001 | Character visible outside the window on Quest 3 | Yes | NOT_RUN | deploy_quest.ps1; look out the window; screencap | | |
| T-MVP-002 | Walls/ceiling/floor occlude; character only visible through the window hole | Yes | NOT_RUN | Look at wall beside window, at ceiling; screencap each | | |
| T-MVP-003 | Parallax: editor probe PNGs at head x=-0.5/0/+0.5 m, then head sway on device | Yes | READY_FOR_DEVICE | run_tests.ps1 -Platform PlayMode -Filter ParallaxProbe; device: sway head 0.5 m left/right | Editor half PASS 2026-10-28: evidence/T-MVP-003/parallax_dx{m0.5,p0.0,p0.5}.png + occlusion_debug_off.png. Measured: hole centre shifts 224 px across 1 m head sway, giant centroid shifts 1.5 px; giant px inside hole 1212 / outside 0. Viewed: hole slides left as head moves right, giant fixed; with real occluder the giant is clipped at the sill. Device half pending | 2026-10-28 editor (Bedroom00 prefab room) |
| T-MVP-004 | Placement restored after app restart (same anchor UUID) | Yes | NOT_RUN | Place, confirm, force-stop, relaunch; logcat `[Place] restored` | | |
| T-MVP-005 | WallAsWindow fallback (DevPanel force mode) frames the facing wall as the window | Yes | READY_FOR_DEVICE | DevPanel → force WallAsWindow → recalibrate | Editor half PASS: SimRigTests.ForcedWallAsWindow — mode=WallAsWindow, facing wall key WALL_FACE:0.4,1.5,4.3, holes 2→3, state→Placement | 2026-10-28 editor |
| T-MVP-006 | HeadFrame fallback (no room / forced) places character ahead with no occluder | Yes | READY_FOR_DEVICE | DevPanel → force HeadFrame → recalibrate | Editor half PASS: SimRigTests.ForcedHeadFrame — mode=HeadFrame, holes=0, frame forward=head yaw, origin under head | 2026-10-28 editor |
| T-MVP-007 | EditMode unit suite green | Yes | PASS | tools\run_tests.ps1 -Platform EditMode | tools/run_tests.ps1 EditMode → 43/43 (TestResults/EditMode.xml); PlayMode → 4/4 | 2026-10-28 editor |
| T-MVP-008 | Android APK builds headless | Yes | NOT_RUN | tools\build_quest.ps1 | | |
| T-MVP-009 | Left stick moves character, right stick turns it, left trigger runs | Yes | NOT_RUN | Device: each input; logcat `[Giant]` + screencap | | |
| T-MVP-010 | Walk/run ≥5-frame sequences and jump/punch/kick/roar ≥3-frame sequences judged frame by frame | Yes | NOT_RUN | run_tests.ps1 -Platform PlayMode -Filter Locomotion; then device | | |
| T-MVP-011 | No visible foot sliding during walk/run (in-place clips + programmatic speed) | Yes | NOT_RUN | Frame sequence from T-MVP-010: stride vs displacement | | |
| T-MVP-012 | Action lock: mashing attack/jump never breaks the state machine | Yes | NOT_RUN | EditMode GiantAnimLogicTests + device mash test | | |
| T-MVP-013 | 72 FPS steady on device | No | NOT_RUN | OVR Metrics Tool overlay or logcat FPS | | |
| T-MVP-014 | Depth API toggle occludes near real objects (hand/curtain) | No | NOT_RUN | DevPanel → Depth API on; hold hand in front | | |

## Regression tests added from failures

| Test ID | Origin failure | Required | Status | Procedure / command | Evidence | Last run |
|---|---|---:|---|---|---|---|
