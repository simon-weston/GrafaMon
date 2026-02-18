// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using GrafaMon.Domain;

namespace GrafaMon.Wpf
{
    /// ViewModel for the alert widget, implementing INotifyPropertyChanged for data binding.
    public sealed class AlertWidgetViewModel : INotifyPropertyChanged
    {
        // Static frozen brushes for performance (reusable, immutable, zero allocations)
        private static readonly SolidColorBrush CriticalBrush = CreateFrozenBrush(0xA0, 0x00, 0x00);
        private static readonly SolidColorBrush WarningBrush = CreateFrozenBrush(0xFF, 0xA5, 0x00);
        private static readonly SolidColorBrush InfoBrush = CreateFrozenBrush(0x00, 0x80, 0xFF);
        private static readonly SolidColorBrush NoAlertsBrush = CreateFrozenBrush(0x00, 0x80, 0x00);
        private static readonly SolidColorBrush ErrorBrush = CreateFrozenBrush(0x00, 0x00, 0x00);
        private static readonly SolidColorBrush DefaultBrush = CreateFrozenBrush(0x1E, 0x1E, 0x1E);

        private int _critical;
        private int _warning;
        private int _info;
        private bool _flash;
        private DateTime? _lastSuccessfulPollUtc;
        private string? _lastPollingError;
        private bool _hasPollingError;

        // Filtering properties
        private bool _filterCritical = true;
        private bool _filterWarning = true;
        private bool _filterInfo = true;
        private string _searchText = string.Empty;
        private List<AlertDetail> _allAlertDetails = new();

        public int Critical { get => _critical; private set { _critical = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); OnPropertyChanged(nameof(HasAlerts)); } }
        public int Warning { get => _warning; private set { _warning = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); OnPropertyChanged(nameof(HasAlerts)); } }
        public int Info { get => _info; private set { _info = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); OnPropertyChanged(nameof(HasAlerts)); } }
        public bool FlashCritical { get => _flash; private set { _flash = value; OnPropertyChanged(); } }

        public bool HasAlerts => Critical > 0 || Warning > 0 || Info > 0;

        public DateTime? LastSuccessfulPollUtc
        {
            get => _lastSuccessfulPollUtc;
            private set
            {
                _lastSuccessfulPollUtc = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(LastPollTimeText));
            }
        }

        public string? LastPollingError
        {
            get => _lastPollingError;
            private set
            {
                _lastPollingError = value;
                OnPropertyChanged();
            }
        }

        public bool HasPollingError
        {
            get => _hasPollingError;
            private set
            {
                _hasPollingError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        // Filter properties
        public bool FilterCritical
        {
            get => _filterCritical;
            set
            {
                if (_filterCritical != value)
                {
                    _filterCritical = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public bool FilterWarning
        {
            get => _filterWarning;
            set
            {
                if (_filterWarning != value)
                {
                    _filterWarning = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public bool FilterInfo
        {
            get => _filterInfo;
            set
            {
                if (_filterInfo != value)
                {
                    _filterInfo = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value ?? string.Empty;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public string LastPollTimeText
        {
            get
            {
                if (LastSuccessfulPollUtc == null)
                    return string.Empty;

                var elapsed = DateTime.UtcNow - LastSuccessfulPollUtc.Value;
                if (elapsed.TotalMinutes < 1)
                    return "<1m";
                else if (elapsed.TotalMinutes < 60)
                    return $"{(int)elapsed.TotalMinutes}m ago";
                else if (elapsed.TotalHours < 24)
                    return $"{(int)elapsed.TotalHours}h ago";
                else
                    return $"{(int)elapsed.TotalDays}d ago";
            }
        }

        public string DisplayText
        {
            get
            {
                var baseText = $"🔥 {Critical} | ⚠️ {Warning} | ℹ️ {Info} |";
                var pollTime = LastSuccessfulPollUtc.HasValue ? $" ⏱️ {LastPollTimeText}" : string.Empty;
                var errorIndicator = HasPollingError ? " ⚠" : string.Empty;
                return baseText + pollTime + errorIndicator;
            }
        }

        public System.Windows.Media.Brush SummaryBackground { get; private set; } = NoAlertsBrush;

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            brush.Freeze(); // Make immutable for thread safety and performance
            return brush;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<AlertDetail> AlertDetails { get; } = new();

        public void UpdateCounts(AlertCounts counts)
        {
            Debug.Assert(System.Windows.Application.Current.Dispatcher.CheckAccess(), "UpdateCounts must be called from UI thread");

            Critical = counts.Critical;
            Warning = counts.Warning;
            Info = counts.Info;
            FlashCritical = counts.HasCritical;

            UpdateSummaryBackground();
        }

        public void UpdateDetails(IEnumerable<AlertDetail> details)
        {
            // Sort by severity (Critical=3, Warning=2, Info=1, Unknown=0) descending, then by newest first
            var ordered = details
                .OrderByDescending(d => d.Severity)
                .ThenByDescending(d => d.LastUpdatedUtc)
                .ToList();

            _allAlertDetails = ordered;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filtered = _allAlertDetails.AsEnumerable();

            // Filter by severity
            filtered = filtered.Where(d =>
                (d.Severity == AlertSeverity.Critical && FilterCritical) ||
                (d.Severity == AlertSeverity.Warning && FilterWarning) ||
                (d.Severity == AlertSeverity.Info && FilterInfo) ||
                (d.Severity == AlertSeverity.Unknown)  // Always show Unknown
            );

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                filtered = filtered.Where(d =>
                    (d.Summary?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.SeverityNormalized?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Environment?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.MonitorTest?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Host?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            AlertDetails.ReplaceAll(filtered);
        }

        public void SetLastSuccessfulPoll()
        {
            LastSuccessfulPollUtc = DateTime.UtcNow;
            ClearPollingError();
        }

        public void SetPollingError(string errorMessage)
        {
            LastPollingError = errorMessage;
            HasPollingError = true;
            UpdateSummaryBackground();
        }

        public void ClearPollingError()
        {
            LastPollingError = null;
            HasPollingError = false;
            UpdateSummaryBackground();
        }

        private void UpdateSummaryBackground()
        {
            SummaryBackground = HasPollingError ? ErrorBrush :
                Critical > 0 ? CriticalBrush :  // Red for critical
                Warning > 0 ? WarningBrush :    // Orange for warning
                Info > 0 ? InfoBrush :          // Blue for info
                (Critical + Warning + Info == 0 ? NoAlertsBrush : DefaultBrush);

            OnPropertyChanged(nameof(SummaryBackground));
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}