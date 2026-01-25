using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using System.Reflection;
using TheMercenaryServer.Models;

namespace TheMercenaryServer.Controllers;

[Injectable(InjectionType.Singleton)]
public sealed class SpawnConfigController
{
    public SpawnConfig Config { get; private set; } = new();

    private readonly ModHelper _modHelper;

    public SpawnConfigController(ModHelper modHelper)
    {
        _modHelper = modHelper;

        var modRoot = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        Config = _modHelper.GetJsonDataFromFile<SpawnConfig>(modRoot, Path.Join("config", "spawn.jsonc"));
    }
}
