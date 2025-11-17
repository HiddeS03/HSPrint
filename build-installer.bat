@echo off
echo Building HSPrint Installer...
echo.

REM Get version from version.txt
set /p VERSION=<version.txt

powershell -ExecutionPolicy Bypass -File build-installer.ps1 -Version %VERSION%

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build completed successfully!
    echo Check the artifacts folder for the MSI file.
) else (
    echo.
    echo Build failed! Check the output above for errors.
)

pause
