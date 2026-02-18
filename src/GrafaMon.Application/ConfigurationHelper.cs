// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using GrafaMon.Application.Guards;
using GrafaMon.Application.Security;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace GrafaMon.Application
{
    public static class ConfigurationHelper
    {
        private const string ConfigFileName = "config.json";

        /// <summary>
        /// Gets the full path to the config file in %APPDATA%\GrafaMon\config.json
        /// </summary>
        public static string GetConfigFilePath()
        {
            // For single-file published apps, use hardcoded app name
            // Assembly.GetExecutingAssembly().Location returns empty for single-file apps
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            Guard.AgainstNullOrWhiteSpace(appDataPath);
            
            var appFolder = Path.Combine(appDataPath, "GrafaMon");
            var configPath = Path.Combine(appFolder, ConfigFileName);
            
            return configPath;
        }

        /// <summary>
        /// Ensures the config file exists. If not, creates a default config.
        /// </summary>
        public static void EnsureConfigFileExists(string configPath, ILogger? logger = null)
        {
            Guard.AgainstNullOrWhiteSpace(configPath, nameof(configPath));
            
            var configDirectory = Path.GetDirectoryName(configPath);

            // Create directory if it doesn't exist
            if (!string.IsNullOrEmpty(configDirectory) && !Directory.Exists(configDirectory))
            {
                logger?.Information("Creating config directory in [{ConfigDirectory}]", configDirectory);
                Directory.CreateDirectory(configDirectory!);
            }

            // Create default config if file doesn't exist
            if (!File.Exists(configPath))
            {
                logger?.Information("Config file not found. Creating default config in [{ConfigPath}]", configPath);
                CreateDefaultConfig(configPath);
            }

            logger?.Information("Config file found in [{ConfigPath}]", configPath);
        }

        private static void CreateDefaultConfig(string configPath)
        {
            Guard.AgainstNullOrWhiteSpace(configPath, nameof(configPath));
            
            var defaultConfig = new
            {
                GrafanaAlertInstance = new
                {
                    GrafanaBaseUrl = "https://YOUR-GRAFANA-INSTANCE.grafana.net",
                    ApiKey = "YOUR-API-KEY-HERE",
                    IsApiKeyEncrypted = false, // Template config has plain-text placeholder
                    PollingIntervalSeconds = 20,
                    ActiveAlertsPath = "/api/alertmanager/grafana/api/v2/alerts?active=true&silenced=false&inhibited=false",
                    GrafanaOrgId = "1",
                    MaxAlertDetailRows = 25,
                    EnableSoundNotifications = true,
                    SoundFilePath = (string?)null,  // null = use Windows default sound (Critical Stop)
                    EnableLogging = true,
                    LogLevel = "Information",  // Debug, Information, Warning, Error, Fatal
                    MaxLogFileSizeMB = 10,
                    MaxLogFileCount = 5
                }
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(defaultConfig, options);
            File.WriteAllText(configPath, json, Encoding.UTF8);
        }

        /// <summary>
        /// Saves configuration with encrypted API key using DPAPI.
        /// </summary>
        /// <param name="configPath">Path to config file</param>
        /// <param name="settings">Settings with plain-text API key</param>
        /// <param name="logger">Optional logger</param>
        public static void SaveEncryptedConfig(string configPath, AppSettings settings, ILogger? logger = null)
        {
            Guard.AgainstNullOrWhiteSpace(configPath, nameof(configPath));
            Guard.AgainstNull(settings, nameof(settings));

            try
            {
                logger?.Debug("Saving configuration with encrypted API key to {ConfigPath}", configPath);
                
                // Encrypt the API key before saving
                var encryptedApiKey = SecureConfigurationManager.Encrypt(settings.ApiKey);
                
                // Create config object with encrypted key
                var configObject = new
                {
                    GrafanaAlertInstance = new
                    {
                        settings.GrafanaBaseUrl,
                        ApiKey = encryptedApiKey,
                        IsApiKeyEncrypted = true, // Mark as encrypted
                        settings.PollingIntervalSeconds,
                        settings.ActiveAlertsPath,
                        settings.GrafanaOrgId,
                        settings.MaxAlertDetailRows,
                        settings.EnableSoundNotifications,
                        settings.SoundFilePath,
                        settings.EnableLogging,
                        settings.LogLevel,
                        settings.MaxLogFileSizeMB,
                        settings.MaxLogFileCount
                    }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(configObject, options);

                var configDirectory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(configDirectory) && !Directory.Exists(configDirectory))
                {
                    Directory.CreateDirectory(configDirectory);
                }

                File.WriteAllText(configPath, json, Encoding.UTF8);
                
                logger?.Information("Configuration saved successfully with encrypted API key");
            }
            catch (Exception ex)
            {
                logger?.Error(ex, "Failed to save encrypted configuration");
                throw;
            }
        }

        /// <summary>
        /// Loads configuration and decrypts API key if encrypted.
        /// </summary>
        /// <param name="configPath">Path to config file</param>
        /// <param name="logger">Optional logger</param>
        /// <returns>Settings with decrypted API key</returns>
        public static AppSettings LoadAndDecrypt(string configPath, ILogger? logger = null)
        {
            Guard.AgainstNullOrWhiteSpace(configPath, nameof(configPath));
            Guard.AgainstFileNotFound(configPath, nameof(configPath));

            try
            {
                logger?.Debug("Loading configuration from {ConfigPath}", configPath);

                var json = File.ReadAllText(configPath, Encoding.UTF8);
                var config = JsonDocument.Parse(json);
                var instanceElement = config.RootElement.GetProperty("GrafanaAlertInstance");
                
                var settings = JsonSerializer.Deserialize<AppSettings>(instanceElement.GetRawText());
                
                Guard.AgainstNull(settings, nameof(settings));

                // If encrypted, decrypt the API key
                if (settings.IsApiKeyEncrypted)
                {
                    logger?.Debug("Decrypting API key");
                    var decryptedApiKey = SecureConfigurationManager.Decrypt(settings.ApiKey);
                    
                    // Return new instance with decrypted key
                    return new AppSettings
                    {
                        GrafanaBaseUrl = settings.GrafanaBaseUrl,
                        ApiKey = decryptedApiKey,
                        IsApiKeyEncrypted = settings.IsApiKeyEncrypted,
                        PollingIntervalSeconds = settings.PollingIntervalSeconds,
                        ActiveAlertsPath = settings.ActiveAlertsPath,
                        GrafanaOrgId = settings.GrafanaOrgId,
                        MaxAlertDetailRows = settings.MaxAlertDetailRows,
                        EnableSoundNotifications = settings.EnableSoundNotifications,
                        SoundFilePath = settings.SoundFilePath,
                        EnableLogging = settings.EnableLogging,
                        LogLevel = settings.LogLevel,
                        MaxLogFileSizeMB = settings.MaxLogFileSizeMB,
                        MaxLogFileCount = settings.MaxLogFileCount
                    };
                }

                // Plain-text API key
                return settings;
            }
            catch (Exception ex)
            {
                logger?.Error(ex, "Failed to load configuration");
                throw;
            }
        }

        /// <summary>
        /// Validates that the config file has required settings.
        /// Throws InvalidOperationException if validation fails.
        /// </summary>
        public static void ValidateConfig(AppSettings settings, ILogger? logger = null)
        {
            Guard.AgainstNull(settings, nameof(settings));
            
            logger?.Information("Validating configuration settings");

            // Validate required string fields
            Guard.AgainstNullOrWhiteSpace(settings.GrafanaBaseUrl, nameof(settings.GrafanaBaseUrl));
            Guard.AgainstNullOrWhiteSpace(settings.ApiKey, nameof(settings.ApiKey));
            Guard.AgainstNullOrWhiteSpace(settings.GrafanaOrgId, nameof(settings.GrafanaOrgId));
            Guard.AgainstNullOrWhiteSpace(settings.ActiveAlertsPath, nameof(settings.ActiveAlertsPath));

            // Validate URL format
            Guard.AgainstInvalidUrl(settings.GrafanaBaseUrl, nameof(settings.GrafanaBaseUrl));

            // Validate numeric ranges
            Guard.AgainstLessThan(settings.PollingIntervalSeconds, 5, nameof(settings.PollingIntervalSeconds));
            Guard.AgainstNegativeOrZero(settings.MaxLogFileSizeMB, nameof(settings.MaxLogFileSizeMB));
            Guard.AgainstNegativeOrZero(settings.MaxLogFileCount, nameof(settings.MaxLogFileCount));
            Guard.AgainstNegativeOrZero(settings.MaxAlertDetailRows, nameof(settings.MaxAlertDetailRows));

            // Validate log level
            var validLogLevels = new[] { "debug", "information", "warning", "error", "fatal" };
            Guard.AgainstCondition(
                Array.Exists(validLogLevels, level => level.Equals(settings.LogLevel, StringComparison.OrdinalIgnoreCase)),
                $"LogLevel must be one of: {string.Join(", ", validLogLevels)}. Current value: {settings.LogLevel}");

            // Validate Active Alerts Path format
            if (!settings.ActiveAlertsPath.StartsWith("/"))
            {
                logger?.Warning("ActiveAlertsPath [{ActiveAlertsPath}] should start with '/'", settings.ActiveAlertsPath);
            }

            // Validate sound file if specified
            if (settings.EnableSoundNotifications && !string.IsNullOrWhiteSpace(settings.SoundFilePath))
            {
                if (!File.Exists(settings.SoundFilePath))
                {
                    logger?.Warning("Sound file not found [{SoundFilePath}], will use Windows default sound", settings.SoundFilePath);
                }
                else
                {
                    var extension = Path.GetExtension(settings.SoundFilePath);
                    if (!string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        logger?.Warning("Sound file [{SoundFilePath}] is not a .wav file, may not play correctly", settings.SoundFilePath);
                    }
                    else
                    {
                        logger?.Debug("Custom sound file found [{SoundFilePath}]", settings.SoundFilePath);
                    }
                }
            }

            logger?.Information("Configuration settings validated successfully");
            logger?.Information("Grafana URL: {GrafanaBaseUrl}, API Key: {ApiKey}, Polling Interval: {PollingIntervalSeconds}s, Logging: {EnableLogging} ({LogLevel})",
                settings.GrafanaBaseUrl, MaskAPIKey(settings.ApiKey), settings.PollingIntervalSeconds, settings.EnableLogging, settings.LogLevel);
        }

        //helper method to mask sensitive data (e.g. api key)
        private static string MaskAPIKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return "[empty]";

            if(apiKey.Length<= 8)
                return "****";

            return $"{apiKey.Substring(0,4)}...{apiKey.Substring(apiKey.Length-4)}";
        }

    }
}