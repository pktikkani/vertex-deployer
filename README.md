# Vertex Central Deployer

**A comprehensive deployment and management platform for Vertex Security Manager**

Vertex Central Deployer is a powerful, cross-platform tool built to deploy, manage, and monitor Vertex installations across large-scale Windows environments (100+ machines) without requiring existing automation infrastructure.

## Features

### Deployment
- **Multi-Method Deployment Engine**: Automatically selects the best deployment method
  - PowerShell Remoting (WinRM) for domain machines
  - WMI remote execution for all machines
  - PsExec for workgroup machines
  - SMB + Scheduled Task fallback
- **Smart Target Discovery**: Active Directory queries, CSV import, network scanning
- **Pre-deployment Validation**: Check connectivity, credentials, disk space, prerequisites
- **Silent Installation**: Fully automated MSI deployment with configurable parameters

### Management
- **Configuration Templates**: Create, save, and deploy Vertex configurations
- **Remote Configuration Push**: Update settings without reinstalling
- **Service Management**: Start, stop, restart Vertex services remotely
- **Version Tracking**: Monitor installed Vertex versions across all machines
- **Health Monitoring**: Real-time status checks and health dashboards

### Scheduling & Automation
- **Job Queue System**: Queue deployments with priority management
- **Maintenance Windows**: Schedule deployments during off-hours
- **Retry Logic**: Automatic retry on failures with exponential backoff
- **Staggered Rollouts**: Deploy in waves to minimize risk

### Monitoring & Reporting
- **Real-time Dashboard**: Live deployment progress and status
- **Deployment History**: Complete audit trail of all operations
- **Success/Failure Reports**: Detailed logs with error messages
- **Export Capabilities**: CSV, JSON, HTML reports

## Architecture

```
VertexDeployer/
├── Models/              # Data models (Target, Deployment, Job, Config)
├── Services/            # Business logic (Deployment, Database, Scheduler)
├── Views/              # Avalonia UI views
├── ViewModels/         # MVVM view models
├── Database/           # SQLite database context and migrations
├── Styles/             # Modern glossy UI themes
├── Assets/             # Icons, images, resources
└── docs/               # Documentation (this file, history, tech docs, user guide)
```

## Technology Stack

- **.NET 8**: Modern cross-platform framework
- **Avalonia UI 11**: Beautiful, modern desktop UI
- **FluentAvalonia**: Windows 11-style fluent design
- **SQLite**: Embedded database for target and job management
- **PowerShell Remoting**: Primary deployment method for domain machines
- **WMI/CIM**: Universal Windows management interface
- **System.Management**: Native Windows management APIs

## Quick Start

### Prerequisites
- Windows 10/11 or Windows Server 2016+
- .NET 8 Runtime
- Administrative privileges
- Network access to target machines
- Vertex MSI installer file

### Installation

1. Download the latest Vertex Central Deployer MSI from releases
2. Run the installer with administrative privileges
3. Launch Vertex Central Deployer from Start Menu

### First Deployment

1. **Add Targets**: Import machines via CSV or Active Directory query
2. **Configure Credentials**: Set up domain or local admin credentials
3. **Select Targets**: Choose machines to deploy to
4. **Configure Deployment**: Select Vertex MSI and deployment settings
5. **Deploy**: Click deploy and monitor real-time progress

## System Requirements

### Vertex Central Deployer (Control Machine)
- Windows 10/11 or Windows Server 2016+
- .NET 8 Runtime
- 2 GB RAM minimum
- 500 MB disk space
- Network connectivity to target machines

### Target Machines
- Windows 10/11 or Windows Server 2016+
- PowerShell 5.1+ (for PowerShell Remoting)
- WMI enabled
- Firewall rules for remote management
- Administrative credentials

## Network Requirements

### For Domain Environments
- Active Directory access
- WinRM enabled (TCP 5985/5986)
- Firewall rules: SMB (445), WMI (135, dynamic RPC)

### For Workgroup Environments
- Local administrative credentials for each machine
- Network shares accessible
- Firewall rules configured manually

## Documentation

- [User Guide](docs/USER_GUIDE.md) - End-user documentation
- [Technical Documentation](docs/TECHNICAL_DOCS.md) - Developer documentation
- [Development History](docs/HISTORY.md) - Project timeline and changelog

## Building from Source

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or JetBrains Rider
- WiX Toolset 3.11+ (for MSI creation)

### Build Steps

```powershell
# Clone the repository
git clone <repository-url>
cd VertexDeployer

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build -c Release

# Create MSI installer
.\buildmsi.ps1
```

The MSI installer will be created in `bin/Release/`

## Support

For issues, questions, or feature requests, please contact the development team or create an issue in the project repository.

## License

Copyright (c) 2025. All rights reserved.

## Version

Current Version: 1.0.0-alpha
Build Date: 2025-11-21

---

Built with precision for enterprise-scale Vertex deployment.
