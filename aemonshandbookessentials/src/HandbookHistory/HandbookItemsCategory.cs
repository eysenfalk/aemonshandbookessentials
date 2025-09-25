using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace AemonsHandbookEssentials.HandbookHistory;

[HarmonyPatch]
public static class HandbookItemsCategory
{
    // Patch genTabs to add the new "Items" tab
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GuiDialogSurvivalHandbook), "genTabs")]
    public static void AddItemsTab(GuiDialogSurvivalHandbook __instance, ref GuiTab[] __result, ref int curTab)
    {
        try
        {
            var tabs = __result.ToList();
            var currentCategoryCode = Traverse.Create(__instance).Field("currentCatgoryCode").GetValue<string>();
            
            // Find the position to insert the "Items" tab (after "Blocks & Items")
            int insertIndex = -1;
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i] is HandbookTab tab && tab.CategoryCode == "stack")
                {
                    insertIndex = i + 1;
                    break;
                }
            }
            
            if (insertIndex > 0)
            {
                // Create the new "Items" tab
                var itemsTab = new HandbookTab()
                {
                    DataInt = insertIndex,
                    Name = Lang.Get("handbook-category-items", "Items"),
                    CategoryCode = "items"
                };
                
                tabs.Insert(insertIndex, itemsTab);
                
                // Update DataInt for subsequent tabs
                for (int i = insertIndex + 1; i < tabs.Count; i++)
                {
                    tabs[i].DataInt = i;
                }
                
                // Adjust curTab if needed
                if (currentCategoryCode == "items")
                {
                    curTab = insertIndex;
                }
                else if (curTab >= insertIndex)
                {
                    curTab++;
                }
            }
            
            __result = tabs.ToArray();
        }
        catch (Exception ex)
        {
            // Log error but don't break the game
            var capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
            capi?.Logger?.Error("HandbookItemsCategory: Error in AddItemsTab: " + ex.Message);
        }
    }

    // Patch FilterItems to handle the new "items" category
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialogHandbook), "FilterItems")]
    public static bool FilterItemsForItemsCategory(GuiDialogHandbook __instance)
    {
        try
        {
            var instanceTraverse = Traverse.Create(__instance);
            var currentCategoryCode = instanceTraverse.Field("currentCatgoryCode").GetValue<string>();
            
            // Only handle our custom "items" category
            if (currentCategoryCode != "items")
            {
                return true; // Continue with original method
            }
            
            // Get private fields we need
            var currentSearchText = instanceTraverse.Field("currentSearchText").GetValue<string>();
            var allHandbookPages = instanceTraverse.Field("allHandbookPages").GetValue<List<GuiHandbookPage>>();
            var shownHandbookPages = instanceTraverse.Field("shownHandbookPages").GetValue<List<IFlatListItem>>();
            var loadingPagesAsync = instanceTraverse.Field("loadingPagesAsync").GetValue<bool>();
            var overviewGui = instanceTraverse.Field("overviewGui").GetValue();
            var listHeight = instanceTraverse.Field("listHeight").GetValue<double>();
            
            // Clear current shown pages
            shownHandbookPages.Clear();
            
            if (!loadingPagesAsync && allHandbookPages != null)
            {
                // Process search text similar to original FilterItems
                string text = currentSearchText?.ToLowerInvariant();
                string[] texts = Array.Empty<string>();
                bool logicalAnd = false;
                
                if (!string.IsNullOrEmpty(text))
                {
                    if (text.Contains(" or "))
                    {
                        texts = text.Split(new[] { " or " }, StringSplitOptions.RemoveEmptyEntries)
                            .OrderBy(str => str.Length).ToArray();
                    }
                    else if (text.Contains(" and "))
                    {
                        texts = text.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries)
                            .OrderBy(str => str.Length).ToArray();
                        logicalAnd = texts.Length > 1;
                    }
                    else
                    {
                        texts = new[] { text };
                    }
                    
                    // Clean up search terms
                    for (int i = 0; i < texts.Length; i++)
                    {
                        texts[i] = texts[i].Trim();
                    }
                    texts = texts.Where(t => !string.IsNullOrEmpty(t)).ToArray();
                    logicalAnd = logicalAnd && texts.Length > 1;
                }
                
                var foundPages = new List<WeightedHandbookPage>();
                
                foreach (var page in allHandbookPages)
                {
                    // Only include pages from "stack" category (Blocks & Items)
                    if (page.CategoryCode != "stack") continue;
                    if (page.IsDuplicate) continue;
                    
                    // Filter to only items (not blocks)
                    if (page is GuiHandbookItemStackPage itemPage)
                    {
                        var collectible = itemPage.Stack?.Collectible;
                        if (collectible == null) continue;
                        
                        // Skip if it's a block (only show items)
                        if (collectible is Block) continue;
                    }
                    else
                    {
                        // Skip non-itemstack pages for items category
                        continue;
                    }
                    
                    // Apply search filtering
                    float weight = 1;
                    bool matched = logicalAnd;
                    
                    if (texts.Length > 0)
                    {
                        for (int j = 0; j < texts.Length; j++)
                        {
                            weight = page.GetTextMatchWeight(texts[j]);
                            if (weight > 0)
                            {
                                if (!logicalAnd) { matched = true; break; }
                            }
                            else
                            {
                                if (logicalAnd) { matched = false; break; }
                            }
                        }
                        if (!matched) continue;
                    }
                    
                    foundPages.Add(new WeightedHandbookPage { Page = page, Weight = weight });
                }
                
                // Sort by weight and add to shown pages
                foreach (var weightedPage in foundPages.OrderByDescending(wp => wp.Weight))
                {
                    shownHandbookPages.Add(weightedPage.Page);
                }
            }
            
            // Update UI elements (similar to original FilterItems)
            if (overviewGui != null)
            {
                var stackListTraverse = Traverse.Create(overviewGui).Method("GetFlatList", "stacklist");
                var stackList = stackListTraverse.GetValue();
                if (stackList != null)
                {
                    Traverse.Create(stackList).Method("CalcTotalHeight").GetValue();
                    
                    var scrollbarTraverse = Traverse.Create(overviewGui).Method("GetScrollbar", "scrollbar");
                    var scrollbar = scrollbarTraverse.GetValue();
                    if (scrollbar != null)
                    {
                        var insideBounds = Traverse.Create(stackList).Property("insideBounds").GetValue();
                        var fixedHeight = Traverse.Create(insideBounds).Property("fixedHeight").GetValue<double>();
                        Traverse.Create(scrollbar).Method("SetHeights", (float)listHeight, (float)fixedHeight).GetValue();
                    }
                }
            }
            
            return false; // Skip original method
        }
        catch (Exception ex)
        {
            var capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
            capi?.Logger?.Error("HandbookItemsCategory: Error in FilterItemsForItemsCategory: " + ex.Message);
            return true; // Fall back to original method on error
        }
    }

    // Helper class for weighted pages (matches the original implementation)
    private class WeightedHandbookPage
    {
        public GuiHandbookPage Page { get; set; } = null!;
        public float Weight { get; set; }
    }
}
