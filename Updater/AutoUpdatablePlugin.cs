// -----------------------------------------------------------------------
// <copyright file="AutoUpdatablePlugin.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using Exiled.API.Features;

namespace Mistaken.API
{
    /// <inheritdoc/>
    public abstract class AutoUpdatablePlugin<TConfig> : Plugin<TConfig>
        where TConfig : IAutoUpdatableConfig, new()
    {
        /// <summary>
        /// Gets current plugin version.
        /// </summary>
        public string CurrentVersion { get; private set; }

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            var path = Path.Combine(Paths.Configs, "AutoUpdater");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, $"{this.Author}.{this.Name}.txt");
            if (!File.Exists(path))
                this.AutoUpdate(true);
            else
            {
                this.CurrentVersion = File.ReadAllText(path);
                Exiled.Events.Handlers.Server.RestartingRound += this.Server_RestartingRound;
                this.AutoUpdate(false);
            }

            base.OnEnabled();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
            base.OnDisabled();
        }

        private void AutoUpdate(bool force)
        {
            if (!force)
            {
                if (File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "AutoUpdater", $"{this.Author}.{this.Name}.txt")) != this.CurrentVersion)
                {
                    Log.Info("Update is downloaded, server will restart next round");
                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                    return;
                }
            }

            switch (this.Config.AutoUpdateType)
            {
                case AutoUpdateType.GITHUB:
                    using (var client = new WebClient())
                    {
                        try
                        {
                            client.Headers.Add($"Authorization: token {this.Config.AutoUpdateToken}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "PluginUpdater");
                            var rawResult = client.DownloadString(this.Config.AutoUpdateURL);
                            if (rawResult == string.Empty)
                            {
                                Log.Error("AutoUpdate Failed: AutoUpdate URL returned empty page");
                                return;
                            }

                            Console.WriteLine(rawResult);
                            var decoded = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(rawResult);
                            var result = decoded;
                            if (!force && result.tag_name == this.CurrentVersion)
                            {
                                Log.Debug("Up to date", this.Config.AutoUpdateVerbouseOutput);
                                return;
                            }

                            foreach (var link in result.assets)
                            {
                                Log.Debug("Downloading |" + link.url, this.Config.AutoUpdateVerbouseOutput);
                                using (var client2 = new WebClient())
                                {
                                    client2.Headers.Add($"Authorization: token {this.Config.AutoUpdateToken}");
                                    client2.Headers.Add(HttpRequestHeader.UserAgent, "PluginUpdater");
                                    client2.Headers.Add(HttpRequestHeader.Accept, "application/octet-stream");
                                    string name = (string)link.name;
                                    if (name.StartsWith("Dependencie-"))
                                    {
                                        name = name.Substring(12);
                                        string path = Path.Combine(Environment.CurrentDirectory, "AutoUpdater", name);
                                        client2.DownloadFile((string)link.url, path);
                                        File.Copy(path, Path.Combine(Paths.Dependencies, name));
                                    }
                                    else
                                    {
                                        string path = Path.Combine(Environment.CurrentDirectory, "AutoUpdater", name);
                                        client2.DownloadFile((string)link.url, path);
                                        File.Copy(path, Path.Combine(Paths.Plugins, name));
                                    }
                                }
                            }

                            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "AutoUpdater", $"{this.Author}.{this.Name}.txt"), (string)result.tag_name);
                            Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
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
                            client.Headers.Add($"PRIVATE-TOKEN: {this.Config.AutoUpdateToken}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "PluginUpdater");
                            var rawResult = client.DownloadString(this.Config.AutoUpdateURL);
                            if (rawResult == string.Empty)
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
                            if (!force && result.tag_name == this.CurrentVersion)
                            {
                                Log.Debug("Up to date", this.Config.AutoUpdateVerbouseOutput);
                                return;
                            }

                            foreach (var link in result.assets.links)
                            {
                                Log.Debug("Downloading |" + link.direct_asset_url, this.Config.AutoUpdateVerbouseOutput);
                                string name = (string)link.name;
                                if (name.StartsWith("Dependencie-"))
                                {
                                    name = name.Substring(12);
                                    string path = Path.Combine(Environment.CurrentDirectory, "AutoUpdater", name);
                                    client.DownloadFile((string)link.direct_asset_url, path);
                                    File.Copy(path, Path.Combine(Paths.Dependencies, name));
                                }
                                else
                                {
                                    string path = Path.Combine(Environment.CurrentDirectory, "AutoUpdater", name);
                                    client.DownloadFile((string)link.direct_asset_url, path);
                                    File.Copy(path, Path.Combine(Paths.Plugins, name));
                                }
                            }

                            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "AutoUpdater", $"{this.Author}.{this.Name}.txt"), (string)result.tag_name);
                            this.CurrentVersion = result.tag__name;
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
                    throw new NotImplementedException();
            }
        }

        private void Server_RestartingRound()
        {
            this.AutoUpdate(false);
        }
    }
}
