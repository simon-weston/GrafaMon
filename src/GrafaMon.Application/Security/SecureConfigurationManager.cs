// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Security.Cryptography;
using System.Text;
using GrafaMon.Application.Guards;

namespace GrafaMon.Application.Security
{
    /// <summary>
    /// Provides secure encryption and decryption of sensitive configuration data
    /// using Windows Data Protection API (DPAPI).
    /// </summary>
    public static class SecureConfigurationManager
    {
        /// <summary>
        /// Encrypts a plain-text string using DPAPI (per-user scope).
        /// </summary>
        /// <param name="plainText">The plain-text string to encrypt</param>
        /// <returns>Base64-encoded encrypted string</returns>
        /// <exception cref="ArgumentException">Thrown if plainText is null or empty</exception>
        /// <exception cref="CryptographicException">Thrown if encryption fails</exception>
        public static string Encrypt(string plainText)
        {
            Guard.AgainstNullOrWhiteSpace(plainText, nameof(plainText));

            try
            {
                // Convert plain text to bytes
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

                // Encrypt using DPAPI (CurrentUser scope - machine and user specific)
                var encryptedBytes = ProtectedData.Protect(
                    plainTextBytes,
                    optionalEntropy: null, // No additional entropy - simpler
                    scope: DataProtectionScope.CurrentUser // Per-user encryption
                );

                // Return as Base64 for JSON storage
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException(
                    "Failed to encrypt data using DPAPI. Ensure Windows user profile is not corrupted.", 
                    ex);
            }
        }

        /// <summary>
        /// Decrypts a DPAPI-encrypted Base64 string back to plain text.
        /// </summary>
        /// <param name="encryptedText">Base64-encoded encrypted string</param>
        /// <returns>Decrypted plain-text string</returns>
        /// <exception cref="ArgumentException">Thrown if encryptedText is null or empty</exception>
        /// <exception cref="CryptographicException">Thrown if decryption fails</exception>
        public static string Decrypt(string encryptedText)
        {
            Guard.AgainstNullOrWhiteSpace(encryptedText, nameof(encryptedText));

            try
            {
                // Convert Base64 back to bytes
                var encryptedBytes = Convert.FromBase64String(encryptedText);

                // Decrypt using DPAPI
                var decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    optionalEntropy: null,
                    scope: DataProtectionScope.CurrentUser
                );

                // Convert bytes back to string
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (FormatException ex)
            {
                throw new CryptographicException(
                    "Encrypted text is not valid Base64. The configuration file may be corrupted.", 
                    ex);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException(
                    "Failed to decrypt data using DPAPI. The encrypted key may have been created by a different user or on a different machine.", 
                    ex);
            }
        }

        /// <summary>
        /// Checks if a string appears to be DPAPI-encrypted (Base64 format check).
        /// </summary>
        /// <param name="text">String to check</param>
        /// <returns>True if likely encrypted, false otherwise</returns>
        public static bool IsEncrypted(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // DPAPI encrypted strings are Base64 and typically longer than plain text
            // This is a heuristic check, not cryptographically secure validation
            try
            {
                var bytes = Convert.FromBase64String(text);
                
                // DPAPI encrypted data is typically at least 32 bytes
                // Plain API keys are usually shorter when Base64 decoded
                return bytes.Length >= 32;
            }
            catch (FormatException)
            {
                // Not valid Base64 = definitely not encrypted with our method
                return false;
            }
        }
    }
}
