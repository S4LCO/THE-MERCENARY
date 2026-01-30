#nullable enable
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace TheMercenary;

[BepInDependency("xyz.drakia.bigbrain", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("me.sol.sain", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(ClientInfo.GUID, ClientInfo.PluginName, ClientInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    public static ManualLogSource? LogSource;

    internal static ConfigEntry<bool> EnableHunt { get; private set; } = null!;

    private void Awake()
    {
        LogSource = Logger;

        EnableHunt = Config.Bind(
            "Odin",
            "EnableHunt",
            true,
            "Enable/disable Odin hunt behavior (BigBrain layer + target tracking)."
        );

        // Only enable patches.
        PatchBootstrap.Enable();
    }
}