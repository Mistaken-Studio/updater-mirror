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
        public override Version RequiredExiledVersion => new Version(2, 11, 0);

        /// <inheritdoc/>
        public override string Prefix => "MUPDATER";

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

            var path = Path.Combine(Paths.Plugins, "AutoUpdater");
            if (!Directory.Exists(path))
            {
                Log.Debug($"{path} doesn't exist, creating...", this.Config.AutoUpdateConfig.VerbouseOutput);
                Directory.CreateDirectory(path);
            }
            else
                Log.Debug($"{path} exist", this.Config.AutoUpdateConfig.VerbouseOutput);

            Exiled.Events.Handlers.Server.RestartingRound += this.Server_RestartingRound;
            this.DoAutoUpdates();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
        }

        internal static AutoUpdater Instance { get; private set; }

        internal bool DoAutoUpdates()
        {
            bool changed = false;
            foreach (var plugin in Exiled.Loader.Loader.Plugins.Where(x => x.Config is IAutoUpdatableConfig).Select(x => x as IPlugin<IAutoUpdatableConfig>))
            {
                string path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.txt");
                if (!File.Exists(path))
                {
                    Log.Debug($"[{plugin.Name}]{path} doesn't exist, forcing auto update", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    if (this.DoAutoUpdate(plugin, true).GetAwaiter().GetResult())
                        changed = true;
                }
                else
                {
                    Log.Debug($"[{plugin.Name}] {path} exist, checking version", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    if (this.DoAutoUpdate(plugin, false).GetAwaiter().GetResult())
                        changed = true;
                }
            }

            return changed;
        }

        internal async Task<bool> DoAutoUpdate(IPlugin<IAutoUpdatableConfig> plugin, bool force)
        {
            Log.Debug($"[{plugin.Name}] Running AutoUpdate...", plugin.Config.AutoUpdateConfig.VerbouseOutput);
            if (string.IsNullOrWhiteSpace(plugin.Config.AutoUpdateConfig.Url))
            {
                Log.Debug("AutoUpdate is disabled", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                return false;
            }

            if (!force)
            {
                if (File.ReadAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.txt")) != plugin.Version.ToString())
                {
                    Log.Info($"[{plugin.Name}] Update is downloaded, server will restart next round");
                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                    return false;
                }
            }

            string newVersion;

            switch (plugin.Config.AutoUpdateConfig.Type)
            {
                case AutoUpdateType.GITHUB:
                    Log.Debug($"[{plugin.Name}] Checking for update using GITHUB, chekcing latest release", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    try
                    {
                        var release = await GitHub.Release.DownloadLatest(plugin);

                        if (!force && release.Tag == plugin.Version.ToString())
                        {
                            Log.Debug($"[{plugin.Name}] Up to date", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                            return false;
                        }

                        foreach (var asset in release.Assets)
                            await asset.DownloadAsset(plugin);

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
                    Log.Debug($"[{plugin.Name}] Checking for update using GITHUB, chekcing for artifacts", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    try
                    {
                        var artifacts = await GitHub.Artifacts.Download(plugin);
                        if (artifacts.ArtifactsArray.Length == 0)
                        {
                            Log.Error($"[{plugin.Name}] No artifacts found");
                            return false;
                        }

                        var artifact = artifacts.ArtifactsArray.OrderByDescending(x => x.Id).First();
                        if (!force && artifact.NodeId == plugin.Version.ToString())
                        {
                            Log.Debug($"[{plugin.Name}] Up to date", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                            return false;
                        }

                        await artifact.Download(plugin);

                        newVersion = artifact.NodeId;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[{plugin.Name}] AutoUpdate Failed: {ex.Message}");
                        Log.Error(ex.StackTrace);
                        return false;
                    }

                    break;

                case AutoUpdateType.GITLAB:
                    Log.Debug($"[{plugin.Name}] Checking for update using GITLAB, chekcing for releases", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    try
                    {
                        var releases = await GitLab.Release.Download(plugin);
                        if (releases.Length == 0)
                        {
                            Log.Error($"[{plugin.Name}] AutoUpdate Failed: No releases found");
                            return false;
                        }

                        var release = releases[0];
                        if (!force && release.Tag == plugin.Version.ToString())
                        {
                            Log.Debug($"[{plugin.Name}] Up to date", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                            return false;
                        }

                        foreach (var link in release.Assets.Links)
                            await link.Download(plugin);

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
                    Log.Debug($"[{plugin.Name}] Checking for update using GITLAB, chekcing for artifacts", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    try
                    {
                        var jobs = await GitLab.Job.Download(plugin);
                        if (jobs.Length == 0)
                        {
                            Log.Error($"[{plugin.Name}] AutoUpdate Failed: No jobs found");
                            return false;
                        }

                        var job = jobs[0];
                        if (!force && job.Commit.ShortId == plugin.Version.ToString())
                        {
                            Log.Debug($"[{plugin.Name}] Up to date", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                            return false;
                        }

                        await job.DownloadArtifacts(plugin);

                        newVersion = job.Commit.ShortId;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"AutoUpdate Failed: {ex.Message}");
                        Log.Error(ex.StackTrace);
                        return false;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException("AutoUpdateType", $"Unknown AutoUpdateType ({plugin.Config.AutoUpdateConfig.Type})");
            }

            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.txt"), newVersion);
            Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
            return true;
        }

        private void Server_RestartingRound()
        {
            this.DoAutoUpdates();
        }
    }
}
