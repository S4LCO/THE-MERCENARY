using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace TheMercenaryServer.Controllers;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 90000)]
public sealed class SpawnApplyOnLoad(SpawnController spawnController) : IOnLoad
{
    public Task OnLoad()
    {
        spawnController.ApplySpawnConfig(force: true);
        return Task.CompletedTask;
    }
}
