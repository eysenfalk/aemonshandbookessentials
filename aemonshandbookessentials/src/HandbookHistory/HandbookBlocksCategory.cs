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
public static class HandbookBlocksCategory
{
    // Patch genTabs to add the new "Blocks" tab
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GuiDialogSurvivalHandbook), "genTabs")]
    public static void AddBlocksTab(GuiDialogSurvivalHandbook __instance, ref GuiTab[] __result, ref int curTab)
    {
        try
        {
            var tabs = __result.ToList();
            var currentCategoryCode = Traverse.Create(__instance).Field("currentCatgoryCode").GetValue<string>();
            
            // Find the position to insert the "Blocks" tab (after "Items Only" tab)
            int insertIndex = -1;
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i] is HandbookTab tab && tab.CategoryCode == "items")
                {
                    insertIndex = i + 1;
                    break;
                }
            }
            
            // Fallback: insert after "Blocks & Items" if "Items Only" not found
            if (insertIndex == -1)
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    if (tabs[i] is HandbookTab tab && tab.CategoryCode == "stack")
                    {
                        insertIndex = i + 1;
                        break;
                    }
                }
            }
            
            if (insertIndex > 0)
            {
                // Create the new "Blocks" tab
                var blocksTab = new HandbookTab()
                {
                    DataInt = insertIndex,
                    Name = "Blocks Only",  // Direct name instead of Lang.Get for now
                    CategoryCode = "blocks"  // Changed from "items" to "blocks"
                };
                
                tabs.Insert(insertIndex, blocksTab);
                
                // Update DataInt for subsequent tabs
                for (int i = insertIndex + 1; i < tabs.Count; i++)
                {
                    tabs[i].DataInt = i;
                }
                
                // Adjust curTab if needed
                if (currentCategoryCode == "blocks")
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
            capi?.Logger?.Error("HandbookBlocksCategory: Error in AddBlocksTab: " + ex.Message);
        }
    }

    // Patch FilterItems to handle the new "blocks" category
    [HarmonyPrefix] 
    [HarmonyPatch(typeof(GuiDialogHandbook), "FilterItems")]
    public static bool FilterItemsForBlocksCategory(GuiDialogHandbook __instance)
    {
        try
        {
            var trav = Traverse.Create(__instance);
            string? currentCategoryCode = trav.Field("currentCatgoryCode").GetValue<string?>();
            
            // Only handle blocks category, let original method run for everything else
            if (currentCategoryCode != "blocks") 
            {
                return true; // let original run
            }
            
            // Handle blocks category by showing only blocks (not items)
            var capi = trav.Field("capi").GetValue<ICoreClientAPI>();
            var currentSearchText = trav.Field("currentSearchText").GetValue<string>() ?? "";
            var allPages = trav.Field("allHandbookPages").GetValue<List<GuiHandbookPage>>();
            var shownPages = trav.Field("shownHandbookPages").GetValue<List<IFlatListItem>>();
            var loading = trav.Field("loadingPagesAsync").GetValue() as bool? ?? false;

            if (shownPages == null) return true; // defensive
            shownPages.Clear();

            if (!loading && allPages != null)
            {
                // Process search text like vanilla does
                string searchLower = currentSearchText.ToLowerInvariant();
                bool hasSearch = !string.IsNullOrEmpty(searchLower);
                
                var foundPages = new List<WeightedHandbookPage>();

                foreach (var page in allPages)
                {
                    if (page.IsDuplicate) continue;
                    
                    // Filter to show only blocks (not items)
                    if (!IsBlockPage(page)) continue;
                    
                    float weight = 1;
                    bool matched = true;

                    // Apply search filtering if there's search text
                    if (hasSearch)
                    {
                        try
                        {
                            weight = page.GetTextMatchWeight(searchLower);
                            matched = weight > 0;
                        }
                        catch
                        {
                            // Fallback search method
                            var pageText = page.ToString()?.ToLowerInvariant() ?? "";
                            matched = pageText.Contains(searchLower);
                        }
                    }

                    if (matched)
                    {
                        foundPages.Add(new WeightedHandbookPage { Page = page, Weight = weight });
                    }
                }

                // Sort by weight and add to shown pages
                foreach (var weightedPage in foundPages.OrderByDescending(wp => wp.Weight))
                {
                    shownPages.Add(weightedPage.Page);
                }
                
                capi?.Logger?.Debug($"HandbookBlocksCategory: Showing {foundPages.Count} blocks pages (search: '{currentSearchText}')");
            }

            return false; // skip original FilterItems for blocks category
        }
        catch (Exception ex)
        {
            try
            {
                var capi = Traverse.Create(__instance).Field("capi").GetValue<ICoreClientAPI>();
                capi?.Logger?.Error("HandbookBlocksCategory: FilterItemsForBlocksCategory failed: " + ex);
            }
            catch { }
            return true; // fallback to original
        }
    }

    // Helper method to determine if a handbook page represents a block
    private static bool IsBlockPage(GuiHandbookPage page)
    {
        try
        {
            // Try to get the collectible object from the page
            if (page is GuiHandbookItemStackPage itemStackPage)
            {
                // Use reflection to get the ItemStack from the page
                var itemStackField = itemStackPage.GetType().GetField("Stack", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (itemStackField?.GetValue(itemStackPage) is ItemStack stack)
                {
                    return stack.Class == EnumItemClass.Block;
                }
            }
            
            // Fallback: check page code for block-like patterns
            var pageCode = page.PageCode?.ToLowerInvariant() ?? "";
            
            // Common block prefixes/patterns
            return pageCode.Contains("block-") || 
                   pageCode.StartsWith("stone") || 
                   pageCode.StartsWith("wood") ||
                   pageCode.StartsWith("clay") ||
                   pageCode.StartsWith("metal") ||
                   pageCode.Contains("planks") ||
                   pageCode.Contains("brick") ||
                   pageCode.Contains("cobble");
        }
        catch
        {
            return false;
        }
    }

    // Helper class for weighted pages
    private class WeightedHandbookPage
    {
        public GuiHandbookPage Page { get; set; } = null!;
        public float Weight { get; set; }
    }
}
