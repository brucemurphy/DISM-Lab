# Changelog

All notable changes to DISM Lab are recorded here. Releases follow Semantic Versioning and use the categories shown below.

## Unreleased

### New Features

- None.

### Bug Fixes

- None.

### Other Changes

- None.

## 1.2.0 - 2026-09-25

### New Features

- Add FFU image selection, metadata inspection, mounting, unmounting, and offline servicing.
- Support driver extraction from FFU images through temporary read-only mounts.
- Support adding drivers and Windows update packages to FFU images with commit or discard handling.

### Bug Fixes

- None.

### Other Changes

- Generalize image mount, unmount, startup cleanup, and recovery operations across WIM, ESD, and FFU formats.
- Hide WIM-only image export actions when an FFU image is selected.

## 1.1.1 - 2026-08-04

### New Features

- None.

### Bug Fixes

- Remove `findstr` and `find` dependencies that are not guaranteed in base WinPE.
- Select deployment menu entries using only built-in `cmd.exe` control flow.
- Detect offline Windows installations by filesystem structure while explicitly excluding the running WinPE `X:` drive.
- Correct recovery drive discovery, delayed variable output, and invalid batch conditional syntax.

### Other Changes

- Audit embedded deployment scripts against base WinPE command availability.

## 1.1.0 - 2026-08-04

### New Features

- Add an optional embedded WinPE deployment toolkit for applying WIM and FFU images, capturing WIM images, and configuring recovery partitions.
- Add configurable Mount and WinPE workspace folders with persisted defaults, validation, browsing, and active-workflow locking.
- Add a current-image summary card showing the selected image index, name, edition, architecture, build, size, mount state, and mount path.
- Add WIM and ESD image selection through the redesigned image workflow.

### Bug Fixes

- Keep workspace paths locked while mount content, mounted images, DISM operations, or WinPE creation workflows are active.
- Improve WinPE deployment menu image discovery, input validation, path handling, startup behavior, and reboot handling.
- Prevent stale asynchronous image metadata from replacing details for a newer index selection.
- Standardize disabled button backgrounds, borders, text colors, and cursors across the interface.

### Other Changes

- Embed deployment scripts into the single-file executable while retaining editable source copies in the repository.
- Redesign and space the left-side image and WinPE controls for a clearer task flow.
- Expand Settings with WinPE deployment and workspace configuration controls.

## 1.0.1 - 2026-08-03

### New Features

- None.

### Bug Fixes

- None.

### Other Changes

- Publish a staged patch release to validate the in-app upgrade path from v1.0.0.

## 1.0.0 - 2026-08-03

### New Features

- Browse Windows Image (WIM) indexes and view image metadata.
- Mount and unmount Windows images with progress and activity reporting.
- Export selected images into standalone WIM files.
- Add drivers and Windows update packages to mounted images.
- Export image drivers and capture drivers from the running system.
- Build x64 and ARM64 Windows PE media with selectable optional components.
- Create bootable USB media using the generated Windows PE environment.
- Detect and install Windows ADK and Windows PE prerequisites.
- Check GitHub Releases for updates at startup or from the Help menu.
- Display categorized release notes before downloading an update.
- Verify update packages with SHA-256, replace the portable app in place, and restart automatically with rollback protection.

### Bug Fixes

- None. This is the initial public release baseline.

### Other Changes

- Display the application version in the main window title.
- Publish as a self-contained Windows x64 portable ZIP that does not require a separate .NET installation.
- Add a manually triggered GitHub Actions release workflow with deterministic assets and categorized release notes.
- Document versioning, publishing, update security, and recovery procedures.
