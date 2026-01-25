using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Services;

namespace TheMercenaryServer.Controllers;

[Injectable(InjectionType = InjectionType.Singleton)]
public sealed class SpawnController(
    SpawnConfigController configController,
    DatabaseService databaseService,
    MercenaryLogger logger
)
{
    private const string BotTypeName = "mercenary";

    public void ApplySpawnConfig()
    {
        var config = configController.Config;
        var locations = databaseService.GetLocations();

        foreach (var (mapId, mapCfg) in config.maps)
        {
            if (!mapCfg.enabled)
                continue;

            if (mapCfg.chance < 0 || mapCfg.chance > 100)
            {
                logger.Warn($"Invalid chance for map '{mapId}', must be 0-100. Skipping.");
                continue;
            }

            var mappedKey = locations.GetMappedKey(mapId);
            var dict = locations.GetDictionary();

            if (!dict.ContainsKey(mappedKey))
            {
                logger.Warn($"No location data found for '{mapId}'. Skipping.");
                continue;
            }

            var spawns = dict[mappedKey].Base.BossLocationSpawn;

            spawns.RemoveAll(x => x.BossName == BotTypeName);

            var zones = (mapCfg.zones ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (zones.Count == 0)
            {
                logger.Warn($"No zones configured for map '{mapId}'. Skipping.");
                continue;
            }

            var bossSpawn = new BossLocationSpawn
            {
                BossName = BotTypeName,
                BossChance = mapCfg.chance,
                BossDifficulty = "normal",

                // IMPORTANT: cannot be empty string in this SPT/EFT version (client Enum.Parse crash)
                BossEscortType = "followerBully",
                BossEscortAmount = "0",
                BossEscortDifficulty = "normal",

                IsBossPlayer = false,
                BossZone = string.Join(",", zones),
                Delay = 0,
                ForceSpawn = false,
                IgnoreMaxBots = false,
                IsRandomTimeSpawn = false,

                // Keep minimal to avoid client-side enum parsing issues
                SpawnMode = ["regular"],
                Supports = [],

                Time = -1,
                TriggerId = "",
                TriggerName = ""
            };

            spawns.Add(bossSpawn);
            logger.Info($"Added boss spawn for '{BotTypeName}' on '{mapId}' (chance={mapCfg.chance}).");
        }
    }
}
