using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TheMercenaryServer.Controllers;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnUpdateOrder.InsuranceCallbacks + 100)]
public sealed class RagnarokAchievementRewardService(
    ProfileHelper profileHelper,
    MailSendService mailSendService,
    SaveServer saveServer
) : IOnUpdate
{
    private static readonly MongoId RagnarokAchievementId = new("b95eb2f0eca36c8fcaafbf0e");

    // Generated unique marker ID (safe to generate as you allowed).
    private static readonly MongoId RewardMarkerId = new("7f3b2c2f4c6d4b1aa7b2d9e1");

    private static readonly MongoId RewardContainerTpl = new("696b9d0c1b507313be9f2d65");

    private DateTime _nextRunUtc = DateTime.MinValue;
    private static readonly TimeSpan RunInterval = TimeSpan.FromSeconds(5);

    public async Task<bool> OnUpdate(long _)
    {
        var now = DateTime.UtcNow;
        if (now < _nextRunUtc)
            return true;

        _nextRunUtc = now.Add(RunInterval);

        var profiles = profileHelper.GetProfiles();
        if (profiles is null || profiles.Count == 0)
            return true;

        foreach (var kvp in profiles)
        {
            var sessionId = kvp.Key;
            var profile = kvp.Value;

            var pmc = profile?.CharacterData?.PmcData;
            if (pmc is null)
                continue;

            Dictionary<MongoId, long>? achievements = pmc.Achievements;
            if (achievements is null)
                continue;

            // Achievement complete?
            if (!achievements.ContainsKey(RagnarokAchievementId))
                continue;

            // Already sent reward?
            if (achievements.TryGetValue(RewardMarkerId, out var markerValue) && markerValue >= 1)
                continue;

            var items = new List<Item>
            {
                new Item
                {
                    Id = new MongoId(),
                    Template = RewardContainerTpl
                }
            };

            mailSendService.SendSystemMessageToPlayer(
                sessionId,
                "Ragnarök: You've killed Odin 50 times. Here is your reward:",
                items
            );

            // Set marker + save
            achievements[RewardMarkerId] = 1;
            await saveServer.SaveProfileAsync(sessionId);
        }

        return true;
    }
}