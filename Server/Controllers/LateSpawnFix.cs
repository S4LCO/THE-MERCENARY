using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace TheMercenaryServer.Controllers;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 69421)]
public sealed class LateSpawnFix(SpawnController spawnController) : IOnLoad
{
    public Task OnLoad()
    {
        spawnController.ApplySpawnConfig();
        return Task.CompletedTask;
    }
}
