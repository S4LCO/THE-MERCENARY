#nullable enable
using EFT;
using UnityEngine;

namespace TheMercenary.Components;

public sealed class BotHuntManager : MonoBehaviour
{
    public BotOwner botOwner = null!;
    public HuntManager huntManager = null!;
    public IPlayer huntTarget = null!;
    public Vector3 knownLocation;

    private float nextTargetUpdate;
    private float nextLocationUpdate;

    public void Init(BotOwner bot, HuntManager manager)
    {
        botOwner = bot;
        huntManager = manager;
        nextTargetUpdate = Time.time + 2f;
        nextLocationUpdate = Time.time + 1f;
    }

    public bool HasTarget()
    {
        return huntTarget != null && huntTarget.HealthController.IsAlive;
    }

    public void Update()
    {
        if (Time.time > nextTargetUpdate)
        {
            nextTargetUpdate = Time.time + 3f;

            if (!HasTarget())
                huntManager.FindTarget(this);
        }

        if (HasTarget() && Time.time > nextLocationUpdate)
        {
            nextLocationUpdate = Time.time + 0.75f;
            knownLocation = huntTarget.Position;
        }
    }
}
