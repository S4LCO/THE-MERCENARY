#nullable enable
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.InputSystem;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using TheMercenary.Behavior.Layers;

namespace TheMercenary.Patches;

internal sealed class TarkovInitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TarkovApplication).GetMethod(
            nameof(TarkovApplication.Init),
            BindingFlags.Public | BindingFlags.Instance
        );
    }

    [PatchPostfix]
    private static void PatchPostfix(IAssetsManager assetsManager, InputTree inputTree)
    {
        var brains = new List<string>
        {
            "PMC",
            "ExUsec",
            "Assault",
            "PmcUsec",
            "PmcBear",
            "PmcUSEC",
            "PmcBEAR"
        };

        var types = new List<WildSpawnType>
        {
            (WildSpawnType)836500
        };

        // BigBrain in your setup does not accept named parameters
        BrainManager.AddCustomLayer(typeof(HuntTargetLayer), brains, 5, types);
    }
}
