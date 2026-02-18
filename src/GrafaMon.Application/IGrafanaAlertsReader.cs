// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GrafaMon.Domain;

namespace GrafaMon.Application
{
    public interface IGrafanaAlertsReader
    {
        // Returns the current active alert counts and their details.
        Task<(AlertCounts Counts, IReadOnlyList<AlertDetail> Details)> GetActiveAlertsAsync(CancellationToken cancellationToken);
    }
}
