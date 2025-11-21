@echo off
echo Deleting old Vertex Deployer database...
echo.

taskkill /F /IM WSMDeployer.exe 2>nul
timeout /t 2 /nobreak >nul

del /F /Q "C:\ProgramData\VertexDeployer\deployer.db" 2>nul

if exist "C:\ProgramData\VertexDeployer\deployer.db" (
    echo ERROR: Could not delete database file
    echo Please close Vertex Central Deployer and run this again
    pause
) else (
    echo SUCCESS: Database deleted!
    echo Next time you launch the app, it will be clean with no data
    pause
)
