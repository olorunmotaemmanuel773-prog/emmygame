# APEX RUSH: MODERN COMBAT - Milestone 1
First-person player controller + test range. **Status: written but NOT yet run in Unity.** Please run the tests below and report results.

## 1. Install
1. Download **Unity Hub**: https://unity.com/download (free Personal licence; check Unity's current terms).
2. In Unity Hub: Installs > Install Editor > **Unity 6.3 LTS** (6000.3.x, supported to Dec 2027).
3. Tick modules: **Android Build Support** (+ OpenJDK, Android SDK & NDK Tools) and **Windows Build Support (IL2CPP)**. Android isn't used until Milestone 5, but adding it now avoids a reinstall.
4. Code editor: let Hub install **Visual Studio Community** (tick "Game development with Unity"), or use VS Code with the C# Dev Kit.
5. PC: 64-bit Windows 10/11, 8 GB RAM minimum (16 GB better), ~15 GB free disk.

## 2. Create the project
Hub > Projects > New project > template **Universal 3D** > name `ApexRush` > Create.
(This template already includes URP and the Input System package.)
Then check: Edit > Project Settings > Player > Other Settings > **Active Input Handling** = *Input System Package (New)* (or *Both*). If Unity asks to restart, click Yes.

## 3. Add the files
Close Unity or leave it open, then copy this ZIP's `Assets/_Project` folder into your project's `Assets` folder. Wait for compilation (spinner bottom-right).

```
Assets/
  _Project/
    Scripts/
      Player/PlayerController.cs
      Utility/PlayerDebugOverlay.cs
    Editor/Milestone1SceneBuilder.cs   (editor-only scene generator)
    Scenes/      (created for you: TestRange.unity)
    Materials/   (created for you)
```

## 4. Build the scene (automatic)
Menu bar: **Apex Rush > Milestone 1 > Build Test Scene** > OK. Then press **Play**.

What it creates: TestRange (ground, walls, training building, cover, crouch gate, jump blocks, stairs, ramp) and Player > CameraRoot > Main Camera.

### Manual setup (only if you don't use the menu)
- Empty GameObject `Player` at (0, 0.05, -20). Add **CharacterController**: Height 1.8, Radius 0.35, Center (0, 0.9, 0), Step Offset 0.3, Slope Limit 45, Skin Width 0.03.
- Add `PlayerController` to Player. Child empty `CameraRoot` at local (0, 1.65, 0). Child of that: a Camera tagged MainCamera (FOV 75, Near 0.05) + Audio Listener. Delete the scene's original Main Camera.
- Drag CameraRoot into the **Camera Root** slot on PlayerController. All other values have working defaults.
- Optional: empty GameObject with `PlayerDebugOverlay`.

## 5. Controls
| Action | Keyboard/Mouse | Gamepad |
|---|---|---|
| Move | W A S D | Left stick |
| Look | Mouse | Right stick |
| Sprint | Left Shift (forward only) | Left stick click |
| Jump | Space | A / South |
| Crouch (toggle) | Left Ctrl or C | B / East |
| Free / recapture cursor | Esc / left-click | - |
| Sensitivity - / + | [ and ] | - |
| Hide debug text | F3 | - |

Touch controls arrive in Milestone 5.

## 6. Tests (all must pass)
1. **Look**: mouse moves the view; you can't flip past straight up/down.
2. **Walk**: WASD; diagonal is not faster than straight. Debug text shows ~4.5 m/s.
3. **Sprint**: hold Shift + W shows ~7.5 m/s; Shift without W doesn't sprint.
4. **Acceleration**: speed ramps up/down smoothly, not instantly.
5. **Jump**: Space jumps; you can land on the 0.5 m and 1.0 m orange blocks (west side).
6. **Crouch**: Ctrl/C lowers the camera, speed ~2.2 m/s. Walk under the orange gate (east): standing is blocked, crouching passes.
7. **Headroom**: crouch inside the gate and press Ctrl again: you stay crouched until you leave.
8. **Collision**: you can't walk through walls, crates, or the building; the doorway is passable.
9. **Stairs/ramp**: walk up the 6 steps and up the ramp without jumping.
10. **Sensitivity**: [ and ] change the value in the debug text; it persists after stopping and replaying.
11. **Cursor**: Esc frees the cursor and stops input; click returns control.
12. **Controller (optional)**: plug in a gamepad and repeat 1-6.
13. **Console**: no red errors.

## 7. Troubleshooting
| Problem | Fix |
|---|---|
| Menu "Apex Rush" missing | Scripts have compile errors: fix Console errors first, or Assets > Reimport All. |
| `InputSystem does not exist` | Window > Package Manager > Unity Registry > install **Input System**. |
| Keys/mouse do nothing | Project Settings > Player > Active Input Handling: set to Input System Package (New) or Both, restart. Also click the Game view once. |
| Everything pink | Materials need URP. Use the Universal 3D template; check Graphics settings have a URP asset. |
| "Camera Root is not assigned" | Drag CameraRoot into the PlayerController slot. |
| Mouse too fast/slow | Press [ or ]; or edit Mouse Sensitivity. |
| Stuck in cursor-free mode in the Editor | Click the Game view. |
| Fall through the floor | Player must start at y ≈ 0.05 above ground whose top is at y = 0. |
| Debug text missing | F3 toggles it; check the DebugOverlay object exists. |

## 8. Implemented vs planned
Implemented (untested until you run it): everything in section 6. Placeholder: all geometry is primitive cubes. Not yet: weapons, enemies, health, missions, HUD, touch controls, audio, multiplayer.

## 9. Design notes
- Input actions are created in code, so there's no asset to misconfigure. In Milestone 5 the touch controls will feed the same movement/look values, so PC and Android share one controller.
- The Player root rotates left/right; CameraRoot pitches up/down. This keeps aiming and weapons simple later.
- `Respawn()` is public so PlayerHealth (Milestone 3/4) can call it.
- The debug overlay uses OnGUI and allocates strings each frame; it is for testing only.
