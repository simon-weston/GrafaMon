// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using GrafaMon.Application.Guards;
using Serilog;
using AppSettings = GrafaMon.Application.AppSettings;
using ConfigHelper = GrafaMon.Application.ConfigurationHelper;

namespace GrafaMon.Wpf
{
    public partial class SettingsWindow : Window
    {
        private readonly string _configPath;
        private readonly ILogger? _logger;
        private readonly bool _isFirstRun;
        private AppSettings? _currentSettings;

        // Static HttpClient for connection testing 
        private static readonly HttpClient _testHttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

        public SettingsWindow(string configPath, ILogger? logger = null, bool isFirstRun = false)
        {
            InitializeComponent();
            _configPath = configPath;
            _logger = logger;
            _isFirstRun = isFirstRun;

            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    // Use ConfigurationHelper to load and decrypt settings
                    _currentSettings = ConfigHelper.LoadAndDecrypt(_configPath, _logger);

                    if (_currentSettings != null)
                    {
                        TxtGrafanaBaseUrl.Text = _currentSettings.GrafanaBaseUrl;
                        PwdApiKey.Password = _currentSettings.ApiKey; // Already decrypted
                        TxtOrgId.Text = _currentSettings.GrafanaOrgId;
                        TxtActiveAlertsPath.Text = _currentSettings.ActiveAlertsPath;
                        TxtPollingInterval.Text = _currentSettings.PollingIntervalSeconds.ToString();
                        TxtMaxAlertDetailRows.Text = _currentSettings.MaxAlertDetailRows.ToString();
                        ChkEnableSoundNotifications.IsChecked = _currentSettings.EnableSoundNotifications;
                        TxtSoundFilePath.Text = _currentSettings.SoundFilePath ?? string.Empty;
                        ChkEnableLogging.IsChecked = _currentSettings.EnableLogging;
                        CmbLogLevel.SelectedIndex = GetLogLevelIndex(_currentSettings.LogLevel);
                        TxtMaxLogFileSizeMB.Text = _currentSettings.MaxLogFileSizeMB.ToString();
                        TxtMaxLogFileCount.Text = _currentSettings.MaxLogFileCount.ToString();
                    }
                }
                else
                {
                    SetDefaultValues();
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Failed to load settings from config file");
                SetDefaultValues();
            }

            UpdateSoundControlsState();
        }

        private void SetDefaultValues()
        {
            TxtGrafanaBaseUrl.Text = "https://YOUR-GRAFANA-INSTANCE.grafana.net";
            TxtOrgId.Text = "1";
            TxtActiveAlertsPath.Text = "/api/alertmanager/grafana/api/v2/alerts?active=true&silenced=false&inhibited=false";
            TxtPollingInterval.Text = "20";
            TxtMaxAlertDetailRows.Text = "25";
            ChkEnableSoundNotifications.IsChecked = true;
            TxtSoundFilePath.Text = string.Empty;
            ChkEnableLogging.IsChecked = true;
            CmbLogLevel.SelectedIndex = 1; // Information
            TxtMaxLogFileSizeMB.Text = "10";
            TxtMaxLogFileCount.Text = "5";
        }

        private int GetLogLevelIndex(string logLevel)
        {
            return logLevel?.ToLowerInvariant() switch
            {
                "debug" => 0,
                "information" => 1,
                "warning" => 2,
                "error" => 3,
                "fatal" => 4,
                _ => 1
            };
        }

        private string GetLogLevelString(int index)
        {
            return index switch
            {
                0 => "Debug",
                1 => "Information",
                2 => "Warning",
                3 => "Error",
                4 => "Fatal",
                _ => "Information"
            };
        }

        private void OnEnableSoundChanged(object sender, RoutedEventArgs e)
        {
            UpdateSoundControlsState();
        }

        private void UpdateSoundControlsState()
        {
            bool isEnabled = ChkEnableSoundNotifications.IsChecked ?? false;
            TxtSoundFilePath.IsEnabled = isEnabled;
            BtnBrowseSound.IsEnabled = isEnabled;
        }

        private void OnBrowseSoundClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*",
                Title = "Select Sound File"
            };

            if (dialog.ShowDialog() == true)
            {
                TxtSoundFilePath.Text = dialog.FileName;
            }
        }

        private void OnTestConnectionClick(object sender, RoutedEventArgs e)
        {
            // Fire and forget with proper error handling
            _ = TestConnectionAsync();
        }

        private async Task TestConnectionAsync()
        {
            _logger?.Debug("Test Connection button clicked");
            
            try
            {
                // Update UI on UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    TxtConnectionStatus.Visibility = Visibility.Collapsed;
                    BtnTestConnection.IsEnabled = false;
                    BtnTestConnection.Content = "Testing...";
                });

                var baseUrl = string.Empty;
                var apiKey = string.Empty;
                var orgId = string.Empty;
                var alertsPath = string.Empty;

                // Get values from UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    baseUrl = TxtGrafanaBaseUrl.Text?.Trim() ?? string.Empty;
                    apiKey = PwdApiKey.Password?.Trim() ?? string.Empty;
                    orgId = TxtOrgId.Text?.Trim() ?? string.Empty;
                    alertsPath = TxtActiveAlertsPath.Text?.Trim() ?? string.Empty;
                });

                _logger?.Debug("Test connection parameters - BaseUrl: {BaseUrl}, OrgId: {OrgId}, Path: {Path}, ApiKeyLength: {ApiKeyLength}", 
                    baseUrl, orgId, alertsPath, apiKey.Length);

                try
                {
                    Guard.AgainstNullOrWhiteSpace(baseUrl);
                    Guard.AgainstNullOrWhiteSpace(apiKey);
                }
                catch (ArgumentException ex)
                {
                    _logger?.Warning("Test connection failed - {Message}", ex.Message);
                    ShowConnectionStatus("Please fill in Grafana Base URL and API Key", false);
                    return;
                }

                try
                {
                    Guard.AgainstInvalidUrl(baseUrl);
                }
                catch (ArgumentException)
                {
                    _logger?.Warning("Test connection failed - invalid base URL: {BaseUrl}", baseUrl);
                    ShowConnectionStatus("Invalid Grafana Base URL format", false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(orgId))
                {
                    orgId = "1"; // Default to org 1
                }

                if (string.IsNullOrWhiteSpace(alertsPath))
                {
                    alertsPath = "/api/alertmanager/grafana/api/v2/alerts";
                }

                // Use static HttpClient 
                var request = new HttpRequestMessage(HttpMethod.Get, 
                    new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), alertsPath.TrimStart('/')));
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Headers.Add("X-Grafana-Org-Id", orgId);
                request.Headers.Add("User-Agent", "GrafaMon/1.0");

                var url = request.RequestUri;
                _logger?.Debug("Testing connection to URL: {Url}", url);
                
                var response = await _testHttpClient.SendAsync(request);
                _logger?.Debug("Received response with status code: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger?.Information("Test connection successful - Status: {StatusCode}, Content length: {ContentLength}", 
                        response.StatusCode, content.Length);
                    ShowConnectionStatus("? Connection successful! Grafana API is accessible.", true);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.Warning("Test connection failed - Status: {StatusCode}, Reason: {ReasonPhrase}, Content: {Content}", 
                        response.StatusCode, response.ReasonPhrase, errorContent);
                    
                    var errorMessage = $"? Connection failed: HTTP {(int)response.StatusCode}";
                    if (!string.IsNullOrWhiteSpace(response.ReasonPhrase))
                    {
                        errorMessage += $" - {response.ReasonPhrase}";
                    }
                    ShowConnectionStatus(errorMessage, false);
                }
            }
            catch (TaskCanceledException)
            {
                _logger?.Warning("Test connection timed out");
                ShowConnectionStatus("? Connection timed out (10 seconds). Check URL and network.", false);
            }
            catch (HttpRequestException ex)
            {
                _logger?.Error(ex, "Test connection HTTP request failed");
                ShowConnectionStatus($"? Connection failed: {ex.Message}", false);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Test connection threw exception");
                ShowConnectionStatus($"? Unexpected error: {ex.Message}", false);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    BtnTestConnection.IsEnabled = true;
                    BtnTestConnection.Content = "Test Connection";
                });
                _logger?.Debug("Test connection completed, button re-enabled");
            }
        }

        private void ShowConnectionStatus(string message, bool success)
        {
            _logger?.Debug("Showing connection status: {Message}, Success: {Success}", message, success);
            
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowConnectionStatus(message, success));
                return;
            }
            
            TxtConnectionStatus.Text = message;
            TxtConnectionStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                success ? System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50) : System.Windows.Media.Color.FromRgb(0xF4, 0x43, 0x36));
            TxtConnectionStatus.Visibility = Visibility.Visible;
            
            // Force layout update
            TxtConnectionStatus.InvalidateVisual();
            TxtConnectionStatus.UpdateLayout();
            
            _logger?.Debug("Connection status visibility set to Visible, text: {Text}", TxtConnectionStatus.Text);
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = new AppSettings
                {
                    GrafanaBaseUrl = TxtGrafanaBaseUrl.Text.Trim(),
                    ApiKey = PwdApiKey.Password.Trim(),
                    IsApiKeyEncrypted = false, // Will be encrypted during save
                    GrafanaOrgId = TxtOrgId.Text.Trim(),
                    ActiveAlertsPath = TxtActiveAlertsPath.Text.Trim(),
                    PollingIntervalSeconds = int.Parse(TxtPollingInterval.Text),
                    MaxAlertDetailRows = int.Parse(TxtMaxAlertDetailRows.Text),
                    EnableSoundNotifications = ChkEnableSoundNotifications.IsChecked ?? false,
                    SoundFilePath = string.IsNullOrWhiteSpace(TxtSoundFilePath.Text) ? null : TxtSoundFilePath.Text.Trim(),
                    EnableLogging = ChkEnableLogging.IsChecked ?? true,
                    LogLevel = GetLogLevelString(CmbLogLevel.SelectedIndex),
                    MaxLogFileSizeMB = int.Parse(TxtMaxLogFileSizeMB.Text),
                    MaxLogFileCount = int.Parse(TxtMaxLogFileCount.Text)
                };

                ConfigHelper.ValidateConfig(settings, _logger);

                // Use ConfigurationHelper to save with encryption
                ConfigHelper.SaveEncryptedConfig(_configPath, settings, _logger);

                _logger?.Information("Configuration saved successfully to {ConfigPath}", _configPath);

                // On first run, don't offer to restart - just close
                if (_isFirstRun)
                {
                    System.Windows.MessageBox.Show(
                        "Settings saved successfully!\n\nClick OK to continue.",
                        "Settings Saved",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    // Normal settings change - offer to restart
                    var result = System.Windows.MessageBox.Show(
                        "Settings saved successfully!\n\nThe application needs to restart to apply changes.\n\nDo you want to restart now?",
                        "Settings Saved",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Use Environment.ProcessPath for secure restart
                        var exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                        
                        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = true,
                                WorkingDirectory = Path.GetDirectoryName(exePath)
                            });
                        }
                        
                        System.Windows.Application.Current.Shutdown();
                    }
                    else
                    {
                        DialogResult = true;
                        Close();
                    }
                }
            }
            catch (FormatException)
            {
                System.Windows.MessageBox.Show(
                    "Invalid input format. Please check numeric fields.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (InvalidOperationException ex)
            {
                System.Windows.MessageBox.Show(
                    $"Validation failed:\n\n{ex.Message}",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Failed to save configuration");
                System.Windows.MessageBox.Show(
                    $"Failed to save settings:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            if (_isFirstRun)
            {
                var result = System.Windows.MessageBox.Show(
                    "No configuration exists. The application will exit.\n\nAre you sure you want to cancel?",
                    "Cancel Setup",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    System.Windows.Application.Current.Shutdown();
                }
            }
            else
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
