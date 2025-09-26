using System;
using HarmonyLib;
using Vintagestory.API.Client;

namespace AemonsHandbookEssentials.HandbookHistory
{
    /// <summary>
    /// Handles scrollbar sizing fixes for handbook categories with limited items
    /// </summary>
    public static class HandbookScrollbarFixes
    {
        /// <summary>
        /// Fixes scrollbar sizing for the history category to properly reflect the actual number of items
        /// </summary>
        public static void FixScrollbarForHistoryCategory(object overviewGui, ICoreClientAPI? capi)
        {
            try
            {
                // Get the flat list component
                var stackListTraverse = Traverse.Create(overviewGui).Method("GetFlatList", "stacklist");
                var stackList = stackListTraverse.GetValue();
                
                if (stackList != null)
                {
                    // Force recalculation of total height for our limited items
                    Traverse.Create(stackList).Method("CalcTotalHeight").GetValue();
                    
                    // Get the scrollbar
                    var scrollbarTraverse = Traverse.Create(overviewGui).Method("GetScrollbar", "scrollbar");
                    var scrollbar = scrollbarTraverse.GetValue();
                    
                    if (scrollbar != null)
                    {
                        ApplyScrollbarFix(stackList, scrollbar, capi);
                    }
                }
            }
            catch (Exception ex)
            {
                capi?.Logger?.Error($"HandbookScrollbarFixes: Failed to fix scrollbar: {ex}");
            }
        }

        private static void ApplyScrollbarFix(object stackList, object scrollbar, ICoreClientAPI? capi)
        {
            try
            {
                // Get bounds information
                var insideBounds = Traverse.Create(stackList).Property("insideBounds").GetValue();
                var contentHeight = Traverse.Create(insideBounds).Property("fixedHeight").GetValue<double>();
                
                // Get the visible area height (clipHeight) from the stacklist bounds
                var stackListBounds = Traverse.Create(stackList).Property("Bounds").GetValue();
                var clipHeight = Traverse.Create(stackListBounds).Property("InnerHeight").GetValue<double>();
                
                // Set scrollbar heights correctly: SetHeights(clipHeight, contentHeight)
                Traverse.Create(scrollbar).Method("SetHeights", new Type[] { typeof(float), typeof(float) })
                    .GetValue((float)clipHeight, (float)contentHeight);
                
                // Reset scroll position to top
                ResetScrollPosition(scrollbar);
                
                capi?.Logger?.Debug($"HandbookScrollbarFixes: Fixed scrollbar - clipHeight: {clipHeight}, contentHeight: {contentHeight}");
            }
            catch (Exception ex)
            {
                capi?.Logger?.Error($"HandbookScrollbarFixes: ApplyScrollbarFix failed: {ex}");
            }
        }

        private static void ResetScrollPosition(object scrollbar)
        {
            try
            {
                // Try different methods to reset scroll position
                Traverse.Create(scrollbar).Method("SetScrollbarPosition", new Type[] { typeof(float) })
                    .GetValue(0.0f);
            }
            catch
            {
                try
                {
                    Traverse.Create(scrollbar).Property("CurrentYPosition").SetValue(0.0);
                }
                catch { }
            }
        }
    }
}
