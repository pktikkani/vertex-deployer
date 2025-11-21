# Clear Database with Admin Rights
# Run this if regular ClearDatabase.ps1 fails due to permissions

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Clear Vertex Deployer Database (Admin)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script requires Administrator privileges" -ForegroundColor Red
    Write-Host "Please right-click this script and select 'Run as Administrator'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

# Stop any running instances
Write-Host "Checking for running instances of Vertex Central Deployer..." -ForegroundColor Yellow
$processes = Get-Process | Where-Object { $_.ProcessName -like "*WSMDeployer*" -or $_.ProcessName -like "*Vertex*" }

if ($processes) {
    Write-Host "Found running processes. Stopping them..." -ForegroundColor Yellow
    foreach ($proc in $processes) {
        try {
            $proc.Kill()
            $proc.WaitForExit(5000)
            Write-Host "  Stopped: $($proc.ProcessName)" -ForegroundColor Gray
        } catch {
            Write-Host "  Failed to stop: $($proc.ProcessName)" -ForegroundColor Red
        }
    }
    Start-Sleep -Seconds 2
}

# Database locations to check
$dbPaths = @(
    "$env:ProgramData\VertexDeployer\deployer.db",
    "$env:ProgramData\VertexDeployer\Database\deployer.db"
)

$foundDb = $false
foreach ($dbPath in $dbPaths) {
    if (Test-Path $dbPath) {
        $foundDb = $true
        Write-Host ""
        Write-Host "Found database at:" -ForegroundColor Yellow
        Write-Host "  $dbPath" -ForegroundColor Gray
        Write-Host ""

        try {
            # Force delete the file
            Remove-Item -Path $dbPath -Force -ErrorAction Stop
            Write-Host "SUCCESS!" -ForegroundColor Green
            Write-Host "Database deleted successfully." -ForegroundColor White
            Write-Host ""
        }
        catch {
            Write-Host "ERROR: Failed to delete database" -ForegroundColor Red
            Write-Host "Error details: $($_.Exception.Message)" -ForegroundColor Gray
            Write-Host ""
        }
    }
}

if (-not $foundDb) {
    Write-Host "No database found in these locations:" -ForegroundColor Yellow
    foreach ($path in $dbPaths) {
        Write-Host "  $path" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "The database may have already been deleted." -ForegroundColor White
    Write-Host ""
}

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
