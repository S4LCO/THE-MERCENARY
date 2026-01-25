#nullable enable
using BepInEx;

namespace TheMercenary.Prepatch;

[BepInDependency("com.morebotsapiprepatch.tacticaltoaster", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(TheMercenary.ClientInfo.PreLoadGUID, TheMercenary.ClientInfo.PreLoadName, TheMercenary.ClientInfo.Version)]
public sealed class TheMercenaryPrepatchPlugin : BaseUnityPlugin
{
    public static TheMercenaryPrepatchPlugin? Instance;

    private void Awake()
    {
        Instance = this;
    }
}
