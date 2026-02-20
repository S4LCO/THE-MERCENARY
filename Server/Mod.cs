using MoreBotsServer.Models;
using MoreBotsServer.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using System.Reflection;
using TheMercenaryServer.Controllers;

namespace TheMercenaryServer;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "de.salco.themercenary";
    public override string Name { get; init; } = "The Mercenary";
    public override string Author { get; init; } = "Salco";
    public override List<string>? Contributors { get; init; } = new() { };
    public override SemanticVersioning.Version Version { get; init; } = new(1, 5, 4);
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.3");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new()
    {
        { "com.morebotsapi.tacticaltoaster", new SemanticVersioning.Range(">=1.1.0") },
        { "com.wtt.commonlib", new SemanticVersioning.Range("~2.0.15") },
        { "de.salco.salcosarsenal", new SemanticVersioning.Range("~2.0.0") }
    };
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "MIT";
}

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = MoreBotsServer.MoreBotsLoadOrder.LoadBots)]
public sealed class TheMercenaryServer(
    MoreBotsServer.MoreBotsAPI moreBotsApi,
    MoreBotsCustomBotTypeService customBotTypeService,
    FactionService factionService,
    WTTServerCommonLib.WTTServerCommonLib wttCommon
) : IOnLoad
{
    private const int WildSpawnTypeValue = 836500;
    private const string BotTypeName = "mercenary";

    public async Task OnLoad()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var botTypes = new List<string> { BotTypeName };

        var typeNames = new Dictionary<int, string>
        {
            { WildSpawnTypeValue, BotTypeName }
        };

        await moreBotsApi.LoadBotsShared(assembly, BotTypeName, botTypes);

        await wttCommon.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly);
        await wttCommon.CustomLocaleService.CreateCustomLocales(assembly);

        // Load custom achievements (ODIN 50x)
        await wttCommon.CustomAchievementService.CreateCustomAchievements(assembly);

        customBotTypeService.AddCustomWildSpawnTypeNames(typeNames);

        // Make Mercenary neutral to bots by default:
        // Instead of defining enemies + revenge, define friendlies for both sides.
        factionService.AddFriendlyByFaction(botTypes, "savage");
        factionService.AddFriendlyByFaction(botTypes, "rogues");
        factionService.AddFriendlyByFaction(botTypes, "usec");
        factionService.AddFriendlyByFaction(botTypes, "bear");
        factionService.AddFriendlyByFaction(botTypes, "infected");

        factionService.AddFriendlyByFaction("savage", BotTypeName);
        factionService.AddFriendlyByFaction("rogues", BotTypeName);
        factionService.AddFriendlyByFaction("usec", BotTypeName);
        factionService.AddFriendlyByFaction("bear", BotTypeName);
        factionService.AddFriendlyByFaction("infected", BotTypeName);

        await Task.CompletedTask;
    }
}

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = MoreBotsServer.MoreBotsLoadOrder.LoadFactions)]
public sealed class TheMercenaryFaction(FactionService factionService) : IOnLoad
{
    private const int WildSpawnTypeValue = 836500;
    private const string FactionName = "mercenary";

    public async Task OnLoad()
    {
        if (!factionService.Factions.ContainsKey(FactionName))
        {
            factionService.Factions.Add(FactionName, new Faction()
            {
                Name = FactionName,
                BotTypes = { (WildSpawnType)WildSpawnTypeValue }
            });
        }

        await Task.CompletedTask;
    }
}
