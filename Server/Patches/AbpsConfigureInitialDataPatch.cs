using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using System;
using System.Reflection;
using TheMercenaryServer.Controllers;

namespace TheMercenaryServer.Patches;

public sealed class AbpsConfigureInitialDataPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        var mapSpawnsType = AccessTools.TypeByName("_botplacementsystem.Controllers.MapSpawns");
        return AccessTools.Method(mapSpawnsType, "ConfigureInitialData");
    }

    [PatchPostfix]
    public static void Postfix()
    {
        try
        {
            var spawnController = ServiceLocator.ServiceProvider.GetService<SpawnController>();
            spawnController?.ApplySpawnConfig();
        }
        catch
        {
            // IMPORTANT: Optional compat patch must never crash server.
        }
    }
}
