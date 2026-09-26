# DISM Lab

## What is DISM Lab?
DISM Lab is a Windows application that makes it easy to work with WIM, ESD, and FFU images and create Windows PE boot media. Whether you need to customize Windows installation images, add drivers, apply updates, extract drivers, or create bootable USB drives, DISM Lab provides a simple visual interface to get the job done.

## Download and Install

DISM Lab 1.0 is distributed from [GitHub Releases](https://github.com/brucemurphy/DISM-Lab/releases) as a self-contained Windows x64 portable ZIP.

1. Download `DISM-Lab-v1.0.0-win-x64.zip` from the latest release.
2. Optionally verify it using the accompanying `.sha256` file.
3. Extract the single `DISM Lab.exe` file into a writable folder.
4. Run `DISM Lab.exe` and approve the administrator prompt.

No installer, supporting application files, or separate .NET Desktop Runtime is required. All runtime dependencies are bundled into the executable.

## Application Updates

DISM Lab checks the repository's latest stable GitHub Release once at startup. You can also open **Settings** from the bottom-right corner and select **Check for updates**. When a newer version is available, the app displays its **New Features**, **Bug Fixes**, and **Other Changes** before asking whether to install it.

Accepted updates are downloaded over HTTPS, verified against the release's SHA-256 checksum, staged safely, and applied after the app closes. DISM Lab then restarts automatically. Application data under `%LOCALAPPDATA%\DISM_Lab` and Windows PE working files under `C:\WinPE` are preserved.

## What Can You Do With It?

### Working with Windows Images
- **View Image Contents** - Open WIM, ESD, or FFU images and inspect their available image metadata
- **Mount Images** - Mount a Windows image to your computer so you can modify it, with live progress tracking showing how much data has been copied
- **Unmount Images** - Safely unmount images with the option to save or discard your changes
- **Export Images** - Extract specific Windows versions from a WIM or ESD file into a new standalone WIM file

### Managing Drivers
- **Add Drivers to Images** - Select a folder containing driver files and add them to your Windows image - great for pre-loading hardware drivers
- **Export Drivers from Images** - Pull all drivers out of a Windows image and save them to a folder for backup or reuse
- **Capture System Drivers** - Export all drivers currently installed on your running computer to a folder - perfect for creating driver backup collections

### Applying Updates
- **Apply Windows Updates** - Select .msu or .cab update files and apply them to your Windows image, so your installation media is already up-to-date

### Creating Windows PE Boot Media
- **Build WinPE Images** - Create a lightweight Windows PE boot environment by choosing your architecture (x64 or arm64) and optional components like PowerShell, WMI, or networking tools
- **Include Deployment Scripts** - Optionally embed a startup menu for applying WIM/FFU images, capturing WIM images, and configuring Windows Recovery Environment
- **Create Bootable USB Drives** - Turn any USB drive into a bootable Windows PE recovery drive with separate partitions for the boot files and your Windows images

## How to Use It

### Basic Workflow
1. **Select a WIM, ESD, or FFU file** with the image selection button
2. **Choose what you want to do** - the available buttons change based on the image format and whether it is mounted
3. **Follow the prompts** - each operation guides you through folder selection or confirmation dialogs
4. **Watch the progress** - a live indicator shows you exactly what's happening and how much is complete

### Common Tasks

**To add drivers to a Windows installation:**
1. Select your WIM file
2. Choose the Windows version you want to modify
3. Click "Add Drivers"
4. Select the folder with your driver files
5. Choose which drivers to include
6. Wait for them to be added - you'll see a log of what was successful

**To create a Windows PE USB drive:**
1. Click "Create WinPE"
2. Choose your architecture (x64 or arm64)
3. Optionally add components like PowerShell
4. In **Settings**, enable **WinPE deployment scripts** if you want WinPE to launch the deployment toolkit automatically (it is off by default)
5. Click Finish and wait for it to build
6. Click "Create USB"
7. Select your USB drive (WARNING: this will erase everything on it!)
8. Place WIM or FFU files in the `Images` folder on the USB Images partition

The deployment toolkit is embedded in the WinPE image at `X:\Scripts`, so it does not depend on the USB boot partition's drive letter. Image apply and recovery operations can erase and repartition the selected target disk; review every confirmation carefully.

**To backup your computer's drivers:**
1. Click "Capture System Drivers"
2. Choose or type the destination folder
3. Wait for all drivers to be exported
4. Done! You now have a complete driver backup

## What You Need
- Windows 10 or Windows 11
- Administrator privileges (the app will ask to restart as admin if needed)
- For WinPE creation: Windows ADK and Windows PE add-on installed
- Internet connection (optional - for the Bing wallpaper background)

## Workspace Folder Settings
- **Mount folder** defaults to `C:\Mount` and is used for Windows image mount operations.
- **WinPE folder** defaults to `C:\WinPE` and contains the WinPE `Mount`, `media`, and manifest structure.
- Both locations can be changed from **Settings** before starting a mount or WinPE workflow. WinPE paths cannot contain spaces.
- Folder controls and Settings access are locked while an operation, mounted image, detected mount content, or WinPE creation workflow is active.
- Changing a location starts using the selected workspace; existing files are not moved.

## Tips
- The green activity light shows when DISM is working
- Progress percentages appear at the bottom during long operations
- Mount an image to see operation logs in the lower panel
- The app automatically cleans up mount folders if they're not empty at startup
- All operations can be cancelled if something goes wrong