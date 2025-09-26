using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.GameContent;

namespace AemonsHandbookEssentials.HandbookHistory
{
    /// <summary>
    /// Manages the recent pages data structure and provides core operations for tracking handbook history
    /// </summary>
    public static class RecentPagesManager
    {
        // 30 most recently opened handbook page keys (most recent first)
        private static readonly LinkedList<string> RecentPages = new LinkedList<string>();
        private const int MaxRecent = 30;

        /// <summary>
        /// Records a page in the history, moving it to the front if it already exists
        /// </summary>
        public static void RecordPage(GuiHandbookPage page)
        {
            try
            {
                string key = HandbookPageHelper.GetPageKey(page);
                if (string.IsNullOrEmpty(key)) return;
                
                lock (RecentPages)
                {
                    var existing = RecentPages.FirstOrDefault(s => string.Equals(s, key, StringComparison.OrdinalIgnoreCase));
                    if (existing != null) RecentPages.Remove(existing);
                    RecentPages.AddFirst(key);
                    while (RecentPages.Count > MaxRecent) RecentPages.RemoveLast();
                }
            }
            catch { }
        }

        /// <summary>
        /// Gets a snapshot of the recent pages list
        /// </summary>
        public static List<string> GetRecentPages()
        {
            lock (RecentPages)
            {
                return RecentPages.ToList();
            }
        }

        /// <summary>
        /// Gets the count of recent pages
        /// </summary>
        public static int Count
        {
            get
            {
                lock (RecentPages)
                {
                    return RecentPages.Count;
                }
            }
        }

        /// <summary>
        /// Clears all recent pages
        /// </summary>
        public static void Clear()
        {
            lock (RecentPages)
            {
                RecentPages.Clear();
            }
        }
    }

    /// <summary>
    /// Helper class for working with handbook pages
    /// </summary>
    public static class HandbookPageHelper
    {
        /// <summary>
        /// Gets a unique key for a handbook page
        /// </summary>
        public static string GetPageKey(GuiHandbookPage? page)
        {
            if (page == null) return string.Empty;

            try
            {
                var t = page.GetType();
                string[] props = new[] { "Code", "code", "PageCode", "pageCode", "UniqueName", "uniqueName", "Title", "title" };

                foreach (string p in props)
                {
                    try
                    {
                        var pi = t.GetProperty(p, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);
                        if (pi != null)
                        {
                            var val = pi.GetValue(page)?.ToString();
                            if (!string.IsNullOrEmpty(val)) return val;
                        }

                        var fi = t.GetField(p, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.IgnoreCase);
                        if (fi != null)
                        {
                            var val = fi.GetValue(page)?.ToString();
                            if (!string.IsNullOrEmpty(val)) return val;
                        }
                    }
                    catch { }
                }

                return page.ToString() ?? string.Empty;
            }
            catch { return string.Empty; }
        }
    }

    public class HandbookManager
    {
    }
}