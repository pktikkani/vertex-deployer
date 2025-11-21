# Vertex Central Deployer - Build and Package Script
# This script builds the Vertex Central Deployer application and creates an MSI installer

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter(Mandatory=$false)]
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'

# Colors for output
function Write-Info { param($msg) Write-Host $msg -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host $msg -ForegroundColor Green }
function Write-Error { param($msg) Write-Host $msg -ForegroundColor Red }

Write-Info "================================"
Write-Info "Vertex Central Deployer Builder"
Write-Info "================================"
Write-Info "Configuration: $Configuration"
Write-Info "Version: $Version"
Write-Info ""

# Set paths
$ProjectRoot = $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "WSMDeployer\WSMDeployer.csproj"
$BuildDir = Join-Path $ProjectRoot "WSMDeployer\bin\$Configuration\net8.0"
$InstallerDir = Join-Path $ProjectRoot "installer"
$OutputDir = Join-Path $ProjectRoot "bin"

# Step 1: Clean previous builds
Write-Info "[1/6] Cleaning previous builds..."
if (Test-Path $InstallerDir) {
    Remove-Item -Path $InstallerDir -Recurse -Force
}
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
Write-Success "✓ Cleaned"

# Step 2: Restore NuGet packages
Write-Info "[2/6] Restoring NuGet packages..."
dotnet restore $ProjectFile
if ($LASTEXITCODE -ne 0) {
    Write-Error "✗ NuGet restore failed"
    exit 1
}
Write-Success "✓ Packages restored"

# Step 3: Build the project
Write-Info "[3/6] Building project..."
dotnet build $ProjectFile -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "✗ Build failed"
    exit 1
}
Write-Success "✓ Build succeeded"

# Step 4: Verify build output
Write-Info "[4/6] Verifying build output..."
if (-not (Test-Path $BuildDir)) {
    Write-Error "✗ Build directory not found: $BuildDir"
    exit 1
}
Write-Success "✓ Build output verified: $BuildDir"

# Step 5: Create installer directory structure
Write-Info "[5/6] Preparing installer directory..."
New-Item -ItemType Directory -Path $InstallerDir -Force | Out-Null
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Copy build files
Write-Info "Copying build files..."
Copy-Item -Path "$BuildDir\*" -Destination $InstallerDir -Recurse -Force

# Create simple batch installer (for now, until WiX is set up)
$InstallerScript = @'
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
'@

$InstallerScript | Out-File -FilePath (Join-Path $InstallerDir "install.bat") -Encoding ASCII

# Create uninstaller
$UninstallerScript = @'
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
'@

$UninstallerScript | Out-File -FilePath (Join-Path $InstallerDir "uninstall.bat") -Encoding ASCII

Write-Success "✓ Installer prepared"

# Step 6: Create ZIP package
Write-Info "[6/6] Creating ZIP package..."
$ZipFileName = "VertexCentralDeployer-v$Version-$Configuration.zip"
$ZipPath = Join-Path $OutputDir $ZipFileName

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Compress-Archive -Path "$InstallerDir\*" -DestinationPath $ZipPath -Force
Write-Success "✓ ZIP created: $ZipPath"

# Summary
Write-Info ""
Write-Info "================================"
Write-Success "Build completed successfully!"
Write-Info "================================"
Write-Info "Package: $ZipPath"
Write-Info "Size: $([math]::Round((Get-Item $ZipPath).Length / 1MB, 2)) MB"
Write-Info ""
Write-Info "To install:"
Write-Info "1. Extract the ZIP file"
Write-Info "2. Run install.bat as administrator"
Write-Info ""
Write-Info "To uninstall:"
Write-Info "1. Run uninstall.bat as administrator"
Write-Info "================================"
