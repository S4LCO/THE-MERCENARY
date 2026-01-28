#nullable enable
using Comfort.Common;
using EFT;
using SPT.SinglePlayer.Utils.InRaid;
using System.Collections.Generic;
using UnityEngine;

namespace TheMercenary.Components;

public sealed class HuntManager : MonoBehaviourSingleton<HuntManager>
{
    public readonly Dictionary<BotsGroup, IPlayer> huntTargets = new();

    public void InitRaid()
    {
        Singleton<IBotGame>.Instance.BotsController.BotSpawner.OnBotCreated += OnBotCreated;
    }

    private void OnBotCreated(BotOwner bot)
    {
        if (!WildSpawnTypeExtensions.IsMercenary(bot.Profile.Info.Settings.Role))
            return;

        var huntManager = bot.gameObject.GetOrAddComponent<BotHuntManager>();
        huntManager.Init(bot, this);
        FindTarget(huntManager);
    }

    public void FindTarget(BotHuntManager hunter)
    {
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
