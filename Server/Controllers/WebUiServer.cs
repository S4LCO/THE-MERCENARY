// Pfad: Server/Controllers/WebUiServer.cs

using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheMercenaryServer.Models;

namespace TheMercenaryServer.Controllers;

/// <summary>
/// Lightweight local Web UI (HttpListener) so users can configure Odin spawns without editing JSON.
/// </summary>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 700)]
public sealed class WebUiServer(
    WebUiConfigController webUiConfig,
    SpawnConfigController spawnConfig,
    SpawnController spawnController,
    MercenaryLogger logger
) : IOnLoad
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    public Task OnLoad()
    {
        var cfg = webUiConfig.Config;

        if (!cfg.enabled)
        {
            logger.Info("WebUI disabled (config/webui.jsonc).");
            return Task.CompletedTask;
        }

        var host = string.IsNullOrWhiteSpace(cfg.host) ? "127.0.0.1" : cfg.host;
        var port = cfg.port <= 0 ? 6970 : cfg.port;
        var prefix = $"http://{host}:{port}/";

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ListenLoop(_listener, _cts.Token));

            logger.Info($"WebUI listening on {prefix} (local config editor).");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to start WebUI on {prefix}: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private async Task ListenLoop(HttpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext? ctx = null;
            try
            {
                ctx = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch
            {
                if (!listener.IsListening)
                    break;
            }

            if (ctx == null)
                continue;

            _ = Task.Run(() => Handle(ctx), ct);
        }
    }

    private async Task Handle(HttpListenerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            var res = ctx.Response;

            // Safety: by default only allow localhost.
            var allowRemote = webUiConfig.Config.allowRemote;
            if (!allowRemote && req.RemoteEndPoint != null && !IPAddress.IsLoopback(req.RemoteEndPoint.Address))
            {
                res.StatusCode = 403;
                await WriteText(res, "Forbidden").ConfigureAwait(false);
                return;
            }

            var path = (req.Url?.AbsolutePath ?? "/").TrimEnd('/');
            if (path == "")
                path = "/";

            // API
            if (path == "/api/config" && req.HttpMethod == "GET")
            {
                await HandleGetConfig(res).ConfigureAwait(false);
                return;
            }

            if (path == "/api/config" && req.HttpMethod == "POST")
            {
                await HandlePostConfig(req, res).ConfigureAwait(false);
                return;
            }

            if (path == "/api/meta" && req.HttpMethod == "GET")
            {
                await HandleGetMeta(res).ConfigureAwait(false);
                return;
            }

            // Static files
            if (path == "/")
            {
                await ServeFile(res, "ui/index.html", "text/html; charset=utf-8").ConfigureAwait(false);
                return;
            }

            if (path == "/style.css")
            {
                await ServeFile(res, "ui/style.css", "text/css; charset=utf-8").ConfigureAwait(false);
                return;
            }

            if (path == "/background.png")
            {
                await ServeFile(res, "ui/background.png", "image/png").ConfigureAwait(false);
                return;
            }

            res.StatusCode = 404;
            await WriteText(res, "Not found").ConfigureAwait(false);
        }
        catch
        {
            // Never crash the server for UI.
            try { ctx.Response.StatusCode = 500; ctx.Response.Close(); } catch { }
        }
    }

    private Task HandleGetConfig(HttpListenerResponse res)
    {
        var cfg = spawnConfig.Config;

        var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
        res.StatusCode = 200;
        res.ContentType = "application/json; charset=utf-8";
        return WriteBytes(res, Encoding.UTF8.GetBytes(json));
    }

    private async Task HandlePostConfig(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
        var body = await reader.ReadToEndAsync().ConfigureAwait(false);

        SpawnConfig? newCfg;
        try
        {
            newCfg = JsonSerializer.Deserialize<SpawnConfig>(body, new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            res.StatusCode = 400;
            await WriteText(res, $"Invalid JSON: {ex.Message}").ConfigureAwait(false);
            return;
        }

        if (newCfg == null)
        {
            res.StatusCode = 400;
            await WriteText(res, "Invalid JSON: empty payload").ConfigureAwait(false);
            return;
        }

        // Minimal validation: clamp chance, normalize zones
        foreach (var kvp in newCfg.maps)
        {
            var m = kvp.Value;

            if (m.chance < 0) m.chance = 0;
            if (m.chance > 100) m.chance = 100;

            if (m.zones != null)
            {
                for (var i = m.zones.Count - 1; i >= 0; i--)
                {
                    if (string.IsNullOrWhiteSpace(m.zones[i]))
                        m.zones.RemoveAt(i);
                }
            }
        }

        spawnConfig.SaveToDisk(newCfg);
        spawnController.ApplySpawnConfig(force: true);

        res.StatusCode = 200;
        await WriteText(res, "Saved and applied.").ConfigureAwait(false);
    }

    /// <summary>
    /// ✅ IMPORTANT: Meta now comes from spawn.jsonc (authoritative),
    /// not from SPT DB (which contains internal/variant location keys).
    /// </summary>
    private async Task HandleGetMeta(HttpListenerResponse res)
    {
        var cfg = spawnConfig.Config;

        // Maps from config (no fake maps)
        var maps = cfg.maps.Keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Zones per map from config (full list you provided)
        var zonesByMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in cfg.maps)
        {
            var mapId = kvp.Key;
            var zones = kvp.Value.zones ?? new List<string>();

            zonesByMap[mapId] = zones
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Select(z => z.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(z => z, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var payload = new { maps, zonesByMap };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        res.StatusCode = 200;
        res.ContentType = "application/json; charset=utf-8";
        await WriteBytes(res, Encoding.UTF8.GetBytes(json)).ConfigureAwait(false);
    }

    private async Task ServeFile(HttpListenerResponse res, string relativePath, string contentType)
    {
        var abs = Path.Join(webUiConfig.GetModRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(abs))
        {
            res.StatusCode = 404;
            await WriteText(res, "Not found").ConfigureAwait(false);
            return;
        }

        var bytes = await File.ReadAllBytesAsync(abs).ConfigureAwait(false);
        res.StatusCode = 200;
        res.ContentType = contentType;
        await WriteBytes(res, bytes).ConfigureAwait(false);
    }

    private static Task WriteText(HttpListenerResponse res, string text)
        => WriteBytes(res, Encoding.UTF8.GetBytes(text));

    private static Task WriteBytes(HttpListenerResponse res, byte[] bytes)
    {
        res.ContentLength64 = bytes.Length;
        return res.OutputStream.WriteAsync(bytes, 0, bytes.Length)
            .ContinueWith(_ => { try { res.OutputStream.Close(); res.Close(); } catch { } });
    }
}
