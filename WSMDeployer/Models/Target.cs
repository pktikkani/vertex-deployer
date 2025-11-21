using System;

namespace WSMDeployer.Models
{
    public class Target
    {
        public int Id { get; set; }
        public string Hostname { get; set; } = string.Empty;
        public string? IPAddress { get; set; }
        public TargetType Type { get; set; }
        public TargetStatus Status { get; set; }
        public int? CredentialProfileId { get; set; }
        public string? VertexVersion { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        // Navigation property
        public CredentialProfile? CredentialProfile { get; set; }
    }

    public enum TargetType
    {
        Domain,
        Workgroup
    }

    public enum TargetStatus
    {
        Unknown,
        Online,
        Offline,
        Deploying,
        Error
    }
}
