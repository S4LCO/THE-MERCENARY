using System.Collections.Generic;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace TheMercenaryServer.Controllers;

/// <summary>
/// ABPS compatibility: re-apply mercenary BossLocationSpawn right before raid start.
/// </summary>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 95000)]
public sealed class SpawnApplyWebMiddleware : StaticRouter
{
    public SpawnApplyWebMiddleware(
        JsonUtil jsonUtil,
        SpawnController spawnController,
        MercenaryLogger logger
    ) : base(jsonUtil, GetRoutes(spawnController, logger))
    {
        logger.Info("[Spawn] ABPS-Compat active: will force re-apply on /client/match/local/start");
    }

    private static List<RouteAction> GetRoutes(SpawnController spawnController, MercenaryLogger logger)
    {
        return
        [
            new RouteAction(
                "/client/match/local/start",
                (url, info, sessionId, output) =>
                {
                    try
                    {
                        spawnController.ApplySpawnConfig(force: true);
                        logger.Info("[Spawn] Forced re-apply on raid start (/client/match/local/start).");
                    }
                    catch (System.Exception ex)
                    {
                        logger.Error($"[Spawn] Forced re-apply on raid start failed: {ex}");
                    }

                    // RouteAction expects ValueTask<object> in your SPT core.
                    // "output" is usually a JSON string -> box it as object.
                    return new ValueTask<object>(output ?? string.Empty);
                },
                typeof(StartLocalRaidRequestData)
            )
        ];
    }
}
