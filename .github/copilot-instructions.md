# DISM Lab - Copilot Instructions

## Build & Run

```shell
# Build from command line
dotnet build "DISM Lab.sln"

# Build release
dotnet build "DISM Lab.sln" -c Release

# Run (requires admin privileges for DISM operations)
dotnet run --project "DISM Lab.vbproj"
```

No test suite exists. Verify changes by building successfully and testing the UI manually.

## Architecture

This is a single-project **WPF desktop app** written in **VB.NET** targeting **.NET 8.0-windows**. It provides a GUI for Windows DISM operations (image mounting/unmounting, driver injection, WinPE creation, bootable USB formatting).

### Key files

- **MainWindow.xaml / .xaml.vb** — The entire application logic lives here (~5000+ lines). All DISM operations, WinPE wizard state machine, USB disk formatting, progress monitoring, and UI state management are in this single code-behind.
- **DismProgressWindow** — A reusable modal window that runs `dism.exe` as a child process and parses stdout for progress percentages.
- **AdkInstallationWindow** — Handles Windows ADK + PE Add-on installation via `winget`.
- **DiskSelectionWindow** — USB disk picker dialog filtering to removable drives only.
- **DiskInfo.vb** — Simple data class for disk metadata.

### Process execution pattern

DISM and diskpart operations are run as **external processes** (`Process.Start` with redirected stdout/stderr), not via managed DISM APIs. Output is parsed with regex to extract progress percentages and status.

### Concurrency model

- A `SemaphoreSlim(1,1)` (`_dismOpLock`) serializes all DISM operations to prevent overlapping process executions.
- `_activeProcessCount` tracks running background processes to gate UI button states.
- Mount progress uses a `FileSystemWatcher` + polling timer to monitor directory size growth rather than DISM callbacks.

### WinPE wizard state machine

The WinPE creation flow uses `WinPeWizardState` enum (`Idle` → `SelectingArchitecture` → `SelectingOptionalComponents`) to repurpose the main ListBox for wizard step selection.

### Storage paths

- App data: `%LOCALAPPDATA%\DISM_Lab\`
- WinPE working directory: `C:\WinPE\` (resolved to avoid paths with spaces)
- Default mount point: `C:\Mount`
- Manifest: `labpe-manifest.json` in WinPE root

## UI Design

- Avoid a traditional top menu.
- Place the settings button at the bottom-right of the main window and ensure it does not overlap the progress display or activity indicator.
- Use a dark-mode settings control at the bottom right that opens a rounded overlay/flyout for actions such as update checks and performance settings.
- Apply the Watermark Lab-inspired yellow (`#FFC107`) only to button colors and selection/hover highlight colors.
  - Keep only the `Browse...` and `Create WinPE` buttons permanently yellow.
  - Every other button must retain its original dark appearance at rest and switch to yellow only while the mouse cursor is hovering over it.
- Keep toggles dark when off, use yellow when checked/on, and use a yellow border emphasis on hover without changing toggle layout or template behavior.
- Never alter existing layouts, dimensions, spacing, control templates, behavior, backgrounds, panels, text, progress indicators, status colors, or toggles as part of this color request. Preserve the DISM activity light's original green colors and glow exactly as-is. Preserve semantic warning and error colors.

## Settings Configuration

- Include WinPE deployment script inclusion in the Settings flyout, defaulting to off, rather than appearing in the WinPE wizard.
- Expose configurable default Mount and WinPE root folder locations in the Settings flyout, defaulting to `C:\Mount` and `C:\WinPE`.
- Keep deployment scripts as editable source files in the repository, but embed them into the application so published deployments do not require a Scripts folder alongside the executable.

## Workflow Management

- Once a mount action begins or an existing mount is detected, lock workspace path settings. Users must not be allowed to change locations until the mount/task flow is complete. Changing a path never migrates existing files.

## Conventions

- **Language:** VB.NET exclusively — do not introduce C# files (the existing `DismProgressWindow.xaml.cs` is legacy/duplicate of the `.xaml.vb` version).
- **UI pattern:** WPF code-behind (no MVVM framework). UI state is managed directly via element visibility toggling and event handlers.
- **Dark theme:** Preserve the existing dark theme and layout. The approved yellow scope is buttons and selection highlights only.
- **External tools:** Operations shell out to `dism.exe`, `diskpart.exe`, and `winget.exe` — do not replace with managed libraries without explicit instruction.
- **Progress parsing:** Use compiled `Regex` instances (defined as `Private Shared ReadOnly`) for any new output parsing patterns.
- **Admin requirement:** The app requires elevation. Check `WindowsIdentity`/`WindowsPrincipal` for admin status.
- **Async pattern:** Use `Async Sub` for event handlers and `Async Function ... As Task(Of T)` for awaitable operations. Always dispatch UI updates via `Dispatcher.InvokeAsync` or `Dispatcher.BeginInvoke`.

## Release Packaging

For the DISM Lab v1.0.0 release, prefer a portable ZIP package with the minimum practical number of files and reduced download size, provided functionality and portability are preserved. The user explicitly approved replacing the existing release assets even though the ZIP has one recorded download. For future releases, use the established publishing policy: a compressed, self-contained, single-file Windows x64 executable inside a ZIP containing only `DISM Lab.exe`, with a SHA-256 sidecar.

For every DISM Lab release, keep the release unavailable while validating, download the actual GitHub-hosted ZIP and checksum after upload, verify the checksum and prove the ZIP has exactly one entry, then publish it as stable. Never declare a release successful based only on local output or GitHub metadata. Verify the actual GitHub-hosted ZIP contents before declaring a release successful. Preserve this approach for future releases unless the user explicitly changes the policy.

### Upgrade Path

Use v1.0.1 as a staged stable release to test DISM Lab's in-app upgrade path from the user's downloaded v1.0.0 copy.

## General Guidelines

- Keep progress updates concise and avoid repetitive narration; perform the requested operation directly and report the result clearly.
