namespace TheMercenaryServer.Models;

public sealed class SpawnConfig
{
    public bool enableLogs { get; set; } = false;
    public Dictionary<string, MapSpawnConfig> maps { get; set; } = new();
}

public sealed class MapSpawnConfig
{
    public bool enabled { get; set; } = true;
    public int chance { get; set; } = 100;
    public List<string> zones { get; set; } = new();
}
