using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace AemonsHandbookEssentials.HandbookHistory
{
    /// <summary>
    /// Handles tracking user interactions with handbook pages
    /// </summary>
    [HarmonyPatch]
    public static class HandbookPageTracker
    {
        /// <summary>
        /// Hook into user clicks on handbook items to track page views
        /// </summary>
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
                        string key = HandbookPageHelper.GetPageKey(clickedPage);
                        if (!string.IsNullOrEmpty(key))
                        {
                            RecentPagesManager.RecordPage(clickedPage);
                            LogPageClick(trav, key, index);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(__instance, "OnLeftClickListElement_Postfix", ex);
            }
        }
        
        /// <summary>
        /// Hook into programmatic page opens to track them as well
        /// </summary>
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
                        RecentPagesManager.RecordPage(page);
                        LogProgrammaticOpen(trav, pageCode);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(__instance, "OpenDetailPageFor_Postfix", ex);
            }
        }

        private static void LogPageClick(Traverse trav, string key, int index)
        {
            try
            {
                var capi = trav.Field("capi").GetValue<ICoreClientAPI>();
                capi?.Logger?.Debug($"HandbookHistory: User clicked on handbook item: {key} (index {index})");
            }
            catch { }
        }

        private static void LogProgrammaticOpen(Traverse trav, string pageCode)
        {
            try
            {
                var capi = trav.Field("capi").GetValue<ICoreClientAPI>();
                capi?.Logger?.Debug($"HandbookHistory: Programmatically opened page: {pageCode}");
            }
            catch { }
        }

        private static void LogError(GuiDialogHandbook instance, string method, Exception ex)
        {
            try
            {
                var capi = Traverse.Create(instance).Field("capi").GetValue<ICoreClientAPI>();
                capi?.Logger?.Error($"HandbookPageTracker: {method} failed: {ex}");
            }
            catch { }
        }
    }
}
