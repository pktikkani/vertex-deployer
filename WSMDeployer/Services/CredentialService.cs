using System;
using System.Security.Cryptography;
using System.Text;

namespace WSMDeployer.Services
{
    /// <summary>
    /// Service for encrypting and decrypting credentials using Windows DPAPI
    /// Data Protection API ensures credentials are encrypted per-user and per-machine
    /// </summary>
    public static class CredentialService
    {
        /// <summary>
        /// Encrypt a password using Windows DPAPI
        /// The encrypted data can only be decrypted by the same user on the same machine
        /// </summary>
        /// <param name="password">Plain text password to encrypt</param>
        /// <returns>Base64 encoded encrypted password</returns>
        public static string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            try
            {
                // Convert password to bytes
                byte[] data = Encoding.UTF8.GetBytes(password);

                // Encrypt using DPAPI (CurrentUser scope)
                byte[] encryptedData = ProtectedData.Protect(
                    data,
                    null, // No additional entropy
                    DataProtectionScope.CurrentUser);

                // Convert to Base64 for storage
                return Convert.ToBase64String(encryptedData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to encrypt password: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypt a password that was encrypted using DPAPI
        /// Can only be decrypted by the same user on the same machine
        /// </summary>
        /// <param name="encryptedPassword">Base64 encoded encrypted password</param>
        /// <returns>Plain text password</returns>
        public static string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;

            try
            {
                // Convert from Base64
                byte[] encryptedData = Convert.FromBase64String(encryptedPassword);

                // Decrypt using DPAPI
                byte[] data = ProtectedData.Unprotect(
                    encryptedData,
                    null, // No additional entropy
                    DataProtectionScope.CurrentUser);

                // Convert back to string
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to decrypt password: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate that credentials can be encrypted and decrypted
        /// Useful for testing encryption functionality
        /// </summary>
        public static bool TestEncryption(string testPassword)
        {
            try
            {
                var encrypted = EncryptPassword(testPassword);
                var decrypted = DecryptPassword(encrypted);
                return testPassword == decrypted;
            }
            catch
            {
                return false;
            }
        }
    }
}
