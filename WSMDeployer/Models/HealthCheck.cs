using System;

namespace WSMDeployer.Models
{
    public class HealthCheck
    {
        public int Id { get; set; }
        public int TargetId { get; set; }
        public string ServiceStatus { get; set; } = "Unknown"; // 'Running', 'Stopped', 'Unknown'
        public double? CPUUsage { get; set; }
        public int? MemoryUsageMB { get; set; }
        public int? DiskSpaceGB { get; set; }
        public string? ConfigHash { get; set; } // MD5 hash of current config
        public DateTime CheckTime { get; set; }

        // Navigation property
        public Target? Target { get; set; }

        // Computed property
        public HealthStatus OverallStatus
        {
            get
            {
                if (ServiceStatus == "Running")
                    return HealthStatus.Healthy;
                else if (ServiceStatus == "Stopped")
                    return HealthStatus.Critical;
                else
                    return HealthStatus.Unknown;
            }
        }
    }

    public enum HealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Unknown
    }

    public class SystemMetrics
    {
        public double CPU { get; set; }
        public int Memory { get; set; }
        public int Disk { get; set; }
    }
}
