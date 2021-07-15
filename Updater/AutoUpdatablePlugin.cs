// -----------------------------------------------------------------------
// <copyright file="AutoUpdatablePlugin.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using Exiled.API.Features;
using Newtonsoft.Json;

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
            var path = Path.Combine(Paths.Plugins, "AutoUpdater");
            if (!Directory.Exists(path))
            {
                Log.Debug($"{path} doesn't exist, creating...", this.Config.AutoUpdateVerbouseOutput);
                Directory.CreateDirectory(path);
            }
            else
                Log.Debug($"{path} exist", this.Config.AutoUpdateVerbouseOutput);
            path = Path.Combine(path, $"{this.Author}.{this.Name}.txt");
            if (!File.Exists(path))
            {
                Log.Debug($"{path} doesn't exist, forcing auto update", this.Config.AutoUpdateVerbouseOutput);
                this.AutoUpdate(true);
            }
            else
            {
                Log.Debug($"{path} exist, checking version", this.Config.AutoUpdateVerbouseOutput);
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
            Log.Debug("Running AutoUpdate...", this.Config.AutoUpdateVerbouseOutput);
            if (string.IsNullOrWhiteSpace(this.Config.AutoUpdateUrl))
            {
                Log.Debug("AutoUpdate is disabled", this.Config.AutoUpdateVerbouseOutput);
                return;
            }

            if (!force)
            {
                if (File.ReadAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Author}.{this.Name}.txt")) != this.CurrentVersion)
                {
                    Log.Info("Update is downloaded, server will restart next round");
                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                    return;
                }
            }

            switch (this.Config.AutoUpdateType)
            {
                case AutoUpdateType.GITHUB:
                    Log.Debug($"Checking for update using GITHUB, chekcing latest release", this.Config.AutoUpdateVerbouseOutput);
                    using (var client = new WebClient())
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(this.Config.AutoUpdateToken))
                                client.Headers.Add($"Authorization: token {this.Config.AutoUpdateToken}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "PluginUpdater");
                            var rawResult = client.DownloadString(this.Config.AutoUpdateUrl);
                            if (rawResult == string.Empty)
                            {
                                Log.Error("AutoUpdate Failed: AutoUpdate URL returned empty page");
                                return;
                            }

                            var decoded = Newtonsoft.Json.JsonConvert.DeserializeObject<GitHub.Release>(rawResult);
                            var result = decoded;
                            if (!force && result.Tag == this.CurrentVersion)
                            {
                                Log.Debug("Up to date", this.Config.AutoUpdateVerbouseOutput);
                                return;
                            }

                            foreach (var link in result.Assets)
                            {
                                Log.Debug("Downloading |" + link.Url, this.Config.AutoUpdateVerbouseOutput);
                                using (var client2 = new WebClient())
                                {
                                    client2.Headers.Add($"Authorization: token {this.Config.AutoUpdateToken}");
                                    client2.Headers.Add(HttpRequestHeader.UserAgent, "PluginUpdater");
                                    client2.Headers.Add(HttpRequestHeader.Accept, "application/octet-stream");
                                    string name = link.Name;
                                    if (name.StartsWith("Dependencie-"))
                                    {
                                        name = name.Substring(12);
                                        string path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                                        client2.DownloadFile(link.Url, path);
                                        File.Copy(path, Path.Combine(Paths.Dependencies, name), true);
                                    }
                                    else
                                    {
                                        string path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                                        client2.DownloadFile(link.Url, path);
                                        File.Copy(path, Path.Combine(Paths.Plugins, name), true);
                                    }
                                }
                            }

                            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Author}.{this.Name}.txt"), result.Tag);
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
                    Log.Debug($"Checking for update using GITLAB, chekcing for releases", this.Config.AutoUpdateVerbouseOutput);
                    using (var client = new WebClient())
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(this.Config.AutoUpdateToken))
                                client.Headers.Add($"PRIVATE-TOKEN: {this.Config.AutoUpdateToken}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                            Log.Debug($"Downloading release list from {this.Config.AutoUpdateUrl}", this.Config.AutoUpdateVerbouseOutput);
                            var rawResult = client.DownloadString(this.Config.AutoUpdateUrl);
                            if (rawResult == string.Empty)
                            {
                                Log.Error("AutoUpdate Failed: AutoUpdate URL returned empty page");
                                return;
                            }

                            var decoded = Newtonsoft.Json.JsonConvert.DeserializeObject<GitLab.Release[]>(rawResult);
                            if (decoded.Length == 0)
                            {
                                Log.Error("AutoUpdate Failed: No releases found");
                                return;
                            }

                            var result = decoded[0];
                            if (!force && result.Tag == this.CurrentVersion)
                            {
                                Log.Debug("Up to date", this.Config.AutoUpdateVerbouseOutput);
                                return;
                            }

                            foreach (var link in result.Assets.Links)
                            {
                                Log.Debug("Downloading |" + link.Url, this.Config.AutoUpdateVerbouseOutput);
                                string name = link.Name;
                                if (name.StartsWith("Dependencie-"))
                                {
                                    name = name.Substring(12);
                                    string path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                                    client.DownloadFile(link.Url, path);
                                    File.Copy(path, Path.Combine(Paths.Dependencies, name), true);
                                }
                                else
                                {
                                    string path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                                    client.DownloadFile(link.Url, path);
                                    File.Copy(path, Path.Combine(Paths.Plugins, name), true);
                                }
                            }

                            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Author}.{this.Name}.txt"), result.Tag);
                            this.CurrentVersion = result.Tag;
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
