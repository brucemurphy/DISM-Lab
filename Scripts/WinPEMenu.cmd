@echo off
setlocal EnableExtensions EnableDelayedExpansion
color 1F
cd /d "%~dp0"

:MENU
set "IMAGESDRIVE="
for %%a in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do if exist "%%a:\Images\" set "IMAGESDRIVE=%%a:"
set "M="
cls
echo =========================================================
echo DISM Lab - Image Deployment Toolkit
echo =========================================================
echo.
if defined IMAGESDRIVE (
	echo Images folder: %IMAGESDRIVE%\Images
) else (
	echo WARNING: No drive containing an Images folder was found.
)
echo.
echo [1] - Apply Windows image file (.wim and .ffu)
echo [2] - Apply Recovery
echo [3] - Capture Windows image file (.wim)
echo [4] - Open new CMD Window
echo [5] - Reboot
echo.
set /p "M=Select option then press ENTER: "
if "%M%"=="1" goto APPLYWIM
if "%M%"=="2" goto APPLYREC
if "%M%"=="3" goto CAPTUREWIM
if "%M%"=="4" goto NEWWIN
if "%M%"=="5" goto REBOOT
goto MENU

:APPLYWIM
if not defined IMAGESDRIVE (
	echo.
	echo No Images folder is available. Attach the image media and try again.
	pause
	goto MENU
)

set "IMAGE_LIST=%TEMP%\DISMLab-Images.txt"
del /f /q "%IMAGE_LIST%" >nul 2>&1
for %%F in ("%IMAGESDRIVE%\Images\*.wim" "%IMAGESDRIVE%\Images\*.ffu") do if exist "%%~F" echo %%~nxF>>"%IMAGE_LIST%"

if not exist "%IMAGE_LIST%" (
	echo.
	echo No WIM or FFU files were found in %IMAGESDRIVE%\Images.
	pause
	goto MENU
)

set /a x=1
for /f "usebackq delims=" %%i in ("%IMAGE_LIST%") do (
	echo [!x!] - %%i
	set /a x+=1
)

echo.
set "Choice="
set /p "Choice=Select image then press ENTER: "
set "Result="
set /a Current=0
for /f "usebackq delims=" %%i in ("%IMAGE_LIST%") do (
	set /a Current+=1
	if "!Current!"=="%Choice%" set "Result=%%i"
)
del /f /q "%IMAGE_LIST%" >nul 2>&1
if not defined Result (
	echo Invalid image selection.
	pause
	goto MENU
)

:APPLYPROC
echo Applying "%IMAGESDRIVE%\Images\%Result%"
call "%~dp0Apply-Image.bat" "%IMAGESDRIVE%\Images\%Result%"
pause
goto MENU

:APPLYREC
call "%~dp0ApplyRecovery.bat"
pause
goto MENU

:CAPTUREWIM
call "%~dp0Capture-Image.cmd"
goto MENU

:NEWWIN
start "DISM Lab Command Prompt" cmd.exe
goto MENU

:REBOOT
wpeutil reboot
exit /b
