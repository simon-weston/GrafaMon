// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrafaMon.Application
{
    public sealed class AppSettings
    {
        public required string GrafanaBaseUrl { get; init; }   // e.g. https://grafana.mycompany.com
        public required string ApiKey { get; init; }           // Bearer token
        
        // Security: Indicates if the API key is encrypted using DPAPI
        public bool IsApiKeyEncrypted { get; init; } = false;
        
        public int PollingIntervalSeconds { get; init; } = 60;

        // Make this configurable because Grafana endpoints vary by setup.
        public string ActiveAlertsPath { get; init; } = "/api/alertmanager/grafana/api/v2/alerts";
        public string GrafanaOrgId { get; init; } = "1";

        // UI settings
        public int MaxAlertDetailRows { get; init; } = 25;

        // Sound notification settings
        public bool EnableSoundNotifications { get; init; } = true;
        public string? SoundFilePath { get; init; } = null;  // null = use Windows default sound

        // Logging settings
        public bool EnableLogging { get; init; } = true;
        public string LogLevel { get; init; } = "Information";  // Debug, Information, Warning, Error, Fatal
        public int MaxLogFileSizeMB { get; init; } = 10;        // Maximum size per log file in MB
        public int MaxLogFileCount { get; init; } = 5;          // Maximum number of log files to keep
    }
}