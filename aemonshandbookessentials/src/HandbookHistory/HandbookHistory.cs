using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using AemonsHandbookEssentials.Utils;

namespace AemonsHandbookEssentials.HandbookHistory;

// Implements IPatchModule so this class can be used in the ModSystem candidates list.
// Register applies Harmony patches for the HandbookHistory functionality.
// Unregister is intentionally minimal: ModSystem already calls UnpatchAll for the Harmony id.
public class HandbookHistory : IPatchModule
{
    // Identifier used in config EnabledModules (case-insensitive matching).
    public string Id => "HandbookHistory";

    // Called during Start when this module is enabled. Apply Harmony patches here.
    public void Register(Harmony harmony, ICoreClientAPI clientApi)
    {
        ArgumentNullException.ThrowIfNull(harmony);
        ArgumentNullException.ThrowIfNull(clientApi);

        try
        {
            // Apply all Harmony patches found in the HandbookHistory namespace
            harmony.PatchAll(typeof(HandbookRecentCategory).GetTypeInfo().Assembly);
            clientApi.Logger.Notification($"HandbookHistory: patches applied.");
        }
        catch (Exception ex)
        {
            clientApi.Logger.Warning($"HandbookHistory: failed to register patches: {ex.Message}");
            throw;
        }
    }

    // Called from ModSystem.Dispose() before Harmony.UnpatchAll — implement module-specific cleanup if needed.
    public void Unregister(Harmony harmony)
    {
        // No per-module unpatching required in this minimal setup.
        // If you call harmony.Patch(...) for specific methods, unpatch them here using harmony.Unpatch(...)
    }
}
