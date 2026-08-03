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

## Conventions

- **Language:** VB.NET exclusively — do not introduce C# files (the existing `DismProgressWindow.xaml.cs` is legacy/duplicate of the `.xaml.vb` version).
- **UI pattern:** WPF code-behind (no MVVM framework). UI state is managed directly via element visibility toggling and event handlers.
- **Dark theme:** All UI uses a custom dark theme with semi-transparent black backgrounds (`#BF000000`) and light foreground text. Match existing color constants when adding UI.
- **External tools:** Operations shell out to `dism.exe`, `diskpart.exe`, and `winget.exe` — do not replace with managed libraries without explicit instruction.
- **Progress parsing:** Use compiled `Regex` instances (defined as `Private Shared ReadOnly`) for any new output parsing patterns.
- **Admin requirement:** The app requires elevation. Check `WindowsIdentity`/`WindowsPrincipal` for admin status.
- **Async pattern:** Use `Async Sub` for event handlers and `Async Function ... As Task(Of T)` for awaitable operations. Always dispatch UI updates via `Dispatcher.InvokeAsync` or `Dispatcher.BeginInvoke`.
