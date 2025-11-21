# Clear Old Database with Sample Data
# Run this script if you installed the old version and want to start fresh

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Clear Vertex Deployer Database" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Database locations to check (old and new paths)
$dbPaths = @(
    "$env:ProgramData\VertexDeployer\deployer.db",
    "$env:ProgramData\VertexDeployer\Database\deployer.db"
)

$dbPath = $null
foreach ($path in $dbPaths) {
    if (Test-Path $path) {
        $dbPath = $path
        break
    }
}

if ($dbPath) {
    Write-Host "Found existing database at:" -ForegroundColor Yellow
    Write-Host "  $dbPath" -ForegroundColor Gray
    Write-Host ""

    $response = Read-Host "Delete this database and start fresh? (y/n)"

    if ($response -eq 'y' -or $response -eq 'Y') {
        try {
            Remove-Item -Path $dbPath -Force
            Write-Host ""
            Write-Host "SUCCESS!" -ForegroundColor Green
            Write-Host "Database cleared. Next time you launch the app, it will create a clean database." -ForegroundColor White
            Write-Host ""
        }
        catch {
            Write-Host ""
            Write-Host "ERROR: Failed to delete database" -ForegroundColor Red
            Write-Host "Make sure Vertex Central Deployer is closed and try again." -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Error details: $($_.Exception.Message)" -ForegroundColor Gray
            Write-Host ""
            exit 1
        }
    }
    else {
        Write-Host ""
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        Write-Host ""
    }
}
else {
    Write-Host "No database found in these locations:" -ForegroundColor Yellow
    foreach ($path in $dbPaths) {
        Write-Host "  $path" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "Either the app hasn't been run yet, or it's using a different location." -ForegroundColor White
    Write-Host ""
}

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
