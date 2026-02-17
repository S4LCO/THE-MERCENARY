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

            // SPT location keys are not guaranteed to match JSON casing.
            // Try exact lookup first, then fall back to case-insensitive lookup.
            if (!locDict.TryGetValue(location, out var loc))
            {
                var foundKey = locDict.Keys.FirstOrDefault(k =>
                    string.Equals(k, location, StringComparison.OrdinalIgnoreCase));

                if (foundKey != null)
                    loc = locDict[foundKey];
            }

            if (loc?.Base?.BossLocationSpawn == null)
            {
                if (mapCfg.enabled)
                    logger.Info($"[Spawn] Map key '{location}' not found (or missing BossLocationSpawn) in Locations DB. Skipping.");
                continue;
            }

            // Remove any old mercenary entries
            loc.Base.BossLocationSpawn.RemoveAll(x =>
                string.Equals(x.BossName, "mercenary", StringComparison.OrdinalIgnoreCase));

            if (!mapCfg.enabled)
                continue;

            var chance = ClampChance(mapCfg.chance);
            if (chance <= 0)
            {
                // Explicitly disabled by chance
                logger.Info($"[Spawn] '{location}' enabled but chance is 0%. Skipping.");
                continue;
            }

            var enabledZones = GetEnabledZones(mapCfg);
            if (enabledZones.Count == 0)
            {
                // Map enabled but no zones enabled => skip spawn (nothing we can do)
                logger.Info($"[Spawn] '{location}' enabled but has no effective zones (zones - disabledZones is empty). Skipping.");
                continue;
            }

            loc.Base.BossLocationSpawn.Add(new BossLocationSpawn
            {
                BossName = "mercenary",
                BossChance = chance,
                BossDifficulty = "normal",

                // IMPORTANT: cannot be empty string (client Enum.Parse crash)
                BossEscortType = "followerBully",
                BossEscortAmount = "0",
                BossEscortDifficulty = "normal",

                IsBossPlayer = false,
                BossZone = string.Join(",", enabledZones),

                Delay = 0,
                ForceSpawn = false,
                IgnoreMaxBots = true,
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
