// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System.Reflection;
using System.Windows;

namespace GrafaMon.Wpf
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            
            // Display version from assembly
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            TxtVersion.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
