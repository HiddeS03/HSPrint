@echo off
REM Build HSPrint Installer
REM Run this script to build the MSI installer

echo ================================
echo  HSPrint Installer Builder
echo ================================
echo.

REM Check for admin rights
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running as Administrator: OK
) else (
    echo WARNING: Not running as Administrator
    echo Some features may not work correctly
    echo.
)

REM Get version from version.txt
set /p VERSION=<version.txt
echo Building version: %VERSION%
echo.

REM Run the PowerShell build script
powershell.exe -ExecutionPolicy Bypass -File ".\build-installer.ps1" -Version "%VERSION%"

if %errorLevel% == 0 (
    echo.
    echo ================================
    echo  Build completed successfully!
    echo ================================
    echo.
    echo Installer location: artifacts\HSPrintSetup-%VERSION%.msi
    echo.
) else (
    echo.
    echo ================================
    echo  Build failed!
    echo ================================
    echo.
)

pause
