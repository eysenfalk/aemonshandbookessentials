using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using AemonsHandbookEssentials.HandbookRecall;
using AemonsHandbookEssentials.Utils;

namespace AemonsHandbookEssentials;

public sealed class AemonshandbookessentialsModSystem : ModSystem
{
    // Harmony instance for patching
    private Harmony? _harmony;

    private readonly List<IPatchModule> _registered = [];

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        if (api is not ICoreClientAPI clientApi)
        {
            api.Logger.Notification(
                "aemonshandbookessentials ModSystem loaded on server, skipping client-only initialization");
            return;
        }

        // Create unique harmony ID
        _harmony = new Harmony("com.aemon.aemonshandbookessentials");

        // Load mod configuration
        var config = ModConfig.Load(api, "aemonshandbookessentials.json");
        var enabled = config?.EnabledModules ?? [];

        var candidates = new List<IPatchModule>
        {
            new HandbookRecall.HandbookRecall()
            // Add new modules here
        };

        foreach (var module in candidates)
            if (enabled.Contains(module.Id, StringComparer.OrdinalIgnoreCase))
                try
                {
                    module.Register(_harmony, clientApi);
                    _registered.Add(module);
                    api.Logger.Notification($"aemonshandbookessentials: Registered module {module.Id}");
                }
                catch (Exception ex)
                {
                    api.Logger.Error($"aemonshandbookessentials: Failed to register module {module.Id}: {ex}");
                }
            else
                api.Logger.Notification($"aemonshandbookessentials: Module {module.Id} is disabled in config");

        api.Logger.Notification("aemonshandbookessentials ModSystem started");
    }

    // list all loaded code submodules from this mod
    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);
        api.Logger.Notification("aemonshandbookessentials ModSystem assets loaded");
    }


    // clean up resources
    public override void Dispose()
    {
        if (_harmony != null)
        {
            foreach (var module in _registered)
                try
                {
                    module.Unregister(_harmony);
                }
                catch (Exception ex)
                {
                    // Log but continue cleanup
                    Console.WriteLine($"aemonshandbookessentials: Failed to unregister module {module.Id}: {ex}");
                }

            _harmony.UnpatchAll(_harmony.Id);
            _harmony = null;
        }

        _registered.Clear();
        base.Dispose();
    }
}