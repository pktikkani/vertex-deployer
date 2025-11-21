@echo off
echo ================================
echo Vertex Central Deployer Installer
echo ================================
echo.

:: Check for admin rights
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This installer requires administrator privileges.
    echo Please run as administrator.
    pause
    exit /b 1
)

:: Set installation directory
set "INSTALL_DIR=%ProgramFiles%\VertexDeployer"

echo Installing to: %INSTALL_DIR%
echo.

:: Create installation directory
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

:: Copy files
echo Copying files...
xcopy /E /I /Y /Q "%~dp0*" "%INSTALL_DIR%\" >nul
if %errorlevel% neq 0 (
    echo ERROR: Failed to copy files
    pause
    exit /b 1
)

:: Create data directory
set "DATA_DIR=%ProgramData%\VertexDeployer"
if not exist "%DATA_DIR%" mkdir "%DATA_DIR%"

:: Create Start Menu shortcut
echo Creating Start Menu shortcut...
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%ProgramData%\Microsoft\Windows\Start Menu\Programs\Vertex Central Deployer.lnk'); $s.TargetPath = '%INSTALL_DIR%\WSMDeployer.exe'; $s.Save()"

:: Create Desktop shortcut
echo Creating Desktop shortcut...
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%PUBLIC%\Desktop\Vertex Central Deployer.lnk'); $s.TargetPath = '%INSTALL_DIR%\WSMDeployer.exe'; $s.Save()"

echo.
echo ================================
echo Installation completed successfully!
echo ================================
echo.
echo Start Menu: Vertex Central Deployer
echo Desktop: Vertex Central Deployer
echo Install Location: %INSTALL_DIR%
echo.
pause
