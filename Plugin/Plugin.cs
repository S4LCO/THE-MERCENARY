#nullable enable
using BepInEx;
using BepInEx.Logging;

namespace TheMercenary;

[BepInDependency("xyz.drakia.bigbrain", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("me.sol.sain", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(ClientInfo.GUID, ClientInfo.PluginName, ClientInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    public static ManualLogSource? LogSource;

    private void Awake()
    {
        LogSource = Logger;

        // Only enable patches.
        PatchBootstrap.Enable();
    }
}
