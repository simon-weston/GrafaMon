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
    /// Represents the count of alerts grouped by severity level.
    /// This is an immutable record type.
    /// </summary>
    /// <param name="Critical">Number of critical alerts.</param>
    /// <param name="Warning">Number of warning alerts.</param>
    /// <param name="Info">Number of informational alerts.</param>
    public sealed record AlertCounts(int Critical, int Warning, int Info)
    {
        // Total number of alerts
        public int Total => Critical + Warning + Info;
        // Indicates if there are any critical alerts
        public bool HasCritical => Critical > 0;
    }
}
