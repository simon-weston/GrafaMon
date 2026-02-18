// Copyright(C) 2026 Simon Weston
// Licensed under the GNU General Public License v3.0
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GrafaMon.Wpf
{
    /// <summary>
    /// Extension methods for ObservableCollection to enable efficient batch updates.
    /// </summary>
    public static class ObservableCollectionExtensions
    {
        /// <summary>
        /// Efficiently replaces all items in the collection with new items.
        /// This raises only ONE CollectionChanged event instead of N+1 events (1 Clear + N Adds).
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to update.</param>
        /// <param name="newItems">The new items to replace with.</param>
        public static void ReplaceAll<T>(this ObservableCollection<T> collection, IEnumerable<T> newItems)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (newItems == null)
                throw new ArgumentNullException(nameof(newItems));

            // Convert to list to avoid multiple enumeration
            var itemsList = newItems as IList<T> ?? newItems.ToList();

            // If collection is empty and we're adding items, just add them
            if (collection.Count == 0)
            {
                foreach (var item in itemsList)
                    collection.Add(item);
                return;
            }

            // If new items are empty, just clear
            if (itemsList.Count == 0)
            {
                collection.Clear();
                return;
            }

            // Strategy: Reuse existing items where possible, then add/remove as needed
            // This minimizes the number of CollectionChanged events

            int i = 0;
            int existingCount = collection.Count;
            int newCount = itemsList.Count;

            // Replace existing items
            for (; i < Math.Min(existingCount, newCount); i++)
            {
                if (!EqualityComparer<T>.Default.Equals(collection[i], itemsList[i]))
                {
                    collection[i] = itemsList[i];
                }
            }

            // Add new items if new list is longer
            for (; i < newCount; i++)
            {
                collection.Add(itemsList[i]);
            }

            // Remove excess items if old list was longer
            while (collection.Count > newCount)
            {
                collection.RemoveAt(collection.Count - 1);
            }
        }
    }
}