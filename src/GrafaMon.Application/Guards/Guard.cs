// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GrafaMon.Application.Guards
{
    /// <summary>
    /// Provides guard clauses for parameter validation.
    /// Reduces boilerplate code and ensures consistent error messages.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if the value is null.
        /// </summary>
        /// <typeparam name="T">The type of the parameter</typeparam>
        /// <param name="value">The value to check</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
        public static void AgainstNull<T>(
            [NotNull] T? value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : class
        {
            if (value is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the string is null, empty, or whitespace.
        /// </summary>
        /// <param name="value">The string value to check</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when string is null, empty, or whitespace</exception>
        public static void AgainstNullOrWhiteSpace(
            [NotNull] string? value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the string is null or empty (allows whitespace).
        /// </summary>
        /// <param name="value">The string value to check</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException"/>Thrown when string is null or empty</exception>
        public static void AgainstNullOrEmpty(
            [NotNull] string? value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Value cannot be null or empty.", paramName);
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is less than or equal to zero.
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to zero</exception>
        public static void AgainstNegativeOrZero(
            int value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Value must be greater than zero.");
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is less than the minimum.
        /// </summary>
        /// <typeparam name="T">The type of the parameter</typeparam>
        /// <param name="value">The value to check</param>
        /// <param name="min">The minimum allowed value (inclusive)</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than minimum</exception>
        public static void AgainstLessThan<T>(
            T value,
            T min,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Value must be at least {min}.");
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is out of the specified range.
        /// </summary>
        /// <typeparam name="T">The type of the parameter</typeparam>
        /// <param name="value">The value to check</param>
        /// <param name="min">The minimum allowed value (inclusive)</param>
        /// <param name="max">The maximum allowed value (inclusive)</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is out of range</exception>
        public static void AgainstOutOfRange<T>(
            T value,
            T min,
            T max,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.");
            }
        }

        /// <summary>
        /// Throws <see cref="FileNotFoundException"/> if the file does not exist.
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace</exception>
        /// <exception cref="FileNotFoundException">Thrown when file does not exist</exception>
        public static void AgainstFileNotFound(
            string filePath,
            [CallerArgumentExpression(nameof(filePath))] string? paramName = null)
        {
            AgainstNullOrWhiteSpace(filePath, paramName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }
        }

        /// <summary>
        /// Throws <see cref="DirectoryNotFoundException"/> if the directory does not exist.
        /// </summary>
        /// <param name="directoryPath">The directory path to check</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when directoryPath is null or whitespace</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when directory does not exist</exception>
        public static void AgainstDirectoryNotFound(
            string directoryPath,
            [CallerArgumentExpression(nameof(directoryPath))] string? paramName = null)
        {
            AgainstNullOrWhiteSpace(directoryPath, paramName);

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the URL is not valid.
        /// </summary>
        /// <param name="url">The URL string to validate</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when URL is null, whitespace, or invalid</exception>
        public static void AgainstInvalidUrl(
            string url,
            [CallerArgumentExpression(nameof(url))] string? paramName = null)
        {
            AgainstNullOrWhiteSpace(url, paramName);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException($"Invalid URL: {url}. Must be a valid HTTP or HTTPS URL.", paramName);
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the file extension is not valid.
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <param name="expectedExtension">The expected file extension (e.g., ".wav")</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when file extension doesn't match expected</exception>
        public static void AgainstInvalidFileExtension(
            string filePath,
            string expectedExtension,
            [CallerArgumentExpression(nameof(filePath))] string? paramName = null)
        {
            AgainstNullOrWhiteSpace(filePath, paramName);
            AgainstNullOrWhiteSpace(expectedExtension, nameof(expectedExtension));

            var actualExtension = Path.GetExtension(filePath);
            if (!string.Equals(actualExtension, expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Invalid file extension: {actualExtension}. Expected: {expectedExtension}", 
                    paramName);
            }
        }

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if the condition is false.
        /// </summary>
        /// <param name="condition">The condition to check</param>
        /// <param name="message">The error message</param>
        /// <exception cref="InvalidOperationException">Thrown when condition is false</exception>
        public static void AgainstCondition(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is negative.
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative</exception>
        public static void AgainstNegative(
            int value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the port number is invalid (not between 1 and 65535).
        /// </summary>
        /// <param name="port">The port number to validate</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when port is invalid</exception>
        public static void AgainstInvalidPort(
            int port,
            [CallerArgumentExpression(nameof(port))] string? paramName = null)
        {
            if (port < 1 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(paramName, port, "Port must be between 1 and 65535.");
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the email address is not valid.
        /// </summary>
        /// <param name="email">The email address to validate</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when email is null, whitespace, or invalid format</exception>
        public static void AgainstInvalidEmail(
            string email,
            [CallerArgumentExpression(nameof(email))] string? paramName = null)
        {
            AgainstNullOrWhiteSpace(email, paramName);

            // Simple email validation: contains @ and . after @
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0 || atIndex == email.Length - 1)
            {
                throw new ArgumentException($"Invalid email address: {email}", paramName);
            }

            // Ensure there is only one @ symbol
            if (email.IndexOf('@', atIndex + 1) != -1)
            {
                throw new ArgumentException($"Invalid email address: {email}", paramName);
            }

            var domainPart = email.Substring(atIndex + 1);
            if (!domainPart.Contains('.') || domainPart.StartsWith('.') || domainPart.EndsWith('.'))
            {
                throw new ArgumentException($"Invalid email address: {email}", paramName);
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the enum value is not defined in the enum type.
        /// </summary>
        /// <typeparam name="TEnum">The enum type</typeparam>
        /// <param name="value">The enum value to check</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when enum value is not defined</exception>
        public static void AgainstUndefinedEnum<TEnum>(
            TEnum value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new ArgumentException($"Undefined enum value: {value} for type {typeof(TEnum).Name}", paramName);
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the collection is null or empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="collection">The collection to check</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when collection is null or empty</exception>
        public static void AgainstNullOrEmptyCollection<T>(
            System.Collections.Generic.IEnumerable<T>? collection,
            [CallerArgumentExpression(nameof(collection))] string? paramName = null)
        {
            if (collection == null || !collection.Any())
            {
                throw new ArgumentException("Collection cannot be null or empty.", paramName);
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the GUID is empty.
        /// </summary>
        /// <param name="guid">The GUID to check</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when GUID is empty</exception>
        public static void AgainstEmptyGuid(
            Guid guid,
            [CallerArgumentExpression(nameof(guid))] string? paramName = null)
        {
            if (guid == Guid.Empty)
            {
                throw new ArgumentException("GUID cannot be empty.", paramName);
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the path contains invalid characters.
        /// </summary>
        /// <param name="path">The file or directory path to validate</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when path contains invalid characters</exception>
        public static void AgainstInvalidPath(
            string path,
            [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            AgainstNullOrWhiteSpace(path, paramName);

            var invalidChars = Path.GetInvalidPathChars()
                .Concat(new[] { '<', '>', '|', '\"', '*', '?' })
                .Distinct()
                .ToArray();
                
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                throw new ArgumentException($"Path contains invalid characters: {path}", paramName);
            }
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the string exceeds the maximum length.
        /// </summary>
        /// <param name="value">The string value to check</param>
        /// <param name="maxLength">The maximum allowed length</param>
        /// <param name="paramName">The parameter name (automatically captured)</param>
        /// <exception cref="ArgumentException">Thrown when string exceeds max length</exception>
        public static void AgainstTooLong(
            string value,
            int maxLength,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            AgainstNullOrWhiteSpace(value, paramName);
            AgainstNegativeOrZero(maxLength, nameof(maxLength));

            if (value.Length > maxLength)
            {
                throw new ArgumentException($"String length ({value.Length}) exceeds maximum allowed length ({maxLength}).", paramName);
            }
        }
    }
}
