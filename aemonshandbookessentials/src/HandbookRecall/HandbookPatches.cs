using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace AemonsHandbookEssentials.HandbookRecall;

[HarmonyPatch]
public static class HandbookPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialogHandbook), "OnGuiClosed")]
    public static bool KeepBrowserHistory(GuiDialogHandbook __instance)
    {
        try
        {
            var instanceTraverser = Traverse.Create(__instance);
            var overviewGui = instanceTraverser.Field("overviewGui").GetValue();
            var capi = instanceTraverser.Field("capi").GetValue<ICoreClientAPI>();

            // preserve vanilla pause/unpause behaviour
            if (capi != null && capi.IsSinglePlayer && !capi.OpenedToLan && !capi.Settings.Bool["noHandbookPause"])
                capi.PauseGame(false);

            if (overviewGui != null)
            {
                var searchFieldObject = Traverse.Create(overviewGui).Field("searchField").GetValue();
                if (searchFieldObject != null)
                {
                    var searchFieldType = searchFieldObject.GetType();

                    var setMethod = searchFieldType.GetMethod("SetValue", [typeof(string), typeof(bool)]);
                    if (setMethod != null)
                    {
                        // Clear search field without triggering text changed event
                        setMethod.Invoke(searchFieldObject, [string.Empty, false]);
                    }

                    else
                    {
                        setMethod = searchFieldType.GetMethod("SetValue", [typeof(string)]);
                        if (setMethod != null)
                            // Clear search field without triggering text changed event
                            setMethod.Invoke(searchFieldObject, [string.Empty]);
                    }
                }
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }
}