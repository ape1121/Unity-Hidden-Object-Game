# Search It Vertical Slice - Project Structure

This document summarizes the current codebase structure for the Search It vertical slice submission.

## 1) Requirement Coverage Map

| Case requirement | Implementation in project |
| --- | --- |
| General application structure and architecture | `Assets/_Game/Scripts/App`, `Assets/_Game/Scripts/Core`, `Assets/_Game/Scripts/Game`, `Assets/_Game/Scripts/UI`, `Assets/_Game/Scripts/Save` |
| Game session management (start, pause, end) | `GameSessionManager`, `GameManager`, popup flow in `PopupManager` + popup views |
| Basic in-game mechanics (item finding, camera zoom/pan) | `HiddenItemSpawner`, `HiddenItemInput`, `HiddenItemCollector`, `CameraRigController`, `RemainingItems` |
| Coin system based on elapsed time | `CoinManager` ticked from `App.Update`; persistence via `SaveManager` |
| UI screen management (menus, transitions) | `MainUI`, `GameUI`, `CanvasGroupUserInterface`, `PopupManager`, DOTween transitions |
| AI-generated art asset integration | Sprite assets under `Assets/_Game/Assets` wired through `AllItems.asset`, `LevelData.asset`, and scene/prefab UI |

## 2) Project Layout (High-Level)

```text
SearchIt/
  Assets/
    _Game/
      Assets/                # Art sprites/backgrounds used by gameplay and UI
      Prefabs/               # Hidden items, remaining item slot, popup prefabs
      Scenes/                # Loader, Main, Game
      Scriptables/           # AppConfig, LevelData, AllItems
      Scripts/
        App/                 # Application bootstrap + scene flow
        Core/
          Session/           # Session lifecycle state machine
          Economy/           # Coin progression
        Game/
          Level/             # Runtime level map + camera + authoring support
          HiddenItems/       # Spawn, input, collect, item data
          Boosters/          # Booster hint/focus system
        UI/
          Main/              # Main menu UI
          Game/              # In-game HUD + collection animation flow
          Popups/            # Pause/win popup system
          RemainingItems/    # Remaining items board
        Save/                # PlayerPrefs persistence
        Editor/              # Level authoring editor tooling
```

## 3) Scene Architecture

### Loader scene (`Assets/_Game/Scenes/Loader.unity`)
- Hosts `App` singleton bootstrap.
- `App.Start()` initializes managers and loads Main scene.

### Main scene (`Assets/_Game/Scenes/Main.unity`)
- Main menu UI via `MainUI`.
- Play button triggers `App.Scenes.LoadGame()`.
- Gold/coin display subscribes through base `SceneUserInterface`.

### Game scene (`Assets/_Game/Scenes/Game.unity`)
Primary runtime composition:
- `GameManager`
- `LevelRuntimeMap`
- `HiddenItemSpawner`
- `HiddenItemInput`
- `HiddenItemCollector`
- `CameraRigController`
- `GameUI` + `UICollectionAnimator`
- `RemainingItems`
- `BoosterManager`
- `LevelAuthoringRoot` (authoring helper present in scene)

## 4) Runtime Flow

1. `App` initializes core managers: scene flow, session, economy, save, popup.
2. Main scene opens; user taps Play.
3. Game scene loads; `GameManager.InitializeGame()` wires UI/gameplay systems.
4. `LevelRuntimeMap` applies `LevelData` (background, camera bounds, item placements).
5. `HiddenItemSpawner` instantiates runtime hidden item objects from `AllItems` + level placements.
6. `GameSessionManager.StartSession()` transitions to `Running`.
7. During play:
   - Item clicks are read by `HiddenItemInput`.
   - `HiddenItemCollector` marks items collected.
   - `GameUI` triggers collect-to-board animation and commits inventory updates.
   - `App.Update()` ticks session elapsed time and time-based coin rewards when session is not paused/completed.
8. When all remaining entries are consumed, `RemainingItems` raises completion; `GameManager` completes session.
9. `GameUI` runs exit transition; Win popup opens.

## 5) Domain Breakdown

### App Layer (`Assets/_Game/Scripts/App`)
- `App.cs`
  - Global composition root (`DontDestroyOnLoad`).
  - Owns static manager instances: `Scenes`, `Sessions`, `Coins`, `Saves`, `Popups`.
  - Central tick loop for session timer and coin system.
- `SceneFlowManager.cs`
  - Type-safe scene loading helpers (`Loader/Main/Game`) with scene loaded event handling.
- `AppConfig.cs`
  - Scriptable configuration for scenes, default level id, coin interval/reward, popup prefabs.
- `AppState.cs`
  - Snapshot container for synchronized app/session/economy state.

### Core Layer (`Assets/_Game/Scripts/Core`)
- Session (`Session/`)
  - `GameSessionManager` state machine: `Idle -> Running -> Paused -> Completed`.
  - Emits `OnSessionStarted`, `OnSessionEnded`, `OnStateChanged`.
  - Tracks `CurrentLevelId` and `ElapsedSeconds`.
- Economy (`Economy/`)
  - `CoinManager` rewards coins by elapsed time interval.
  - Session coins reset at session start; total coins persist across sessions.

### Game Layer (`Assets/_Game/Scripts/Game`)
- `GameManager.cs`
  - Scene-level orchestrator for gameplay initialization, pause/resume, completion, return-to-main.
  - Binds session state to input/camera enablement and popup transitions.
- Hidden item subsystem (`HiddenItems/`)
  - `HiddenItemSpawner`: data-driven instantiation from placements.
  - `HiddenItemInput`: input-system click/touch ray checks with UI guard.
  - `HiddenItemCollector`: collection rules + events.
  - `HiddenItem`: runtime item state and sprite binding.
- Level subsystem (`Level/`)
  - `LevelRuntimeMap`: applies level background, spawns items, sets camera bounds.
  - `CameraRigController`: pinch/scroll zoom + drag pan + clamped world bounds.
  - `LevelData`: ScriptableObject for world composition and placements.
- Booster subsystem (`Boosters/`)
  - `BoosterManager`: finds next target item, optionally camera-focuses it, and draws animated spotlight mask/ring overlay.

### UI Layer (`Assets/_Game/Scripts/UI`)
- Base UI abstraction
  - `UserInterface` + `CanvasGroupUserInterface`: shared enter/exit transitions.
  - `SceneUserInterface`: common pause and gold display behavior.
- Main UI (`Main/MainUI.cs`)
  - Menu entry behavior and play flow.
- Game UI (`Game/GameUI.cs`)
  - HUD lifecycle, panel transitions, action buttons (pause/home/booster).
  - Collected-item queue processing and synchronization with `RemainingItems`.
- Collection animation (`Game/UICollectionAnimator.cs`)
  - DOTween-based world-to-UI fly animation with bezier path and size/rotation blending.
- Popups (`Popups/`)
  - `PopupManager`: runtime popup canvas + popup registry.
  - `PausePopupView`: resume flow.
  - `WinPopupView`: return to main flow.
- Remaining item board (`RemainingItems/`)
  - Aggregates spawn results, tracks counts by item id, raises all-items-collected signal.

### Save Layer (`Assets/_Game/Scripts/Save`)
- `SaveManager`
  - Persists total coins and current level id to PlayerPrefs (`searchit_save_v1`).
  - Saves on coin change and on session start.
  - Applies saved total coin value at startup.
- `SaveData`
  - Serializable DTO (`totalGold`, `currentLevelId`).

### Editor Layer (`Assets/_Game/Scripts/Editor` + level authoring scripts)
- `PlayModeStartFromLoader`
  - Forces Play Mode start scene to Loader for consistent bootstrap behavior.
- `LevelAuthoringRootEditor`
  - Inspector tooling to move data between scene markers/background and `LevelData`.
- `LevelAuthoringRoot` + `LevelItemMarker`
  - Marker-based level authoring workflow for placing hidden items in scene and saving to `LevelData`.

## 6) Content/Data Assets

### Scriptables (`Assets/_Game/Scriptables`)
- `AppConfig.asset`
  - Scenes: Loader/Main/Game
  - Default level: `level_01`
  - Coin reward config currently set to `0.5s` interval and `+1` per interval
  - Popup canvas and popup prefab bindings
- `LevelData.asset`
  - Level id, background sprite transform state, camera bounds, and item placements
- `AllItems.asset`
  - Item catalog with ids and icons

### Prefabs (`Assets/_Game/Prefabs`)
- `HiddenItem.prefab`
- `RemainingItem.prefab`
- `PausePopup.prefab`
- `WinPopup.prefab`
- `PopupCanvas.prefab`
- `ItemMarker.prefab`

### Art assets (`Assets/_Game/Assets`)
Integrated into gameplay/UI via scriptables and scene objects:
- Backgrounds: `background.png`, `SearchIt_MainMenu.jpg`
- Item icons/sprites: `patates.png`, `havuc.png`, `turp.png`, others in same folder
- UI visuals: button/gold assets (home/pause/booster/gold UI)

## 7) Architectural Characteristics

- Data-driven content: level and item definitions are ScriptableObject-based.
- Manager-oriented composition: one bootstrap (`App`) with domain managers for scene, session, economy, save, popup.
- Event-driven coordination: item collection, spawn, state transitions, and coin updates are propagated by C# events.
- Clear scene responsibilities: Loader (bootstrap), Main (menu), Game (play loop).
- Vertical-slice friendly: architecture already supports adding more levels/items/popups with limited code changes.

## 8) AI-Assisted Development Fit

- Domain folders and focused classes reduce prompt ambiguity for coding agents.
- ScriptableObject-driven content allows adding/changing level content without broad code rewrites.
- Event boundaries (`OnStateChanged`, `OnItemsSpawned`, `OnHiddenItemCollected`, `OnCoinsChanged`) make iterative AI-generated changes lower risk and easier to validate.
