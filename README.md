# DISM Lab

## Overview
DISM Lab is a WPF front end for the Deployment Image Servicing and Management (DISM) tooling. It targets `net8.0-windows` and focuses on day-to-day image servicing tasks: inspecting indexes, mounting WIMs with live disk-usage tracking, adding or exporting drivers, applying MSU/CAB updates, exporting images, and preparing WinPE boot media or USB keys. The UI is optimized for touch-friendly kiosk deployments with a dark theme, live Bing wallpaper, and clear progress readouts.

## Feature Highlights
- **WIM exploration** – enumerate indexes, descriptions, sizes, and architectures directly from `dism.exe /Get-WimInfo` output.
- **Mount management** – one-click mount/unmount with automatic mount folder hygiene, debounced size monitoring, and activity light feedback.
- **Driver + update servicing** – pick `.inf`, `.cab`, or `.msu` files via multi-select UX, log every package result, and optionally remount existing sessions.
- **Image export** – export selected indexes to new WIMs with adaptive progress tracking and stall detection.
- **Driver export** – extract drivers from mounted images into a chosen folder while keeping the primary image read-only.
- **WinPE workflow** – guided wizard to select architecture/optional components, run `copype`, mount `boot.wim`, add packages, and persist a manifest for later runs.
- **USB creation** – enumerate removable disks, repartition with DiskPart, copy WinPE media, and expose boot/data volumes (default `P:` + `I:`).
- **Workspace hygiene** – automatic log storage under `%LOCALAPPDATA%\DISM_Lab`, manifest watching with `FileSystemWatcher`, and defensive cleanup on shutdown.

## Requirements
- Windows 10/11 with DISM available on the path.
- .NET 8.0 SDK (`dotnet --version` ≥ 8.0).
- Administrator rights (the app relaunches elevated if needed).
- Windows ADK + WinPE add-ons when running the WinPE wizard (provides `copype.cmd` and optional component CABs).
- Network access (optional) for daily Bing wallpaper downloads.

## Build & Run
```bash
# Restore, build, and launch
 dotnet restore
 dotnet build
 dotnet run --project "DISM Lab.vbproj"
```
Running outside Visual Studio still requires elevation; use `Run as administrator` or accept the UAC prompt triggered on startup.

## Typical Workflows
- **Inspect + mount a WIM**: `Select WIM → choose index → Mount Image`. The footer progress module shows bytes copied vs. total, while the DISM activity light reflects process state. Use `Open Mount Folder` to browse the mount root.
- **Add drivers or updates**: select the image index, click `Add Drivers` or `Apply Updates`, pick a folder, choose packages from the multi-select list (`Select All`, `Start`, `Cancel` controls), then let DISM handle `/Add-Driver` or `/Add-Package`. Logs are saved to `Mount\DISM_Lab\Logs\labpe.log` and rendered in `Log_read_out`.
- **Export an image**: select an index, click `Export Image`, provide a destination `.wim`, and monitor the adaptive progress display. Exports reuse existing mount size metadata for better estimates.
- **Create WinPE media**: `Create WinPE → pick architecture → optionally add components → Finish`. The app locates `copype.cmd`, sets up the workspace, adds packages, stores `labpe-manifest.json`, and enables `Create USB` once media exists.
- **Provision a USB stick**: click `Create USB`, pick a removable disk from `DiskSelectionWindow`, confirm destructive actions, and wait while DiskPart, file copy, and readiness checks complete. The boot volume is labeled per architecture (e.g., `WinPE_AMD64`).

## Project Layout
- `MainWindow.xaml` / `MainWindow.xaml.vb` – primary UI, DISM orchestration, WinPE wizard, progress logic, disk prep, and log handling.
- `DiskSelectionWindow.xaml(.vb)` – modal picker for removable disks with refresh-on-demand support.
- `DiskInfo.vb` – POCO describing enumerated disks.
- `DismProgressWindow.xaml(.vb)` – auxiliary window for streaming `dism.exe` output and progress parsing (e.g., `/Get-WimInfo`).
- `DISM Lab.vbproj` – .NET 8 WPF project configuration, references, and packaging metadata.

## Troubleshooting
- **copype not found** – install the Windows ADK + WinPE add-ons; verify `copype.cmd` is under `C:\Program Files (x86)\Windows Kits`.
- **Mount folder not empty** – the app offers `/Unmount-WIM /Discard` followed by `/Cleanup-Mountpoints`; accept the prompt or manually clean `C:\Mount`.
- **USB letters in use** – release `P:` or `I:` before launching USB creation, or adjust constants in `MainWindow.xaml.vb` if your lab standard differs.
- **Log viewer empty** – confirm the operation wrote `labpe.log` beneath the mount path; some read-only actions (e.g., driver export) skip log generation on purpose.
