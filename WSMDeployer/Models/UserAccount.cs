namespace WSMDeployer.Models
{
    /// <summary>
    /// Represents a user account on a target machine
    /// </summary>
    public class UserAccount
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SID { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsEnabled { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsSelected { get; set; }

        public string DisplayName => string.IsNullOrEmpty(FullName)
            ? Username
            : $"{FullName} ({Username})";
    }
}
