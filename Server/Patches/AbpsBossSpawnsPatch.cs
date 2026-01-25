using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheMercenaryServer.Controllers;

namespace TheMercenaryServer.Patches;

public sealed class AbpsBossSpawnsPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        var bossSpawnsType = AccessTools.TypeByName("_botplacementsystem.Controllers.BossSpawns");
        return AccessTools.Method(bossSpawnsType, "GetCustomMapData");
    }

    [PatchPostfix]
    public static void Postfix(ref List<BossLocationSpawn> __result, string location)
    {
        try
        {
            var cfg = ServiceLocator.ServiceProvider.GetService<SpawnConfigController>()?.Config;
            if (cfg?.maps == null || cfg.maps.Count == 0)
                return;

            var mapPair = cfg.maps.FirstOrDefault(x =>
                string.Equals(x.Key, location, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(mapPair.Key))
                return;

            var mapCfg = mapPair.Value;
            if (mapCfg == null || !mapCfg.enabled)
                return;

            var zones = (mapCfg.zones ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (zones.Count == 0)
                return;

            __result.RemoveAll(x => x.BossName == "mercenary");

            __result.Add(new BossLocationSpawn
            {
                BossName = "mercenary",
                BossChance = mapCfg.chance,
                BossDifficulty = "normal",

                // IMPORTANT: cannot be empty string (client Enum.Parse crash)
                BossEscortType = "followerBully",
                BossEscortAmount = "0",
                BossEscortDifficulty = "normal",

                IsBossPlayer = false,
                BossZone = string.Join(",", zones),
                Delay = 0,
                ForceSpawn = false,
                IgnoreMaxBots = false,
                IsRandomTimeSpawn = false,

                SpawnMode = ["regular"],
                Supports = [],

                Time = -1,
                TriggerId = "",
                TriggerName = ""
            });
        }
        catch
        {
            // IMPORTANT: Optional compat patch must never crash server.
        }
    }
}
