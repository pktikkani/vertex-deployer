@echo off
echo ================================
echo Vertex Central Deployer Uninstaller
echo ================================
echo.

:: Check for admin rights
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This uninstaller requires administrator privileges.
    echo Please run as administrator.
    pause
    exit /b 1
)

set "INSTALL_DIR=%ProgramFiles%\VertexDeployer"
set "DATA_DIR=%ProgramData%\VertexDeployer"

echo This will remove Vertex Central Deployer from your system.
echo.
choice /C YN /M "Do you want to continue"
if errorlevel 2 exit /b 0

:: Remove shortcuts
echo Removing shortcuts...
del "%ProgramData%\Microsoft\Windows\Start Menu\Programs\Vertex Central Deployer.lnk" 2>nul
del "%PUBLIC%\Desktop\Vertex Central Deployer.lnk" 2>nul

:: Remove installation directory
echo Removing program files...
if exist "%INSTALL_DIR%" (
    rmdir /S /Q "%INSTALL_DIR%"
)

:: Ask about data directory
echo.
choice /C YN /M "Do you want to remove all data (database, logs)"
if errorlevel 2 goto :skip_data

echo Removing data directory...
if exist "%DATA_DIR%" (
    rmdir /S /Q "%DATA_DIR%"
)

:skip_data
echo.
echo ================================
echo Uninstallation completed!
echo ================================
echo.
pause
