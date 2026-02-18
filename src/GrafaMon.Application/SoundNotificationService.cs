// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Media;
using GrafaMon.Application.Guards;
using Serilog;

namespace GrafaMon.Application
{
    public sealed class SoundNotificationService
    {
        private readonly AppSettings _settings;
        private readonly ILogger _logger;
        private DateTime _lastSoundPlayedUtc = DateTime.MinValue;
        private readonly TimeSpan _debounceInterval = TimeSpan.FromSeconds(5);

        public SoundNotificationService(AppSettings settings, ILogger logger)
        {
            Guard.AgainstNull(settings, nameof(settings));
            Guard.AgainstNull(logger, nameof(logger));
            
            _settings = settings;
            _logger = logger.ForContext<SoundNotificationService>();
        }

        public void PlayCriticalAlertSound()
        {
            if (!_settings.EnableSoundNotifications)
            {
                _logger.Debug("Sound notifications disabled, skipping sound");
                return;
            }

            //Debounce: Ensure that multiple rapid calls do not overlap sounds or if sound recently played
            var nowUtc = DateTime.UtcNow;
            if ((nowUtc - _lastSoundPlayedUtc) < _debounceInterval)
            {
                _logger.Debug("Sound notification recently played, skipping sound (last played {Seconds}s ago)", (nowUtc - _lastSoundPlayedUtc).TotalSeconds);
                return;
            }
            _lastSoundPlayedUtc = nowUtc;

            try
            {
                // Use default Windows sound if no custom sound file specified OR custom sound file does not exist
                if (string.IsNullOrWhiteSpace(_settings.SoundFilePath) || !File.Exists(_settings.SoundFilePath))
                {
                    // Use Windows default critical stop sound
                    _logger.Debug("Playing Windows default critical stop sound");
                    SystemSounds.Hand.Play();
                }
                else
                {
                    //Check sound file format
                    var soundFileExtension = Path.GetExtension(_settings.SoundFilePath).ToLowerInvariant();
                    if (soundFileExtension != ".wav")
                    {
                        _logger.Warning("Unsupported sound file format: {SoundFileExtension}. Only .wav files are supported. Falling back to default sound.", soundFileExtension);
                        SystemSounds.Hand.Play();
                        return;
                    }

                    // Use custom sound file
                    _logger.Debug("Playing custom sound file: {SoundFilePath}", _settings.SoundFilePath);
                    Task.Run(() =>
                    {
                        try
                        {
                            using var player = new SoundPlayer(_settings.SoundFilePath);
                            player.LoadAsync();
                            player.PlaySync(); //Blocks background thread and not the UI thread
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "Failed to play custom sound file {SoundFilePath}", _settings.SoundFilePath);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to play sound notification");
            }
        }
    }
}