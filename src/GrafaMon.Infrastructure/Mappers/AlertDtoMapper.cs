// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Globalization;
using GrafaMon.Application.Guards;
using GrafaMon.Domain;
using GrafaMon.Infrastructure.Dtos;

namespace GrafaMon.Infrastructure.Mappers
{
    /// <summary>
    /// Maps Grafana alert DTOs to domain models with robust null handling.
    /// </summary>
    internal static class AlertDtoMapper
    {
        /// <summary>
        /// Maps a GrafanaAlertDto to an AlertDetail domain model.
        /// Handles missing/null fields gracefully with sensible defaults.
        /// </summary>
        public static AlertDetail MapToAlertDetail(GrafanaAlertDto dto)
        {
            Guard.AgainstNull(dto, nameof(dto));

            // Extract severity (default to Unknown if missing)
            var severity = ParseSeverity(dto.Labels?.Severity);

            // Extract environment (fallback: "unknown")
            var environment = GetValueOrFallback( dto.Labels?.Environment, "unknown");

            // Extract service name (fallback: alertname or "unknown")
            var serviceName = GetValueOrFallback(dto.Labels?.ServiceName, null)
                ?? GetValueOrFallback(dto.Labels?.AlertName, "unknown");

            // Extract host (fallback: instance or "unknown")
            var host = GetValueOrFallback(dto.Labels?.Host, null)
                ?? GetValueOrFallback(dto.Labels?.Instance, "unknown");

            // Extract summary (fallback: description or alertname or "No summary")
            var summary = GetValueOrFallback(dto.Annotations?.Summary, null )
                ?? GetValueOrFallback(dto.Annotations?.Description, null)
                ?? GetValueOrFallback(dto.Labels?.AlertName, "No summary available");

            // Extract timestamps (fallback: UtcNow if parsing fails)
            var updatedAt = TryParseDateTime(dto.UpdatedAt) ?? DateTime.UtcNow;
            var startsAt = TryParseDateTime(dto.StartsAt) ?? updatedAt;

            // Extract Grafana URL (fallback: empty string)
            var grafanaUrl = dto.GeneratorUrl ?? string.Empty;

            return new AlertDetail(
                environment,
                serviceName,
                host,
                summary,
                severity,
                updatedAt,
                startsAt,
                grafanaUrl
            );
        }

        /// <summary>
        /// Parses severity string to AlertSeverity enum.
        /// Returns Unknown if null/empty/unrecognized.
        /// </summary>
        private static AlertSeverity ParseSeverity(string? severityString)
        {
            if (string.IsNullOrWhiteSpace(severityString))
                return AlertSeverity.Unknown;

            return severityString.Trim().ToLowerInvariant() switch
            {
                "critical" => AlertSeverity.Critical,
                "warning" => AlertSeverity.Warning,
                "info" => AlertSeverity.Info,
                _ => AlertSeverity.Unknown
            };
        }

        /// <summary>
        /// Tries to parse ISO 8601 datetime string.
        /// Returns null if parsing fails.
        /// </summary>
        private static DateTime? TryParseDateTime(string? dateTimeString)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString))
                return null;

            // Try ISO 8601 format (Grafana uses this)
            if (DateTime.TryParse(dateTimeString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
            {
                //Sanity check: reject dates before or after 1 year
                if (result < DateTime.UtcNow.AddYears(-1) || result > DateTime.UtcNow.AddYears(1)) { return null; }

                //Passed sanity check - return result
                return result;
            }
            return null;
        }

        /// <summary>
        /// Returns the trimmed value if it's not null/empty/whitespace, otherwise returns the fallback.
        /// Treats whitespace-only strings as empty.
        /// </summary>
        private static string GetValueOrFallback(string? value, string? fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Trim();
        }
    }
}