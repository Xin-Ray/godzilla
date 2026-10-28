# Godzilla Failure Log

This file is append-only. Never delete a resolved failure. Add a resolution and linked regression test.

## Failure entry template

### F-L2-001 — Example title

- Status: OPEN
- First observed:
- Last reproduced:
- Linked test:
- Goal blocked:
- Build/commit:
- Unity version:
- Device:
- XR/stereo mode:
- Environment:
- User-visible symptom:
- Exact reproduction steps:
- Expected result:
- Actual result:
- Logs:
- Screenshot/video/evidence:
- Initial hypotheses:
- Confirmed root cause:
- Fix:
- Changed files/settings:
- Verification evidence:
- Regression test added:
- Resolution date:
- Remaining risk:

---

## Known recurring failure categories

Create a real failure entry when any is observed:

- splat visible in one Unity view but not truly volumetric;
- scene/prefab loses splat references after reopen;
- incorrect origin, rotation, scale, axis, or handedness;
- drift, swimming, jumps, or incorrect world anchoring on Quest;
- splat rendered in only one eye;
- shader variant/stereo mode/culling difference between eyes;
- Unity editor success but Quest build failure;
- previously fixed bug returns after a later change.
