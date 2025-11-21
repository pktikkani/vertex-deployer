# Build Vertex Central Deployer MSI Installer
# Automatically builds, harvests all files and creates MSI

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Vertex Central Deployer MSI Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$scriptDir = $PSScriptRoot
$solutionDir = Split-Path -Parent $scriptDir
$projectDir = Join-Path $solutionDir "WSMDeployer"
$publishDir = Join-Path $projectDir "bin\$Configuration\net8.0\win-$Platform\publish"
$objDir = Join-Path $scriptDir "obj"
$binDir = Join-Path $scriptDir "bin"

# Create directories
@($objDir, $binDir) | ForEach-Object {
    if (!(Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ | Out-Null
    }
}

Write-Host "[1/5] Publishing Deployer application..." -ForegroundColor Green
try {
    $publishArgs = @(
        "publish"
        "$projectDir\WSMDeployer.csproj"
        "-c", $Configuration
        "-r", "win-$Platform"
        "--self-contained", "true"
        "-p:PublishSingleFile=false"
        "-p:PublishReadyToRun=false"
        "-o", $publishDir
    )

    & dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to publish Deployer"
    }

    Write-Host "      Application published successfully" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: Failed to publish application" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    exit 1
}
Write-Host ""

# Validate output
if (!(Test-Path $publishDir)) {
    Write-Host "ERROR: Publish output not found!" -ForegroundColor Red
    Write-Host "Expected: $publishDir" -ForegroundColor Yellow
    exit 1
}

Write-Host "[2/5] Publish directory validated" -ForegroundColor Green
Write-Host "      Location: $publishDir" -ForegroundColor Gray

# Count files
$fileCount = (Get-ChildItem -Path $publishDir -Recurse -File).Count
Write-Host "      Files: $fileCount" -ForegroundColor Gray
Write-Host ""

Write-Host "[3/5] Installing WiX extensions..." -ForegroundColor Green
try {
    & wix extension add -g WixToolset.UI.wixext 2>$null
    & wix extension add -g WixToolset.Util.wixext 2>$null
    Write-Host "      Extensions installed" -ForegroundColor Gray
} catch {
    Write-Host "      Extensions already installed" -ForegroundColor Gray
}
Write-Host ""

Write-Host "[4/5] Generating WXS file..." -ForegroundColor Green
try {
    & powershell -ExecutionPolicy Bypass -File "$scriptDir\GenerateWXS.ps1" `
        -SourceDir "$publishDir" `
        -OutputFile "$scriptDir\Generated.wxs"

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate WXS file"
    }
    Write-Host "      WXS generated successfully" -ForegroundColor Gray
} catch {
    throw "Failed to generate WXS: $_"
}
Write-Host ""

Write-Host "[5/5] Building MSI (this may take a few minutes)..." -ForegroundColor Green
Write-Host ""

try {
    # Build the MSI
    $wixArgs = @(
        "build"
        "-arch", "$Platform"
        "-ext", "WixToolset.UI.wixext"
        "-ext", "WixToolset.Util.wixext"
        "-out", "$binDir\VertexCentralDeployer.msi"
        "$scriptDir\Product.wxs"
        "$scriptDir\Generated.wxs"
        "-d", "DeployerDir=$publishDir"
        "-v"
    )

    & wix @wixArgs

    if ($LASTEXITCODE -ne 0) {
        throw "WiX build failed with exit code $LASTEXITCODE"
    }

    $msiFile = Get-Item "$binDir\VertexCentralDeployer.msi"
    $msiSizeMB = [math]::Round($msiFile.Length / 1MB, 2)

    Write-Host ""
    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " MSI Installer Created Successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Location: " -NoNewline
    Write-Host "$binDir\VertexCentralDeployer.msi" -ForegroundColor Yellow
    Write-Host "Size: " -NoNewline
    Write-Host "$msiSizeMB MB" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "This installer includes:" -ForegroundColor White
    Write-Host "  - Vertex Central Deployer application" -ForegroundColor Gray
    Write-Host "  - Desktop and Start Menu shortcuts" -ForegroundColor Gray
    Write-Host "  - Database and Logs directories" -ForegroundColor Gray
    Write-Host "  - All required dependencies" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Installation requires Administrator privileges." -ForegroundColor White
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host " BUILD FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack Trace:" -ForegroundColor Yellow
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    Write-Host ""
    exit 1
}
