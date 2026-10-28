# Godzilla City-Scale MR — Validation Goals

## Project metadata

- Unity project: C:\Users\xxian\godzilla
- Unity version: 6000.2.15f1
- Render pipeline: URP 17.2.0 (Forward, MSAA 4, shadows off)
- XR provider/plugin: OpenXR 1.14.3 + Meta OpenXR 2.1.0; Meta XR Core SDK 85 (OVRCameraRig, OVRPassthroughLayer), MRUK 85
- Target device: Meta Quest 3 (Xin's)
- Current build ID: (none yet)
- Last updated: 2026-10-28

## Goals

| Goal ID | Goal | Required | Status | Dependencies | Acceptance summary |
|---|---|---:|---|---|---|
| G-MVP-01 | MVP 0.1 — a 30–200 m character stands in the real city outside the window: correct parallax, window-frame occlusion, placement persisted across restarts | Yes | OPEN | None | T-MVP-001…008 PASS on Quest 3 |
| G-MVP-02 | MVP 0.2 — the same character is driven by Touch controllers: move, turn, run, jump, punch, kick, roar with clean transitions | Yes | OPEN | G-MVP-01 | T-MVP-009…012 PASS on Quest 3 |
