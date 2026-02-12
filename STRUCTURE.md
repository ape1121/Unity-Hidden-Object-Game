# Search It - Application Structure

This document captures two things:
- The current state of the project (what is already implemented).
- The target vertical-slice structure with a single app entry point (`App.cs`).

## Current State (Audited)

## Scenes
- `Assets/_Game/Scenes/Loader.unity`
- `Assets/_Game/Scenes/Main.unity`
- `Assets/_Game/Scenes/Game.unity`

Build order is already correct:
1. Loader
2. Main
3. Game

## Runtime Scripts Already Implemented

### Game orchestration
- `GameManager`
  - Initializes `UIManager` and `LevelRuntimeMap`.
  - Wires existing managers via scene references.

### Level and item runtime
- `LevelRuntimeMap`
  - Applies `LevelData` (background, camera bounds, spawn request).
- `HiddenItemSpawner`
  - Spawns hidden items from `LevelData` and `AllItems`.
- `HiddenItemInput`
  - Handles click/touch item selection with UI blocking.
- `HiddenItemCollector`
  - Validates and collects items via events.
- `HiddenItem`
  - Item runtime state and collected flag.

### Camera controls
- `CameraRigController`
  - Pan + pinch/mouse zoom.
  - Camera bound clamping.

### UI runtime
- `RemainingItems` / `RemainingItem`
  - Builds and updates "remaining targets" UI list.
- `UIManager`
  - Subscribes to spawn/collect events.
  - Queues collection animations and commits UI updates.
- `UICollectionAnimator`
  - DOTween-based fly-to-target animation.

### Data and authoring
- `AllItems` (`ScriptableObject`)
- `LevelData` (`ScriptableObject`)
- `LevelAuthoringRoot`, `LevelItemMarker`, and editor tooling.

## Gap vs Target Vertical Slice
- No persistent app entry point (`App.cs`) yet.
- No scene-flow controller that owns transitions (`Loader -> Main -> Game` and return).
- No game session state manager (`start/pause/end`).
- No coin system (elapsed-time reward).
- No dedicated popup flow (pause popup, level-complete popup).
- No explicit UI enter/exit screen transition system for scene-level menus.

---

## Target Structure (Single Entry Point)

Design goals:
- Keep scripts simple and modular.
- Keep scene references local to scene installers.
- Keep cross-scene state in one persistent `App`.

## Scene Ownership Model
- `Loader` scene:
  - Contains only bootstrap objects.
  - Creates/loads `App` and transitions to `Main`.
- `Main` scene:
  - Main menu and navigation.
  - Start game, return from game, optional continue.
- `Game` scene:
  - Runtime level loop and gameplay UI.
  - Reports pause/complete/back events to `App`.

## Persistent Root

`App` (`DontDestroyOnLoad`) owns global managers:
- `SceneFlowManager`
- `GameSessionManager`
- `CoinManager`
- `AppState` (small shared runtime data)

Scene objects stay scene-local and are discovered by scene installers/controllers.

---

## Proposed Folder Layout

```text
Assets/_Game/Scripts
  App/
    App.cs
    AppBootstrap.cs
    AppState.cs
    SceneFlowManager.cs
  Core/
    Session/
      GameSessionManager.cs
      GameSessionState.cs
    Economy/
      CoinManager.cs
      CoinRewardConfig.cs
  Features/
    Gameplay/
      GameManager.cs
      HiddenItems/
      Level/
    UI/
      UIManager.cs
      Animation/
      Screens/
        MainScreenController.cs
        PausePopupController.cs
        LevelCompletePopupController.cs
      Transitions/
        UIScreenTransitionController.cs
  Data/
    RuntimeSaveData.cs
```

Notes:
- Existing scripts can stay in place initially; migrate gradually.
- Keep classes single-purpose and event-driven.

---

## Manager Responsibilities (Target)

### `App`
- Single entry point for the whole game.
- Creates global managers once.
- Registers scene callbacks.

### `SceneFlowManager`
- Loads scenes by enum/id:
  - `Loader`
  - `Main`
  - `Game`
- Central place for scene transitions and back navigation.

### `GameSessionManager`
- Owns session lifecycle:
  - `Idle`
  - `Running`
  - `Paused`
  - `Completed`
- Public API:
  - `StartSession(levelId)`
  - `PauseSession()`
  - `ResumeSession()`
  - `CompleteSession()`
  - `AbortToMain()`

### `CoinManager`
- Tracks total coins and current-session coin gain.
- Rewards by elapsed running time (not by hidden-item click).
- Example simple rule:
  - `coins += floor(elapsedSeconds / rewardIntervalSeconds) * rewardPerInterval`

### `UI screen/popup controllers`
- Show/hide screens with enter/exit animations.
- Hook to session events:
  - Pause popup opens when paused.
  - Level complete popup opens when session completes.

### Existing `GameManager` (scene-local)
- Keeps orchestration for runtime scene components.
- Not global; called by app/session flow.

---

## Runtime Flow (Target)

1. `Loader` scene starts.
2. `AppBootstrap` ensures one `App` instance exists.
3. `App` initializes managers, then `SceneFlowManager.LoadMain()`.
4. In `Main`, user presses Play.
5. `SceneFlowManager.LoadGame()`.
6. On game scene ready:
   - `GameSessionManager.StartSession(level_01)`.
   - `GameManager.InitializeGame()`.
7. During play:
   - Hidden item mechanics run (already implemented).
   - Camera pan/zoom runs (already implemented).
   - CoinManager increments over elapsed running time.
8. Pause:
   - `GameSessionManager.PauseSession()`.
   - Pause popup shown.
9. Resume or Exit to Main.
10. Level complete:
    - `GameSessionManager.CompleteSession()`.
    - Level-complete popup shown.
    - Option to go back to Main or replay Game.

---

## Conventions

- Keep scene references in `[SerializeField]` fields on scene-local controllers.
- Keep global references inside `App`.
- Prefer events over direct cross-feature calls.
- Prefer one public responsibility per manager.
- Keep update loops minimal and centralized (`CoinManager` and camera/gameplay only where needed).

---

## Existing Assets Snapshot

- AI/placeholder-like sprite assets currently in `Assets/_Game/Assets`:
  - `background.png`
  - `havuc.png`
  - `kırmızıturp.png`
  - `patates.png`
  - `turp.png`
- Item data currently in `Assets/_Game/Scriptables/AllItems.asset` (3 ids: `a`, `b`, `c`).
- Level setup currently in `Assets/_Game/Scriptables/LevelData.asset`.
