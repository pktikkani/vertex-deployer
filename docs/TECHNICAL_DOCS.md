# Vertex Central Deployer - Technical Documentation

**Comprehensive developer documentation for the Vertex Central Deployer codebase**

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Database Schema](#database-schema)
3. [Core Services](#core-services)
4. [Deployment Engine](#deployment-engine)
5. [UI Components](#ui-components)
6. [Configuration System](#configuration-system)
7. [Security](#security)
8. [API Reference](#api-reference)
9. [Build & Deployment](#build--deployment)
10. [Testing Strategy](#testing-strategy)

---

## Architecture Overview

### Technology Stack

```
┌─────────────────────────────────────────────────┐
│           Presentation Layer (Avalonia)         │
│  ┌─────────────┐  ┌──────────────┐            │
│  │    Views    │◄─┤  ViewModels  │            │
│  └─────────────┘  └──────────────┘            │
└─────────────────────────────────────────────────┘
                      │
┌─────────────────────────────────────────────────┐
│              Business Logic Layer               │
│  ┌──────────────┐  ┌──────────────┐           │
│  │  Deployment  │  │  Scheduler   │           │
│  │   Service    │  │   Service    │           │
│  └──────────────┘  └──────────────┘           │
│  ┌──────────────┐  ┌──────────────┐           │
│  │   Config     │  │   Health     │           │
│  │   Service    │  │   Service    │           │
│  └──────────────┘  └──────────────┘           │
└─────────────────────────────────────────────────┘
                      │
┌─────────────────────────────────────────────────┐
│               Data Access Layer                 │
│  ┌──────────────────────────────────────────┐  │
│  │      DatabaseService (SQLite)            │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
                      │
┌─────────────────────────────────────────────────┐
│              External Integrations              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │PowerShell│  │   WMI    │  │  PsExec  │     │
│  └──────────┘  └──────────┘  └──────────┘     │
└─────────────────────────────────────────────────┘
```

### Design Patterns

- **MVVM (Model-View-ViewModel)**: Separation of UI and business logic
- **Repository Pattern**: Abstract data access
- **Strategy Pattern**: Multiple deployment methods
- **Observer Pattern**: Real-time UI updates
- **Factory Pattern**: Target and deployment creation
- **Singleton Pattern**: Service instances

### Project Structure

```
WSMDeployer/
├── Models/
│   ├── Target.cs                 # Target machine model
│   ├── Deployment.cs             # Deployment job model
│   ├── DeploymentStatus.cs       # Status enumeration
│   ├── Job.cs                    # Scheduled job model
│   ├── ConfigurationTemplate.cs  # Config template model
│   ├── CredentialProfile.cs      # Credential model
│   └── HealthCheck.cs            # Health status model
│
├── Services/
│   ├── DatabaseService.cs        # SQLite database operations
│   ├── DeploymentService.cs      # Core deployment logic
│   ├── IDeploymentMethod.cs      # Deployment method interface
│   ├── PowerShellDeployment.cs   # PS Remoting implementation
│   ├── WmiDeployment.cs          # WMI implementation
│   ├── PsExecDeployment.cs       # PsExec implementation
│   ├── SchedulerService.cs       # Job scheduling
│   ├── ConfigurationService.cs   # Config management
│   ├── HealthCheckService.cs     # Health monitoring
│   └── CredentialService.cs      # Credential encryption
│
├── ViewModels/
│   ├── MainWindowViewModel.cs    # Main window VM
│   ├── DashboardViewModel.cs     # Dashboard page VM
│   ├── TargetsViewModel.cs       # Target management VM
│   ├── DeploymentsViewModel.cs   # Deployment page VM
│   ├── MonitoringViewModel.cs    # Health monitoring VM
│   ├── ConfigurationViewModel.cs # Config templates VM
│   └── SettingsViewModel.cs      # Settings page VM
│
├── Views/
│   ├── MainWindow.axaml          # Main application window
│   ├── DashboardView.axaml       # Dashboard page
│   ├── TargetsView.axaml         # Target management
│   ├── DeploymentsView.axaml     # Deployment page
│   ├── MonitoringView.axaml      # Health monitoring
│   ├── ConfigurationView.axaml   # Config templates
│   └── SettingsView.axaml        # Settings page
│
├── Database/
│   └── DatabaseContext.cs        # SQLite context
│
├── Styles/
│   ├── GlossyTheme.axaml         # Modern glossy theme
│   └── Colors.axaml              # Color definitions
│
└── Assets/
    └── Icons/                     # Application icons
```

---

## Database Schema

### SQLite Tables

#### Targets Table
```sql
CREATE TABLE Targets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Hostname TEXT NOT NULL,
    IPAddress TEXT,
    Type TEXT NOT NULL,           -- 'Domain' or 'Workgroup'
    Status TEXT NOT NULL,         -- 'Online', 'Offline', 'Unknown'
    CredentialProfileId INTEGER,
    VertexVersion TEXT,
    LastSeen DATETIME,
    CreatedDate DATETIME NOT NULL,
    ModifiedDate DATETIME NOT NULL,
    FOREIGN KEY (CredentialProfileId) REFERENCES CredentialProfiles(Id)
);

CREATE INDEX IX_Targets_Hostname ON Targets(Hostname);
CREATE INDEX IX_Targets_Status ON Targets(Status);
```

#### Deployments Table
```sql
CREATE TABLE Deployments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TargetId INTEGER NOT NULL,
    Status TEXT NOT NULL,         -- 'Queued', 'InProgress', 'Success', 'Failed'
    Method TEXT,                  -- 'PowerShell', 'WMI', 'PsExec', 'SMB'
    StartTime DATETIME,
    EndTime DATETIME,
    ErrorMessage TEXT,
    Log TEXT,
    ConfigurationTemplateId INTEGER,
    CreatedDate DATETIME NOT NULL,
    FOREIGN KEY (TargetId) REFERENCES Targets(Id),
    FOREIGN KEY (ConfigurationTemplateId) REFERENCES ConfigurationTemplates(Id)
);

CREATE INDEX IX_Deployments_Status ON Deployments(Status);
CREATE INDEX IX_Deployments_TargetId ON Deployments(TargetId);
```

#### Jobs Table
```sql
CREATE TABLE Jobs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Type TEXT NOT NULL,           -- 'Deployment', 'Update', 'ConfigPush'
    Status TEXT NOT NULL,         -- 'Scheduled', 'Running', 'Completed', 'Failed'
    ScheduledTime DATETIME NOT NULL,
    MaintenanceWindowStart TIME,
    MaintenanceWindowEnd TIME,
    RetryCount INTEGER DEFAULT 0,
    MaxRetries INTEGER DEFAULT 3,
    TargetIds TEXT,               -- JSON array of target IDs
    ConfigData TEXT,              -- JSON configuration
    CreatedDate DATETIME NOT NULL,
    ModifiedDate DATETIME NOT NULL
);

CREATE INDEX IX_Jobs_Status ON Jobs(Status);
CREATE INDEX IX_Jobs_ScheduledTime ON Jobs(ScheduledTime);
```

#### ConfigurationTemplates Table
```sql
CREATE TABLE ConfigurationTemplates (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Description TEXT,
    ConfigJson TEXT NOT NULL,     -- Full Vertex configuration as JSON
    CreatedDate DATETIME NOT NULL,
    ModifiedDate DATETIME NOT NULL
);
```

#### CredentialProfiles Table
```sql
CREATE TABLE CredentialProfiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Username TEXT NOT NULL,
    EncryptedPassword TEXT NOT NULL,  -- DPAPI encrypted
    Type TEXT NOT NULL,           -- 'Domain' or 'Local'
    CreatedDate DATETIME NOT NULL
);
```

#### HealthChecks Table
```sql
CREATE TABLE HealthChecks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TargetId INTEGER NOT NULL,
    ServiceStatus TEXT NOT NULL,  -- 'Running', 'Stopped', 'Unknown'
    CPUUsage REAL,
    MemoryUsageMB INTEGER,
    DiskSpaceGB INTEGER,
    ConfigHash TEXT,              -- MD5 hash of current config
    CheckTime DATETIME NOT NULL,
    FOREIGN KEY (TargetId) REFERENCES Targets(Id)
);

CREATE INDEX IX_HealthChecks_TargetId ON HealthChecks(TargetId);
CREATE INDEX IX_HealthChecks_CheckTime ON HealthChecks(CheckTime);
```

---

## Core Services

### DatabaseService.cs

**Purpose**: Handles all SQLite database operations

**Key Methods**:

```csharp
public class DatabaseService
{
    private readonly string _connectionString;

    // Initialize database connection
    public DatabaseService(string dbPath)

    // Initialize database schema
    public void InitializeDatabase()

    // Target operations
    public List<Target> GetAllTargets()
    public Target GetTargetById(int id)
    public int AddTarget(Target target)
    public void UpdateTarget(Target target)
    public void DeleteTarget(int id)
    public List<Target> SearchTargets(string query)

    // Deployment operations
    public int CreateDeployment(Deployment deployment)
    public void UpdateDeploymentStatus(int id, DeploymentStatus status)
    public void UpdateDeploymentLog(int id, string log)
    public List<Deployment> GetDeploymentsByStatus(DeploymentStatus status)
    public List<Deployment> GetDeploymentsForTarget(int targetId)

    // Job operations
    public int CreateJob(Job job)
    public List<Job> GetScheduledJobs()
    public void UpdateJobStatus(int id, JobStatus status)
    public Job GetNextScheduledJob()

    // Configuration operations
    public int SaveConfigurationTemplate(ConfigurationTemplate template)
    public ConfigurationTemplate GetConfigurationTemplate(int id)
    public List<ConfigurationTemplate> GetAllConfigurationTemplates()

    // Credential operations
    public int SaveCredentialProfile(CredentialProfile profile)
    public CredentialProfile GetCredentialProfile(int id)
    public List<CredentialProfile> GetAllCredentialProfiles()

    // Health check operations
    public void SaveHealthCheck(HealthCheck check)
    public HealthCheck GetLatestHealthCheck(int targetId)
    public List<HealthCheck> GetHealthHistory(int targetId, DateTime since)
}
```

**Implementation Details**:
- Uses `Microsoft.Data.Sqlite` for database access
- Connection pooling enabled for performance
- All write operations wrapped in transactions
- Parameterized queries to prevent SQL injection
- Automatic schema migration on version changes

---

### DeploymentService.cs

**Purpose**: Orchestrates the deployment process

**Key Methods**:

```csharp
public class DeploymentService
{
    private readonly DatabaseService _db;
    private readonly List<IDeploymentMethod> _deploymentMethods;

    // Constructor: Initialize deployment methods in priority order
    public DeploymentService(DatabaseService db)

    // Deploy to a single target
    public async Task<DeploymentResult> DeployToTarget(
        Target target,
        string msiPath,
        ConfigurationTemplate config = null)

    // Deploy to multiple targets (parallel)
    public async Task<List<DeploymentResult>> DeployToMultipleTargets(
        List<Target> targets,
        string msiPath,
        ConfigurationTemplate config = null,
        int maxParallel = 10)

    // Select best deployment method for target
    private IDeploymentMethod SelectDeploymentMethod(Target target)

    // Pre-deployment checks
    private async Task<bool> PreflightCheck(Target target)

    // Post-deployment verification
    private async Task<bool> VerifyDeployment(Target target)

    // Handle deployment failure
    private async Task HandleDeploymentFailure(
        Deployment deployment,
        Exception ex)
}
```

**Deployment Flow**:

```
1. PreflightCheck(target)
   ├─ Ping target
   ├─ Check credentials
   ├─ Verify disk space
   └─ Check prerequisites

2. SelectDeploymentMethod(target)
   ├─ Try PowerShell Remoting
   ├─ Fallback to WMI
   ├─ Fallback to PsExec
   └─ Last resort: SMB + Task

3. CopyMSI(target, msiPath)
   ├─ Create temp folder
   ├─ Copy MSI to target
   └─ Handle copy errors

4. ExecuteInstaller(target, msiPath)
   ├─ Run MSI silently
   ├─ Monitor progress
   └─ Capture output

5. DeployConfiguration(target, config)
   ├─ Serialize config to JSON
   ├─ Copy to Vertex config folder
   └─ Reload Vertex service

6. VerifyDeployment(target)
   ├─ Check service status
   ├─ Verify version
   └─ Test functionality

7. Cleanup(target)
   ├─ Delete temp files
   └─ Update database
```

---

### Deployment Methods

#### IDeploymentMethod.cs

**Purpose**: Interface for different deployment strategies

```csharp
public interface IDeploymentMethod
{
    string MethodName { get; }
    int Priority { get; }  // Lower = higher priority

    // Check if this method can be used for target
    Task<bool> CanDeploy(Target target);

    // Execute deployment
    Task<DeploymentResult> Deploy(
        Target target,
        string msiPath,
        string arguments);

    // Copy file to target
    Task<bool> CopyFile(
        Target target,
        string sourcePath,
        string destPath);
}
```

#### PowerShellDeployment.cs

**Purpose**: Deploy using PowerShell Remoting (WinRM)

**Implementation**:

```csharp
public class PowerShellDeployment : IDeploymentMethod
{
    public string MethodName => "PowerShell Remoting";
    public int Priority => 1;  // Highest priority

    public async Task<bool> CanDeploy(Target target)
    {
        // Test WinRM connectivity
        using (var ps = PowerShell.Create())
        {
            ps.AddCommand("Test-WSMan")
              .AddParameter("ComputerName", target.Hostname);

            try
            {
                var result = await ps.InvokeAsync();
                return result.Any();
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task<DeploymentResult> Deploy(
        Target target,
        string msiPath,
        string arguments)
    {
        var credential = GetCredential(target);

        using (var runspace = RunspaceFactory.CreateRunspace())
        {
            runspace.Open();

            using (var ps = PowerShell.Create())
            {
                ps.Runspace = runspace;

                // Copy MSI to target
                ps.AddCommand("Copy-Item")
                  .AddParameter("Path", msiPath)
                  .AddParameter("Destination", $"\\\\{target.Hostname}\\C$\\Temp\\")
                  .AddParameter("Credential", credential);

                await ps.InvokeAsync();
                ps.Commands.Clear();

                // Install MSI remotely
                ps.AddCommand("Invoke-Command")
                  .AddParameter("ComputerName", target.Hostname)
                  .AddParameter("Credential", credential)
                  .AddParameter("ScriptBlock", ScriptBlock.Create(@"
                      param($msiPath, $args)
                      Start-Process msiexec.exe -ArgumentList ""/i $msiPath $args /quiet /norestart"" -Wait -NoNewWindow
                  "))
                  .AddParameter("ArgumentList", new object[] {
                      $"C:\\Temp\\{Path.GetFileName(msiPath)}",
                      arguments
                  });

                var results = await ps.InvokeAsync();

                return new DeploymentResult
                {
                    Success = !ps.HadErrors,
                    Method = MethodName,
                    Output = string.Join("\n", results.Select(r => r.ToString())),
                    Error = ps.HadErrors ? string.Join("\n", ps.Streams.Error) : null
                };
            }
        }
    }
}
```

**Requirements**:
- WinRM enabled on target (`Enable-PSRemoting`)
- Firewall allows TCP 5985/5986
- Valid credentials with admin rights

#### WmiDeployment.cs

**Purpose**: Deploy using WMI (Windows Management Instrumentation)

**Implementation**:

```csharp
public class WmiDeployment : IDeploymentMethod
{
    public string MethodName => "WMI";
    public int Priority => 2;

    public async Task<bool> CanDeploy(Target target)
    {
        try
        {
            var options = new ConnectionOptions
            {
                Username = target.CredentialProfile.Username,
                Password = target.CredentialProfile.DecryptedPassword,
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy
            };

            var scope = new ManagementScope(
                $"\\\\{target.Hostname}\\root\\cimv2", options);

            scope.Connect();
            return scope.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    public async Task<DeploymentResult> Deploy(
        Target target,
        string msiPath,
        string arguments)
    {
        var options = new ConnectionOptions
        {
            Username = target.CredentialProfile.Username,
            Password = target.CredentialProfile.DecryptedPassword
        };

        var scope = new ManagementScope(
            $"\\\\{target.Hostname}\\root\\cimv2", options);

        scope.Connect();

        // Copy MSI via SMB
        var remotePath = $"\\\\{target.Hostname}\\C$\\Temp\\{Path.GetFileName(msiPath)}";
        File.Copy(msiPath, remotePath, true);

        // Execute installer via WMI
        using (var processClass = new ManagementClass(scope,
            new ManagementPath("Win32_Process"), null))
        {
            var inParams = processClass.GetMethodParameters("Create");
            inParams["CommandLine"] = $"msiexec.exe /i C:\\Temp\\{Path.GetFileName(msiPath)} {arguments} /quiet /norestart";

            var outParams = processClass.InvokeMethod("Create", inParams, null);

            var returnValue = (uint)outParams["returnValue"];
            var processId = (uint)outParams["processId"];

            if (returnValue == 0)
            {
                // Wait for process to complete
                await WaitForProcess(scope, processId);

                return new DeploymentResult
                {
                    Success = true,
                    Method = MethodName
                };
            }
            else
            {
                return new DeploymentResult
                {
                    Success = false,
                    Method = MethodName,
                    Error = $"WMI Create process failed with return value: {returnValue}"
                };
            }
        }
    }

    private async Task WaitForProcess(ManagementScope scope, uint processId)
    {
        while (true)
        {
            var query = new ObjectQuery(
                $"SELECT * FROM Win32_Process WHERE ProcessId = {processId}");

            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                var processes = searcher.Get();
                if (processes.Count == 0)
                    break;  // Process completed
            }

            await Task.Delay(5000);  // Check every 5 seconds
        }
    }
}
```

**Requirements**:
- WMI service running on target
- Firewall allows TCP 135 and dynamic RPC ports
- Admin credentials

---

### SchedulerService.cs

**Purpose**: Manage scheduled deployment jobs

**Key Methods**:

```csharp
public class SchedulerService
{
    private readonly DatabaseService _db;
    private readonly DeploymentService _deploymentService;
    private readonly Timer _timer;

    // Start scheduler
    public void Start()
    {
        _timer = new Timer(CheckScheduledJobs, null,
            TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    // Check for jobs due to run
    private async void CheckScheduledJobs(object state)
    {
        var dueJobs = _db.GetScheduledJobs()
            .Where(j => j.ScheduledTime <= DateTime.Now
                     && j.Status == JobStatus.Scheduled)
            .ToList();

        foreach (var job in dueJobs)
        {
            if (IsInMaintenanceWindow(job))
            {
                await ExecuteJob(job);
            }
        }
    }

    // Execute a scheduled job
    private async Task ExecuteJob(Job job)
    {
        _db.UpdateJobStatus(job.Id, JobStatus.Running);

        try
        {
            var targets = GetTargetsForJob(job);
            var results = await _deploymentService.DeployToMultipleTargets(
                targets, job.MsiPath);

            var successCount = results.Count(r => r.Success);
            var failCount = results.Count - successCount;

            if (failCount > 0 && job.RetryCount < job.MaxRetries)
            {
                // Retry failed targets
                job.RetryCount++;
                job.ScheduledTime = DateTime.Now.AddMinutes(30);
                _db.UpdateJob(job);
            }
            else
            {
                _db.UpdateJobStatus(job.Id, JobStatus.Completed);
            }
        }
        catch (Exception ex)
        {
            _db.UpdateJobStatus(job.Id, JobStatus.Failed);
            // Log error
        }
    }

    // Check if current time is within maintenance window
    private bool IsInMaintenanceWindow(Job job)
    {
        if (job.MaintenanceWindowStart == null)
            return true;

        var now = DateTime.Now.TimeOfDay;
        return now >= job.MaintenanceWindowStart
            && now <= job.MaintenanceWindowEnd;
    }
}
```

---

### ConfigurationService.cs

**Purpose**: Manage Vertex configuration templates and deployment

**Key Methods**:

```csharp
public class ConfigurationService
{
    private readonly DatabaseService _db;

    // Create configuration template
    public int CreateTemplate(string name, VertexConfig config)
    {
        var template = new ConfigurationTemplate
        {
            Name = name,
            ConfigJson = JsonConvert.SerializeObject(config),
            CreatedDate = DateTime.Now
        };

        return _db.SaveConfigurationTemplate(template);
    }

    // Deploy configuration to target
    public async Task<bool> DeployConfiguration(
        Target target,
        ConfigurationTemplate template)
    {
        var config = JsonConvert.DeserializeObject<VertexConfig>(
            template.ConfigJson);

        // Copy config to target
        var remotePath = $"\\\\{target.Hostname}\\C$\\ProgramData\\Vertex\\config.json";
        File.WriteAllText(remotePath, template.ConfigJson);

        // Restart Vertex service to apply config
        await RestartVertexService(target);

        return true;
    }

    // Export template to file
    public void ExportTemplate(ConfigurationTemplate template, string filePath)
    {
        File.WriteAllText(filePath, template.ConfigJson);
    }

    // Import template from file
    public int ImportTemplate(string name, string filePath)
    {
        var json = File.ReadAllText(filePath);
        var config = JsonConvert.DeserializeObject<VertexConfig>(json);
        return CreateTemplate(name, config);
    }
}
```

---

### HealthCheckService.cs

**Purpose**: Monitor health of deployed Vertex instances

**Key Methods**:

```csharp
public class HealthCheckService
{
    private readonly DatabaseService _db;
    private readonly Timer _timer;

    // Start health monitoring
    public void Start(TimeSpan interval)
    {
        _timer = new Timer(PerformHealthChecks, null,
            TimeSpan.Zero, interval);
    }

    // Perform health checks on all targets
    private async void PerformHealthChecks(object state)
    {
        var targets = _db.GetAllTargets();

        var tasks = targets.Select(CheckTargetHealth);
        await Task.WhenAll(tasks);
    }

    // Check health of single target
    private async Task CheckTargetHealth(Target target)
    {
        try
        {
            var check = new HealthCheck
            {
                TargetId = target.Id,
                CheckTime = DateTime.Now
            };

            // Check service status
            check.ServiceStatus = await GetServiceStatus(target);

            // Get system metrics
            var metrics = await GetSystemMetrics(target);
            check.CPUUsage = metrics.CPU;
            check.MemoryUsageMB = metrics.Memory;
            check.DiskSpaceGB = metrics.Disk;

            // Get config hash
            check.ConfigHash = await GetConfigHash(target);

            _db.SaveHealthCheck(check);

            // Update target status
            target.Status = check.ServiceStatus == "Running"
                ? TargetStatus.Online
                : TargetStatus.Offline;
            target.LastSeen = DateTime.Now;
            _db.UpdateTarget(target);
        }
        catch (Exception ex)
        {
            // Mark target as unknown
            target.Status = TargetStatus.Unknown;
            _db.UpdateTarget(target);
        }
    }

    // Get service status via WMI
    private async Task<string> GetServiceStatus(Target target)
    {
        var options = new ConnectionOptions
        {
            Username = target.CredentialProfile.Username,
            Password = target.CredentialProfile.DecryptedPassword
        };

        var scope = new ManagementScope(
            $"\\\\{target.Hostname}\\root\\cimv2", options);

        scope.Connect();

        var query = new ObjectQuery(
            "SELECT * FROM Win32_Service WHERE Name = 'VertexService'");

        using (var searcher = new ManagementObjectSearcher(scope, query))
        {
            foreach (ManagementObject service in searcher.Get())
            {
                return service["State"].ToString();
            }
        }

        return "Unknown";
    }
}
```

---

## UI Components

### MVVM Pattern Implementation

All ViewModels inherit from `ViewModelBase`:

```csharp
public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this,
            new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value,
        [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

### DashboardViewModel.cs

**Purpose**: Main dashboard with metrics and activity

```csharp
public class DashboardViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly Timer _refreshTimer;

    // Observable properties
    public int TotalTargets { get; set; }
    public int ActiveDeployments { get; set; }
    public double SuccessRate { get; set; }
    public int HealthyTargets { get; set; }

    public ObservableCollection<Deployment> RecentDeployments { get; set; }
    public ObservableCollection<HealthAlert> Alerts { get; set; }

    // Commands
    public ICommand DeployNowCommand { get; }
    public ICommand AddTargetsCommand { get; }
    public ICommand ViewReportsCommand { get; }

    // Constructor
    public DashboardViewModel(DatabaseService db)
    {
        _db = db;

        DeployNowCommand = new RelayCommand(OnDeployNow);
        AddTargetsCommand = new RelayCommand(OnAddTargets);
        ViewReportsCommand = new RelayCommand(OnViewReports);

        // Refresh every 30 seconds
        _refreshTimer = new Timer(RefreshData, null,
            TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    // Refresh dashboard data
    private async void RefreshData(object state)
    {
        TotalTargets = _db.GetAllTargets().Count;

        ActiveDeployments = _db.GetDeploymentsByStatus(
            DeploymentStatus.InProgress).Count;

        // Calculate success rate (last 30 days)
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        var recentDeployments = _db.GetDeploymentsForTarget(0)
            .Where(d => d.CreatedDate >= thirtyDaysAgo);

        var successCount = recentDeployments
            .Count(d => d.Status == DeploymentStatus.Success);

        SuccessRate = recentDeployments.Any()
            ? (double)successCount / recentDeployments.Count() * 100
            : 0;

        // Get healthy targets
        HealthyTargets = _db.GetAllTargets()
            .Count(t => t.Status == TargetStatus.Online);

        // Recent activity
        RecentDeployments.Clear();
        var recent = _db.GetDeploymentsByStatus(DeploymentStatus.Success)
            .OrderByDescending(d => d.EndTime)
            .Take(10);

        foreach (var deployment in recent)
        {
            RecentDeployments.Add(deployment);
        }
    }
}
```

---

## Configuration System

### VertexConfig Model

```csharp
public class VertexConfig
{
    // Restrictions
    public RestrictionsConfig Restrictions { get; set; }

    // Applications
    public ApplicationsConfig Applications { get; set; }

    // Compliance
    public ComplianceConfig Compliance { get; set; }

    // Logging
    public LoggingConfig Logging { get; set; }
}

public class RestrictionsConfig
{
    public bool BlockTaskManager { get; set; }
    public bool BlockControlPanel { get; set; }
    public bool BlockRegistry { get; set; }
    public bool BlockCMD { get; set; }
    public bool BlockFileExplorer { get; set; }
    public bool BlockDragDrop { get; set; }
    public bool BlockAltKey { get; set; }
    public bool BlockActionBar { get; set; }
    // ... more restrictions
}
```

---

## Security

### Credential Encryption

Uses Windows DPAPI (Data Protection API) for secure credential storage:

```csharp
public class CredentialService
{
    // Encrypt password using DPAPI
    public static string EncryptPassword(string password)
    {
        var data = Encoding.UTF8.GetBytes(password);
        var encrypted = ProtectedData.Protect(data,
            null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    // Decrypt password
    public static string DecryptPassword(string encryptedPassword)
    {
        var data = Convert.FromBase64String(encryptedPassword);
        var decrypted = ProtectedData.Unprotect(data,
            null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }
}
```

**Security Notes**:
- Credentials are encrypted per-user using DPAPI
- Database file permissions restricted to admin only
- No credentials in logs or error messages
- Secure communication over encrypted channels (HTTPS, encrypted PowerShell)

---

## Build & Deployment

### Build Configuration

```xml
<!-- WSMDeployer.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <ApplicationIcon>Assets\icon.ico</ApplicationIcon>
    <Version>1.0.0</Version>
    <Authors>Your Team</Authors>
    <Product>Vertex Central Deployer</Product>
  </PropertyGroup>
</Project>
```

### buildmsi.ps1

**(To be implemented in next phase)**

---

## Testing Strategy

### Unit Tests

Test individual components in isolation:
- DatabaseService CRUD operations
- Deployment method selection logic
- Configuration serialization/deserialization
- Credential encryption/decryption

### Integration Tests

Test component interactions:
- End-to-end deployment workflow
- Database transactions
- Service communication

### Manual Testing

Test on real environments:
- Domain and workgroup machines
- Different Windows versions
- Firewall configurations
- Network conditions

---

This technical documentation will be updated as features are implemented and the codebase evolves.

**Last Updated**: 2025-11-21
**Document Version**: 1.0.0-alpha
