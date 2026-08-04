@echo off
wpeinit
powercfg /s 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c >nul 2>&1
cd /d "%SystemDrive%\Scripts"
call "%SystemDrive%\Scripts\WinPEMenu.cmd"
