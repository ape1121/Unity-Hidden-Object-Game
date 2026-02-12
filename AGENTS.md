# AGENTS.md

## Purpose
Use this file as the working reference for coding agents contributing to this Unity project.

Project: **Search It** hidden-object vertical slice  
Unity: **6000.3.6f1**  
Target slice scope: app architecture, session lifecycle, hidden-item mechanics, camera pan/zoom, time-based coin economy, UI screen/popup transitions, and AI-generated art integration.

## Project Map

### Runtime Scripts
- `Assets/_Game/Scripts/App`
  - Global bootstrap and manager composition (`App`, `SceneFlowManager`, `AppConfig`).
- `Assets/_Game/Scripts/Core/Session`
  - Session state machine (`GameSessionManager`).
- `Assets/_Game/Scripts/Core/Economy`
  - Time-based rewards (`CoinManager`).
- `Assets/_Game/Scripts/Game`
  - Scene gameplay orchestration (`GameManager`), hidden-item pipeline, level runtime map, camera rig, booster logic.
- `Assets/_Game/Scripts/UI`
  - Main and game UI, popups, remaining-item board, UI animation helpers.
- `Assets/_Game/Scripts/Save`
  - PlayerPrefs persistence (`SaveManager`, `SaveData`).

### Data/Content
- `Assets/_Game/Scriptables/AppConfig.asset`
- `Assets/_Game/Scriptables/LevelData.asset`
- `Assets/_Game/Scriptables/AllItems.asset`
- `Assets/_Game/Assets` (sprites/background/UI images)
- `Assets/_Game/Prefabs` (hidden item, remaining item, popup prefabs)

### Scenes
- `Assets/_Game/Scenes/Loader.unity`
- `Assets/_Game/Scenes/Main.unity`
- `Assets/_Game/Scenes/Game.unity`

Build order is Loader -> Main -> Game.

## Runtime Architecture Contracts

1. **App is the global composition root**
   - `App` is `DontDestroyOnLoad`.
   - Global managers are accessed through `App.Scenes`, `App.Sessions`, `App.Coins`, `App.Saves`, `App.Popups`.

2. **Scene-local orchestration stays in GameManager**
   - `GameManager` binds UI actions (pause/home/booster), session transitions, and gameplay component wiring.
   - Avoid moving scene-specific logic into `App`.

3. **Session state drives pause/completion behavior**
   - Source of truth: `GameSessionManager.State`.
   - In paused/completed state, gameplay input and camera controls are disabled by `GameManager`.

4. **Economy is elapsed-time based**
   - `CoinManager.Tick()` is called from `App.Update()` when session is active.
   - Coin rewards are configured in `AppConfig`.

5. **UI and popups are event/state driven**
   - In-game HUD: `GameUI`.
   - Global popup lifecycle: `PopupManager`, `PausePopupView`, `WinPopupView`.
   - Keep transition logic in UI layer, gameplay decisions in manager layer.

6. **Data is ScriptableObject-driven**
   - Level/item content should be authored through `LevelData` and `AllItems`.
   - Prefer data changes over hardcoded runtime constants.

## Hidden Item Flow (Current)

1. `LevelRuntimeMap` applies `LevelData`.
2. `HiddenItemSpawner` spawns runtime hidden item objects.
3. `HiddenItemInput` captures click/touch selection.
4. `HiddenItemCollector` validates and collects.
5. `GameUI` animates collection and updates `RemainingItems`.
6. `RemainingItems` raises all-collected event.
7. `GameManager` completes session and opens win flow.

## Agent Guardrails

- Keep responsibilities separated by folder/domain (`App`, `Core`, `Game`, `UI`, `Save`).
- Preserve event subscription hygiene (`OnEnable`/`OnDisable`, constructor/shutdown pairs).
- Do not couple UI components directly to low-level gameplay internals when `GameManager` already mediates.
- Prefer extending existing managers/components over adding duplicate global singletons.
- Keep scene name/config changes synchronized with `AppConfig` and Build Settings.
- Avoid introducing frame-by-frame allocations in hot paths (`Update`, `LateUpdate`, high-frequency callbacks).

## Change Checklist (Before Hand-off)

- Project compiles without script errors.
- Boot flow works: Loader -> Main -> Game.
- Session flow works: start, pause, resume, complete, return to main.
- Hidden-item collection still updates remaining board correctly.
- Camera pan/zoom still respects UI input blocking and bounds.
- Coin count increments over elapsed time and persists across relaunch.
- Popup behavior remains consistent (pause/win open/close).

## Common Extension Points

- Add item type:
  - Add sprite in `Assets/_Game/Assets`.
  - Register in `AllItems.asset`.
  - Add placements in `LevelData.asset`.
- Tune economy:
  - Edit coin interval/reward in `AppConfig.asset`.
- Add popup:
  - Create popup prefab and register in `AppConfig.popupPrefabs`.
- Add level:
  - New `LevelData` asset + authoring markers via `LevelAuthoringRoot`.
