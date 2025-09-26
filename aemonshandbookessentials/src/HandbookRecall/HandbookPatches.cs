using System;
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
            var capi = instanceTraverser.Field("capi").GetValue<ICoreClientAPI>();

            // preserve vanilla pause/unpause behaviour
            if (capi != null && capi.IsSinglePlayer && !capi.OpenedToLan && !capi.Settings.Bool["noHandbookPause"])
                capi.PauseGame(false);

            // Removed search field clearing functionality as requested by user
            // We no longer clear the search field or reset currentSearchText
        }
        catch
        {
            // ignored
        }

        return false;
    }
}