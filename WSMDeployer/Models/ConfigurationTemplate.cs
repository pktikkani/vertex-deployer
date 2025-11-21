using System;

namespace WSMDeployer.Models
{
    public class ConfigurationTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ConfigJson { get; set; } = string.Empty; // Full Vertex configuration as JSON
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    // Represents the Vertex configuration structure
    public class VertexConfig
    {
        public RestrictionsConfig? Restrictions { get; set; }
        public ApplicationsConfig? Applications { get; set; }
        public ComplianceConfig? Compliance { get; set; }
        public LoggingConfig? Logging { get; set; }
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
        public bool BlockRightClick { get; set; }
        public bool BlockSystemShortcuts { get; set; }
    }

    public class ApplicationsConfig
    {
        public string[]? AllowedApps { get; set; }
        public string[]? BlockedApps { get; set; }
    }

    public class ComplianceConfig
    {
        public bool GDPR { get; set; }
        public bool HIPAA { get; set; }
        public bool SOC2 { get; set; }
        public bool ISO27001 { get; set; }
    }

    public class LoggingConfig
    {
        public string? LogLevel { get; set; }
        public int RetentionDays { get; set; }
        public bool EnableRemoteLogging { get; set; }
    }
}
