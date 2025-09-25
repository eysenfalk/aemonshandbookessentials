using Vintagestory.API.Common;

namespace AemonsHandbookEssentials.LearnFromSubmods._01HelloWorld;

public class HelloMod : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.Logger.Notification(api is Vintagestory.API.Client.ICoreClientAPI
            ? "HelloMod: client Start() — C# submod skeleton OK."
            : "HelloMod: server Start() — C# submod skeleton OK.");
    }
}