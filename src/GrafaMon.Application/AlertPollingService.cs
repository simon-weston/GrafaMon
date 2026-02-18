// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GrafaMon.Application.Guards;
using GrafaMon.Domain;
using Serilog;

namespace GrafaMon.Application
{
    public sealed class AlertPollingService
    {
        private readonly IGrafanaAlertsReader _reader;
        private readonly AppSettings _settings;
        private readonly ILogger _logger;
        private PeriodicTimer? _timer;
        private int _previousCriticalCount = 0;

        public AlertPollingService(IGrafanaAlertsReader reader, AppSettings settings, ILogger logger)
        {
            Guard.AgainstNull(reader, nameof(reader));
            Guard.AgainstNull(settings, nameof(settings));
            Guard.AgainstNull(logger, nameof(logger));
            
            _reader = reader;
            _settings = settings;
            _logger = logger.ForContext<AlertPollingService>();
        }

        public event EventHandler<AlertCounts>? CountsUpdated;
        public event EventHandler<string>? PollingError;
        public event EventHandler<IReadOnlyList<AlertDetail>>? DetailsUpdated;
        public event EventHandler? PollingSucceeded;
        public event EventHandler<int>? NewCriticalAlertDetected;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var period = TimeSpan.FromSeconds(Math.Max(5, _settings.PollingIntervalSeconds));
            _timer = new PeriodicTimer(period);

            _logger.Information("Alert polling service started. Polling interval: {PollingIntervalSeconds}s", period.TotalSeconds);

            try
            {
                // Immediate first fetch (so user doesn't wait 60s)
                await PollOnce(cancellationToken);

                while (await _timer.WaitForNextTickAsync(cancellationToken))
                {
                    await PollOnce(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Information("Alert polling service stopped (cancellation requested)");
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Alert polling service crashed unexpectedly");
            }
            finally
            {
                _timer?.Dispose();
            }
        }

        private async Task PollOnce(CancellationToken cancellationToken)
        {
            const int maxRetries = 3;
            var delays = new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) };

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.Debug("Polling Grafana alerts (attempt {Attempt}/{MaxAttempts})...", attempt + 1, maxRetries + 1);

                    var result = await _reader.GetActiveAlertsAsync(cancellationToken).ConfigureAwait(false);

                    _logger.Debug("Poll successful: {CriticalCount} critical, {WarningCount} warning, {InfoCount} info",
                        result.Counts.Critical, result.Counts.Warning, result.Counts.Info);

                    // Detect NEW critical alerts (count increased)
                    if (result.Counts.Critical > _previousCriticalCount)
                    {
                        var newCount = result.Counts.Critical - _previousCriticalCount;
                        _logger.Information("NEW critical alert detected! Count increased from {PreviousCount} to {CurrentCount} (+{NewCount})",
                            _previousCriticalCount, result.Counts.Critical, newCount);
                        NewCriticalAlertDetected?.Invoke(this, result.Counts.Critical);
                    }
                    _previousCriticalCount = result.Counts.Critical;

                    CountsUpdated?.Invoke(this, result.Counts);
                    DetailsUpdated?.Invoke(this, result.Details);
                    PollingSucceeded?.Invoke(this, EventArgs.Empty);
                    return; // Success - exit retry loop
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    // Retry with exponential backoff
                    _logger.Warning(ex, "Poll attempt {Attempt} failed, retrying in {DelaySeconds}s...",
                        attempt + 1, delays[attempt].TotalSeconds);
                    await Task.Delay(delays[attempt], cancellationToken);
                }
                catch (Exception ex)
                {
                    // Final failure after all retries - raise error and keep running
                    _logger.Error(ex, "Poll failed after {MaxAttempts} attempts", maxRetries + 1);
                    PollingError?.Invoke(this, ex.Message);
                    return;
                }
            }
        }
    }
}