$dbPath = "C:\ProgramData\VertexDeployer\deployer.db"

if (Test-Path $dbPath) {
    try {
        # Try to take ownership
        $acl = Get-Acl $dbPath
        $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($currentUser, "FullControl", "Allow")
        $acl.SetAccessRule($accessRule)
        Set-Acl -Path $dbPath -AclObject $acl

        # Now delete
        Remove-Item -Path $dbPath -Force
        Write-Host "SUCCESS: Database deleted!" -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please run this in PowerShell as Administrator:" -ForegroundColor Yellow
        Write-Host "Remove-Item -Path 'C:\ProgramData\VertexDeployer\deployer.db' -Force" -ForegroundColor Cyan
    }
}
else {
    Write-Host "Database file not found - already deleted or doesn't exist" -ForegroundColor Green
}
