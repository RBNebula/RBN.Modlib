# RBN.Modlib

Shared utility library/plugin for MineMogul BepInEx mods.

## What It Provides

- `RBN.Modlib.Game`: reflection helpers, runtime hook/event relays, and hotkey gating helpers.
- `RBN.Modlib.Persistence`: save path helpers and lightweight JSON settings helpers.
- `RBN.Modlib.UI`: reusable IMGUI utilities for config rows, style presets, hover prompts, toasts, keybind capture, and option widgets.

## Build

1. Ensure your sibling `Libs` folder contains game/BepInEx references used by `RBN.Modlib.csproj`.
2. Build `RBN.Modlib.csproj`.
3. Output DLL: `bin/Release/net472/RBN.Modlib.dll`.

## Use In Other Mods

- Reference `RBN.Modlib.dll` from your mod project.
- Import namespaces such as `RBN.Modlib.Game`, `RBN.Modlib.Persistence`, and `RBN.Modlib.UI`.
- Subscribe to `GameHooks` events after plugin startup as needed.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).