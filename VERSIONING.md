# DISM Lab Versioning

DISM Lab follows [Semantic Versioning](https://semver.org/) using `MAJOR.MINOR.PATCH` numbers.

## Version rules

- **MAJOR**: incompatible behavior, removed workflows, or significant user-facing redesigns.
- **MINOR**: backward-compatible features and meaningful capability additions.
- **PATCH**: backward-compatible bug fixes, security fixes, and minor refinements.
- Git tags and GitHub Release names use a leading `v`, for example `v1.0.0`.
- Published asset names include the same tag: `DISM-Lab-v1.0.0-win-x64.zip`.

Pre-release tags are not currently published or offered by the application updater. The updater uses GitHub's latest stable release endpoint and accepts three-part numeric versions only.

## Source of truth

`VersionPrefix` in `DISM Lab.vbproj` is the source of truth for the application version. The release workflow refuses to publish when its version input does not match this value.

The project derives these values from `VersionPrefix`:

- Assembly version: `MAJOR.MINOR.PATCH.0`
- File version: `MAJOR.MINOR.PATCH.0`
- Informational/display version: `MAJOR.MINOR.PATCH`

The main window reads the entry assembly version and displays it as `DISM Lab vMAJOR.MINOR.PATCH`.

## Preparing a version

1. Decide whether the change is major, minor, or patch.
2. Update `VersionPrefix` in `DISM Lab.vbproj`.
3. Move the applicable entries in `CHANGELOG.md` from `Unreleased` into a heading for the new version.
4. Use the release date in `YYYY-MM-DD` format when the release is ready.
5. Commit and push the version and changelog changes before running the release workflow.
6. Run the workflow with the matching `vMAJOR.MINOR.PATCH` tag.

Never reuse or move a published release tag. If a release is defective, fix the issue and publish a higher patch version.

## Release notes contract

Every GitHub Release description must contain these headings:

- `## New Features`
- `## Bug Fixes`
- `## Other Changes`

Use `None.` under a heading when that category has no entries. The application displays these sections in the update prompt before installation.
