@ECHO off
Color 1f
ECHO =========================================================
ECHO EDT 11 - Image Capture .wim file Framework Script                
ECHO Copyright (C) Microsoft Corporation. All rights reserved.
ECHO =========================================================
ECHO.
SET "vWindows="
FOR %%a in (C D E F G H I J K L M N O P Q R S T U V W Y Z) do IF EXIST "%%a:\Windows\System32\Config\SYSTEM" SET "vWindows=%%a:"
IF "%vWindows%"=="" (ECHO 'Cannot find volume Windows'
PAUSE
GOTO END
) ELSE (
GOTO IMAGEFOLDER
)

:IMAGEFOLDER
@FOR %%b in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do @IF exist %%b:\Images\ set IMAGESDRIVE=%%b:
IF "%IMAGESDRIVE%"=="" (ECHO 'Cannot find Images folder'
GOTO END
) ELSE (
GOTO CAPTURE
)

:CAPTURE
if exist C:\Windows\OEM\project.project (
SET /p vPROJECT=< C:\Windows\OEM\project.project
) else (
:Custom
SET /P vPROJECT=Enter a name for this custom image.wim file [e.g. Windows11Pro_x64_XYZ] 
IF "%vPROJECT%"=="" GOTO Custom
)
ECHO Capturing Drive %vWindows% to %IMAGESDRIVE%\Images for Project - %vPROJECT%
ECHO Saving %vPROJECT%_Final.wim
REM DISM /Image:%vWindows%\ /optimize-image /boot
DISM /Capture-Image /ImageFile:"%IMAGESDRIVE%\Images\%vPROJECT%_Final.wim" /CaptureDir:%vWindows%\ /Name:%vPROJECT%

Pause

:END