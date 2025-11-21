# Test script to query users from target machine
param(
    [Parameter(Mandatory=$true)]
    [string]$TargetHostname,
    [Parameter(Mandatory=$false)]
    [string]$Username,
    [Parameter(Mandatory=$false)]
    [string]$Password
)

Write-Host "=== Testing User Query on $TargetHostname ===" -ForegroundColor Cyan
Write-Host ""

try {
    $isLocal = ($TargetHostname -eq $env:COMPUTERNAME) -or ($TargetHostname -eq "localhost") -or ($TargetHostname -eq "127.0.0.1")

    if ($isLocal) {
        Write-Host "Detected LOCAL connection - using current user context" -ForegroundColor Yellow
        Write-Host "Current user: $env:USERNAME" -ForegroundColor Gray
        Write-Host ""
        $credential = $null
    } else {
        if ([string]::IsNullOrEmpty($Username) -or [string]::IsNullOrEmpty($Password)) {
            Write-Host "ERROR: Username and Password are required for remote connections" -ForegroundColor Red
            exit 1
        }
        Write-Host "Detected REMOTE connection - using provided credentials" -ForegroundColor Yellow
        Write-Host "Username: $Username" -ForegroundColor Gray
        Write-Host ""
        $securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
        $credential = New-Object System.Management.Automation.PSCredential($Username, $securePassword)
    }

    Write-Host "1. Testing WMI connection..." -ForegroundColor Yellow

    if ($credential) {
        $testConnection = Get-WmiObject -Class Win32_OperatingSystem -ComputerName $TargetHostname -Credential $credential -ErrorAction Stop
    } else {
        $testConnection = Get-WmiObject -Class Win32_OperatingSystem -ComputerName $TargetHostname -ErrorAction Stop
    }

    Write-Host "   OK - WMI connection successful" -ForegroundColor Green
    Write-Host "   OS: $($testConnection.Caption)" -ForegroundColor Gray
    Write-Host ""

    Write-Host "2. Querying Administrator group members..." -ForegroundColor Yellow

    if ($credential) {
        $adminMembers = Get-WmiObject -Class Win32_GroupUser -ComputerName $TargetHostname -Credential $credential | Where-Object { $_.GroupComponent -match "Administrators" }
    } else {
        $adminMembers = Get-WmiObject -Class Win32_GroupUser -ComputerName $TargetHostname | Where-Object { $_.GroupComponent -match "Administrators" }
    }

    $adminUsernames = @()
    foreach ($member in $adminMembers) {
        if ($member.PartComponent -match 'Name="([^"]+)"') {
            $adminUsernames += $matches[1]
        }
    }
    Write-Host "   Administrators: $($adminUsernames -join ', ')" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "3. Querying all user accounts..." -ForegroundColor Yellow

    if ($credential) {
        $allUsers = Get-WmiObject -Class Win32_UserAccount -ComputerName $TargetHostname -Credential $credential
    } else {
        $allUsers = Get-WmiObject -Class Win32_UserAccount -ComputerName $TargetHostname
    }

    Write-Host "   Total user accounts found: $($allUsers.Count)" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "4. User analysis:" -ForegroundColor Yellow
    Write-Host ""

    $enabledNonAdmins = 0

    foreach ($user in $allUsers) {
        $isAdmin = $adminUsernames -contains $user.Name
        $isEnabled = -not $user.Disabled
        $isLocal = $user.LocalAccount

        $adminLabel = if ($isAdmin) { "[ADMIN]" } else { "[USER]" }
        $statusLabel = if ($isEnabled) { "[ENABLED]" } else { "[DISABLED]" }
        $localLabel = if ($isLocal) { "[LOCAL]" } else { "[DOMAIN]" }

        $shouldInclude = $isLocal -and $isEnabled -and (-not $isAdmin)
        $includeLabel = if ($shouldInclude) { "INCLUDE" } else { "SKIP" }
        $color = if ($shouldInclude) { "Green" } else { "Gray" }

        Write-Host "   [$includeLabel] $($user.Name)" -ForegroundColor $color
        Write-Host "      $adminLabel $statusLabel $localLabel" -ForegroundColor Gray
        if ($user.FullName) {
            Write-Host "      Full Name: $($user.FullName)" -ForegroundColor Gray
        }

        if ($shouldInclude) {
            $enabledNonAdmins++
        }

        Write-Host ""
    }

    Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
    Write-Host "   Total users: $($allUsers.Count)" -ForegroundColor White
    Write-Host "   Eligible for deployment: $enabledNonAdmins" -ForegroundColor Green
    Write-Host ""

    if ($enabledNonAdmins -eq 0) {
        Write-Host "   NO ELIGIBLE USERS!" -ForegroundColor Red
        Write-Host "   All users are either disabled or administrators" -ForegroundColor Yellow
    }

} catch {
    Write-Host ""
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}
