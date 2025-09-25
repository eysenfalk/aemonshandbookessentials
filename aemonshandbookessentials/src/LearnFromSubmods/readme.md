[Beast Mode ON]
Remembering... Memory unavailable; proceeding stateless.
Role: Senior Developer

Plan (quick)
- Break C#/.NET topics into small, focused submods you can implement one-by-one.
- For each submod: define learning goal, implement minimal code in `src/Submods/<NN_Name>/`, build, run in Rider, test in-game.
- Iterate: increase complexity (interfaces → generics → async → reflection → Harmony → GUI → networking → tests → CI).

Execution — learning-by-doing submod roadmap (each is a small exercise you can complete in an hour or two)
1. `01_HelloMod` — basics: syntax, classes, project build, Rider flow.
    - Goal: create a `ModSystem` that logs on `Start`.
    - Files: `src/Submods/01_HelloMod/HelloMod.cs`
2. `02_ConfigMod` — properties, collections, JSON config via VintageStory API.
    - Goal: `ModConfig` load/save using `api.LoadModConfig<T>` / `api.StoreModConfig`.
    - Files: `src/Submods/02_ConfigMod/ModConfig.cs`
3. `03_OOPMod` — classes, interfaces, inheritance, DI-like pattern.
    - Goal: implement a small service interface + concrete implementation used by `ModSystem`.
4. `04_CollectionsLinq` — List/Dictionary, LINQ queries, extension methods.
    - Goal: implement a bookmark/history manager using lists/dictionaries and LINQ.
5. `05_GenericsErrors` — generics, constraints, nullable/reference types, exceptions.
6. `06_AsyncAwait` — Task, async/await, cancellation tokens, safe UI calls on client.
7. `07_Reflection` — reflection and Harmony\`Traverse`-style access, reading private fields safely.
8. `08_HarmonyPatches` — Harmony prefixes/postfixes, patching patterns, testing patches.
9. `09_GUI` — Vintage Story GUI types, composing dialogs, responding to input.
10. `10_Serialization` — System.Text.Json usage, custom converters, config migration.
11. `11_Networking` — client/server checks, RPC-style messaging, modchannel usage.
12. `12_TestsAndCI` — unit tests with xUnit, simple CI pipeline (GitHub Actions) building and packaging mod.

First small runnable task (do this now)
- Create `01_HelloMod` to verify build/run process and Rider usage.

Brief explanation: a minimal `ModSystem` that logs on Start and distinguishes client/server. Place the file under the project `src` tree so Rider includes it.

```csharp
// csharp
using Vintagestory.API.Common;

namespace AemonsHandbookEssentials.Submods.HelloMod;

public class HelloMod : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        if (api is Vintagestory.API.Client.ICoreClientAPI)
            api.Logger.Notification("HelloMod: client Start() — C# submod skeleton OK.");
        else
            api.Logger.Notification("HelloMod: server Start() — C# submod skeleton OK.");
    }
}
```

How to proceed after adding the file
- Build in Rider: Ctrl+Shift+B or run `dotnet build` in project root.
- Package/publish for Vintage Story: `dotnet publish -c Debug` (or copy `bin/Debug/Mods/aemonshandbookessentials/aemonshandbookessentials.dll` into the game `Mods` folder for quick testing).
- Use Rider: open Solution Explorer, right-click file -> Include in Project (if needed). Use Alt+Enter to fix usings. F12 to navigate.
- Iterate: pick next submod (`02_ConfigMod`) and implement one focused feature, run, and confirm in-game logs/behavior.

Quick tips
- Keep each submod small and reversible (single responsibility).
- Commit frequently with small messages. Use branches for experiments.
- When stuck, paste the specific error or the file you edited and ask for targeted help.

Next step you can do immediately
- Add the `HelloMod` file to `src/Submods/01_HelloMod/`, build, and report any compile errors shown in Rider or `dotnet build`.