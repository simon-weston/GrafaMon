// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GrafaMon.Application;
using GrafaMon.Infrastructure;
using Serilog;
using Serilog.Events;
using System.Reflection;
using Microsoft.Extensions.Primitives;
using System.Drawing;
using System.Windows.Forms;

namespace GrafaMon.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private IHost? _host;
        private NotifyIcon? _trayIcon;
        private HelpWindow? _helpWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Disable Windows in-app toolbar
            AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableStylusAndTouchSupport", true);

            try
            {
                // Build host with Serilog
                _host = Host.CreateDefaultBuilder()
                    .UseSerilog((context, services, configuration) =>
                    {
                        // Get settings to configure Serilog - use LoadAndDecrypt to handle encrypted keys
                        var configPath = ConfigurationHelper.GetConfigFilePath();
                        AppSettings? settings = null;
                        
                        try
                        {
                            if (File.Exists(configPath))
                            {
                                settings = ConfigurationHelper.LoadAndDecrypt(configPath, null);
                            }
                        }
                        catch
                        {
                            // If decryption fails, try standard deserialization (for backward compatibility)
                            try
                            {
                                settings = context.Configuration.GetRequiredSection("GrafanaAlertInstance").Get<AppSettings>();
                            }
                            catch
                            {
                                // Ignore - we'll use default logging
                            }
                        }

                        if (settings == null || !settings.EnableLogging)
                        {
                            // Disable logging
                            configuration.MinimumLevel.Fatal();
                            return;
                        }

                        // Parse log level
                        var logLevel = ParseLogLevel(settings.LogLevel);

                        // Configure log directory - use hardcoded app name for single-file apps
                        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        var logDirectory = Path.Combine(appDataPath, "GrafaMon", "logs");
                        Directory.CreateDirectory(logDirectory);

                        var logFilePath = Path.Combine(logDirectory, "GrafaMon_.log");

                        // Configure Serilog
                        configuration
                            .MinimumLevel.Is(logLevel)
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                            .MinimumLevel.Override("System", LogEventLevel.Warning)
                            .Enrich.FromLogContext()
                            .Enrich.WithProperty("Application", $"GrafaMon")
                            .Enrich.WithProperty("Version", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown")
                            .Enrich.WithThreadId()
                            .WriteTo.File(
                                path: logFilePath,
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [Thread:{ThreadId:D5}] {Message:lj}{NewLine}{Exception}",
                                rollingInterval: RollingInterval.Day,
                                rollOnFileSizeLimit: true,
                                fileSizeLimitBytes: settings.MaxLogFileSizeMB * 1024L * 1024L,
                                retainedFileCountLimit: settings.MaxLogFileCount,
                                shared: false,
                                flushToDiskInterval: TimeSpan.FromSeconds(1)
                            );

                        // Also log to console in debug builds
#if DEBUG
                        configuration.WriteTo.Console(
                            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [Thread:{ThreadId:D5}] {Message:lj}{NewLine}{Exception}"
                        );
#endif
                    })
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        var configPath = ConfigurationHelper.GetConfigFilePath();
                        
                        // Validate configPath before using it
                        if (string.IsNullOrWhiteSpace(configPath))
                        {
                            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            var errorMsg = $"CRITICAL ERROR:\nGetConfigFilePath() returned empty!\n\nAppData folder: {appData}\nIs AppData empty? {string.IsNullOrEmpty(appData)}";
                            System.Windows.MessageBox.Show(errorMsg, "Configuration Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            throw new InvalidOperationException(errorMsg);
                        }
                        
                        // Check if the config file exists BEFORE creating it
                        bool configExists = File.Exists(configPath);

                        // Ensure config exists
                        ConfigurationHelper.EnsureConfigFileExists(configPath);
                        config.AddJsonFile(configPath, optional: false, reloadOnChange: true);

                        // If config was just created, show settings window on first run
                        if (!configExists)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var settingsWindow = new SettingsWindow(configPath, null, true);
                                settingsWindow.ShowDialog();
                            });
                        }
                    })
                    .ConfigureServices((context, services) =>
                    {
                        // Load settings with decryption (if encrypted)
                        var configPath = ConfigurationHelper.GetConfigFilePath();
                        AppSettings settings;
                        
                        try
                        {
                            // Use LoadAndDecrypt to properly handle encrypted API keys
                            settings = ConfigurationHelper.LoadAndDecrypt(configPath, null);
                        }
                        catch (Exception ex)
                        {
                            // Fallback to standard deserialization if LoadAndDecrypt fails
                            var fallbackSettings = context.Configuration.GetRequiredSection("GrafanaAlertInstance").Get<AppSettings>();
                            if (fallbackSettings == null)
                            {
                                throw new InvalidOperationException($"Failed to load configuration: {ex.Message}", ex);
                            }
                            settings = fallbackSettings;
                        }

                        var logger = _host?.Services.GetRequiredService<Serilog.ILogger>();

                        // Validate config (with decrypted API key)
                        ConfigurationHelper.ValidateConfig(settings, logger);

                        services.AddSingleton(settings);

                        // Monitor for config changes - log warning
                        ChangeToken.OnChange(
                            () => context.Configuration.GetReloadToken(),
                            () =>
                            {
                                logger?.Warning("Configuration file has changed. Please restart the application to apply changes.");
                            });

                        // HttpClient
                        services.AddHttpClient<IGrafanaAlertsReader, GrafanaAlertsReader>(client =>
                        {
                            client.Timeout = TimeSpan.FromSeconds(30); //30 seconds timeout
                            client.DefaultRequestHeaders.UserAgent.ParseAdd($"GrafaMon/1.0");
                        }
                        );

                        // Core services
                        services.AddSingleton<AlertPollingService>();
                        services.AddSingleton<SoundNotificationService>();

                        // ViewModel + window
                        services.AddSingleton<AlertWidgetViewModel>();
                        services.AddSingleton<MainWindow>();
                    })
                    .Build();

                var logger = _host.Services.GetRequiredService<Serilog.ILogger>();

                logger.Information("=== Application starting ===");
                logger.Information("OS: {OS}, User: {User}", Environment.OSVersion, Environment.UserName);

                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();

                InitializeTrayIcon();

                logger.Information("Main window displayed successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to start application");

                // Use hardcoded app name for error messages
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var configPath = Path.Combine(appDataPath, "GrafaMon", "config.json");

                System.Windows.MessageBox.Show(
                    $"Failed to start application:\n\n{ex.Message}\n\nConfig file location:\n{configPath}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_host != null)
            {
                var logger = _host.Services.GetService<Serilog.ILogger>();
                logger?.Information("=== Application exiting (exit code: {ExitCode}) ===", e.ApplicationExitCode);

                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await _host.StopAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                    logger?.Warning("Host shutdown timed out after 5 seconds");
                }
                finally
                {
                    _host.Dispose();
                }
            }

            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private void InitializeTrayIcon()
        {
            if (_trayIcon != null)
            {
                return;
            }

            // For single-file apps, Assembly.GetExecutingAssembly().Location returns empty
            // Use Process.GetCurrentProcess().MainModule.FileName instead
            Icon icon;
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                icon = !string.IsNullOrEmpty(exePath) 
                    ? Icon.ExtractAssociatedIcon(exePath) ?? SystemIcons.Application
                    : SystemIcons.Application;
            }
            catch
            {
                // Fallback to system icon if extraction fails
                icon = SystemIcons.Application;
            }

            var contextMenu = new ContextMenuStrip();
            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (_, _) => ShowSettingsWindow();

            var helpItem = new ToolStripMenuItem("Help");
            helpItem.Click += (_, _) => ShowHelpWindow();

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) => Shutdown();

            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(helpItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _trayIcon = new NotifyIcon
            {
                Icon = icon,
                Text = "GrafaMon",
                Visible = true,
                ContextMenuStrip = contextMenu
            };
        }

        private void ShowSettingsWindow()
        {
            Dispatcher.Invoke(() =>
            {
                var configPath = ConfigurationHelper.GetConfigFilePath();
                var logger = _host?.Services.GetService<Serilog.ILogger>();
                var settingsWindow = new SettingsWindow(configPath, logger, false);
                settingsWindow.ShowDialog();
            });
        }

        private void ShowHelpWindow()
        {
            Dispatcher.Invoke(() =>
            {
                if (_helpWindow == null)
                {
                    _helpWindow = new HelpWindow();
                    _helpWindow.Closed += (_, _) => _helpWindow = null;
                }

                _helpWindow.Show();
                _helpWindow.Activate();
            });
        }

        private static LogEventLevel ParseLogLevel(string level)
        {
            return level?.Trim().ToLowerInvariant() switch
            {
                "debug" => LogEventLevel.Debug,
                "information" => LogEventLevel.Information,
                "warning" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                "fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }
    }
}