2026-02-17
Features
- Added shared runtime hook hub `GameHooks` (`RBN.Modlib.Game`) for cross-mod event subscription without per-mod patch duplication.
- Added lifecycle bootstrap in `RbnModlibPlugin` (`Awake`/`OnDestroy`) to auto-initialize and shutdown the hook runtime.
- Expanded `GameHooks` with additional event relays and polled state hooks:
  - Shop/research unlock relays (`ShopItemUnlocked`, `ResearchItemResearched`)
  - Inventory/tool state (`InventoryOpenStateChanged`, `ActiveToolChanged`)
  - Contract/ore-limit state (`ActiveContractChanged`, `OreLimitStateChanged`)
  - Quest progression (`QuestActivated`, `QuestPaused`, `QuestCompleted`, active/available quest count)
  - Economy progression (`ShopPurchaseTotalChanged`)
  - Debug cleanup relay (`WorldOreCleared`)
- Added support for both CLR `event` signals and delegate-field signals (for game APIs that expose `Action` fields).
- Added robust polling fallbacks for systems that do not expose direct events.
- Added safe subscriber invocation and best-effort relay rebinding/cleanup across scene changes.

2026-02-15
Bug Fixes
- Added shared popup-visibility gating in `UiControlUtils.ShouldHidePopupUi()` and hid Modlib popup UI while `MainMenu` is active.
- Applied popup gating to `UiToastQueue.Draw(...)` and `UiHoverPromptRenderer.Draw(...)` so hover cards/toasts are suppressed in main menu.
- Expanded popup-visibility gating to also suppress popup UI when `UIManager` reports active in-game menus (pause, inventory, quest tree, edit-text, and related menu states).

2026-02-11
Features
- Added shared JSON structure helpers to `JsonSettingsUtils`:
  - `ReadNestedRaw(json, key, open, close)`
  - `SplitTopLevelObjects(rawArray)`
- Unified nested JSON array/object extraction path for both Color Control and Configurable Miners persistence loading.
- Added shared config-panel layout helpers in `UiConfigMenuShell`:
  - `GetPaddedContentRect(...)`
  - `DrawPaddedContent(...)`
  - `DrawScrollablePaddedContent(...)`

Bug Fixes
- Prevented cross-mod parser divergence by centralizing top-level object splitting and nested payload extraction into Modlib.
- Reduced repeated panel layout/scroll boilerplate across mods by centralizing padded/scrollable config content scaffolding.

2026-02-10
Features
- Added shared UI style preset system via `UiStylePresets` and `UiStyleSet`.
- Added normalized style preset ids (`style_1`) with settings-reference helpers for mod config files.
- Added reusable style construction for window, label, text field, rich button, and optional slider styles.
- Added `style_2` preset (warm amber/brown palette with orange slider accent) for both mods.
- Added shared `JsonSettingsUtils` for lightweight settings read/write helpers (string/float/int/bool/arrays + JSON escaping).
- Added shared `UiToastQueue` + shared `ToastType` enum for consistent toast queueing and rendering across mods.
- Added shared `UiKeybindCaptureState` for reusable keybind capture flows.
- Added shared `UiDrawUtils` for solid-rect and RGB-wave border rendering.
- Added shared `UiConfigRowUtils.DrawCenteredHorizontalSlider` for consistent centered slider rows.
- Added shared `UiHoverPromptRenderer` (center/top/bottom anchors, hotbar-aware placement, key/title/subtitle layout).
- Added shared `UiConfigMenuShell` for single-panel and split menu/detail config layouts.
- Added shared `UiConfigRowWidgets` for common config rows (float/int slider+text and keybind capture rows).
- Upgraded shared button style preset rendering with explicit normal/hover/active visuals for consistent button feedback.
- Standardized settings helper usage by making both mods call `JsonSettingsUtils` directly instead of local pass-through helper methods.

2026-02-09
Features
- Created shared library plugin foundation for MineMogul mods.
- Added `GameReflection` helpers for game type lookup and singleton instance resolution.
- Added `SavePathUtils` helpers for active save name resolution and safe folder-name sanitization.
- Added `UiControlUtils` helpers for cursor unlock, player control toggling, and UI manager suppression.
- Added global usings and base project wiring for net472 + Unity/BepInEx references.
- Renamed library from `ModLib` to `RBN.Modlib` (folder, project, assembly name, and root namespace).
