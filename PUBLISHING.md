# Publishing DISM Lab

DISM Lab is distributed as a compressed, self-contained, single-file Windows x64 executable inside a portable ZIP. Users extract and run `DISM Lab.exe`; no installer, supporting application files, or separate .NET Desktop Runtime is required.

## Prerequisites

- Changes are committed and pushed to the `master` branch.
- `DISM Lab.vbproj` contains the intended `VersionPrefix`.
- `CHANGELOG.md` contains the final categorized notes for that version.
- GitHub Actions is enabled for the repository.
- Workflow permissions allow `GITHUB_TOKEN` to create repository contents.
- The intended tag and GitHub Release do not already exist.

## Publish a release

1. Build the solution in Release configuration.
2. Test the portable publish locally using the profile in `Properties/PublishProfiles/FolderProfile.pubxml`.
3. Push the final version and changelog commit.
4. Open the repository's **Actions** page on GitHub.
5. Select **Publish portable release** and choose **Run workflow**.
6. Enter the tag in `vMAJOR.MINOR.PATCH` format.
7. Enter Markdown lists for **New Features**, **Bug Fixes**, and **Other Changes**. Use `None.` for an empty category.
8. Run the workflow and confirm every step succeeds.

The workflow validates that the requested version matches `VersionPrefix`, restores and publishes a compressed single-file executable for `win-x64`, verifies that the publish output contains only `DISM Lab.exe`, creates the ZIP, writes its SHA-256 sidecar, generates categorized release notes, creates the tag, and publishes the GitHub Release.

## Release assets

For version `v1.0.0`, the release must contain exactly these update assets:

- `DISM-Lab-v1.0.0-win-x64.zip`
- `DISM-Lab-v1.0.0-win-x64.zip.sha256`

The ZIP contains exactly one application file: `DISM Lab.exe` at the archive root. The .NET runtime and native dependencies are bundled into that executable and extracted internally by .NET when required.

The checksum file uses this format:

`<64-character SHA-256 hash>  <ZIP file name>`

Do not rename assets after publishing. The updater selects assets by their exact versioned names.

## Post-publish validation

1. Confirm the release is not marked as draft or prerelease.
2. Confirm the tag, title, ZIP, and checksum all use the same version.
3. Download both assets from the public release page.
4. Recalculate the ZIP SHA-256 hash and compare it with the sidecar.
5. Confirm the ZIP contains only `DISM Lab.exe`, then extract it into a new writable folder.
6. Run `DISM Lab.exe` and approve elevation.
7. Confirm the title displays the released version.
8. Open **Settings**, use **Check for updates**, and confirm it reports that the current version is latest.
9. Keep the GitHub Actions run artifact until release validation is complete.

## Failed workflow or release

A workflow failure before the final step does not create a release. Correct the cause and rerun it with the same version. If a release was created but is unusable, do not replace its files in place for public users; remove the defective release if it has not been distributed, or publish a corrected patch version if it has.
