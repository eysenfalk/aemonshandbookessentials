using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace aemonshandbookessentials.LearnFromSubmods._03HotReload
{
    public class HotReloadTestSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            if (api is ICoreServerAPI serverApi)
            {
                serverApi.ChatCommands.Create("aemonhotreload")
                    .WithDescription("Prints a hot-reload test message.")
                    .RequiresPrivilege(Privilege.chat)
                    .HandleWith(_ => TextCommandResult.Success("HotReloadTestSystem: " + GetHotReloadMessage()));
                
                serverApi.Logger.Notification("HotReloadTestSystem: .aemonhotreload command registered");
            }

            else if (api is Vintagestory.API.Client.ICoreClientAPI clientApi)
            {
                clientApi.Logger.Notification("HotReloadTestSystem started on client (server command available when server side present).");
            }
        }

        private static string GetHotReloadMessage()
        {
            return "HotReload test message v1 - edit this string and press Alt+F10 to apply.";
        }
    }
}