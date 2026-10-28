# Godzilla Test Matrix

Allowed statuses: `NOT_RUN` `RUNNING` `PASS` `FAIL` `BLOCKED` `READY_FOR_DEVICE` `WAIVED` (needs reason + approval).
Device rows may only become PASS after a run on Xin's Quest 3 with image evidence under `docs/validation/evidence/<Test ID>/`.

| Test ID | Test | Required | Status | Procedure / command | Evidence / measurement | Last run / environment |
|---|---|---:|---|---|---|---|
| T-MVP-001 | Character visible outside the window on Quest 3 | Yes | NOT_RUN | deploy_quest.ps1; look out the window; screencap | | |
| T-MVP-002 | Walls/ceiling/floor occlude; character only visible through the window hole | Yes | NOT_RUN | Look at wall beside window, at ceiling; screencap each | | |
| T-MVP-003 | Parallax: editor probe PNGs at head x=-0.5/0/+0.5 m, then head sway on device | Yes | NOT_RUN | run_tests.ps1 -Platform PlayMode -Filter ParallaxProbe; device: sway head 0.5 m left/right | | |
| T-MVP-004 | Placement restored after app restart (same anchor UUID) | Yes | NOT_RUN | Place, confirm, force-stop, relaunch; logcat `[Place] restored` | | |
| T-MVP-005 | WallAsWindow fallback (DevPanel force mode) frames the facing wall as the window | Yes | NOT_RUN | DevPanel → force WallAsWindow → recalibrate | | |
| T-MVP-006 | HeadFrame fallback (no room / forced) places character ahead with no occluder | Yes | NOT_RUN | DevPanel → force HeadFrame → recalibrate | | |
| T-MVP-007 | EditMode unit suite green | Yes | NOT_RUN | tools\run_tests.ps1 -Platform EditMode | | |
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
