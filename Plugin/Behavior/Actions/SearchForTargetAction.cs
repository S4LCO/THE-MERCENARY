#nullable enable
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using TheMercenary.Components;
using UnityEngine;

namespace TheMercenary.Behavior.Actions;

internal sealed class SearchForTargetAction : CustomLogic
{
    private BotHuntManager huntManager = null!;
    private float endTime;

    public SearchForTargetAction(BotOwner botOwner) : base(botOwner)
    {
    }

    public override void Start()
    {
        base.Start();
        huntManager = BotOwner.GetComponent<BotHuntManager>();
        endTime = Time.time + 30f;
    }

    public override void Update(CustomLayer.ActionData data)
    {
        if (Time.time > endTime)
            return;

        BotOwner.GoToPoint(huntManager.knownLocation, mustHaveWay: false);
    }
}
