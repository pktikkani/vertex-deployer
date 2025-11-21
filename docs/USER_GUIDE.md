# Vertex Central Deployer - User Guide

## Table of Contents
1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Getting Started](#getting-started)
4. [Adding Target Machines](#adding-target-machines)
5. [Deploying Packages](#deploying-packages)
6. [Monitoring Deployments](#monitoring-deployments)
7. [Troubleshooting](#troubleshooting)
8. [Advanced Configuration](#advanced-configuration)

---

## Overview

Vertex Central Deployer is a centralized application deployment tool that allows you to remotely install MSI packages on multiple Windows machines from a single console. It supports automatic failover between deployment methods to ensure reliable installations across different network configurations.

### Key Features
- Remote MSI deployment to multiple machines
- Automatic deployment method selection (PowerShell Remoting → WMI)
- Real-time deployment status tracking
- Deployment history and logging
- Support for both system-wide and per-user installations
- Simple, clean interface with dark/light themes

---

## Prerequisites

### Deployer Machine Requirements

The machine running Vertex Central Deployer needs:

**Operating System:**
- Windows 10/11 or Windows Server 2016+
- .NET 8.0 Runtime (included in installer)

**Network Access:**
- Network connectivity to target machines
- Administrator credentials for target machines
- Firewall rules allowing outbound connections on:
  - Port 5985 (WinRM/PowerShell Remoting)
  - Port 135 (WMI/DCOM)
  - Port 445 (SMB file sharing)

**Permissions:**
- Local administrator rights (to install Vertex Central Deployer)
- Domain admin or local admin credentials for target machines

---

### Target Machine Requirements

Each target machine must meet these requirements for successful deployment:

#### Required Configuration

**1. Remote Administration Enabled**

At least ONE of the following must be enabled:

**Option A: PowerShell Remoting (Recommended)**
```powershell
# Run on target machine as Administrator
Enable-PSRemoting -Force
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "*" -Force
Restart-Service WinRM
```

**Option B: WMI/DCOM (Fallback)**
```powershell
# Run on target machine as Administrator
# Enable WMI through firewall
netsh advfirewall firewall set rule group="Windows Management Instrumentation (WMI)" new enable=yes

# Enable DCOM
Set-ItemProperty -Path "HKLM:\Software\Microsoft\Ole" -Name "EnableDCOM" -Value "Y"
```

**2. Administrative Shares Enabled**

Administrative shares (like `C$`) must be accessible:

```powershell
# Verify administrative shares are enabled
net share

# Should show: C$, ADMIN$, IPC$
```

If not enabled:
```powershell
# Enable administrative shares
reg add "HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters" /v AutoShareWks /t REG_DWORD /d 1 /f
net stop server && net start server
```

**3. Firewall Rules**

Required incoming ports must be open:

```powershell
# Enable WinRM (PowerShell Remoting)
netsh advfirewall firewall add rule name="WinRM-HTTP" dir=in action=allow protocol=TCP localport=5985

# Enable WMI
netsh advfirewall firewall set rule group="Windows Management Instrumentation (WMI)" new enable=yes

# Enable File and Printer Sharing (for SMB/C$)
netsh advfirewall firewall set rule group="File and Printer Sharing" new enable=yes
```

**4. User Account Control (UAC) Configuration**

For non-domain environments, disable UAC remote restrictions:

```powershell
# Run on target machine as Administrator
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v LocalAccountTokenFilterPolicy /t REG_DWORD /d 1 /f
```

**5. Credentials**

- Administrator account with password authentication
- Account must be a member of the local Administrators group

---

### Quick Setup Script for Target Machines

Copy and run this PowerShell script on each target machine as Administrator:

```powershell
# Vertex Central Deployer - Target Machine Setup
# Run as Administrator

Write-Host "Setting up target machine for Vertex Central Deployer..." -ForegroundColor Green

# 1. Enable PowerShell Remoting
Write-Host "Enabling PowerShell Remoting..." -ForegroundColor Yellow
Enable-PSRemoting -Force -SkipNetworkProfileCheck
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "*" -Force
Restart-Service WinRM -Force

# 2. Enable WMI Firewall Rules
Write-Host "Configuring WMI firewall rules..." -ForegroundColor Yellow
netsh advfirewall firewall set rule group="Windows Management Instrumentation (WMI)" new enable=yes

# 3. Enable File and Printer Sharing
Write-Host "Enabling administrative shares..." -ForegroundColor Yellow
netsh advfirewall firewall set rule group="File and Printer Sharing" new enable=yes

# 4. Enable WinRM Firewall Rule
Write-Host "Configuring WinRM firewall rules..." -ForegroundColor Yellow
netsh advfirewall firewall add rule name="WinRM-HTTP" dir=in action=allow protocol=TCP localport=5985

# 5. Disable UAC Remote Restrictions (for non-domain environments)
Write-Host "Configuring UAC settings..." -ForegroundColor Yellow
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v LocalAccountTokenFilterPolicy /t REG_DWORD /d 1 /f

# 6. Ensure administrative shares are enabled
Write-Host "Verifying administrative shares..." -ForegroundColor Yellow
reg add "HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters" /v AutoShareWks /t REG_DWORD /d 1 /f
net stop server
net start server

Write-Host "`nSetup complete! This machine is ready for Vertex Central Deployer." -ForegroundColor Green
Write-Host "Available shares:" -ForegroundColor Cyan
net share
```

---

## Getting Started

### Installation

1. **Download the Installer**
   - Location: `WSMDeployerInstaller\bin\VertexCentralDeployer.msi`
   - Size: ~11.4 MB

2. **Run the Installer**
   - Right-click the MSI file → **Run as administrator**
   - Follow the installation wizard
   - Choose installation directory (default: `C:\Program Files\Vertex Central Deployer`)

3. **Launch the Application**
   - Desktop shortcut: **Vertex Central Deployer**
   - Start Menu: **Vertex Central Deployer**

### First Launch

When you first launch Vertex Central Deployer:

1. The application creates its database at:
   ```
   C:\ProgramData\VertexDeployer\Database\deployer.db
   ```

2. You'll see the **Dashboard** with:
   - Total Targets: 0
   - Active Deployments: 0
   - Success Rate: 0%
   - Failed Deployments: 0

---

## Adding Target Machines

### Step 1: Navigate to Targets Page

Click **Targets** in the left sidebar.

### Step 2: Add a New Target

1. Click the **+ Add Target** button in the top-right corner

2. Fill in the target machine details:

   **Hostname or IP Address** (Required)
   - Examples:
     - `192.168.1.100`
     - `WORKSTATION-01`
     - `server.domain.local`

   **Operating System** (Optional)
   - Windows 10, Windows 11, Windows Server 2016, etc.

   **Description** (Optional)
   - Any notes about this machine
   - Example: "Marketing Department - John's PC"

   **Group** (Optional)
   - Organize targets into groups
   - Examples: "Marketing", "IT Department", "Production Servers"

   **Credential Profile** (Optional - Future Feature)
   - Currently uses default credentials
   - Will allow selecting different credential sets per target

3. Click **Add Target**

### Step 3: Verify Target Connectivity

After adding a target, the application will attempt to detect its status:

- **Online** (Green): Target is reachable and ready for deployment
- **Offline** (Gray): Target is not reachable
- **Error** (Red): Connection error occurred
- **Deploying** (Blue): Deployment currently in progress

---

## Deploying Packages

### Step 1: Prepare Your MSI Package

Ensure you have:
- A valid MSI installer file
- The MSI is accessible on the deployer machine
- The MSI doesn't require user interaction (silent install compatible)

### Step 2: Select Targets for Deployment

From the **Targets** page:

**Option A: Deploy to a Single Target**
1. Find the target machine in the list
2. Click the **Deploy** button on the target row

**Option B: Deploy to Multiple Targets**
1. Select checkboxes next to multiple targets
2. Click **Deploy Selected** at the top

### Step 3: Configure Deployment

The deployment dialog appears with these options:

**Select MSI File**
- Click **Browse** to select your MSI installer
- File path will be displayed

**Installation Scope**
- **System-wide (All Users)**: Installs for all users on the machine (default)
- **Per-User**: Installs only for the current user

**Deployment Method**
- **Automatic (Recommended)**: Try PowerShell first, fallback to WMI
- **PowerShell Remoting Only**: Use only PowerShell (fastest)
- **WMI Only**: Use only WMI (more compatible)

**Target Summary**
- Shows: Hostname, OS, Current Status
- Validates: Target is online and ready

### Step 4: Start Deployment

1. Review all settings
2. Click **Deploy Now**
3. Deployment begins immediately

### Step 5: Monitor Progress

The deployment dialog shows real-time progress:

**Status Indicators:**
- **Queued**: Waiting to start
- **In Progress**: Currently deploying
  - Copying MSI to target...
  - Installing package...
  - Verifying installation...
- **Success**: Deployment completed successfully
- **Failed**: Deployment failed (see error message)

**Deployment Steps:**
1. Validating MSI file
2. Establishing connection to target
3. Copying MSI to target machine (`C:\Temp`)
4. Executing remote installation (`msiexec.exe`)
5. Verifying installation
6. Cleaning up temporary files

**Typical Duration:**
- Small MSI (<10 MB): 30-60 seconds
- Medium MSI (10-50 MB): 1-3 minutes
- Large MSI (>50 MB): 3-10 minutes

---

## Monitoring Deployments

### Dashboard Overview

The **Dashboard** page provides at-a-glance metrics:

**Total Targets**
- Number of machines in your deployment list

**Active Deployments**
- Deployments currently in progress

**Success Rate**
- Percentage of successful deployments (last 30 days)

**Failed Deployments**
- Number of failed deployments requiring attention

**Recent Activity**
- Timeline of recent deployments
- Click any deployment to view details

### Deployments History

Navigate to the **Deployments** page to see:

**Deployment List** showing:
- Target hostname
- Deployment method used (PowerShell Remoting / WMI)
- Status (Success / Failed)
- Start time and duration
- Error message (if failed)

**Filtering Options:**
- Filter by status (All / Success / Failed / In Progress)
- Filter by target
- Filter by date range

**Deployment Details:**
Click any deployment to view:
- Complete deployment log
- Method selection process
- Installation output
- Error details (if failed)
- Timestamps for each step

---

## Troubleshooting

### Common Issues and Solutions

#### Issue 1: "Cannot reach target with any deployment method"

**Cause:** Target machine is not configured for remote management.

**Solution:**
1. Verify target machine is online: `ping <hostname>`
2. Run the target setup script (see [Quick Setup Script](#quick-setup-script-for-target-machines))
3. Check firewall rules on target machine
4. Verify credentials are correct

**Test Connectivity:**
```powershell
# From deployer machine, test WinRM
Test-WSMan -ComputerName <target-hostname>

# Test administrative share access
dir \\<target-hostname>\C$
```

---

#### Issue 2: "PowerShell Remoting: Not available or connectivity test failed"

**Cause:** PowerShell Remoting is not enabled on target.

**Solution:**
On target machine, run:
```powershell
Enable-PSRemoting -Force
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "*" -Force
Restart-Service WinRM
```

**Verify:**
```powershell
# From deployer machine
Enter-PSSession -ComputerName <target-hostname> -Credential (Get-Credential)
```

---

#### Issue 3: "WMI: Failed to connect to target via WMI"

**Cause:** WMI/DCOM is blocked or not accessible.

**Solution:**
1. Enable WMI firewall rules on target:
```powershell
netsh advfirewall firewall set rule group="Windows Management Instrumentation (WMI)" new enable=yes
```

2. Verify WMI service is running:
```powershell
Get-Service winmgmt
# Should show: Running
```

3. Test WMI connectivity:
```powershell
# From deployer machine
Get-WmiObject -Class Win32_OperatingSystem -ComputerName <target-hostname>
```

---

#### Issue 4: "Failed to copy MSI: Access denied"

**Cause:** Administrative shares are disabled or blocked.

**Solution:**
1. Enable administrative shares on target:
```powershell
reg add "HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters" /v AutoShareWks /t REG_DWORD /d 1 /f
net stop server && net start server
```

2. Verify shares exist:
```powershell
net share
# Should show: C$, ADMIN$, IPC$
```

3. Test access from deployer:
```powershell
dir \\<target-hostname>\C$
```

---

#### Issue 5: "Installation failed with exit code: 1603"

**Cause:** MSI installation error (generic).

**Solution:**
1. Check the installation log on target machine:
   ```
   C:\Temp\vertex_install.log
   ```

2. Common causes:
   - Insufficient disk space
   - Application already installed
   - Missing dependencies
   - MSI package is corrupted

3. Try manual installation on target to identify issue:
```powershell
msiexec /i C:\path\to\package.msi /l*v C:\install.log
```

**Common Exit Codes:**
- `0`: Success
- `1603`: Fatal error during installation
- `1618`: Another installation is in progress
- `1619`: Package could not be opened
- `1620`: Package cannot be installed
- `3010`: Restart required

---

#### Issue 6: "Access is denied" or "Logon failure"

**Cause:** Credential issues or UAC blocking remote access.

**Solution:**
1. Verify credentials are correct
2. Ensure user is in Administrators group on target
3. Disable UAC remote restrictions (non-domain):
```powershell
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v LocalAccountTokenFilterPolicy /t REG_DWORD /d 1 /f
```

---

#### Issue 7: Target shows "Offline" but is actually running

**Cause:** Connectivity detection timeout or firewall blocking.

**Solution:**
1. Verify ping response: `ping <hostname>`
2. Check DNS resolution: `nslookup <hostname>`
3. Temporarily disable Windows Firewall on target to test
4. Use IP address instead of hostname

---

### Diagnostic Commands

Run these on the **deployer machine** to diagnose issues:

```powershell
# Test PowerShell Remoting
Test-WSMan -ComputerName <target>
Enter-PSSession -ComputerName <target> -Credential (Get-Credential)

# Test WMI
Get-WmiObject -Class Win32_OperatingSystem -ComputerName <target>

# Test administrative share access
Test-Path "\\<target>\C$"
dir \\<target>\C$

# Test specific ports
Test-NetConnection -ComputerName <target> -Port 5985  # WinRM
Test-NetConnection -ComputerName <target> -Port 135   # WMI
Test-NetConnection -ComputerName <target> -Port 445   # SMB

# View deployment logs (in database)
# Check: C:\ProgramData\VertexDeployer\Database\deployer.db
```

---

## Advanced Configuration

### Changing Theme

1. Navigate to **Dashboard**
2. Click the **theme selector** in the top-right corner
3. Choose from:
   - **System (Auto)**: Follows your Windows theme
   - **Light**: Claude.ai-inspired cream background
   - **Dark**: Dark background with orange accents

Theme preference is saved and persists across sessions.

---

### Database Location

Vertex Central Deployer stores all data in:
```
C:\ProgramData\VertexDeployer\Database\deployer.db
```

**Database Contents:**
- Target machine list
- Deployment history
- Deployment logs
- Configuration settings

**Backup:**
```powershell
# Stop the application first, then copy
Copy-Item "C:\ProgramData\VertexDeployer\Database\deployer.db" "C:\Backup\deployer_backup.db"
```

**Restore:**
```powershell
# Stop the application, replace database, restart
Copy-Item "C:\Backup\deployer_backup.db" "C:\ProgramData\VertexDeployer\Database\deployer.db"
```

---

### Logs Location

Deployment logs are stored in the database. To export logs:

1. Navigate to **Deployments** page
2. Click on a deployment
3. View full log output
4. Copy log text for external analysis

---

### Credential Management

**Current Implementation:**
- Default credentials stored securely with Windows DPAPI
- Located in application settings

**Future Feature:**
- Credential profiles for different target groups
- Separate credentials per target
- Integration with Windows Credential Manager

---

### Uninstallation

To remove Vertex Central Deployer:

**Method 1: Control Panel**
1. Open **Settings** → **Apps** → **Installed apps**
2. Find **Vertex Central Deployer**
3. Click **Uninstall**

**Method 2: Command Line**
```powershell
# Find product code
Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "*Vertex*" }

# Uninstall
msiexec /x VertexCentralDeployer.msi /quiet
```

**Cleanup:**
The uninstaller removes:
- Application files from `Program Files`
- Database from `ProgramData\VertexDeployer`
- Desktop and Start Menu shortcuts
- Registry entries

---

## Best Practices

### Security
- Use dedicated service accounts with minimum required permissions
- Regularly rotate credentials
- Deploy only trusted MSI packages
- Test deployments on a small group before mass deployment

### Network
- Use wired connections for deployer machine
- Ensure adequate bandwidth for large MSI files
- Deploy during maintenance windows for production servers

### Organization
- Use target groups to organize machines
- Add descriptions to targets for easy identification
- Review deployment logs regularly

### Testing
- Always test MSI packages manually before mass deployment
- Validate target prerequisites before adding to deployer
- Start with a single test target before deploying to all

---

## Support and Troubleshooting

For additional help:
- Check deployment logs in the **Deployments** page
- Review target machine setup with the quick setup script
- Verify network connectivity and firewall rules
- Consult Windows Event Viewer on target machines

---

**Version:** 1.0.0
**Last Updated:** 2025-11-21
**Application:** Vertex Central Deployer
