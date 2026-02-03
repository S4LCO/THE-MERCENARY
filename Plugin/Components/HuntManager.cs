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

    public readonly Dictionary<BotsGroup, IPlayer> huntTargets = new();

    public void InitRaid()
    {
        _odinNotifiedThisRaid = false;

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

        var allPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList.Randomize();

        foreach (var player in allPlayers)
        {
            if (!player.HealthController.IsAlive)
                continue;

            if (player.IsAI)
                continue;

            huntTargets[hunter.botOwner.BotsGroup] = player;
            hunter.huntTarget = player;
            hunter.knownLocation = player.Position;
            return;
        }
    }
}