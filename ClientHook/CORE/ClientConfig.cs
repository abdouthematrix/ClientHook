using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace ClientHook
{
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

        // Hook toggles
        public bool ConnectHookEnabled { get; set; } = true;
        public bool WebsiteHookEnabled { get; set; } = true;
        public bool FlashHookEnabled { get; set; } = true;
        public bool PacketLoggingEnabled { get; set; } = true;

        // Packet logging
        public string PacketLogPath { get; set; } = "ClientHook.Packet.log";
        public bool PacketViewerEnabled { get; set; } = false;

        // Pattern scanning
        public string SendPatternHex { get; set; } = string.Empty;
        public string ReceivePatternHex { get; set; } = string.Empty;
        public int SendPacketOffset { get; set; } = 4;
        public int MaxPayloadLength { get; set; } = 0x3FD;

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
                cfg.AccountServerRedirect = (bool?)json["account_server"]?["enable_redirect"] ?? cfg.AccountServerRedirect;
                cfg.AccountServerIP = (string?)json["account_server"]?["ip"] ?? cfg.AccountServerIP;
                cfg.AccountServerPort = (int?)json["account_server"]?["port"] ?? cfg.AccountServerPort;

                // Game server
                cfg.GameServerRedirect = (bool?)json["game_server"]?["enable_redirect"] ?? cfg.GameServerRedirect;
                cfg.GameServerIP = (string?)json["game_server"]?["ip"] ?? cfg.GameServerIP;
                cfg.GameServerPort = (int?)json["game_server"]?["port"] ?? cfg.GameServerPort;

                // Websites
                cfg.WebsiteHome = (string?)json["websites"]?["home"] ?? cfg.WebsiteHome;
                cfg.WebsiteForum = (string?)json["websites"]?["forum"] ?? cfg.WebsiteForum;
                cfg.WebsiteCredit = (string?)json["websites"]?["credit"] ?? cfg.WebsiteCredit;
                cfg.WebsiteSignup = (string?)json["websites"]?["signup"] ?? cfg.WebsiteSignup;
                cfg.WebsiteSignout = (string?)json["websites"]?["signout"] ?? cfg.WebsiteSignout;

                // Hook toggles
                cfg.ConnectHookEnabled = (bool?)json["hooks"]?["connect"] ?? cfg.ConnectHookEnabled;
                cfg.WebsiteHookEnabled = (bool?)json["hooks"]?["website"] ?? cfg.WebsiteHookEnabled;
                cfg.FlashHookEnabled = (bool?)json["hooks"]?["flash"] ?? cfg.FlashHookEnabled;

                // Packet logging + patterns
                cfg.PacketLoggingEnabled = (bool?)json["packet_logging"]?["enabled"] ?? cfg.PacketLoggingEnabled;
                cfg.PacketLogPath = (string?)json["packet_logging"]?["log_path"] ?? cfg.PacketLogPath;
                cfg.PacketViewerEnabled = (bool?)json["packet_logging"]?["viewer"] ?? cfg.PacketViewerEnabled;
                cfg.SendPatternHex = (string?)json["packet_logging"]?["send_pattern_hex"] ?? cfg.SendPatternHex;
                cfg.ReceivePatternHex = (string?)json["packet_logging"]?["receive_pattern_hex"] ?? cfg.ReceivePatternHex;
                cfg.SendPacketOffset = (int?)json["packet_logging"]?["send_packet_offset"] ?? cfg.SendPacketOffset;
                cfg.MaxPayloadLength = (int?)json["packet_logging"]?["max_payload_length"] ?? cfg.MaxPayloadLength;

                // Logging summary
                Log.Info(" ---- Client Config ----");

                if (cfg.ConnectHookEnabled)
                {
                    Log.Info("[ConnectHook] Enabled");
                    Log.Info($"AccountServer::Redirect = {cfg.AccountServerRedirect}");
                    Log.Info($"AccountServer::IP = {cfg.AccountServerIP}");
                    Log.Info($"AccountServer::Port = {cfg.AccountServerPort}");
                    Log.Info($"GameServer::Redirect = {cfg.GameServerRedirect}");
                    Log.Info($"GameServer::IP = {cfg.GameServerIP}");
                    Log.Info($"GameServer::Port = {cfg.GameServerPort}");
                }
                else Log.Info("[ConnectHook] Disabled");

                if (cfg.WebsiteHookEnabled)
                {
                    Log.Info("[WebsiteHook] Enabled");
                    Log.Info($"Website::Home = {cfg.WebsiteHome}");
                    Log.Info($"Website::Forum = {cfg.WebsiteForum}");
                    Log.Info($"Website::Credit = {cfg.WebsiteCredit}");
                    Log.Info($"Website::Signup = {cfg.WebsiteSignup}");
                    Log.Info($"Website::Signout = {cfg.WebsiteSignout}");
                }
                else Log.Info("[WebsiteHook] Disabled");

                if (cfg.FlashHookEnabled)
                {
                    Log.Info("[FlashHook] Enabled");
                }
                else Log.Info("[FlashHook] Disabled");

                if (cfg.PacketLoggingEnabled)
                {
                    Log.Info("[PacketHook] Enabled");
                    Log.Info($"PacketLogging::LogPath = {cfg.PacketLogPath}");
                    Log.Info($"PacketLogging::Viewer = {cfg.PacketViewerEnabled}");
                    Log.Info($"PacketLogging::SendPatternHex = {cfg.SendPatternHex}");
                    Log.Info($"PacketLogging::ReceivePatternHex = {cfg.ReceivePatternHex}");
                    Log.Info($"PacketLogging::SendPacketOffset = {cfg.SendPacketOffset}");
                    Log.Info($"PacketLogging::MaxPayloadLength = {cfg.MaxPayloadLength}");
                }
                else Log.Info("[PacketHook] Disabled");

                Log.Info(" ----------------------------");
            }
            catch (Exception ex)
            {
                Log.Info($"[Config] Failed to parse config: {ex.Message}");
            }

            return cfg;
        }
    }
}
