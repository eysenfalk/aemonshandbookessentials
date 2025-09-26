using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace AemonsHandbookEssentials.HandbookHistory
{
    /// <summary>
    /// Handles filtering and displaying items in the history category
    /// </summary>
    [HarmonyPatch]
    public static class HandbookHistoryFilter
    {
        /// <summary>
        /// Handle filtering for the history category with proper search support and scrollbar fixes
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GuiDialogHandbook), "FilterItems")]
        public static bool FilterItemsWithHistoryTracking(GuiDialogHandbook __instance)
        {
            try
            {
                var trav = Traverse.Create(__instance);
                string? currentCategoryCode = trav.Field("currentCatgoryCode").GetValue<string?>();
                
                // For non-history categories, let original method run
                if (currentCategoryCode != "history") 
                {
                    return true; // let original run
                }
                
                // Handle history category
                return HandleHistoryCategory(__instance, trav);
            }
            catch (Exception ex)
            {
                LogError(__instance, "FilterItemsWithHistoryTracking", ex);
                return true; // fallback to original
            }
        }

        private static bool HandleHistoryCategory(GuiDialogHandbook instance, Traverse trav)
        {
            var capi = trav.Field("capi").GetValue<ICoreClientAPI>();
            var currentSearchText = trav.Field("currentSearchText").GetValue<string>() ?? "";
            var allPages = trav.Field("allHandbookPages").GetValue<List<GuiHandbookPage>>();
            var shownPages = trav.Field("shownHandbookPages").GetValue<List<IFlatListItem>>();
            var loading = trav.Field("loadingPagesAsync").GetValue<bool>();
            var overviewGui = trav.Field("overviewGui").GetValue();

            if (shownPages == null) return true; // defensive
            shownPages.Clear();

            if (!loading && allPages != null)
            {
                var foundPages = GetFilteredHistoryPages(allPages, currentSearchText);
                shownPages.AddRange(foundPages);
                
                LogFilterResults(capi, foundPages.Count, currentSearchText);
                FixScrollbar(overviewGui, capi);
            }

            return false; // skip original FilterItems
        }

        private static List<GuiHandbookPage> GetFilteredHistoryPages(List<GuiHandbookPage> allPages, string searchText)
        {
            // Build lookup map for pages
            var pageMap = BuildPageLookupMap(allPages);
            
            // Get recent pages snapshot
            var recentPageKeys = RecentPagesManager.GetRecentPages();
            
            // Apply search filtering
            return FilterPagesBySearch(pageMap, recentPageKeys, searchText);
        }

        private static Dictionary<string, GuiHandbookPage> BuildPageLookupMap(List<GuiHandbookPage> allPages)
        {
            var pageMap = new Dictionary<string, GuiHandbookPage>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var page in allPages)
            {
                try
                {
                    var key = HandbookPageHelper.GetPageKey(page);
                    if (!string.IsNullOrEmpty(key) && !pageMap.ContainsKey(key)) 
                    {
                        pageMap[key] = page;
                    }
                }
                catch { }
            }
            
            return pageMap;
        }

        private static List<GuiHandbookPage> FilterPagesBySearch(
            Dictionary<string, GuiHandbookPage> pageMap, 
            List<string> recentPageKeys, 
            string searchText)
        {
            string searchLower = searchText.ToLowerInvariant();
            bool hasSearch = !string.IsNullOrEmpty(searchLower);
            var foundPages = new List<GuiHandbookPage>();

            foreach (string key in recentPageKeys)
            {
                if (!pageMap.TryGetValue(key, out var page)) continue;
                
                // Apply search filtering if there's search text
                if (hasSearch && !PageMatchesSearch(page, searchLower)) continue;

                foundPages.Add(page);
            }

            return foundPages;
        }

        private static bool PageMatchesSearch(GuiHandbookPage page, string searchLower)
        {
            try
            {
                // Check if the page title/content contains search text
                var pageText = page.ToString()?.ToLowerInvariant() ?? "";
                if (pageText.Contains(searchLower)) return true;
                
                // Also try GetTextMatchWeight if available
                var matchMethod = page.GetType().GetMethod("GetTextMatchWeight");
                if (matchMethod != null)
                {
                    var weight = (float)matchMethod.Invoke(page, new object[] { searchLower });
                    if (weight > 0) return true;
                }
            }
            catch { }
            
            return false;
        }

        private static void FixScrollbar(object? overviewGui, ICoreClientAPI? capi)
        {
            if (overviewGui == null) return;

            try
            {
                HandbookScrollbarFixes.FixScrollbarForHistoryCategory(overviewGui, capi);
            }
            catch (Exception scrollEx)
            {
                capi?.Logger?.Error($"HandbookHistoryFilter: Scrollbar fix failed: {scrollEx}");
            }
        }

        private static void LogFilterResults(ICoreClientAPI? capi, int count, string searchText)
        {
            capi?.Logger?.Debug($"HandbookHistory: Showing {count} history pages (search: '{searchText}')");
        }

        private static void LogError(GuiDialogHandbook instance, string method, Exception ex)
        {
            try
            {
                var capi = Traverse.Create(instance).Field("capi").GetValue<ICoreClientAPI>();
                capi?.Logger?.Error($"HandbookHistoryFilter: {method} failed: {ex}");
            }
            catch { }
        }
    }
}
