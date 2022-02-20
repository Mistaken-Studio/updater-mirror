// -----------------------------------------------------------------------
// <copyright file="AutoUpdater.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Mistaken.Updater.API;
using Mistaken.Updater.Config;
using Newtonsoft.Json;
using RoundRestarting;

namespace Mistaken.Updater.Internal
{
    /// <inheritdoc/>
    public class AutoUpdater : Plugin<AutoUpdaterPluginConfig>
    {
        /// <inheritdoc/>
        public override string Name => "Updater";

        /// <inheritdoc/>
        public override string Author => "Mistaken Devs";

        /// <inheritdoc/>
        public override PluginPriority Priority => PluginPriority.Last;

        /// <inheritdoc/>
        public override Version RequiredExiledVersion => new Version(3, 0, 3);

        /// <inheritdoc/>
        public override string Prefix => "MUPDATER";

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

            MyConfig = new AutoUpdateConfig(this.Config.AutoUpdateConfig);

            var path = Path.Combine(Paths.Plugins, "AutoUpdater");
            if (!Directory.Exists(path))
            {
                Log.Debug($"{path} doesn't exist, creating...", MyConfig.VerbouseOutput);
                Directory.CreateDirectory(path);
            }
            else
                Log.Debug($"{path} exist", MyConfig.VerbouseOutput);

            Exiled.Events.Handlers.Server.WaitingForPlayers += this.Server_WaitingForPlayers;
            Task.Run(() =>
            {
                if (this.DoAutoUpdates())
                {
                    IdleMode.PauseIdleMode = true;
                    Task.Delay(5000);
                    Mirror.NetworkServer.SendToAll<RoundRestartMessage>(new RoundRestartMessage(RoundRestartType.FullRestart, (float)GameCore.ConfigFile.ServerConfig.GetInt("full_restart_rejoin_time", 25), 0, true, true));
                    MEC.Timing.CallDelayed(1, () => Server.Restart());
                }
                else
                    MEC.Timing.CallDelayed(5, () => Exiled.Events.Handlers.Server.RestartingRound += this.Server_RestartingRound);
            });
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= this.Server_WaitingForPlayers;
        }

        internal static AutoUpdateConfig MyConfig { get; private set; }

        internal static AutoUpdater Instance { get; private set; }

        internal enum Action : byte
        {
            NONE,
            RESTART,
            UPDATE_AND_RESTART,
        }

        internal bool DoAutoUpdates()
        {
            bool changed = false;
            foreach (var plugin in Exiled.Loader.Loader.Plugins.Where(x => x.Config is IAutoUpdatableConfig).Select(x => x as IPlugin<IAutoUpdatableConfig>))
            {
                var config = new AutoUpdateConfig(plugin.Config.AutoUpdateConfig);
                string path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.txt");
                if (!File.Exists(path))
                {
                    Log.Debug($"[{plugin.Name}]{path} doesn't exist, forcing auto update", config.VerbouseOutput);
                    if (this.DoAutoUpdate(plugin, true) != Action.NONE)
                        changed = true;
                }
                else
                {
                    Log.Debug($"[{plugin.Name}] {path} exist, checking version", config.VerbouseOutput);
                    if (this.DoAutoUpdate(plugin, false) != Action.NONE)
                        changed = true;
                }
            }

            return changed;
        }

        internal Action DoAutoUpdate(IPlugin<IAutoUpdatableConfig> plugin, bool force, AutoUpdateType forcedConfig = AutoUpdateType.DISABLED)
        {
            try
            {
                var config = new AutoUpdateConfig(plugin.Config.AutoUpdateConfig);
                Log.Debug($"[{plugin.Name}] Running AutoUpdate...", config.VerbouseOutput);
                if (string.IsNullOrWhiteSpace(config.Url) || config.Type == AutoUpdateType.DISABLED)
                {
                    Log.Debug($"[{plugin.Name}] AutoUpdate is disabled", config.VerbouseOutput);
                    return Action.NONE;
                }

                Version pluginVersion = plugin.Version;

                if (pluginVersion.Major >= 3)
                    pluginVersion = plugin.Assembly.GetName().Version;

                string fileVersion = string.Empty;
                if (!force)
                {
                    fileVersion = File.ReadAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.txt"));
                    if (fileVersion.StartsWith("Dev: "))
                        Log.Debug($"[{plugin.Name}] Detected Development build, skipping CurrentVersion check", config.VerbouseOutput);
                    else
                    {
                        if (fileVersion != $"{pluginVersion.Major}.{pluginVersion.Minor}.{pluginVersion.Build}")
                        {
                            Log.Info($"[{plugin.Name}] Update from {pluginVersion.Major}.{pluginVersion.Minor}.{pluginVersion.Build} to {fileVersion} is downloaded, server will restart next round");
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                            return Action.RESTART;
                        }
                    }
                }

                string newVersion;
                if (forcedConfig == AutoUpdateType.DISABLED)
                    forcedConfig = config.Type;
                switch (forcedConfig)
                {
                    case AutoUpdateType.DISABLED:
                        return Action.NONE;
                    case AutoUpdateType.GITHUB:
                        Log.Debug($"[{plugin.Name}] Checking for update using GITHUB, chekcing latest release", config.VerbouseOutput);
                        try
                        {
                            var release = GitHub.Release.DownloadLatest(plugin, config);

                            if (!force && release.Tag == $"{pluginVersion.Major}.{pluginVersion.Minor}.{pluginVersion.Build}")
                            {
                                Log.Debug($"[{plugin.Name}] Up to date", config.VerbouseOutput);
                                return Action.NONE;
                            }

                            if (!force && release.Tag == fileVersion)
                            {
                                Log.Info($"[{plugin.Name}] Update already downloaded, waiting for server restart");
                                ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                                return Action.RESTART;
                            }

                            foreach (var asset in release.Assets)
                                asset.DownloadAsset(plugin, config);

                            newVersion = release.Tag;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"[{plugin.Name}] AutoUpdate Failed: {ex.Message}");
                            Log.Error(ex.StackTrace);
                            return Action.NONE;
                        }

                        break;

                    case AutoUpdateType.GITHUB_DEVELOPMENT:
                        Log.Debug($"[{plugin.Name}] Checking for update using GITHUB, chekcing for artifacts", config.VerbouseOutput);
                        try
                        {
                            var artifacts = GitHub.Artifacts.Download(plugin, config);
                            if (artifacts.ArtifactsArray.Length == 0)
                            {
                                Log.Debug($"[{plugin.Name}] No artifacts found, searching for Releases", config.VerbouseOutput);
                                return this.DoAutoUpdate(plugin, force, AutoUpdateType.GITHUB);
                            }

                            var artifact = artifacts.ArtifactsArray.OrderByDescending(x => x.Id).First();

                            if (!force && "Dev: " + artifact.NodeId == fileVersion)
                            {
                                Log.Debug($"[{plugin.Name}] Up to date", config.VerbouseOutput);
                                return Action.NONE;
                            }

                            artifact.Download(plugin, config);

                            newVersion = "Dev: " + artifact.NodeId;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"[{plugin.Name}] AutoUpdate Failed: {ex.Message}");
                            Log.Error(ex.StackTrace);
                            return Action.NONE;
                        }

                        break;

                    case AutoUpdateType.GITLAB:
                        Log.Debug($"[{plugin.Name}] Checking for update using GITLAB, chekcing for releases", config.VerbouseOutput);
                        try
                        {
                            var releases = GitLab.Release.Download(plugin, config);
                            if (releases.Length == 0)
                            {
                                Log.Error($"[{plugin.Name}] AutoUpdate Failed: No releases found");
                                return Action.NONE;
                            }

                            var release = releases[0];
                            if (!force && release.Tag == $"{pluginVersion.Major}.{pluginVersion.Minor}.{pluginVersion.Build}")
                            {
                                Log.Debug($"[{plugin.Name}] Up to date", config.VerbouseOutput);
                                return Action.NONE;
                            }
                            else
                                Log.Debug($"[{plugin.Name}] Not up to date, Current: {pluginVersion.Major}.{pluginVersion.Minor}.{pluginVersion.Build}, Newest: {release.Tag}", config.VerbouseOutput);

                            if (!force && release.Tag == fileVersion)
                            {
                                Log.Info($"[{plugin.Name}] Update already downloaded, waiting for server restart");
                                ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                                return Action.RESTART;
                            }

                            foreach (var link in release.Assets.Links)
                                link.Download(plugin, config);

                            newVersion = release.Tag;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"[{plugin.Name}] AutoUpdate Failed: {ex.Message}");
                            Log.Error(ex.StackTrace);
                            return Action.NONE;
                        }

                        break;

                    case AutoUpdateType.GITLAB_DEVELOPMENT:
                        Log.Debug($"[{plugin.Name}] Checking for update using GITLAB, chekcing for artifacts", config.VerbouseOutput);
                        try
                        {
                            var jobs = GitLab.Job.Download(plugin, config);
                            if (jobs.Where(x => x.ArtifactsFile.HasValue).Count() == 0)
                            {
                                Log.Debug($"[{plugin.Name}] No jobs found, searching for releases", config.VerbouseOutput);
                                return this.DoAutoUpdate(plugin, force, AutoUpdateType.GITLAB);
                            }

                            var job = jobs.First(x => x.ArtifactsFile.HasValue);
                            if (!force && "Dev: " + job.Commit.ShortId == fileVersion)
                            {
                                Log.Debug($"[{plugin.Name}] Up to date", config.VerbouseOutput);
                                return Action.NONE;
                            }

                            job.DownloadArtifacts(plugin, config);

                            newVersion = "Dev: " + job.Commit.ShortId;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"[{plugin.Name}] AutoUpdate Failed: {ex.Message}");
                            Log.Error(ex.StackTrace);
                            return Action.NONE;
                        }

                        break;
                    case AutoUpdateType.HTTP:
                        Log.Debug($"[{plugin.Name}] Checking for update using HTTP, chekcing for releases", config.VerbouseOutput);
                        using (WebClient client = new WebClient())
                        {
                            try
                            {
                                Manifest manifest = JsonConvert.DeserializeObject<Manifest>(client.DownloadString($"{config.Url}/manifest.json"));
                                if (!force && manifest.Version == $"{pluginVersion.Major}.{pluginVersion.Minor}.{pluginVersion.Build}")
                                {
                                    Log.Debug($"[{plugin.Name}] Up to date", config.VerbouseOutput);
                                    return Action.NONE;
                                }
                                else if (!force && manifest.Version == fileVersion)
                                {
                                    Log.Info($"[{plugin.Name}] Update already downloaded, waiting for server restart");
                                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                                    return Action.RESTART;
                                }

                                client.DownloadFile($"{config.Url}/{manifest.PluginName}", Path.Combine(Paths.Plugins, manifest.PluginName));
                                newVersion = manifest.Version;
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[{plugin.Name}] AutoUpdate Failed: {ex.Message}");
                                Log.Error(ex.StackTrace);
                                return Action.NONE;
                            }
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException("AutoUpdateType", $"Unknown AutoUpdateType ({config.Type})");
                }

                File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.txt"), newVersion);
                Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
                ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                Log.Info($"[{plugin.Name}] Update from {pluginVersion.Major}.{pluginVersion.Minor}.{pluginVersion.Build} to {newVersion} downloaded, server will restart next round");
                return Action.UPDATE_AND_RESTART;
            }
            catch (WebException ex)
            {
                Log.Error($"[{plugin.Name}] AutoUpdate thrown web exception, your config may be invalid:");
                Log.Error(ex);
                return Action.NONE;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[{plugin.Name}] AutoUpdate thrown exception:");
                Log.Error(ex);
                return Action.NONE;
            }
        }

        private bool ignoreRestartingRound = false;

        private void Server_RestartingRound()
        {
            if (this.ignoreRestartingRound)
                return;
            if (ServerStatic.StopNextRound != ServerStatic.NextRoundAction.DoNothing)
                return;

            MEC.Timing.CallDelayed(5f, () =>
            {
                if (this.DoAutoUpdates())
                {
                    this.ignoreRestartingRound = true;

                    // Due to CallDelayed this will not work
                    /*Server.Host.ReferenceHub.playerStats.RpcRoundrestart((float)GameCore.ConfigFile.ServerConfig.GetInt("full_restart_rejoin_time", 25), true);
                    IdleMode.PauseIdleMode = true;
                    MEC.Timing.CallDelayed(1, () => Server.Restart());*/
                }
            });
        }

        private void Server_WaitingForPlayers()
        {
            this.ignoreRestartingRound = false;
        }
    }
}
