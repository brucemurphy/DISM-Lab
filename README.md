# DISM Lab

## What is DISM Lab?
DISM Lab is a Windows application that makes it easy to work with Windows Image (WIM) files and create Windows PE boot media. Whether you need to customize Windows installation images, add drivers, apply updates, or create bootable USB drives, DISM Lab provides a simple visual interface to get the job done.

## What Can You Do With It?

### Working with Windows Images
- **View WIM Contents** - Open any Windows image file and see all the different versions (indexes) it contains, including their names, descriptions, and sizes
- **Mount Images** - Mount a Windows image to your computer so you can modify it, with live progress tracking showing how much data has been copied
- **Unmount Images** - Safely unmount images with the option to save or discard your changes
- **Export Images** - Extract specific Windows versions from a WIM file into a new standalone image file

### Managing Drivers
- **Add Drivers to Images** - Select a folder containing driver files and add them to your Windows image - great for pre-loading hardware drivers
- **Export Drivers from Images** - Pull all drivers out of a Windows image and save them to a folder for backup or reuse
- **Capture System Drivers** - Export all drivers currently installed on your running computer to a folder - perfect for creating driver backup collections

### Applying Updates
- **Apply Windows Updates** - Select .msu or .cab update files and apply them to your Windows image, so your installation media is already up-to-date

### Creating Windows PE Boot Media
- **Build WinPE Images** - Create a lightweight Windows PE boot environment by choosing your architecture (x64 or arm64) and optional components like PowerShell, WMI, or networking tools
- **Create Bootable USB Drives** - Turn any USB drive into a bootable Windows PE recovery drive with separate partitions for the boot files and your Windows images

## How to Use It

### Basic Workflow
1. **Select a WIM file** from the File menu
2. **Choose what you want to do** - the available buttons change based on whether you've selected an image or mounted one
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
4. Click Finish and wait for it to build
5. Click "Create USB"
6. Select your USB drive (WARNING: this will erase everything on it!)
7. Wait for the files to copy

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

## Tips
- The green activity light shows when DISM is working
- Progress percentages appear at the bottom during long operations
- Mount a WIM file to see operation logs in the lower panel
- The app automatically cleans up mount folders if they're not empty at startup
- All operations can be cancelled if something goes wrong