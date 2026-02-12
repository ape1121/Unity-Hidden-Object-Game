# Search It - TODO

Status legend:
- `[ ]` Not started
- `[~]` In progress / partial
- `[x]` Done

## 0) Baseline Audit (Current Project)

- [x] Build scenes ordered as `Loader -> Main -> Game`
- [x] Hidden-item core loop (spawn -> click -> collect)
- [x] Camera zoom/pan controls
- [x] Remaining-items UI and collection animation
- [~] Scene usage
  - Loader/Main scenes exist but are currently mostly empty (camera-only)
  - Game scene contains full runtime wiring

## 1) App Entry Point and Scene Flow (Highest Priority)

- [x] Create `AppBootstrap` (runtime bootstrap path implemented)
  - Acceptance: app auto-creates persistent `App` before first scene load.
- [x] Create persistent `App.cs` (`DontDestroyOnLoad`)
  - Acceptance: singleton app persists across transitions.
- [x] Create `SceneFlowManager`
  - Acceptance: supports `LoadMain()`, `LoadGame()`, `ReloadGame()`.
- [~] Wire loop `Loader -> Main -> Game -> Main`
  - Implemented via `App` scene handling.
  - Missing: final UI button wiring in Main/Pause/Complete popups.

## 2) Game Session Management

- [x] Add `GameSessionManager` with explicit states
  - States: `Idle`, `Running`, `Paused`, `Completed`.
- [x] Session API
  - `StartSession(levelId)`
  - `PauseSession()`
  - `ResumeSession()`
  - `CompleteSession()`
  - `AbortToMain()`
- [x] Connect gameplay to session lifecycle
  - `GameManager.SetPaused(...)` now disables item input + camera controls.
  - Acceptance: gameplay input disabled while paused/completed.

## 3) Coin System (Elapsed Time Based)

- [x] Add `CoinManager`
  - Tracks total coins and session coins.
- [x] Add reward configuration (interval + reward amount)
  - Example: +1 coin every N seconds while session is running.
- [x] Pause-safe timing
  - Acceptance: no coins awarded while paused.
- [ ] UI binding for coin display
  - Acceptance: coin count updates in Game and persists when returning to Main.

## 4) UI Screen and Popup Management

- [ ] Add UI screen transition controller (enter/exit animation)
  - Main screen transitions
  - In-game screen transitions
- [ ] Add Pause popup
  - Actions: Resume, Exit to Main.
- [ ] Add Level Completed popup
  - Actions: Replay, Back to Main.
- [ ] Integrate with session manager events
  - Acceptance: popup visibility always matches session state.

## 5) Game Completion Rules

- [x] Define completion trigger
  - Implemented: `RemainingItems` emits `OnAllItemsCollected` when list reaches zero.
- [~] Trigger `CompleteSession()` and level-complete popup
  - Session completion is wired (`GameManager` -> `App` -> `GameSessionManager`).
  - Missing: level-complete popup UI presentation.

## 6) Scene Wiring and Installers

- [ ] Add scene-local installers/controllers
  - `MainSceneController`
  - `GameSceneController`
- [ ] Move cross-scene references out of scene objects and into `App` wiring
  - Acceptance: scenes can be reloaded without stale references.

## 7) AI-Generated Art Asset Pass

- [ ] Produce/replace final item icons and backgrounds with AI-generated assets
- [ ] Ensure style consistency (palette, outline/shadow treatment, readability)
- [ ] Export pipeline and attribution notes
  - Acceptance: all gameplay sprites are visually cohesive and production-ready for the slice.

## 8) Quality and Maintainability

- [ ] Add lightweight runtime logging for scene/session transitions
- [ ] Add basic play-mode checks for:
  - Scene loop stability
  - Pause/resume correctness
  - Coin accrual correctness
- [ ] Remove stale/unused assets or scripts
  - Includes investigating `MatchTile` prefab missing script reference.
- [ ] Update docs after each milestone
  - `README.md`, `STRUCTURE.md`, `TODO.md`

## Suggested Execution Order

1. App bootstrap + scene flow
2. Session manager
3. Coin manager
4. Pause and level-complete popups
5. Main/Game transitions polish
6. AI-art pass
7. Cleanup + validation
