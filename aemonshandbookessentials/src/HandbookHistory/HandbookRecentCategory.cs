using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace AemonsHandbookEssentials.HandbookHistory
{
    [HarmonyPatch]
    public static class HandbookRecentCategory
    {
        // 30 most recently opened handbook page keys (most recent first)
        private static readonly LinkedList<string> RecentPages = new LinkedList<string>();
        private const int MaxRecent = 30;

        // Inject a "History" tab into the survival handbook tabs (insert after both items and blocks tabs)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GuiDialogSurvivalHandbook), "genTabs")]
        public static void AddHistoryTab(GuiDialogSurvivalHandbook __instance, ref GuiTab[] __result, ref int curTab)
        {
            try
            {
                var tabs = __result.ToList();

                string? currentCategoryCode = Traverse.Create(__instance).Field("currentCatgoryCode").GetValue<string?>();

                // Find the position to insert the "History" tab (after both "items" and "blocks" tabs)
                int insertIndex = -1;
                
                // Look for blocks tab first (should be last of the items/blocks tabs)
                for (int i = 0; i < tabs.Count; i++)
                {
                    if (tabs[i] is HandbookTab ht && ht.CategoryCode == "blocks")
                    {
                        insertIndex = i + 1;
                        break;
                    }
                }
                
                // Fallback: look for items tab if blocks not found
                if (insertIndex == -1)
                {
                    for (int i = 0; i < tabs.Count; i++)
                    {
                        if (tabs[i] is HandbookTab ht && ht.CategoryCode == "items")
                        {
                            insertIndex = i + 1;
                            break;
                        }
                    }
                }
                
                // Fallback: insert after "Blocks & Items" if neither found
                if (insertIndex == -1)
                {
                    for (int i = 0; i < tabs.Count; i++)
                    {
                        if (tabs[i] is HandbookTab ht && (ht.CategoryCode == "items" || ht.CategoryCode == "stack"))
                        {
                            insertIndex = i + 1;
                            if (ht.CategoryCode == "items") break;
                        }
                    }
                }

                if (insertIndex > 0)
                {
                    var historyTab = new HandbookTab
                    {
                        DataInt = insertIndex,
                        Name = "Recently Viewed",
                        CategoryCode = "history"
                    };

                    tabs.Insert(insertIndex, historyTab);

                    for (int i = insertIndex + 1; i < tabs.Count; i++)
                    {
                        tabs[i].DataInt = i;
                    }

                    if (currentCategoryCode == "history")
                    {
                        curTab = insertIndex;
                    }
                    else if (curTab >= insertIndex)
                    {
                        curTab++;
                    }

                    __result = tabs.ToArray();
                }
                
                // Also fix the "Items" category name if it exists
                for (int i = 0; i < tabs.Count; i++)
                {
                    if (tabs[i] is HandbookTab ht && ht.CategoryCode == "items")
                    {
                        ht.Name = "Items Only";
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    var capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
                    capi?.Logger?.Error("HandbookRecentCategory: AddHistoryTab failed: " + ex);
                }
                catch { }
            }
        }

        // Hook into the actual method that handles clicking on handbook items
        // Based on DeepWiki research: onLeftClickListElement is called when users click on handbook items
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GuiDialogHandbook), "onLeftClickListElement")]
        public static void OnLeftClickListElement_Postfix(GuiDialogHandbook __instance, int index)
        {
            try
            {
                var trav = Traverse.Create(__instance);
                string? currentCategoryCode = trav.Field("currentCatgoryCode").GetValue<string?>();
                
                // Skip our own history category to avoid loops
                if (currentCategoryCode == "history") return;
                
                // Get the clicked page from shownHandbookPages using the index
                var shownPages = trav.Field("shownHandbookPages").GetValue<List<IFlatListItem>>();
                if (shownPages != null && index >= 0 && index < shownPages.Count)
                {
                    if (shownPages[index] is GuiHandbookPage clickedPage)
                    {
                        string key = GetPageKey(clickedPage);
                        if (!string.IsNullOrEmpty(key))
                        {
                            RecordPageInHistory(clickedPage);
                            
                            // Debug logging
                            var capi = trav.Field("capi").GetValue<ICoreClientAPI>();
                            capi?.Logger?.Debug($"HandbookHistory: User clicked on handbook item: {key} (index {index})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    var capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
                    capi?.Logger?.Error($"HandbookHistory: OnLeftClickListElement_Postfix failed: {ex}");
                }
                catch { }
            }
        }
        
        // Also hook into OpenDetailPageFor method for programmatic page opens
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GuiDialogHandbook), "OpenDetailPageFor")]
        public static void OpenDetailPageFor_Postfix(GuiDialogHandbook __instance, string pageCode)
        {
            try
            {
                // Get the page by code and record it
                var trav = Traverse.Create(__instance);
                var pageNumberByPageCode = trav.Field("pageNumberByPageCode").GetValue<Dictionary<string, int>>();
                var allPages = trav.Field("allHandbookPages").GetValue<List<GuiHandbookPage>>();
                
                if (pageNumberByPageCode != null && allPages != null && pageNumberByPageCode.TryGetValue(pageCode, out int pageNumber))
                {
                    if (pageNumber >= 0 && pageNumber < allPages.Count)
                    {
                        var page = allPages[pageNumber];
                        RecordPageInHistory(page);
                        
                        var capi = trav.Field("capi").GetValue<ICoreClientAPI>();
                        capi?.Logger?.Debug($"HandbookHistory: Programmatically opened page: {pageCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    var capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
                    capi?.Logger?.Error($"HandbookHistory: OpenDetailPageFor_Postfix failed: {ex}");
                }
                catch { }
            }
        }

        // make this accept nullable page to avoid nullable-analysis warnings
        private static string GetPageKey(GuiHandbookPage? page)
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

        // Simplified page recording that just adds to our MRU list
        private static void RecordPageInHistory(GuiHandbookPage page)
        {
            try
            {
                string key = GetPageKey(page);
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

        // When the 'history' category is active, populate the shownHandbookPages list from our MRU
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GuiDialogHandbook), "FilterItems")]
        public static bool FilterItemsHistoryOnly(GuiDialogHandbook __instance)
        {
            try
            {
                var trav = Traverse.Create(__instance);
                string? currentCategoryCode = trav.Field("currentCatgoryCode").GetValue<string?>();
                
                // Only handle history category, let original method run for everything else
                if (currentCategoryCode != "history") 
                {
                    return true; // let original run
                }
                
                // Handle history category by showing MRU pages with proper search support
                var capi = trav.Field("capi").GetValue<ICoreClientAPI>();
                var currentSearchText = trav.Field("currentSearchText").GetValue<string>() ?? "";
                var allPages = trav.Field("allHandbookPages").GetValue<List<GuiHandbookPage>>();
                var shownPages = trav.Field("shownHandbookPages").GetValue<List<IFlatListItem>>();
                var loading = trav.Field("loadingPagesAsync").GetValue() as bool? ?? false;

                if (shownPages == null) return true; // defensive
                shownPages.Clear();

                // Declare foundPages at method level to avoid scope issues
                var foundPages = new List<GuiHandbookPage>();

                if (!loading && allPages != null)
                {
                    // Build lookup maps to handle different key formats
                    var pageMap = new Dictionary<string, GuiHandbookPage>(StringComparer.OrdinalIgnoreCase);
                    
                    foreach (var p in allPages)
                    {
                        try
                        {
                            var key = GetPageKey(p);
                            if (!string.IsNullOrEmpty(key) && !pageMap.ContainsKey(key)) 
                            {
                                pageMap[key] = p;
                              }
                        }
                        catch { }
                    }

                    List<string> snapshot;
                    lock (RecentPages)
                    {
                        snapshot = RecentPages.ToList();
                    }

                    // Process search text like vanilla does (convert to lowercase, handle search terms)
                    string searchLower = currentSearchText.ToLowerInvariant();
                    bool hasSearch = !string.IsNullOrEmpty(searchLower);

                    foreach (string key in snapshot)
                    {
                        if (!pageMap.TryGetValue(key, out var page)) continue;
                        
                        // Apply search filtering if there's search text
                        if (hasSearch)
                        {
                            // Use the same search logic as vanilla - check if page matches search
                            bool matches = false;
                            try
                            {
                                // Check if the page title/content contains search text
                                var pageText = page.ToString()?.ToLowerInvariant() ?? "";
                                if (pageText.Contains(searchLower))
                                {
                                    matches = true;
                                }
                                
                                // Also try GetTextMatchWeight if available
                                var matchMethod = page.GetType().GetMethod("GetTextMatchWeight");
                                if (matchMethod != null)
                                {
                                    var weight = (float)matchMethod.Invoke(page, new object[] { searchLower });
                                    if (weight > 0) matches = true;
                                }
                            }
                            catch { }
                            
                            if (!matches) continue;
                        }

                        foundPages.Add(page);
                    }
                    
                    // Add found pages to shownPages
                    shownPages.AddRange(foundPages);
                    
                    capi?.Logger?.Debug($"HandbookHistory: Showing {foundPages.Count} history pages (search: '{currentSearchText}')");
                }

                // Now handle scrollbar using the exact vanilla pattern
                var overviewGui = trav.Field("overviewGui").GetValue();
                if (overviewGui != null)
                {
                    try
                    {
                        // Call the vanilla scrollbar fix pattern exactly as the original does
                        var stacklist = Traverse.Create(overviewGui).Method("GetFlatList", "stacklist").GetValue();
                        if (stacklist != null)
                        {
                            capi?.Logger?.Debug($"HandbookHistory: Found stacklist, attempting scrollbar fix for {foundPages.Count} pages");
                            
                            // Step 1: CalcTotalHeight (critical!)
                            Traverse.Create(stacklist).Method("CalcTotalHeight").GetValue();
                            
                            // Step 2: Get scrollbar and set heights
                            var scrollbar = Traverse.Create(overviewGui).Method("GetScrollbar", "scrollbar").GetValue();
                            if (scrollbar != null)
                            {
                                var listHeight = trav.Field("listHeight").GetValue<double>();
                                var insideBounds = Traverse.Create(stacklist).Property("insideBounds").GetValue();
                                var totalHeight = Traverse.Create(insideBounds).Property("fixedHeight").GetValue<double>();
                                
                                // For empty lists, ensure scrollbar is properly sized
                                if (foundPages.Count == 0)
                                {
                                    // When no items, total height should be 0 or very small
                                    totalHeight = 0;
                                }
                                
                                Traverse.Create(scrollbar).Method("SetHeights", new Type[] { typeof(float), typeof(float) })
                                    .GetValue((float)listHeight, (float)totalHeight);
                                
                                capi?.Logger?.Debug($"HandbookHistory: Fixed scrollbar for history - visible: {listHeight}, total: {totalHeight}, pages: {foundPages.Count}");
                            }
                            else
                            {
                                capi?.Logger?.Warning("HandbookHistory: Could not find scrollbar component");
                            }
                        }
                        else
                        {
                            capi?.Logger?.Warning("HandbookHistory: Could not find stacklist component");
                        }
                    }
                    catch (Exception scrollEx)
                    {
                        capi?.Logger?.Error($"HandbookHistory: Scrollbar fix failed: {scrollEx}");
                    }
                }
                else
                {
                    capi?.Logger?.Warning("HandbookHistory: overviewGui is null");
                }

                return false; // skip original FilterItems for history category
            }
            catch (Exception ex)
            {
                try
                {
                    var capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
                    capi?.Logger?.Error("HandbookRecentCategory: FilterItemsHistoryOnly failed: " + ex);
                }
                catch { }
                return true; // fallback to original
            }
        }
    }
}
