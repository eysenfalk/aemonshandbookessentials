using HarmonyLib;
using Vintagestory.API.Client;

namespace AemonsHandbookEssentials.Utils;

public interface IPatchModule
{
    string Id { get; }
    void Register(Harmony harmony, ICoreClientAPI clientApi);
    void Unregister(Harmony harmony);
}