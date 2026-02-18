// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrafaMon.Domain
{
    /// <summary>
    /// Represents detailed information about a single alert.
    /// This is an immutable record type.
    /// </summary>
    /// <param name="Environment">The environment where the alert originated (e.g., "PROD", "DEV").</param>
    /// <param name="MonitorTest">The name of the monitoring check or service.</param>
    /// <param name="Host">The host or instance where the alert occurred.</param>
    /// <param name="Summary">A human-readable summary of the alert.</param>
    /// <param name="Severity">The severity level of the alert.</param>
    /// <param name="LastUpdatedUtc">The timestamp when the alert was last updated (UTC).</param>
    /// <param name="StartsAtUtc">The timestamp when the alert started (UTC).</param>
    /// <param name="GrafanaUrl">The URL to view the alert in Grafana.</param>
    public sealed record AlertDetail(
        string Environment,
        string MonitorTest,
        string Host,
        string Summary,
        AlertSeverity Severity,
        DateTime LastUpdatedUtc,
        DateTime StartsAtUtc,
        string GrafanaAlertUrl  
    )
    {
        public TimeSpan ActiveFor => DateTime.UtcNow - StartsAtUtc;
        public string SeverityNormalized => Severity.ToString().ToLowerInvariant();
        // Formatted active period as "1d 2h 3m 4s"
        public string ActiveForFormatted
        {
            get
            {
                var days = ActiveFor.Days;
                var hours = ActiveFor.Hours;
                var minutes = ActiveFor.Minutes;
                var seconds = ActiveFor.Seconds;

                var parts = new List<string>();

                if (days > 0)
                    parts.Add($"{days}d");
                if (hours > 0)
                    parts.Add($"{hours}h");
                if (minutes > 0)
                    parts.Add($"{minutes}m");
                if (seconds > 0 || parts.Count == 0)
                    parts.Add($"{seconds}s");

                return string.Join(" ", parts);
            }
        }
    }

}
