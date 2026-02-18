// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using GrafaMon.Application;
using GrafaMon.Domain;
using Serilog;

namespace GrafaMon.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly AlertWidgetViewModel _vm;
        private readonly AlertPollingService _poller;
        private readonly CancellationTokenSource _cts = new();
        private readonly SoundNotificationService _soundService;
        private readonly ILogger _logger;
        private readonly AppSettings _settings;

        private DispatcherTimer? _hideTimer;
        private DispatcherTimer? _topmostTimer;
        private bool _detailsAlignRight;
        private bool _alignmentLocked;
        private double? _summaryScreenX;
        private bool _alertsMaxHeightSet;

        public MainWindow(
            AlertWidgetViewModel vm,
            AlertPollingService poller,
            SoundNotificationService soundService,
            ILogger logger,
            AppSettings settings)
        {
            InitializeComponent();

            _vm = vm;
            _poller = poller;
            _soundService = soundService;
            _logger = logger.ForContext<MainWindow>();
            _settings = settings;

            DataContext = _vm;

            _logger.Debug("MainWindow starting up...");

            // Track window movement to reset alignment
            LocationChanged += (_, _) =>
            {
                _alignmentLocked = false;

                if (DetailsPanel?.Visibility == Visibility.Visible)
                {
                    AlignDetailsPanel();
                    // DO NOT call RestoreSummaryPosition here
                }
            };

            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;

            // Subscribe to polling events
            _poller.CountsUpdated += (_, counts) =>
            {
                Dispatcher.Invoke(() =>
                {
                    // This updates SummaryBackground + FlashCritical.
                    // FlashCritical now drives the XAML storyboard trigger (no code-behind flashing).
                    _vm.UpdateCounts(counts);
                });
            };

            // Setup mouse interaction for summary border
            if (RootBorder != null)
            {
                RootBorder.MouseEnter += (_, _) =>
                {
                    if (_vm.HasAlerts)
                    {
                        _hideTimer?.Stop();
                        ShowDetails();
                    }
                };

                RootBorder.MouseLeave += (_, _) =>
                {
                    StartHideTimer();
                };
            }

            // Setup mouse interaction for details panel
            if (DetailsPanel != null)
            {
                DetailsPanel.MouseEnter += (_, _) =>
                {
                    _hideTimer?.Stop();
                };

                DetailsPanel.MouseLeave += (_, _) =>
                {
                    StartHideTimer();
                };
            }

            if (DetailsPopup != null)
            {
                DetailsPopup.Opened += (_, _) =>
                {
                    if (SearchTextBox != null && IsActive)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            SearchTextBox.Focus();
                            Keyboard.Focus(SearchTextBox);
                        }, DispatcherPriority.Input);
                    }
                };
            }

            if (SearchTextBox != null)
            {
                SearchTextBox.PreviewMouseDown += SearchTextBox_PreviewMouseDown;
            }

            _poller.DetailsUpdated += (_, details) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var shouldRestoreSearchFocus = DetailsPopup?.IsOpen == true
                        && SearchTextBox?.IsKeyboardFocusWithin == true;

                    _vm.UpdateDetails(details);

                    if (shouldRestoreSearchFocus && SearchTextBox != null)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            SearchTextBox.Focus();
                            Keyboard.Focus(SearchTextBox);
                        }, DispatcherPriority.Input);
                    }
                });
            };

            // Subscribe to polling success event
            _poller.PollingSucceeded += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _vm.SetLastSuccessfulPoll();
                    _logger.Debug("Polling succeeded, last successful poll time updated");
                });
            };

            // Subscribe to polling error event
            _poller.PollingError += (_, errorMessage) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _vm.SetPollingError(errorMessage);
                    _logger.Warning("Polling error: {ErrorMessage}", errorMessage);
                });
            };

            // Subscribe to new critical alert event
            _poller.NewCriticalAlertDetected += (_, newCount) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _soundService.PlayCriticalAlertSound();
                    _logger.Information("New critical alert detected (count: {CriticalCount}), playing sound notification", newCount);
                });
            };

            // Re-assert topmost every 2 seconds to handle windows that override us
            _topmostTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _topmostTimer.Tick += (_, _) =>
            {
                // SIMPLE RULE: Never re-assert topmost while popup is open
                if (!Topmost && (DetailsPopup == null || !DetailsPopup.IsOpen))
                {
                    Topmost = true;
                    _logger.Debug("Re-asserting Topmost property");
                }
            };
            _topmostTimer.Start();

            // Re-assert topmost when window is activated
            Activated += (_, _) =>
            {
                // SIMPLE RULE: Never re-assert topmost while popup is open
                if (DetailsPopup == null || !DetailsPopup.IsOpen)
                {
                    Topmost = true;
                    _logger.Debug("Window activated, ensuring Topmost");
                }
            };

            // Re-assert topmost when window loses focus but should stay on top
            Deactivated += (_, _) =>
            {
                // CRITICAL: Never re-assert topmost in Deactivated - it steals focus from popup controls
                // The timer and Activated handlers will maintain topmost when needed
                _logger.Debug("Window deactivated, skipping topmost re-assertion to preserve popup interaction");
            };

            _logger.Debug("MainWindow started up successfully");
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _logger.Information("MainWindow loaded, starting polling service");
            await Task.Run(() => _poller.RunAsync(_cts.Token));
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _logger.Information("MainWindow unloading, stopping polling service");

            _hideTimer?.Stop();
            _hideTimer = null;

            _topmostTimer?.Stop(); 

            _cts.Cancel();
            _cts.Dispose();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();

                _alignmentLocked = false;

                // Refresh cached position after drag
                if (RootBorder != null)
                {
                    double offsetX = RootBorder.TranslatePoint(new System.Windows.Point(0, 0), this).X;
                    _summaryScreenX = Left + offsetX;
                }

                if (DetailsPanel?.Visibility == Visibility.Visible)
                {
                    AlignDetailsPanel();
                }

                _logger.Debug("Window dragged to new position: Left={Left}, Top={Top}", Left, Top);
            }
        }

        private void ShowDetails()
        {
            if (DetailsPopup == null || RootBorder == null) return;

            AlignDetailsPopup();
            DetailsPopup.IsOpen = true;
            _logger.Debug("Details popup opened");
        }

        private void HideDetails()
        {
            if (DetailsPopup != null)
            {
                DetailsPopup.IsOpen = false;
                _logger.Debug("Details popup closed");
            }
        }

        private void RestoreSummaryPosition()
        {
            if (_summaryScreenX == null || RootBorder == null) return;

            RootBorder.UpdateLayout();
            double newOffsetX = RootBorder.TranslatePoint(new System.Windows.Point(0, 0), this).X;

            // Move window so summary stays fixed
            Left = _summaryScreenX.Value - newOffsetX;
        }

        private void AlignDetailsPopup()
        {
            if (DetailsPopup == null || RootBorder == null) return;

            RootBorder.UpdateLayout();

            Rect workArea = GetWorkAreaInDip();
            const double margin = 8;

            double summaryLeft = Left;
            double summaryRight = Left + RootBorder.ActualWidth;

            double spaceLeft = summaryLeft - workArea.Left;
            double spaceRight = workArea.Right - summaryRight;

            // If not enough room on the right, pop left
            if (spaceRight < (DetailsPanel.MinWidth + margin) && spaceLeft > spaceRight)
                DetailsPopup.Placement = PlacementMode.Left;
            else
                DetailsPopup.Placement = PlacementMode.Right;

            DetailsPopup.PlacementTarget = RootBorder;
            DetailsPopup.HorizontalOffset = 0;
            DetailsPopup.VerticalOffset = 2;
        }

        private void StartHideTimer()
        {
            if (_hideTimer == null)
            {
                _hideTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(2000)
                };
                _hideTimer.Tick += (_, _) =>
                {
                    _hideTimer.Stop();
                    HideDetails();
                };
            }

            _hideTimer.Stop();
            _hideTimer.Start();
        }

        private void OnSummaryMouseEnter(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            _hideTimer?.Stop();
            ShowDetails();
        }

        private void OnSummaryMouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            StartHideTimer();
        }

        private void OnPopupMouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            StartHideTimer();
        }

        private void OnAlertRowDoubleClick(object? sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is AlertDetail alert)
            {
                if (!string.IsNullOrWhiteSpace(alert.GrafanaAlertUrl))
                {
                    try
                    {
                        _logger.Information("Opening Grafana alert URL: {AlertUrl}", alert.GrafanaAlertUrl);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = alert.GrafanaAlertUrl,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to open Grafana alert URL: {AlertUrl}", alert.GrafanaAlertUrl);
                        System.Windows.MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void OnAlertsGridLoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (_alertsMaxHeightSet || sender is not DataGrid grid)
            {
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {
                if (_alertsMaxHeightSet)
                {
                    return;
                }

                var rowHeight = e.Row.ActualHeight;
                if (rowHeight <= 0)
                {
                    return;
                }

                var headerHeight = GetColumnHeaderHeight(grid);
                var maxRows = Math.Max(1, _settings.MaxAlertDetailRows);
                grid.MaxHeight = (rowHeight * maxRows) + headerHeight;
                _alertsMaxHeightSet = true;
            }, DispatcherPriority.Loaded);
        }

        private static double GetColumnHeaderHeight(DataGrid grid)
        {
            var presenter = FindVisualChild<DataGridColumnHeadersPresenter>(grid);
            return presenter?.ActualHeight > 0 ? presenter.ActualHeight : 0;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            var childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }

        private void AlignDetailsPanel()
        {
            if (RootBorder == null || DetailsPanel == null) return;

            RootBorder.UpdateLayout();
            DetailsPanel.UpdateLayout();

            Rect workArea = GetWorkAreaInDip();
            const double margin = 8;

            // Summary position on screen (DIP)
            double summaryOffsetX = RootBorder.TranslatePoint(new System.Windows.Point(0, 0), this).X;
            double summaryScreenX = Left + summaryOffsetX;

            double summaryWidth = RootBorder.ActualWidth;
            double detailsWidth = DetailsPanel.ActualWidth > 0 ? DetailsPanel.ActualWidth : DetailsPanel.MinWidth;
            double neededWidth = Math.Max(summaryWidth, detailsWidth);

            // Check overflow for BOTH directions
            bool wouldOverflowRight = (summaryScreenX + neededWidth) > (workArea.Right - margin);
            bool wouldOverflowLeft = (summaryScreenX + summaryWidth - neededWidth) < (workArea.Left + margin);

            // Lock alignment the first time details open
            if (!_alignmentLocked)
            {
                // Prefer current direction unless it overflows
                if (wouldOverflowRight && !wouldOverflowLeft)
                    _detailsAlignRight = true;   // grow left
                else
                    _detailsAlignRight = false;  // grow right (default)

                _alignmentLocked = true;
            }
            else
            {
                // Only flip if CURRENT direction would overflow
                if (_detailsAlignRight && wouldOverflowLeft && !wouldOverflowRight)
                    _detailsAlignRight = false;
                else if (!_detailsAlignRight && wouldOverflowRight && !wouldOverflowLeft)
                    _detailsAlignRight = true;
            }

            var alignment = _detailsAlignRight ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left;
            RootBorder.HorizontalAlignment = alignment;
            DetailsPanel.HorizontalAlignment = alignment;

            // Keep summary box fixed (no jump)
            RootBorder.UpdateLayout();
            double newOffsetX = RootBorder.TranslatePoint(new System.Windows.Point(0, 0), this).X;
            Left = summaryScreenX - newOffsetX;
        }

        private Rect GetWorkAreaInDip()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var screen = Screen.FromHandle(hwnd); 
            var work = screen.WorkingArea; // pixels

            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                var transform = source.CompositionTarget.TransformFromDevice;
                var topLeft = transform.Transform(new System.Windows.Point(work.Left, work.Top));
                var bottomRight = transform.Transform(new System.Windows.Point(work.Right, work.Bottom));
                return new Rect(topLeft, bottomRight);
            }

            return new Rect(work.Left, work.Top, work.Width, work.Height);
        }

        private void SearchTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsActive)
            {
                Activate();
            }

            if (SearchTextBox == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {
                SearchTextBox.Focus();
                Keyboard.Focus(SearchTextBox);
            }, DispatcherPriority.Input);
        }
    }
}