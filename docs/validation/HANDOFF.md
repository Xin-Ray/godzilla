# Handoff

- Handoff type: **DEVICE** — editor halves pass; every Quest 3 row is READY_FOR_DEVICE (never run on the headset yet).
- Build: `Builds/Godzilla.apk` (55.2 MB) from commit `f4a882a` on `main`, 2026-10-28 18:44. App id `com.xin.godzilla`. Suites at that commit: EditMode 65/65, PlayMode 8/8.
- Device assumptions: Quest 3, Horizon OS with Space Setup done for the room (windows marked as **Window** if the scan tool offers it), Wi-Fi shared with the PC for wireless adb.
- Evidence index: `docs/validation/evidence/T-MVP-003/` (parallax + occlusion PNGs), `docs/validation/evidence/T-MVP-010/` (locomotion frames + telemetry.txt). Device captures go under the same test IDs with a `device_` prefix.

## Device steps (Xin)

1. **Connect once over USB**, accept the USB debugging dialog on the headset, then on the PC:
   ```
   tools\wifi_adb.ps1
   ```
   Accept the second "Allow wireless debugging" dialog in the headset, unplug USB.
2. **Install + launch + logcat** (replace the IP the script printed):
   ```
   tools\deploy_quest.ps1 -Serial 192.168.x.y:5555
   ```
   Expected log lines within ~5 s: `[App] Boot --StartLoading--> LoadingRoom`, `[Window] mode=Window anchor=<uuid> holes=N` (or `mode=WallAsWindow` / `mode=HeadFrame`), `[Place] load=None ...`, `[App] LoadingRoom --RoomReady_NeedsPlacement--> Placement`.
3. **T-MVP-001/002 — Placement + visibility.** Stand at the window. Hold the right trigger and point at the city to drop the giant, sticks fine-tune (left X lateral, left Y height, right X yaw, right Y distance). Press **A** to confirm. Look at the wall beside the window and at the ceiling: no part of the giant may show there. PASS = giant visible only through the window; FAIL = any bleed over the frame. Capture: `adb -s <serial> exec-out screencap -p > docs/validation/evidence/T-MVP-002/device_wall.png` (if the capture is black because of the compositor, note it and describe what you saw).
4. **T-MVP-003 — Parallax.** Sway your head 0.5 m left and right. PASS = the window frame moves across the giant and the giant stays planted in the city; FAIL = the giant slides with your head like a sticker.
5. **T-MVP-004 — Restore.** Force-stop and relaunch (`tools\deploy_quest.ps1` again, or close from the Quest menu). Expected: `[Place] load=Restored ...` and `--RoomReady_Restored--> Play`; the giant is where you left it without re-placing.
6. **T-MVP-005/006 — Fallbacks.** Hold the left **Menu** button 1 s to open the DEV panel. Left stick up/down moves the cursor, **A** activates. Cycle "Force mode" to WallAsWindow, then "Re-calibrate now": the wall you face becomes the opening. Then HeadFrame: the giant appears straight ahead with no wall occlusion. Set back to auto and recalibrate.
7. **T-MVP-009/010/011 — Controls.** In Play: left stick walks (left trigger = run), right stick turns, **A** jump, **B** punch, **X** kick, **Y** roar. Watch the feet during walk/run: PASS = strides match travel (no skating); FAIL = feet slide. Mash B/X/Y/A during an action: the giant must finish the action and respond again afterwards (T-MVP-012 device half).
8. **T-MVP-013 — FPS.** Read the DEV panel FPS row while walking; target ≥ 72.
9. **T-MVP-014 (optional) — Depth API.** DEV panel → "Depth API occlusion" ON, hold a hand in front of the window: the hand should occlude the giant. Expect glass reflections to cause artifacts; this is why it is off by default.
10. Report each row as PASS/FAIL with what you saw; FAIL rows become `F-MVP-###` entries in `FAILURE_LOG.md` before any fix.

## Known limitations in this build

- Interim body (RoomAssistantChar, Mixamo Ch02) and clips: run = walk at 1.6×, punch = kick clip, roar = idle clip (roar unlocks via the 11 s timeout). Replace by logging into mixamo.com in Browser 2 and downloading Y Bot + Walking (In Place) + Running (In Place) + Punching + Roar into `Assets/Characters/Placeholder{,/Anim}`, then run `Godzilla/Bootstrap Scene` (re-generates the animator).
- No turn-in-place animation: rotating while idle slides the feet (spec-accepted for MVP).
- Window hole edge is exactly the scan's WINDOW_FRAME rect; no inset/outset.
