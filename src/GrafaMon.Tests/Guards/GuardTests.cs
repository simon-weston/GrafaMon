// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using GrafaMon.Application.Guards;
using NUnit.Framework;

namespace GrafaMon.Tests.Guards
{
    [TestFixture]
    public class GuardTests
    {
        #region AgainstNull Tests

        [Test]
        public void AgainstNull_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange
            string? nullValue = null;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(nullValue));
            Assert.That(ex.ParamName, Is.EqualTo("nullValue"));
        }

        [Test]
        public void AgainstNull_WithNonNullValue_DoesNotThrow()
        {
            // Arrange
            string nonNullValue = "test";

            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstNull(nonNullValue));
        }

        #endregion

        #region AgainstNullOrWhiteSpace Tests

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("   ")]
        [TestCase("\t")]
        [TestCase("\n")]
        public void AgainstNullOrWhiteSpace_WithInvalidValue_ThrowsArgumentException(string? value)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace(value));
            Assert.That(ex.Message, Does.Contain("cannot be null, empty, or whitespace"));
        }

        [Test]
        [TestCase("valid")]
        [TestCase("a")]
        [TestCase("  text  ")]
        public void AgainstNullOrWhiteSpace_WithValidValue_DoesNotThrow(string value)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstNullOrWhiteSpace(value));
        }

        #endregion

        #region AgainstNullOrEmpty Tests

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void AgainstNullOrEmpty_WithInvalidValue_ThrowsArgumentException(string? value)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(value));
            Assert.That(ex.Message, Does.Contain("cannot be null or empty"));
        }

        [Test]
        [TestCase("valid")]
        [TestCase(" ")]
        [TestCase("  ")]
        public void AgainstNullOrEmpty_WithValidValue_DoesNotThrow(string value)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstNullOrEmpty(value));
        }

        #endregion

        #region AgainstNegativeOrZero Tests

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        [TestCase(int.MinValue)]
        public void AgainstNegativeOrZero_WithInvalidValue_ThrowsArgumentOutOfRangeException(int value)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstNegativeOrZero(value));
            Assert.That(ex.Message, Does.Contain("must be greater than zero"));
            Assert.That(ex.ActualValue, Is.EqualTo(value));
        }

        [Test]
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(int.MaxValue)]
        public void AgainstNegativeOrZero_WithValidValue_DoesNotThrow(int value)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstNegativeOrZero(value));
        }

        #endregion

        #region AgainstLessThan Tests

        [Test]
        public void AgainstLessThan_WithValueLessThanMinimum_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            int value = 4;
            int min = 5;

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstLessThan(value, min));
            Assert.That(ex.Message, Does.Contain("must be at least 5"));
            Assert.That(ex.ActualValue, Is.EqualTo(value));
        }

        [Test]
        public void AgainstLessThan_WithValueEqualToMinimum_DoesNotThrow()
        {
            // Arrange
            int value = 5;
            int min = 5;

            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstLessThan(value, min));
        }

        [Test]
        public void AgainstLessThan_WithValueGreaterThanMinimum_DoesNotThrow()
        {
            // Arrange
            int value = 10;
            int min = 5;

            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstLessThan(value, min));
        }

        #endregion

        #region AgainstOutOfRange Tests

        [Test]
        [TestCase(4, 5, 10)]
        [TestCase(0, 5, 10)]
        [TestCase(-1, 5, 10)]
        public void AgainstOutOfRange_WithValueLessThanMinimum_ThrowsArgumentOutOfRangeException(int value, int min, int max)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(value, min, max));
            Assert.That(ex.Message, Does.Contain($"must be between {min} and {max}"));
            Assert.That(ex.ActualValue, Is.EqualTo(value));
        }

        [Test]
        [TestCase(11, 5, 10)]
        [TestCase(100, 5, 10)]
        public void AgainstOutOfRange_WithValueGreaterThanMaximum_ThrowsArgumentOutOfRangeException(int value, int min, int max)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(value, min, max));
            Assert.That(ex.Message, Does.Contain($"must be between {min} and {max}"));
            Assert.That(ex.ActualValue, Is.EqualTo(value));
        }

        [Test]
        [TestCase(5, 5, 10)]
        [TestCase(7, 5, 10)]
        [TestCase(10, 5, 10)]
        public void AgainstOutOfRange_WithValueInRange_DoesNotThrow(int value, int min, int max)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstOutOfRange(value, min, max));
        }

        #endregion

        #region AgainstFileNotFound Tests

        [Test]
        public void AgainstFileNotFound_WithNullFilePath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstFileNotFound(null!));
        }

        [Test]
        public void AgainstFileNotFound_WithEmptyFilePath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstFileNotFound(string.Empty));
        }

        [Test]
        public void AgainstFileNotFound_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");

            // Act & Assert
            var ex = Assert.Throws<FileNotFoundException>(() => Guard.AgainstFileNotFound(nonExistentFile));
            Assert.That(ex.Message, Does.Contain("File not found"));
            Assert.That(ex.FileName, Is.EqualTo(nonExistentFile));
        }

        [Test]
        public void AgainstFileNotFound_WithExistingFile_DoesNotThrow()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            try
            {
                // Act & Assert
                Assert.DoesNotThrow(() => Guard.AgainstFileNotFound(tempFile));
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        #endregion

        #region AgainstDirectoryNotFound Tests

        [Test]
        public void AgainstDirectoryNotFound_WithNullPath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstDirectoryNotFound(null!));
        }

        [Test]
        public void AgainstDirectoryNotFound_WithEmptyPath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstDirectoryNotFound(string.Empty));
        }

        [Test]
        public void AgainstDirectoryNotFound_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            string nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act & Assert
            var ex = Assert.Throws<DirectoryNotFoundException>(() => Guard.AgainstDirectoryNotFound(nonExistentDir));
            Assert.That(ex.Message, Does.Contain("Directory not found"));
        }

        [Test]
        public void AgainstDirectoryNotFound_WithExistingDirectory_DoesNotThrow()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                // Act & Assert
                Assert.DoesNotThrow(() => Guard.AgainstDirectoryNotFound(tempDir));
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir);
            }
        }

        #endregion

        #region AgainstInvalidUrl Tests

        [Test]
        public void AgainstInvalidUrl_WithNullUrl_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidUrl(null!));
        }

        [Test]
        public void AgainstInvalidUrl_WithEmptyUrl_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidUrl(string.Empty));
        }

        [Test]
        [TestCase("not-a-url")]
        [TestCase("ftp://example.com")]
        [TestCase("file:///c:/test.txt")]
        [TestCase("//example.com")]
        [TestCase("example.com")]
        public void AgainstInvalidUrl_WithInvalidUrl_ThrowsArgumentException(string invalidUrl)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidUrl(invalidUrl));
            Assert.That(ex.Message, Does.Contain("Invalid URL"));
            Assert.That(ex.Message, Does.Contain("Must be a valid HTTP or HTTPS URL"));
        }

        [Test]
        [TestCase("http://example.com")]
        [TestCase("https://example.com")]
        [TestCase("http://localhost")]
        [TestCase("https://localhost:3000")]
        [TestCase("http://192.168.1.1")]
        [TestCase("https://example.com:8080/path?query=value")]
        public void AgainstInvalidUrl_WithValidUrl_DoesNotThrow(string validUrl)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstInvalidUrl(validUrl));
        }

        #endregion

        #region AgainstInvalidFileExtension Tests

        [Test]
        public void AgainstInvalidFileExtension_WithNullFilePath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidFileExtension(null!, ".txt"));
        }

        [Test]
        public void AgainstInvalidFileExtension_WithNullExpectedExtension_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidFileExtension("file.txt", null!));
        }

        [Test]
        [TestCase("file.txt", ".wav")]
        [TestCase("file.mp3", ".wav")]
        [TestCase("file", ".wav")]
        [TestCase("file.WAV", ".mp3")]
        public void AgainstInvalidFileExtension_WithWrongExtension_ThrowsArgumentException(string filePath, string expectedExtension)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidFileExtension(filePath, expectedExtension));
            Assert.That(ex.Message, Does.Contain("Invalid file extension"));
            Assert.That(ex.Message, Does.Contain($"Expected: {expectedExtension}"));
        }

        [Test]
        [TestCase("file.txt", ".txt")]
        [TestCase("file.TXT", ".txt")]
        [TestCase("file.wav", ".WAV")]
        [TestCase("C:\\path\\to\\file.json", ".json")]
        public void AgainstInvalidFileExtension_WithCorrectExtension_DoesNotThrow(string filePath, string expectedExtension)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstInvalidFileExtension(filePath, expectedExtension));
        }

        #endregion

        #region AgainstCondition Tests

        [Test]
        public void AgainstCondition_WithFalseCondition_ThrowsInvalidOperationException()
        {
            // Arrange
            bool condition = false;
            string message = "Custom error message";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => Guard.AgainstCondition(condition, message));
            Assert.That(ex.Message, Is.EqualTo(message));
        }

        [Test]
        public void AgainstCondition_WithTrueCondition_DoesNotThrow()
        {
            // Arrange
            bool condition = true;
            string message = "This should not be thrown";

            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstCondition(condition, message));
        }

        [Test]
        public void AgainstCondition_WithComplexCondition_WorksCorrectly()
        {
            // Arrange
            var validLogLevels = new[] { "debug", "information", "warning" };
            string logLevel = "error";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Guard.AgainstCondition(
                    Array.Exists(validLogLevels, level => level.Equals(logLevel, StringComparison.OrdinalIgnoreCase)),
                    $"LogLevel must be one of: {string.Join(", ", validLogLevels)}"));

            Assert.That(ex.Message, Does.Contain("LogLevel must be one of"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void MultipleGuards_CanBeChained_WithoutConflict()
        {
            // Arrange
            string value = "test";
            int number = 5;

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                Guard.AgainstNullOrWhiteSpace(value);
                Guard.AgainstNegativeOrZero(number);
                Guard.AgainstLessThan(number, 1);
            });
        }

        [Test]
        public void CallerArgumentExpression_CapturesCorrectParameterName()
        {
            // Arrange
            string myCustomVariable = null!;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(myCustomVariable));
            Assert.That(ex.ParamName, Is.EqualTo("myCustomVariable"));
        }

        #endregion

        #region AgainstNegative Tests

        [Test]
        [TestCase(-1)]
        [TestCase(-100)]
        [TestCase(int.MinValue)]
        public void AgainstNegative_WithNegativeValue_ThrowsArgumentOutOfRangeException(int value)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstNegative(value));
            Assert.That(ex.Message, Does.Contain("cannot be negative"));
            Assert.That(ex.ActualValue, Is.EqualTo(value));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(int.MaxValue)]
        public void AgainstNegative_WithNonNegativeValue_DoesNotThrow(int value)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstNegative(value));
        }

        #endregion

        #region AgainstInvalidPort Tests

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(65536)]
        [TestCase(100000)]
        public void AgainstInvalidPort_WithInvalidPort_ThrowsArgumentOutOfRangeException(int port)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstInvalidPort(port));
            Assert.That(ex.Message, Does.Contain("Port must be between 1 and 65535"));
            Assert.That(ex.ActualValue, Is.EqualTo(port));
        }

        [Test]
        [TestCase(1)]
        [TestCase(80)]
        [TestCase(443)]
        [TestCase(3000)]
        [TestCase(8080)]
        [TestCase(65535)]
        public void AgainstInvalidPort_WithValidPort_DoesNotThrow(int port)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstInvalidPort(port));
        }

        #endregion

        #region AgainstInvalidEmail Tests

        [Test]
        public void AgainstInvalidEmail_WithNullEmail_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEmail(null!));
        }

        [Test]
        public void AgainstInvalidEmail_WithEmptyEmail_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEmail(string.Empty));
        }

        [Test]
        [TestCase("invalid")]
        [TestCase("@example.com")]
        [TestCase("user@")]
        [TestCase("user@@example.com")]
        [TestCase("user@.com")]
        [TestCase("user@com.")]
        [TestCase("user@example")]
        public void AgainstInvalidEmail_WithInvalidEmail_ThrowsArgumentException(string email)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEmail(email));
            Assert.That(ex.Message, Does.Contain("Invalid email address"));
        }

        [Test]
        [TestCase("user@example.com")]
        [TestCase("test.user@example.com")]
        [TestCase("user+tag@example.co.uk")]
        [TestCase("admin@localhost.local")]
        public void AgainstInvalidEmail_WithValidEmail_DoesNotThrow(string email)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstInvalidEmail(email));
        }

        #endregion

        #region AgainstUndefinedEnum Tests

        private enum TestEnum
        {
            Value1 = 1,
            Value2 = 2,
            Value3 = 3
        }

        [Test]
        public void AgainstUndefinedEnum_WithDefinedValue_DoesNotThrow()
        {
            // Arrange
            var value = TestEnum.Value2;

            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstUndefinedEnum(value));
        }

        [Test]
        public void AgainstUndefinedEnum_WithUndefinedValue_ThrowsArgumentException()
        {
            // Arrange
            var undefinedValue = (TestEnum)99;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstUndefinedEnum(undefinedValue));
            Assert.That(ex.Message, Does.Contain("Undefined enum value"));
            Assert.That(ex.Message, Does.Contain("TestEnum"));
        }

        #endregion

        #region AgainstNullOrEmptyCollection Tests

        [Test]
        public void AgainstNullOrEmptyCollection_WithNullCollection_ThrowsArgumentException()
        {
            // Arrange
            List<string>? nullCollection = null;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmptyCollection(nullCollection));
            Assert.That(ex.Message, Does.Contain("cannot be null or empty"));
        }

        [Test]
        public void AgainstNullOrEmptyCollection_WithEmptyCollection_ThrowsArgumentException()
        {
            // Arrange
            var emptyCollection = new List<string>();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmptyCollection(emptyCollection));
            Assert.That(ex.Message, Does.Contain("cannot be null or empty"));
        }

        [Test]
        public void AgainstNullOrEmptyCollection_WithNonEmptyCollection_DoesNotThrow()
        {
            // Arrange
            var collection = new List<string> { "item1", "item2" };

            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstNullOrEmptyCollection(collection));
        }

        #endregion

        #region AgainstEmptyGuid Tests

        [Test]
        public void AgainstEmptyGuid_WithEmptyGuid_ThrowsArgumentException()
        {
            // Arrange
            var emptyGuid = Guid.Empty;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstEmptyGuid(emptyGuid));
            Assert.That(ex.Message, Does.Contain("GUID cannot be empty"));
        }

        [Test]
        public void AgainstEmptyGuid_WithNonEmptyGuid_DoesNotThrow()
        {
            // Arrange
            var guid = Guid.NewGuid();

            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstEmptyGuid(guid));
        }

        #endregion

        #region AgainstInvalidPath Tests

        [Test]
        public void AgainstInvalidPath_WithNullPath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidPath(null!));
        }

        [Test]
        public void AgainstInvalidPath_WithEmptyPath_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidPath(string.Empty));
        }

        [Test]
        [TestCase("C:\\test\\file<invalid>.txt")]
        [TestCase("C:\\test\\file>invalid.txt")]
        [TestCase("C:\\test\\file|invalid.txt")]
        public void AgainstInvalidPath_WithInvalidCharacters_ThrowsArgumentException(string path)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidPath(path));
            Assert.That(ex.Message, Does.Contain("invalid characters"));
        }

        [Test]
        [TestCase("C:\\test\\file.txt")]
        [TestCase("C:\\Users\\test\\file.txt")]
        [TestCase("relative\\path\\file.txt")]
        [TestCase("file.txt")]
        public void AgainstInvalidPath_WithValidPath_DoesNotThrow(string path)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstInvalidPath(path));
        }

        #endregion

        #region AgainstTooLong Tests

        [Test]
        public void AgainstTooLong_WithNullString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstTooLong(null!, 10));
        }

        [Test]
        public void AgainstTooLong_WithEmptyString_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Guard.AgainstTooLong(string.Empty, 10));
        }

        [Test]
        public void AgainstTooLong_WithStringExceedingMaxLength_ThrowsArgumentException()
        {
            // Arrange
            string longString = "This is a very long string";
            int maxLength = 10;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => Guard.AgainstTooLong(longString, maxLength));
            Assert.That(ex.Message, Does.Contain("exceeds maximum allowed length"));
            Assert.That(ex.Message, Does.Contain(longString.Length.ToString()));
            Assert.That(ex.Message, Does.Contain(maxLength.ToString()));
        }

        [Test]
        [TestCase("short", 10)]
        [TestCase("exact", 5)]
        [TestCase("test", 4)]
        public void AgainstTooLong_WithStringWithinMaxLength_DoesNotThrow(string value, int maxLength)
        {
            // Act & Assert
            Assert.DoesNotThrow(() => Guard.AgainstTooLong(value, maxLength));
        }

        #endregion
    }
}
