// -----------------------------------------------------------------------
// <copyright file="AutoUpdater.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Mistaken.Updater.API;
using Mistaken.Updater.Config;
using Newtonsoft.Json;

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

            Exiled.Events.Handlers.Server.RestartingRound += this.Server_RestartingRound;
            this.DoAutoUpdates();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
        }

        internal static AutoUpdateConfig MyConfig { get; private set; }

        internal static AutoUpdater Instance { get; private set; }

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
                    if (this.DoAutoUpdate(plugin, true))
                        changed = true;
                }
                else
                {
                    Log.Debug($"[{plugin.Name}] {path} exist, checking version", config.VerbouseOutput);
                    if (this.DoAutoUpdate(plugin, false))
                        changed = true;
                }
            }

            return changed;
        }

        internal bool DoAutoUpdate(IPlugin<IAutoUpdatableConfig> plugin, bool force)
        {
            var config = new AutoUpdateConfig(plugin.Config.AutoUpdateConfig);
            Log.Debug($"[{plugin.Name}] Running AutoUpdate...", config.VerbouseOutput);
            if (string.IsNullOrWhiteSpace(config.Url) || config.Type == AutoUpdateType.DISABLED)
            {
                Log.Debug($"[{plugin.Name}] AutoUpdate is disabled", config.VerbouseOutput);
                return false;
            }

            Version pluginVersion = plugin.Version;

#warning Temporiary fix
            if (pluginVersion.Major >= 3)
            {
                Log.Warn($"[{plugin.Name}] Plugin version out of range");
                pluginVersion = plugin.Assembly.GetName().Version;
            }

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
                        return false;
                    }
                }
            }

            string newVersion;

            switch (config.Type)
            {
                case AutoUpdateType.DISABLED:
                    return false;
                case AutoUpdateType.GITHUB:
                    Log.Debug($"[{plugin.Name}] Checking for update using GITHUB, chekcing latest release", config.VerbouseOutput);
                    try
                    {
                        var release = GitHub.Release.DownloadLatest(plugin, config);

                        if (!force && release.Tag == $"{pluginVersion.Major}.{pluginVersion.Minor}.{pluginVersion.Build}")
                        {
                            Log.Debug($"[{plugin.Name}] Up to date", config.VerbouseOutput);
                            return false;
                        }

                        if (!force && release.Tag == fileVersion)
                        {
                            Log.Info($"[{plugin.Name}] Update already downloaded, waiting for server restart");
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                            return false;
                        }

                        foreach (var asset in release.Assets)
                            asset.DownloadAsset(plugin, config);

                        newVersion = release.Tag;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"AutoUpdate Failed: {ex.Message}");
                        Log.Error(ex.StackTrace);
                        return false;
                    }

                    break;

                case AutoUpdateType.GITHUB_DEVELOPMENT:
                    Log.Debug($"[{plugin.Name}] Checking for update using GITHUB, chekcing for artifacts", config.VerbouseOutput);
                    try
                    {
                        var artifacts = GitHub.Artifacts.Download(plugin, config);
                        if (artifacts.ArtifactsArray.Length == 0)
                        {
                            Log.Error($"[{plugin.Name}] No artifacts found");
                            return false;
                        }

                        var artifact = artifacts.ArtifactsArray.OrderByDescending(x => x.Id).First();

                        if (!force && "Dev: " + artifact.NodeId == fileVersion)
                        {
                            Log.Debug($"[{plugin.Name}] Up to date", config.VerbouseOutput);
                            return false;
                        }

                        artifact.Download(plugin, config);

                        newVersion = "Dev: " + artifact.NodeId;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[{plugin.Name}] AutoUpdate Failed: {ex.Message}");
                        Log.Error(ex.StackTrace);
                        return false;
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
                            return false;
                        }

                        var release = releases[0];
                        if (!force && release.Tag == $"{pluginVersion.Major}.{pluginVersion.Minor}.{pluginVersion.Build}")
                        {
                            Log.Debug($"[{plugin.Name}] Up to date", config.VerbouseOutput);
                            return false;
                        }
                        else
                            Log.Debug($"[{plugin.Name}] Not up to date, Current: {pluginVersion.Major}.{pluginVersion.Minor}.{pluginVersion.Build}, Newest: {release.Tag}", config.VerbouseOutput);

                        if (!force && release.Tag == fileVersion)
                        {
                            Log.Info($"[{plugin.Name}] Update already downloaded, waiting for server restart");
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                            return false;
                        }

                        foreach (var link in release.Assets.Links)
                            link.Download(plugin, config);

                        newVersion = release.Tag;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[{plugin.Name}] AutoUpdate Failed: {ex.Message}");
                        Log.Error(ex.StackTrace);
                        return false;
                    }

                    break;

                case AutoUpdateType.GITLAB_DEVELOPMENT:
                    Log.Debug($"[{plugin.Name}] Checking for update using GITLAB, chekcing for artifacts", config.VerbouseOutput);
                    try
                    {
                        var jobs = GitLab.Job.Download(plugin, config);
                        if (jobs.Length == 0)
                        {
                            Log.Error($"[{plugin.Name}] AutoUpdate Failed: No jobs found");
                            return false;
                        }

                        var job = jobs.First(x => x.ArtifactsFile.HasValue);
                        if (!force && "Dev: " + job.Commit.ShortId == fileVersion)
                        {
                            Log.Debug($"[{plugin.Name}] Up to date", config.VerbouseOutput);
                            return false;
                        }

                        job.DownloadArtifacts(plugin, config);

                        newVersion = "Dev: " + job.Commit.ShortId;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"AutoUpdate Failed: {ex.Message}");
                        Log.Error(ex.StackTrace);
                        return false;
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
                                return false;
                            }
                            else if (!force && manifest.Version == fileVersion)
                            {
                                Log.Info($"[{plugin.Name}] Update already downloaded, waiting for server restart");
                                ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                                return false;
                            }

                            client.DownloadFile($"{config.Url}/{manifest.PluginName}", Path.Combine(Paths.Plugins, manifest.PluginName));
                            newVersion = manifest.Version;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message);
                            Log.Error(ex.StackTrace);
                            return false;
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
            return true;
        }

        private void Server_RestartingRound()
        {
            this.DoAutoUpdates();
        }
    }
}
