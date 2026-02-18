// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GrafaMon.Application;
using GrafaMon.Domain;
using GrafaMon.Infrastructure.Dtos;
using GrafaMon.Infrastructure.Mappers;
using Serilog;

namespace GrafaMon.Infrastructure
{
    public sealed class GrafanaAlertsReader(HttpClient http, AppSettings settings, ILogger logger) : IGrafanaAlertsReader
    {
        private readonly HttpClient _http = http;
        private readonly AppSettings _settings = settings;
        private readonly ILogger _logger = logger.ForContext<GrafanaAlertsReader>();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            MaxDepth = 64 //prevent excessively deep JSON from causing stack overflows
        };

        private static bool IsValidAlert(GrafanaAlertDto dto)
        {
            // Minimum required fields for a valid alert
            return dto != null
                && !string.IsNullOrWhiteSpace(dto.Fingerprint)
                && dto.Labels != null;
        }

        public async Task<(AlertCounts Counts, IReadOnlyList<AlertDetail> Details)> GetActiveAlertsAsync(CancellationToken cancellationToken)
        {
            var url = BuildUrl();
            _logger.Debug("Fetching alerts from: {GrafanaUrl}", url);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            req.Headers.Add("X-Grafana-Org-Id", _settings.GrafanaOrgId);

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            //IF not a successful status code - capture code & error body then log
            if(!resp.IsSuccessStatusCode)
            {
                var errorContent = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.Error("Grafana API request failed with status code {StatusCode}. Response: {ResponseContent}",
                    (int)resp.StatusCode, string.IsNullOrWhiteSpace(errorContent) ? "(empty)" : errorContent);
                resp.EnsureSuccessStatusCode(); //will throw error with status code
            }
            _logger.Debug("Grafana API request succeeded with status code {StatusCode}", (int)resp.StatusCode);

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            // Deserialize to DTO list
            List<GrafanaAlertDto>? alertDtos;
            try
            {
                alertDtos = await JsonSerializer.DeserializeAsync<List<GrafanaAlertDto>>(stream, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                _logger.Error(ex, "Failed to parse Grafana alert JSON response");
                throw new InvalidOperationException("Failed to parse Grafana alert JSON response. The API response format may have changed.", ex);
            }

            // Handle null or empty response
            if (alertDtos == null || alertDtos.Count == 0)
            {
                _logger.Debug("No alerts returned from Grafana");
                return (new AlertCounts(0, 0, 0), Array.Empty<AlertDetail>());
            }

            _logger.Debug("Received {AlertCount} alerts from Grafana", alertDtos.Count);

            //Check alertDTOs count is not exceeding a reasonable limit to prevent DoS
            if(alertDtos.Count > 10000)
            {
                _logger.Warning("Grafana returned {AlertCount} alerts, exceeds the maximum expected. Truncating the list to process only the first 10000 alerts.",
                    alertDtos.Count);
                alertDtos = alertDtos.Take(10000).ToList();
            }
            _logger.Debug("Received {AlertCount} alerts from Grafana", alertDtos.Count);

            // Map DTOs to domain models
            var details = new List<AlertDetail>(alertDtos.Count);
            int critical = 0, warning = 0, info = 0;
            int skippedCount = 0;

            foreach (var dto in alertDtos)
            {
                if (!IsValidAlert(dto))
                {
                    _logger.Warning("Skipping alert with missing critical fields (fingerprint: {Fingerprint})", dto?.Fingerprint ?? "null");
                    skippedCount++;
                    continue;
                }

                try
                {
                    var detail = AlertDtoMapper.MapToAlertDetail(dto);
                    details.Add(detail);

                    // Count by severity
                    switch (detail.Severity)
                    {
                        case AlertSeverity.Critical:
                            critical++;
                            break;
                        case AlertSeverity.Warning:
                            warning++;
                            break;
                        case AlertSeverity.Info:
                            info++;
                            break;
                        case AlertSeverity.Unknown:
                            // Don't count unknown in any category
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Log and skip malformed alerts (don't crash the entire poll)
                    _logger.Warning(ex, "Failed to parse alert (fingerprint: {Fingerprint}, Severity: {Severity}, AlertName: {AlertName})"
                            , dto.Fingerprint ?? "unknown"
                            , dto.Labels?.Severity ?? "unknown"
                            , dto.Labels?.AlertName ?? "unknown" );
                    skippedCount++;
                    continue;
                }
            }

            if (skippedCount > 0)
            {
                _logger.Warning("Skipped {SkippedCount} malformed alerts", skippedCount);
            }

            return (new AlertCounts(critical, warning, info), details);
        }

        private Uri BuildUrl()
        {
            // Safe combine base url + path
            var baseUri = new Uri(_settings.GrafanaBaseUrl.TrimEnd('/') + "/");
            var path = _settings.ActiveAlertsPath.TrimStart('/');
            return new Uri(baseUri, path);
        }
    }
}