using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using AemonsHandbookEssentials.Utils;

namespace AemonsHandbookEssentials.HandbookRecall;

// Implements IPatchModule so this class can be used in the ModSystem candidates list.
// Register applies Harmony patches (here we patch the current assembly as a simple default).
// Unregister is intentionally minimal: ModSystem already calls UnpatchAll for the Harmony id.
// Adjust `Register`/`Unregister` to target specific methods if you prefer finer control.
public class HandbookRecall : IPatchModule
{
    // Identifier used in config EnabledModules (case-insensitive matching).
    public string Id => "HandbookRecall";

    // Called during Start when this module is enabled. Apply Harmony patches here.
    public void Register(Harmony harmony, ICoreClientAPI clientApi)
    {
        ArgumentNullException.ThrowIfNull(harmony);
        ArgumentNullException.ThrowIfNull(clientApi);

        try
        {
            // Apply all Harmony patches found in this assembly.
            // Replace with targeted Patch / Unpatch calls for finer control.
            harmony.PatchAll(typeof(HandbookPatches).GetTypeInfo().Assembly);
            clientApi.Logger.Notification($"HandbookRecall: patches applied.");
        }
        catch (Exception ex)
        {
            clientApi.Logger.Warning($"HandbookRecall: failed to register patches: {ex.Message}");
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