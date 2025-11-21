using System;

namespace WSMDeployer.Models
{
    public class CredentialProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty; // DPAPI encrypted
        public CredentialType Type { get; set; }
        public DateTime CreatedDate { get; set; }

        // Helper to get decrypted password (not stored in DB)
        public string? DecryptedPassword { get; set; }
    }

    public enum CredentialType
    {
        Domain,
        Local
    }
}
