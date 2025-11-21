using System;

namespace WSMDeployer.Models
{
    public class Deployment
    {
        public int Id { get; set; }
        public int TargetId { get; set; }
        public DeploymentStatus Status { get; set; }
        public string? Method { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Log { get; set; }
        public int? ConfigurationTemplateId { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public Target? Target { get; set; }
        public ConfigurationTemplate? ConfigurationTemplate { get; set; }

        // Computed properties
        public TimeSpan? Duration => EndTime.HasValue && StartTime.HasValue
            ? EndTime.Value - StartTime.Value
            : null;

        public string StatusDisplay => Status switch
        {
            DeploymentStatus.Queued => "Queued",
            DeploymentStatus.InProgress => "In Progress",
            DeploymentStatus.Success => "Success",
            DeploymentStatus.Failed => "Failed",
            _ => "Unknown"
        };
    }

    public enum DeploymentStatus
    {
        Queued,
        InProgress,
        Success,
        Failed
    }

    public class DeploymentResult
    {
        public bool Success { get; set; }
        public string? Method { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
    }
}
