rem @echo off
cls
setlocal ENABLEDELAYEDEXPANSION
cd /d %~dp0
REM Set scratch folderpath. Folder name must not contain spaces
set TEMP=%TMP%

REM Define color codes
set COLERROR=4F
set COLPROCESS=1F
set COLINPUT=2F
set COLDEFAULT=07

:setdisk
set vDISK=""
echo list disk>>%TEMP%\diskpart.txt
echo Exit>>%TEMP%\diskpart.txt
diskpart /s %TEMP%\diskpart.txt
del %TEMP%\diskpart.txt
echo.
set /p vDISK=Select the Disk number where you applied your image to from the list above: 
if %ERRORLEVEL%==1 goto setdisk
if %vDISK%==0 (goto createpart)

REM Loop through all drive letters
for %%a in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    REM Get the volume label of the drive and check if it contains "Windows"
    for /f "tokens=*" %%b in ('vol %%a: 2^>nul ^| find /i "Windows"') do (
        set "WindowsDrive=%%a"
		echo Windows Drive Letter = %WindowsDrive%:
    )
)

set "RecoveryDrive=none"
for %%a in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    REM Get the volume label of the drive and check if it contains "Recovery"
    for /f "tokens=*" %%d in ('vol %%c: 2^>nul ^| find /i "Recovery"') do (
        set "RecoveryDrive=%%c"
    )
)
if "%RecoveryDrive%"=="none" (
echo No Recovery Drive found
goto createpart
) else (
echo Recovery Drive Letter = %RecoveryDrive%:
)

:copytotoolspartition
md %RecoveryDrive%:\Recovery\WindowsRE
if exist "%WindowsDrive%:\Windows\OEM\Winre.wim" (
echo Using custom WinRE.wim
xcopy /h %WindowsDrive%:\Windows\OEM\Winre.wim %RecoveryDrive%:\Recovery\WindowsRE\
del %WindowsDrive%:\Windows\OEM\Winre.wim
) else (
echo Using generic WinRE.wim
If exist "%WindowsDrive%:\Windows\System32\Recovery\Winre.wim" xcopy /h %WindowsDrive%:\Windows\System32\Recovery\Winre.wim %RecoveryDrive%:\Recovery\WindowsRE\
)


echo Register the location of the recovery tools
%WindowsDrive%:\Windows\System32\Reagentc /Setreimage /Path %RecoveryDrive%:\Recovery\WindowsRE /Target %WindowsDrive%:\Windows
if exist %WindowsDrive%:\Recovery\Customizations\USMT.ppkg (goto customdataimagewim) else goto hidewimrecoverytools


:customdataimagewim
if not exists %WindowsDrive%:\Windows\OEM\compact.txt goto hidewimrecoverytools
echo Looks like you are deploying a CompactOS based image select how to deploy your custom recovery package
echo              Y: Yes, single instance
echo              D: Yes, but defer cleanup steps to first boot.
echo                 Use this if the cleanup steps take more than 30 minutes. Defer the cleanup steps to the first boot.
@SET /P COMPACTOS=Deploy as Compact OS? (Y, N, or D):
@if %COMPACTOS%.==y. set COMPACTOS=Y
@if %COMPACTOS%.==d. set COMPACTOS=D
@if %COMPACTOS%.==Y. dism /Apply-CustomDataImage /CustomDataImage:W:\Recovery\Customizations\USMT.ppkg /ImagePath:W:\ /SingleInstance
@if %COMPACTOS%.==D. dism /Apply-CustomDataImage /CustomDataImage:W:\Recovery\Customizations\USMT.ppkg /ImagePath:W:\ /SingleInstance /Defer



:hidewimrecoverytools
echo Hiding Recovery tools
echo.

:createpart
echo select disk 0 >%TEMP%\diskpart.txt
echo select partition 4 >>%TEMP%\diskpart.txt
echo set id=de94bba4-06d1-4d40-a16a-bfd50179d6ac>>%TEMP%\diskpart.txt
echo gpt attributes=0x8000000000000001>>%TEMP%\diskpart.txt
echo remove>>%TEMP%\diskpart.txt
echo list volume>>%TEMP%\diskpart.txt
echo exit>>%TEMP%\diskpart.txt
set "vFileName=%TEMP%\diskpart.txt"
set "DefaultDisk=select disk 0"
set "SelectedDisk=select disk %vDISK%"
(for /f "delims=" %%i in (%vFileName%) do (
    set "line=%%i"
    set "line=!line:%DefaultDisk%=%SelectedDisk%!"
    echo(!line!
 ))>"%TEMP%\temp.txt"
 del %vFileName%
 rename "%TEMP%\temp.txt" "HideRecoveryPartitions-UEFI.txt"

echo Hiding the recovery tools partition
diskpart /s "%TEMP%\HideRecoveryPartitions-UEFI.txt"


%WindowsDrive%:\Windows\System32\Reagentc /Info /Target %WindowsDrive%:\Windows



@echo    (Note: Windows RE status may appear as Disabled, this is OK.)
@echo *********************************************************************
@echo      All done!
@echo      Disconnect the USB drive from the reference device.
@echo      Type exit to reboot.
@echo.
GOTO END









:CREATEFFURECOVERY
@echo *********************************************************************
@echo == Creating the recovery tools partition
@if %Firmware%==0x1 diskpart /s CreateRecoveryPartitions-BIOS.txt
@if %Firmware%==0x2 diskpart /s CreateRecoveryPartitions-UEFI.txt
@echo finding the Windows Drive
@echo  *********************************************************************
@IF EXIST C:\Windows SET windowsdrive=C:\
@IF EXIST D:\Windows SET windowsdrive=D:\
@IF EXIST E:\Windows SET windowsdrive=E:\
@IF EXIST W:\Windows SET windowsdrive=W:\
@echo The Windows drive is %windowsdrive%





md R:\Recovery\WindowsRE
@echo  *********************************************************************
@echo Finding Winre.wim
@IF EXIST %windowsdrive%Recovery\WindowsRE\winre.wim SET recoveryfolder=%windowsdrive%Recovery\WindowsRE\
@IF EXIST %windowsdrive%Windows\System32\Recovery\winre.wim SET recoveryfolder=%windowsdrive%Windows\System32\Recovery\
@echo  *********************************************************************
@echo copying Winre.wim
xcopy /h %recoveryfolder%Winre.wim R:\Recovery\WindowsRE\
@echo  *********************************************************************
@echo  == Register the location of the recovery tools ==
%windowsdrive%Windows\System32\Reagentc /Setreimage /Path R:\Recovery\WindowsRE /Target %windowsdrive%Windows
@echo  *********************************************************************
@IF EXIST W:\Recovery\Customizations\USMT.ppkg (GOTO CUSTOMDATAIMAGEFFU) else goto HIDERECOVERYTOOLSFFU
:CUSTOMDATAIMAGEFFU
@echo  == If Compact OS, single-instance the recovery provisioning package ==
@echo.     
@echo     *Note: this step only works if you created a ScanState package called
@echo      USMT.ppkg as directed in the OEM Deployment lab. If you aren't
@echo      following the steps in the lab, choose N.
@echo.
@echo     Options: N: No
@echo              Y: Yes
@echo              D: Yes, but defer cleanup steps to first boot.
@echo                 Use this if the cleanup steps take more than 30 minutes.
@echo                 defer the cleanup steps to the first boot.
@SET /P COMPACTOS=Deploy as Compact OS? (Y, N, or D):
@if %COMPACTOS%.==y. set COMPACTOS=Y
@if %COMPACTOS%.==d. set COMPACTOS=D
@if %COMPACTOS%.==Y. dism /Apply-CustomDataImage /CustomDataImage:%windowsdrive%Recovery\Customizations\USMT.ppkg /ImagePath:%windowsdrive% /SingleInstance
@if %COMPACTOS%.==D. dism /Apply-CustomDataImage /CustomDataImage:%windowsdrive%Recovery\Customizations\USMT.ppkg /ImagePath:%windowsdrive% /SingleInstance /Defer







:HIDERECOVERYTOOLSFFU
diskpart /s HideRecoveryPartitions-UEFI.txt
@echo == Verify the configuration status of the images. ==
%windowsdrive%:\Windows\System32\Reagentc /Info /Target %windowsdrive%:\Windows
echo    (Note: Windows RE status may appear as Disabled, this is OK.)
echo      All done!
echo      Disconnect the USB drive from the reference device.
echo      Type exit to reboot.
GOTO END

:HideRecoveryDiskPart
echo select disk 0 >%TEMP%\diskpart.txt
echo select partition 4 >>%TEMP%\diskpart.txt
echo set id=de94bba4-06d1-4d40-a16a-bfd50179d6ac>>%TEMP%\diskpart.txt
echo gpt attributes=0x8000000000000001>>%TEMP%\diskpart.txt
echo remove>>%TEMP%\diskpart.txt
echo list volume>>%TEMP%\diskpart.txt
echo exit>>%TEMP%\diskpart.txt






:END