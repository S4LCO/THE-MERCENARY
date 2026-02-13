#nullable enable
using Comfort.Common;
using EFT;
using EFT.Communications;
using SPT.SinglePlayer.Utils.InRaid;
using System.Collections.Generic;
using UnityEngine;

namespace TheMercenary.Components;

public sealed class HuntManager : MonoBehaviourSingleton<HuntManager>
{
    private const int OdinWildSpawnTypeValue = 836500;

    private bool _odinNotifiedThisRaid;

    // Odin is a single boss-type in this mod -> one raid target is enough (and much cheaper).
    private IPlayer? _raidTarget;

    public readonly Dictionary<BotsGroup, IPlayer> huntTargets = new();

    public void InitRaid()
    {
        _odinNotifiedThisRaid = false;
        _raidTarget = null;
        huntTargets.Clear();

        // Robustness: avoid duplicate subscriptions between raids.
        Singleton<IBotGame>.Instance.BotsController.BotSpawner.OnBotCreated -= OnBotCreated;
        Singleton<IBotGame>.Instance.BotsController.BotSpawner.OnBotCreated += OnBotCreated;
    }

    private void OnBotCreated(BotOwner bot)
    {
        TryNotifyOdinSpawn(bot);

        if (!TheMercenary.Plugin.EnableHunt.Value)
            return;

        if (!WildSpawnTypeExtensions.IsMercenary(bot.Profile.Info.Settings.Role))
            return;

        var huntManager = bot.gameObject.GetOrAddComponent<BotHuntManager>();
        huntManager.Init(bot, this);
        FindTarget(huntManager);
    }

    private void TryNotifyOdinSpawn(BotOwner bot)
    {
        if (_odinNotifiedThisRaid)
            return;

        if ((int)bot.Profile.Info.Settings.Role != OdinWildSpawnTypeValue)
            return;

        _odinNotifiedThisRaid = true;
        NotificationManagerClass.DisplayMessageNotification("ODIN HAS SPAWNED.", ENotificationDurationType.Long);
    }

    public void FindTarget(BotHuntManager hunter)
    {
        if (!TheMercenary.Plugin.EnableHunt.Value)
            return;

        // Fast path: keep using the cached raid target if it's still valid.
        if (IsValidHumanTarget(_raidTarget))
        {
            huntTargets[hunter.botOwner.BotsGroup] = _raidTarget!;
            hunter.huntTarget = _raidTarget!;
            hunter.knownLocation = _raidTarget!.Position;
            return;
        }

        // Find the first valid human player. No shuffle = no allocations.
        var players = Singleton<GameWorld>.Instance.AllAlivePlayersList;
        for (var i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (!IsValidHumanTarget(p))
                continue;

            _raidTarget = p;

            huntTargets[hunter.botOwner.BotsGroup] = p;
            hunter.huntTarget = p;
            hunter.knownLocation = p.Position;
            return;
        }
    }

    private static bool IsValidHumanTarget(IPlayer? player)
    {
        if (player == null)
            return false;

        if (player.IsAI)
            return false;

        if (!player.HealthController.IsAlive)
            return false;

        return true;
    }
}
