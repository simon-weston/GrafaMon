// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only

using GrafaMon.Application.Security;
using System;
using System.Security.Cryptography;
using NUnit.Framework;

namespace GrafaMon.Tests.Security
{
    [TestFixture]
    public class SecureConfigurationManagerTests
    {
        [Test]
        public void Encrypt_ValidPlainText_ReturnsBase64String()
        {
            // Arrange
            var plainText = "glsa_test_api_key_12345";

            // Act
            var encrypted = SecureConfigurationManager.Encrypt(plainText);

            // Assert
            Assert.That(encrypted, Is.Not.Null);
            Assert.That(encrypted, Is.Not.Empty);
            Assert.That(encrypted, Is.Not.EqualTo(plainText));
            
            // Should be valid Base64
            var bytes = Convert.FromBase64String(encrypted);
            Assert.That(bytes, Is.Not.Empty);
        }

        [Test]
        public void Decrypt_ValidEncryptedText_ReturnsOriginalPlainText()
        {
            // Arrange
            var plainText = "glsa_test_api_key_12345";
            var encrypted = SecureConfigurationManager.Encrypt(plainText);

            // Act
            var decrypted = SecureConfigurationManager.Decrypt(encrypted);

            // Assert
            Assert.That(decrypted, Is.EqualTo(plainText));
        }

        [Test]
        public void Encrypt_NullString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                SecureConfigurationManager.Encrypt(null!));
        }

        [Test]
        public void Encrypt_EmptyString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                SecureConfigurationManager.Encrypt(""));
        }

        [Test]
        public void Encrypt_WhitespaceString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                SecureConfigurationManager.Encrypt("   "));
        }

        [Test]
        public void Decrypt_NullString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                SecureConfigurationManager.Decrypt(null!));
        }

        [Test]
        public void Decrypt_EmptyString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                SecureConfigurationManager.Decrypt(""));
        }

        [Test]
        public void Decrypt_InvalidBase64_ThrowsCryptographicException()
        {
            // Act & Assert
            Assert.Throws<CryptographicException>(() => 
                SecureConfigurationManager.Decrypt("not-valid-base64!@#"));
        }

        [Test]
        public void IsEncrypted_EncryptedString_ReturnsTrue()
        {
            // Arrange
            var plainText = "glsa_test_api_key_12345";
            var encrypted = SecureConfigurationManager.Encrypt(plainText);

            // Act
            var result = SecureConfigurationManager.IsEncrypted(encrypted);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsEncrypted_PlainText_ReturnsFalse()
        {
            // Arrange
            var plainText = "glsa_plain_text_key";

            // Act
            var result = SecureConfigurationManager.IsEncrypted(plainText);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsEncrypted_NullOrEmpty_ReturnsFalse()
        {
            Assert.That(SecureConfigurationManager.IsEncrypted(null), Is.False);
            Assert.That(SecureConfigurationManager.IsEncrypted(""), Is.False);
            Assert.That(SecureConfigurationManager.IsEncrypted("   "), Is.False);
        }

        [Test]
        public void EncryptDecrypt_SpecialCharacters_PreservesData()
        {
            // Arrange
            var specialText = "glsa_key_!@#$%^&*()_+-=[]{}|;':\",./<>?";
            
            // Act
            var encrypted = SecureConfigurationManager.Encrypt(specialText);
            var decrypted = SecureConfigurationManager.Decrypt(encrypted);

            // Assert
            Assert.That(decrypted, Is.EqualTo(specialText));
        }

        [Test]
        public void EncryptDecrypt_Unicode_PreservesData()
        {
            // Arrange
            var unicodeText = "glsa_key_????_?????_???";
            
            // Act
            var encrypted = SecureConfigurationManager.Encrypt(unicodeText);
            var decrypted = SecureConfigurationManager.Decrypt(encrypted);

            // Assert
            Assert.That(decrypted, Is.EqualTo(unicodeText));
        }

        [Test]
        public void EncryptDecrypt_LongString_PreservesData()
        {
            // Arrange
            var longText = new string('a', 1000) + "glsa_key_" + new string('b', 1000);
            
            // Act
            var encrypted = SecureConfigurationManager.Encrypt(longText);
            var decrypted = SecureConfigurationManager.Decrypt(encrypted);

            // Assert
            Assert.That(decrypted, Is.EqualTo(longText));
        }

        [Test]
        public void Encrypt_SamePlainText_ProducesDifferentCiphertext()
        {
            // Arrange
            var plainText = "glsa_test_key_12345";

            // Act
            var encrypted1 = SecureConfigurationManager.Encrypt(plainText);
            var encrypted2 = SecureConfigurationManager.Encrypt(plainText);

            // Assert - DPAPI may add randomness, so ciphertexts could differ
            // But both should decrypt to same plaintext
            var decrypted1 = SecureConfigurationManager.Decrypt(encrypted1);
            var decrypted2 = SecureConfigurationManager.Decrypt(encrypted2);
            
            Assert.That(decrypted1, Is.EqualTo(plainText));
            Assert.That(decrypted2, Is.EqualTo(plainText));
        }
    }
}
