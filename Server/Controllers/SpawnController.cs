// Pfad: Server/Controllers/SpawnController.cs

using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Eft.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using TheMercenaryServer.Models;

namespace TheMercenaryServer.Controllers;

[Injectable(InjectionType.Singleton)]
public sealed class SpawnController(
    DatabaseService databaseService,
    SpawnConfigController spawnConfigController,
    MercenaryLogger logger
)
{
    private bool _applied;

    public void ApplySpawnConfig(bool force = false)
    {
        if (_applied && !force)
            return;

        _applied = true;

        var cfg = spawnConfigController.Config;
        var locations = databaseService.GetLocations();
        var locDict = locations.GetDictionary();

        foreach (var kvp in cfg.maps)
        {
            var location = kvp.Key;
            var mapCfg = kvp.Value;

            if (!locDict.TryGetValue(location, out var loc) || loc?.Base?.BossLocationSpawn == null)
                continue;

            // Remove any old mercenary entries
            loc.Base.BossLocationSpawn.RemoveAll(x =>
                string.Equals(x.BossName, "mercenary", StringComparison.OrdinalIgnoreCase));

            if (!mapCfg.enabled)
                continue;

            var enabledZones = GetEnabledZones(mapCfg);
            if (enabledZones.Count == 0)
            {
                // Map enabled but no zones enabled => skip spawn
                continue;
            }

            loc.Base.BossLocationSpawn.Add(new BossLocationSpawn
            {
                BossName = "mercenary",
                BossChance = ClampChance(mapCfg.chance),
                BossDifficulty = "normal",

                // IMPORTANT: cannot be empty string (client Enum.Parse crash)
                BossEscortType = "followerBully",
                BossEscortAmount = "0",
                BossEscortDifficulty = "normal",

                IsBossPlayer = false,
                BossZone = string.Join(",", enabledZones),

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

        logger.Info(force
            ? "Spawn config applied (forced)."
            : "Spawn config applied.");
    }

    private static int ClampChance(int v) => v < 0 ? 0 : (v > 100 ? 100 : v);

    private static List<string> GetEnabledZones(MapSpawnConfig mapCfg)
    {
        var all = (mapCfg.zones ?? new List<string>())
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .Select(z => z.Trim())
            .ToList();

        if (all.Count == 0)
            return new List<string>();

        var disabled = new HashSet<string>(
            (mapCfg.disabledZones ?? new List<string>())
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Select(z => z.Trim()),
            StringComparer.OrdinalIgnoreCase);

        // enabled = all - disabled
        var enabled = all.Where(z => !disabled.Contains(z)).ToList();
        return enabled;
    }
}
