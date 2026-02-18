// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrafaMon.Domain
{
    /// <summary>
    /// Represents the severity level of an alert.
    /// Values are ordered by severity (higher value = more severe).
    /// </summary>
    public enum AlertSeverity
    {
        Info = 1,
        Warning = 2,
        Critical = 3,
        Unknown = 0
    }
}
