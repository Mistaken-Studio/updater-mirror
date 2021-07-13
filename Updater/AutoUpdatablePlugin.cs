using Exiled.API.Features;
using Exiled.API.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Mistaken.API
{
    public abstract class AutoUpdatablePlugin<TConfig> : Plugin<TConfig> where TConfig : IAutoUpdatableConfig, new()
    {
        public string CurrentVersion { get; private set; }
        public override void OnEnabled()
        {
            var path = Path.Combine(Paths.Configs, "AutoUpdater");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, $"{this.Author}.{this.Name}.txt");
            if (!File.Exists(path))
                AutoUpdate(true);
            else
            {
                CurrentVersion = File.ReadAllText(path);
                Exiled.Events.Handlers.Server.RestartingRound += Server_RestartingRound;
                AutoUpdate(false);
            }
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RestartingRound -= Server_RestartingRound;
            base.OnDisabled();
        }

        private void AutoUpdate(bool force)
        {
            if(!force)
            {
                if (File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "AutoUpdater", $"{this.Author}.{this.Name}.txt")) != CurrentVersion)
                {
                    Log.Info("Update is downloaded, server will restart next round");
                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                    return;
                }
            }
            switch (Config.AutoUpdateType)
            {
                case AutoUpdateType.GITHUB:
                    using (var client = new WebClient())
                    {
                        try
                        {
                            client.Headers.Add($"Authorization: token { Config.AutoUpdateToken}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "PluginUpdater");
                            var rawResult = client.DownloadString(Config.AutoUpdateURL);
                            if (rawResult == "")
                            {
                                Log.Error("AutoUpdate Failed: AutoUpdate URL returned empty page");
                                return;
                            }
                            Console.WriteLine(rawResult);
                            var decoded = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(rawResult);
                            var result = decoded;
                            if (!force && result.tag_name == CurrentVersion)
                            {
                                Log.Debug("Up to date", Config.AutoUpdateVerbouseOutput);
                                return;
                            }
                            foreach (var link in result.assets)
                            {
                                Log.Debug("Downloading |" + link.url, Config.AutoUpdateVerbouseOutput);
                                using (var client2 = new WebClient())
                                {
                                    client2.Headers.Add($"Authorization: token {Config.AutoUpdateToken}");
                                    client2.Headers.Add(HttpRequestHeader.UserAgent, "PluginUpdater");
                                    client2.Headers.Add(HttpRequestHeader.Accept, "application/octet-stream");
                                    client2.DownloadFile((string)link.url, Path.Combine(Environment.CurrentDirectory, "AutoUpdater", (string)link.name));
                                }
                            }
                            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "AutoUpdater", $"{this.Author}.{this.Name}.txt"), (string)result.tag_name);
                            Exiled.Events.Handlers.Server.RestartingRound -= Server_RestartingRound;
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"AutoUpdate Failed: {ex.Message}");
                            Log.Error(ex.StackTrace);
                        }
                    }
                    break;
                case AutoUpdateType.GITLAB:
                    using (var client = new WebClient())
                    {
                        try
                        {
                            client.Headers.Add($"PRIVATE-TOKEN: {Config.AutoUpdateToken}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "PluginUpdater");
                            var rawResult = client.DownloadString(Config.AutoUpdateURL);
                            if (rawResult == "")
                            {
                                Log.Error("AutoUpdate Failed: AutoUpdate URL returned empty page");
                                return;
                            }
                            var decoded = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic[]>(rawResult);
                            if (decoded.Length == 0)
                            {
                                Log.Error("AutoUpdate Failed: No releases found");
                                return;
                            }
                            var result = decoded[0];
                            if (!force && result.tag_name == CurrentVersion)
                            {
                                Log.Debug("Up to date", Config.AutoUpdateVerbouseOutput);
                                return;
                            }
                            foreach (var link in result.assets.links)
                            {
                                Log.Debug("Downloading |" + link.direct_asset_url, Config.AutoUpdateVerbouseOutput);
                                client.DownloadFile((string)link.direct_asset_url, Path.Combine(Environment.CurrentDirectory, "AutoUpdater", (string)link.name));
                            }
                            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "AutoUpdater", $"{this.Author}.{this.Name}.txt"), (string)result.tag_name);
                            CurrentVersion = result.tag__name;
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"AutoUpdate Failed: {ex.Message}");
                            Log.Error(ex.StackTrace);
                        }
                    }
                    break;
                case AutoUpdateType.LOGIN:
                    throw new NotImplementedException("How should this work, barwa help?");
            }
        }

        private void Server_RestartingRound()
        {
            AutoUpdate(false);
        }
    }

    public interface IAutoUpdatableConfig : IConfig
    {
        [Description("")]
        bool AutoUpdateVerbouseOutput { get; set; }
        [Description("")]
        string AutoUpdateURL { get; set; }
        [Description("")]
        AutoUpdateType AutoUpdateType { get; set; }
        [Description("")]
        string AutoUpdateLogin { get; set; }
        [Description("")]
        string AutoUpdateToken { get; set; }
    }

    public enum AutoUpdateType
    {
        GITLAB,
        GITHUB,
        LOGIN
    }
}
