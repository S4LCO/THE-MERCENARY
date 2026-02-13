using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace TheMercenaryServer.Controllers;

/// <summary>
/// Applies Odin spawn config once, late in the Post-DB stage,
/// so other mods that touch location spawns earlier are less likely to override us.
/// </summary>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 250)]
public sealed class SpawnApplyOnLoad(SpawnController spawnController) : IOnLoad
{
    public Task OnLoad()
    {
        spawnController.ApplySpawnConfig();
        return Task.CompletedTask;
    }
}
