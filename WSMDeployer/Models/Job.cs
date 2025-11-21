using System;

namespace WSMDeployer.Models
{
    public class Job
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public JobType Type { get; set; }
        public JobStatus Status { get; set; }
        public DateTime ScheduledTime { get; set; }
        public TimeSpan? MaintenanceWindowStart { get; set; }
        public TimeSpan? MaintenanceWindowEnd { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;
        public string? TargetIds { get; set; } // JSON array of target IDs
        public string? ConfigData { get; set; } // JSON configuration
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    public enum JobType
    {
        Deployment,
        Update,
        ConfigPush,
        HealthCheck
    }

    public enum JobStatus
    {
        Scheduled,
        Running,
        Completed,
        Failed,
        Cancelled
    }
}
