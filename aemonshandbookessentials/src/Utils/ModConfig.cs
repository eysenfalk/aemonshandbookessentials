using Vintagestory.API.Common;

namespace AemonsHandbookEssentials.Utils;

public class ModConfig
{
    // Minimal config holder. Replace with persistent load/save as needed.
    public List<string> EnabledModules { get; set; } = new() { "HandbookRecall" };

    // Minimal loader: returns defaults. Swap to real file I/O when ready.
    public static ModConfig Load(ICoreAPI api, string filename)
    {
        try
        {
            var config = api.LoadModConfig<ModConfig>(filename);
            if (config != null) return config;
            api.Logger.Warning($"Config file {filename} not found or invalid. Using defaults.");
            return new ModConfig();
        }
        catch
        {
            api.Logger.Warning($"Error loading config file {filename}. Using defaults.");
            return new ModConfig();
        }
    }
}