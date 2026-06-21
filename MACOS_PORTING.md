# macOS Porting

DJMAX Random Selector V was originally a Windows-only **WPF** application. WPF
does not run on macOS, so porting is done in two stages:

| Stage | Scope | Status |
| ----- | ----- | ------ |
| **1. Cross-platform core** | Make `Dmrsv.RandomSelector` (the selection + input logic) run on macOS as well as Windows. | ✅ Done |
| **2. Cross-platform UI** | Replace the WPF front-end (`DjmaxRandomSelectorV`) with an [Avalonia](https://avaloniaui.net/) app (XAML/MVVM, the closest WPF analogue). | 🚧 In progress — app shell scaffolded in `DjmaxRandomSelectorV.Desktop` |

## Stage 1 — what changed

The only OS-specific code in the core library was the keyboard injection in
`Locator.cs` (Win32 `SendInput` P/Invoke). That is now hidden behind a small
abstraction so the same selection logic drives the game on any OS:

```
Dmrsv.RandomSelector/Input/
├── Key.cs                 # platform-neutral key identifiers
├── IInputSender.cs        # KeyDown(Key) / KeyUp(Key)
├── InputSender.cs         # factory: picks a backend for the current OS
├── WindowsInputSender.cs  # Win32 SendInput + DirectInput scan codes (original behaviour)
├── MacInputSender.cs      # Quartz CGEvent + ANSI virtual key codes
└── NullInputSender.cs     # no-op fallback (Linux / CI)
```

* The project now targets **`net6.0`** (was `net6.0-windows`), so it builds and
  runs on macOS/Linux. The existing `net6.0-windows` WPF app still references it
  unchanged.
* `Locator` keeps all of the navigation logic and timing (`InputInterval`); it
  only delegates the raw key up/down to the selected `IInputSender`.

## macOS specifics

* **Target:** DJMAX RESPECT V has no native macOS build. The macOS backend posts
  system-wide `CGEvent` key events, so it drives the game running through a
  Windows compatibility layer (**CrossOver / Whisky / Parallels**) as long as
  that window has focus — mirroring the original "press the hotkey while the game
  is focused" workflow.
* **Accessibility permission is required.** macOS silently drops synthesized key
  events unless the host application is granted permission under
  *System Settings → Privacy & Security → Accessibility*. The Stage 2 UI should
  detect this (`AXIsProcessTrustedWithOptions`) and prompt the user.
* **Key codes:** `MacInputSender` uses Carbon HIToolbox (`Events.h`) virtual key
  codes (`kVK_ANSI_*`). Held modifiers (Shift / Ctrl) are tracked and applied as
  `CGEventFlags` so the in-game *Ctrl + Numpad* button-mode shortcut works.

## Remaining Windows-only pieces (handled in Stage 2)

These still live in the WPF project and need macOS equivalents when the UI is ported:

* `WindowTitleHelper.cs` — `GetForegroundWindow` / `GetWindowText` to confirm the
  game is focused. macOS equivalent: `NSWorkspace.frontmostApplication` /
  Accessibility window title.
* `ExecutionHelper.cs` — global `RegisterHotKey` (F7) via an HWND message hook.
  macOS equivalent: Carbon `RegisterEventHotKey` or a `CGEventTap`.

## Stage 2 — Avalonia desktop app (`DjmaxRandomSelectorV.Desktop`)

An Avalonia 11 app that references the cross-platform core. It is the starting
point for the full UI port and the thing the macOS installer ships. The WPF views
(`BasicFilterView`, `SettingView`, …) are ported into it incrementally.

Working today:

* **Track loading** — `Services/TrackLoader.cs` reads `AllTrackList.json` into the
  cross-platform `Track` model (no Caliburn/WPF).
* **Random selection** — `Services/SelectorService.cs` runs the real pipeline
  (`BasicFilter` → `PatternPicker` → `SelectorWithHistory` → `Locator`).
* **Global F7 hotkey** — `Platform/MacGlobalHotkey.cs` uses Carbon
  `RegisterEventHotKey` (the macOS counterpart to the Windows `RegisterHotKey` +
  HWND hook). Pressing **F7** (or the in-app button) picks a random song, shows
  it, and drives the focused game window via `MacInputSender`.

> Sending keystrokes to the game needs **Accessibility permission** (System
> Settings → Privacy & Security → Accessibility). The app detects this on launch
> (`AXIsProcessTrusted`), prompts for it, and shows a "Grant Accessibility
> permission" button; without it, selection still works and the picked song is
> shown, but no keys reach the game.
>
> **F7 vs. the button:** keys go to whatever window is focused. Press **F7 while
> the game is focused** to drive it. The in-app button only *previews* a pick —
> when you click it, this app is focused, so the keys would go here, not the game.

Still to port: the filter/settings/favorite/history/VArchive views and DLC
ownership filtering (preview currently treats all tracks as playable).

```bash
# Run the Avalonia app locally (any OS with the .NET 8 SDK)
dotnet run --project DjmaxRandomSelectorV.Desktop/DjmaxRandomSelectorV.Desktop.csproj
```

## Packaging: `.dmg` via GitHub Actions

`.github/workflows/macos-dmg.yml` builds the macOS installer on a `macos-14`
runner:

1. `dotnet publish` the Desktop app self-contained for `osx-arm64` **and**
   `osx-x64` (matrix).
2. `build/macos/make-dmg.sh` wraps the output in a `DJMAX Random Selector V.app`
   bundle (Info.plist + `.icns` icon generated from `Images/icon2.png`),
   ad-hoc code-signs it (so it launches on Apple Silicon), and produces a
   compressed `.dmg` with an `/Applications` drop link.
3. The `.dmg` is uploaded as a build artifact; on a `v*` tag it is also attached
   to the GitHub Release.

The app is **not notarized**, so first launch needs *right-click → Open* (or
*System Settings → Privacy & Security → Open Anyway*). It also needs
Accessibility permission to send keystrokes to the game (see above).

To cut a release: push a tag like `v1.0.0`.

## Building

```bash
# Core library (cross-platform)
dotnet build Dmrsv.RandomSelector/Dmrsv.RandomSelector.csproj

# macOS / cross-platform app (Avalonia)
dotnet build DjmaxRandomSelectorV.Desktop/DjmaxRandomSelectorV.Desktop.csproj

# Windows app (Windows only — WPF)
dotnet build DjmaxRandomSelectorV/DjmaxRandomSelectorV.csproj
```
