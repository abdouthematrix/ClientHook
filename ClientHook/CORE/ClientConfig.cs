using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace ClientHook;

public class ClientConfig
{
    // Account server
    public bool AccountServerRedirect { get; set; } = true;
    public string AccountServerIP { get; set; } = "192.168.1.5";
    public int AccountServerPort { get; set; } = 9960;

    // Game server
    public bool GameServerRedirect { get; set; } = false;
    public string GameServerIP { get; set; } = "192.168.1.5";
    public int GameServerPort { get; set; } = 19000;

    // Website redirects
    public string WebsiteHome { get; set; } = string.Empty;
    public string WebsiteForum { get; set; } = string.Empty;
    public string WebsiteCredit { get; set; } = string.Empty;
    public string WebsiteSignup { get; set; } = string.Empty;
    public string WebsiteSignout { get; set; } = string.Empty;

    public static ClientConfig Load()
    {
        string moduleDir = Path.GetDirectoryName(
               System.Reflection.Assembly.GetExecutingAssembly().Location);
        string path = Path.Combine(moduleDir ?? ".", "ClientHookConfig.json");

        var cfg = new ClientConfig();

        if (!File.Exists(path))
        {
            Log.Info("[Config] No config file found, using defaults.");
            return cfg;
        }

        try
        {
            JObject json = JObject.Parse(File.ReadAllText(path));

            // Account server
            cfg.AccountServerRedirect = (bool?)json["AccountServer"]?["Redirect"] ?? cfg.AccountServerRedirect;
            cfg.AccountServerIP = (string?)json["AccountServer"]?["IP"] ?? cfg.AccountServerIP;
            cfg.AccountServerPort = (int?)json["AccountServer"]?["Port"] ?? cfg.AccountServerPort;

            // Game server
            cfg.GameServerRedirect = (bool?)json["GameServer"]?["Redirect"] ?? cfg.GameServerRedirect;
            cfg.GameServerIP = (string?)json["GameServer"]?["IP"] ?? cfg.GameServerIP;
            cfg.GameServerPort = (int?)json["GameServer"]?["Port"] ?? cfg.GameServerPort;

            // Websites
            cfg.WebsiteHome = (string?)json["Websites"]?["Home"] ?? cfg.WebsiteHome;
            cfg.WebsiteForum = (string?)json["Websites"]?["Forum"] ?? cfg.WebsiteForum;
            cfg.WebsiteCredit = (string?)json["Websites"]?["Credit"] ?? cfg.WebsiteCredit;
            cfg.WebsiteSignup = (string?)json["Websites"]?["Signup"] ?? cfg.WebsiteSignup;
            cfg.WebsiteSignout = (string?)json["Websites"]?["Signout"] ?? cfg.WebsiteSignout;

            Log.Info(" ---- Client Config ----");
            Log.Info($"AccountServer::Redirect = {cfg.AccountServerRedirect}");
            Log.Info($"AccountServer::IP = {cfg.AccountServerIP}");
            Log.Info($"AccountServer::Port = {cfg.AccountServerPort}");
            Log.Info($"GameServer::Redirect = {cfg.GameServerRedirect}");
            Log.Info($"GameServer::IP = {cfg.GameServerIP}");
            Log.Info($"GameServer::Port = {cfg.GameServerPort}");
            Log.Info($"Website::Home = {cfg.WebsiteHome}");
            Log.Info($"Website::Forum = {cfg.WebsiteForum}");
            Log.Info($"Website::Credit = {cfg.WebsiteCredit}");
            Log.Info($"Website::Signup = {cfg.WebsiteSignup}");
            Log.Info($"Website::Signout = {cfg.WebsiteSignout}");
            Log.Info(" ----------------------------");
        }
        catch (Exception ex)
        {
            Log.Info($"[Config] Failed to parse config: {ex.Message}");
        }

        return cfg;
    }
}
