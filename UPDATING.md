# DISM Lab Update System

DISM Lab checks the public `brucemurphy/DISM-Lab` GitHub repository for stable releases. It checks once during startup and exposes **Settings > Check for updates** for manual checks.

## Update flow

1. Request the latest stable release from the GitHub Releases API.
2. Parse its `vMAJOR.MINOR.PATCH` tag and compare it with the running assembly version.
3. Locate the exact Windows x64 ZIP and `.sha256` sidecar for that version.
4. Show the new version and categorized GitHub release notes.
5. Continue only after the user chooses to install.
6. Refuse installation while a DISM operation or tracked child process is active.
7. Download both assets over HTTPS into a unique temporary staging directory.
8. Verify the ZIP against the published SHA-256 value.
9. Safely extract the ZIP and reject archive paths that escape the staging directory.
10. Confirm that `DISM Lab.exe` exists in the staged payload.
11. Start a temporary PowerShell updater and close DISM Lab.
12. Back up each existing file that will be replaced, copy the new payload, and restart DISM Lab.

Update checks do not silently install releases. Startup network/API failures remain non-blocking; manual check failures and failures after the user accepts an update are shown to the user.

## Release contract

The service currently supports stable self-contained `win-x64` releases only. For a version such as `1.2.3`, GitHub must provide:

- Tag: `v1.2.3`
- Package: `DISM-Lab-v1.2.3-win-x64.zip`
- Checksum: `DISM-Lab-v1.2.3-win-x64.zip.sha256`
- Executable at ZIP root: `DISM Lab.exe`

Changing any of these names requires coordinated changes to the release workflow and `GitHubUpdateService.vb`.

## Security model

- The repository owner and repository name are fixed in the application.
- API and asset URLs must use HTTPS.
- Asset URLs must originate from `github.com`.
- Package installation requires a matching SHA-256 checksum.
- Archive extraction rejects traversal outside the staging directory.
- Files are staged before the running application exits.
- Existing application files are backed up before replacement.

The checksum detects corruption and mismatched assets, but it is hosted with the release and is not a substitute for publisher identity signing. Authenticode signing should be added if a code-signing certificate becomes available.

## Failure and recovery

If replacement fails, the updater removes newly created files, restores backed-up files, writes details to `%LOCALAPPDATA%\DISM_Lab\update-error.txt`, and attempts to restart the prior version. On the next launch, DISM Lab displays that report once and removes it.

The app should be extracted to a writable folder. Protected or policy-controlled locations may prevent in-place replacement even when the process is elevated.

Application state is not stored in the portable program directory. Existing data under `%LOCALAPPDATA%\DISM_Lab` and working files under `C:\WinPE` are not part of the release payload and are preserved.

## Maintenance checklist

When changing the updater or release package:

1. Keep release asset names synchronized between the workflow and update service.
2. Preserve the categorized release-note headings.
3. Keep user data outside the application directory.
4. Build the solution and test a local self-contained publish.
5. Test no-update, update-declined, checksum-failure, and successful-restart paths.
6. Test rollback by making a destination file temporarily non-writable in a disposable installation folder.
7. Test the real GitHub update path with a later patch release before depending on it broadly.

Unauthenticated GitHub API requests are rate limited. A failed startup check must never block the app from opening.
