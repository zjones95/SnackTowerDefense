---
name: WebGL Build & Publish
description: Build the WebGL player locally and publish it to GitHub Pages (the licence-free deploy). Use ONLY when the user explicitly asks for a WebGL build — it is a heavy full Unity build plus a force-push of the `webgl` branch.
---

## When to use
Only when the user **explicitly** asks for a WebGL build or publish. Do **not** run
this for routine changes — a batch *compile* (or a Windows player build) is the
normal check. See `AGENTS.md`.

## Prerequisites
- The Unity editor must be **closed** (batch mode locks `Library/`).
- Run from the repo root: `C:\Users\Desktop\SnackTowerDefense`.

## Steps
1. Confirm no editor is holding the project:
   ```powershell
   Get-Process Unity -ErrorAction SilentlyContinue
   ```
   If one is open for this project, stop it (or ask the user to close it).
2. Publish. This builds WebGL via `-executeMethod WebGLBuild.Build` and force-pushes a
   single-commit `webgl` branch (which GitHub Pages serves — no Unity licence in CI):
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File tools\publish-webgl.ps1
   ```
   - `-SkipBuild` publishes the existing `build/WebGL` without rebuilding.
   - The script waits for `WebGLBuild: BUILD OK` in `%TEMP%\opencode\webgl-build.log`
     and prints the live URL when done.
3. Confirm success: the script ends with `Published to the 'webgl' branch.`
4. Report the build size + that the `webgl` branch was updated. The live page
   (<https://zjones95.github.io/SnackTowerDefense/>) refreshes after GitHub Pages'
   CDN TTL (~10 min).

## Notes
- Licence-free deploy details: `docs/HANDOFF.md` → *CI / licence / deploy*.
- Web size matters: this is the only step that is expensive; avoid running it more
  than needed.
- After any **wire-format** change, both peers must be on the same build
  (`NetConfig.GameVersion` + commit), so a WebGL publish also means the local
  Windows player should be rebuilt for testing.
