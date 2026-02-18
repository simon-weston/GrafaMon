// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GrafaMon.Wpf
{
    /// <summary>
    /// Converts a severity string to a background color brush.
    /// This is used to color DataGrid cells based on alert severity.
    /// </summary>
    public class SeverityToBackgroundConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string severityStr)
            {
                return severityStr.ToLowerInvariant() switch
                {
                    "critical" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x8B, 0x00, 0x00)), // Dark Red
                    "warning" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB8, 0x86, 0x0B)), // Dark Orange
                    "info" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x00, 0x8B)), // Dark Blue
                    _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33)) // Default grey
                };
            }

            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33)); // Default grey
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}