# Changelog

All notable changes to DISM Lab are recorded here. Releases follow Semantic Versioning and use the categories shown below.

## Unreleased

### New Features

- None.

### Bug Fixes

- None.

### Other Changes

- None.

## 1.0.0 - Unreleased

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
