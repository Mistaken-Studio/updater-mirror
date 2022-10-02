// -----------------------------------------------------------------------
// <copyright file="AutoUpdater.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Loader;
using JetBrains.Annotations;
using Mistaken.Updater.API;
using Mistaken.Updater.API.Abstract;
using Mistaken.Updater.API.Config;
using Mistaken.Updater.API.Implementations;
using Mistaken.Updater.Config;
using Mistaken.Updater.Internal;
using Newtonsoft.Json;
using RoundRestarting;

#pragma warning disable CS0618

// ReSharper disable MemberCanBePrivate.Global
namespace Mistaken.Updater
{
    /// <inheritdoc cref="IPlugin{TConfig}"/>
    public static class AutoUpdater
    {
        /// <summary>
        /// Gets Plugin Manifest.
        /// </summary>
        /// <param name="plugin">Plugin.</param>
        /// <returns>Plugin Manifest.</returns>
        [CanBeNull]
        public static PluginManifest GetPluginManifest(IPlugin<IConfig> plugin)
        {
            var name = plugin.GetPluginName();

            return ServerManifest.Plugins.TryGetValue(name, out var manifest) ? manifest : null;
        }

        internal static bool VerboseOutput => AutoUpdaterPlugin.Instance.Config.VerboseOutput;

        internal static void Initialize()
        {
            var path = Path.Combine(Paths.Plugins, "AutoUpdater");
            if (!Directory.Exists(path))
            {
                Log.Debug($"{path} doesn't exist, creating...", VerboseOutput);
                Directory.CreateDirectory(path);
            }
            else
                Log.Debug($"{path} exist", VerboseOutput);

            Task.Run(() =>
            {
                if (!DoAutoUpdates())
                    return;

                IdleMode.PauseIdleMode = true;
                Task.Delay(5000);
                RestartServer();
            });
        }

        internal static bool DoAutoUpdates()
        {
            try
            {
                if (LoadServerManifest())
                    return true;

                HandleBackwardsCompatibility();
                UpdateManifest();

                return UpdateBasedOnManifest();
            }
            catch (Exception ex)
            {
                Log.Error("Exception when handling auto update!");
                Log.Error(ex);
                return false;
            }
        }

        internal enum Action : byte
        {
            NONE,
            RESTART,
            UPDATE_AND_RESTART,
        }

        private static readonly GitHub GitHub = new ();
        private static readonly GitLab GitLab = new ();

        private static ServerManifest ServerManifest { get; set; }

        private static Action DoAutoUpdate(PluginManifest pluginManifest, bool force, string pluginVersion, SourceType forcedConfig = SourceType.DISABLED, bool forceStable = false)
        {
            try
            {
                Log.Debug($"[{pluginManifest.PluginName}] Running AutoUpdate...", VerboseOutput);
                if (string.IsNullOrWhiteSpace(pluginManifest.UpdateUrl) || pluginManifest.SourceType == SourceType.DISABLED)
                {
                    Log.Debug($"[{pluginManifest.PluginName}] AutoUpdate is disabled", VerboseOutput);
                    return Action.NONE;
                }

                if (forcedConfig == SourceType.DISABLED)
                    forcedConfig = pluginManifest.SourceType;

                switch (forcedConfig)
                {
                    case SourceType.DISABLED:
                        return Action.NONE;

                    case SourceType.GITLAB:
                    case SourceType.GITHUB:
                        {
                            Log.Debug($"[{pluginManifest.PluginName}] Checking for update using {forcedConfig}, Dev: {!forceStable && pluginManifest.Development}", VerboseOutput);
                            var implementation = GetImplementation(forcedConfig);
                            Action? result;
                            if (!forceStable && pluginManifest.Development)
                            {
                                result = UpdateDevelopment(
                                    implementation,
                                    pluginManifest,
                                    pluginVersion,
                                    force);
                            }
                            else
                            {
                                result = UpdateStable(
                                    implementation,
                                    pluginManifest,
                                    pluginVersion,
                                    force);
                            }

                            Log.Debug($"[{pluginManifest.PluginName}] Checked for update using {forcedConfig}, Result: {result?.ToString() ?? "CONTINUE"}", VerboseOutput);
                            if (result.HasValue)
                                return result.Value;
                            break;
                        }

                    case SourceType.HTTP:
                        {
                            Log.Debug($"[{pluginManifest.PluginName}] Checking for update using HTTP, checking for releases", VerboseOutput);
                            using var client = new WebClient();
                            try
                            {
                                var manifest =
                                    JsonConvert.DeserializeObject<Manifest>(
                                        client.DownloadString($"{pluginManifest.UpdateUrl}/manifest.json"));
                                if (!force && manifest.Version == pluginVersion)
                                {
                                    Log.Debug($"[{pluginManifest.PluginName}] Up to date", VerboseOutput);
                                    return Action.NONE;
                                }

                                if (!force && manifest.Version == pluginManifest.CurrentVersion)
                                {
                                    Log.Info(
                                        $"[{pluginManifest.PluginName}] Update already downloaded, waiting for server restart");
                                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                                    return Action.RESTART;
                                }

                                client.DownloadFile(
                                    $"{pluginManifest.UpdateUrl}/{manifest.PluginName}",
                                    Path.Combine(Paths.Plugins, manifest.PluginName));

                                pluginManifest.UpdatePlugin(manifest);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[{pluginManifest.PluginName}] AutoUpdate Failed: {ex.Message}");
                                Log.Error(ex.StackTrace);
                                return Action.NONE;
                            }

                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(SourceType),
                            $"Unknown SourceType ({pluginManifest.UpdateUrl})");
                }

                ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                Log.Info($"[{pluginManifest.PluginName}] Update from {pluginVersion} to {pluginManifest.CurrentVersion} downloaded, server will restart next round");
                return Action.UPDATE_AND_RESTART;
            }
            catch (WebException ex)
            {
                Log.Error($"[{pluginManifest.PluginName}] AutoUpdate thrown web exception, your config may be invalid:");
                Log.Error(ex);
                return Action.NONE;
            }
            catch (Exception ex)
            {
                Log.Error($"[{pluginManifest.PluginName}] AutoUpdate thrown exception:");
                Log.Error(ex);
                return Action.NONE;
            }
        }

        private static void RestartServer()
        {
            IdleMode.PauseIdleMode = true;
            Mirror.NetworkServer.SendToAll(new RoundRestartMessage(
                RoundRestartType.FullRestart,
                GameCore.ConfigFile.ServerConfig.GetInt("full_restart_rejoin_time", 25),
                0,
                true,
                true));
            MEC.Timing.CallDelayed(1, Server.Restart);
        }

        private static PluginManifest CreatePluginManifest(IPlugin<IConfig> plugin)
        {
            if (plugin is not IAutoUpdateablePlugin autoUpdateablePlugin)
                throw new ArgumentException($"Expected {nameof(IAutoUpdateablePlugin)}", nameof(plugin));

            Log.Debug($"[{plugin.GetPluginName()}] Creating Plugin Manifest", VerboseOutput);
            var tor = new PluginManifest(autoUpdateablePlugin);

            ServerManifest.Plugins.Add(tor.PluginName, tor);

            return tor;
        }

        [Obsolete("Only for Backwards Compatibility")]
        private static PluginManifest CreatePluginManifestBackwardsCompatible(IPlugin<IAutoUpdatableConfig> plugin)
        {
            Log.Debug($"[{plugin.GetPluginName()}] Creating Plugin Manifest with Backwards Compatibility", VerboseOutput);
            plugin.Config.AutoUpdateConfig ??= new Dictionary<string, string>()
            {
                { "Url", null },
                { "Type", null },
            };

            var tor = new PluginManifest
            {
                PluginName = plugin.GetPluginName(),
                UpdateUrl = plugin.Config.AutoUpdateConfig.TryGetValue("Url", out var url) ? url : null,
                SourceType = plugin.Config.AutoUpdateConfig
                    .TryGetValue("Type", out var type)
                    ? Enum.TryParse<SourceType>(type.Split('_')[0], out var sourceType)
                        ? sourceType
                        : SourceType.DISABLED
                    : SourceType.DISABLED,
            };

            ServerManifest.Plugins.Add(tor.PluginName, tor);

            return tor;
        }

        private static void SaveServerManifest()
        {
            Log.Debug("Saving Server Manifest...", VerboseOutput);
            var path = Path.Combine(Paths.Plugins, "AutoUpdater");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, "manifest.json");

            ServerManifest.UnApplyTokens();
            File.WriteAllText(path, JsonConvert.SerializeObject(ServerManifest, Formatting.Indented));
            ServerManifest.ApplyTokens();
            Log.Debug("Saved Server Manifest", VerboseOutput);
        }

        private static bool LoadServerManifest()
        {
            Log.Debug("Loading Server Manifest...", VerboseOutput);
            try
            {
                var path = Path.Combine(Paths.Plugins, "AutoUpdater");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                path = Path.Combine(path, "manifest.json");

                if (!File.Exists(path))
                {
                    ServerManifest = new ServerManifest();

                    SaveServerManifest();

                    return false;
                }

                var newManifest = JsonConvert.DeserializeObject<ServerManifest>(File.ReadAllText(path));
                newManifest.ApplyTokens();

                if (ServerManifest != null && ServerManifest.LastUpdateCheck != newManifest.LastUpdateCheck)
                {
                    Log.Info("Manifest had changed since it was read, requesting server restart ...");
                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                    ServerManifest = newManifest;
                    return true;
                }

                ServerManifest = newManifest;

                Log.Debug("Loaded Server Manifest", VerboseOutput);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("Exception when loading Server Manifest");
                Log.Error(ex);

                throw;
            }
        }

        private static IImplementation GetImplementation(SourceType type)
        {
            return type switch
            {
                SourceType.GITHUB => GitHub,
                SourceType.GITLAB => GitLab,
                SourceType.DISABLED => throw new NotImplementedException(),
                SourceType.HTTP => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
        }

        private static Action? UpdateStable(
            IImplementation implementation,
            PluginManifest pluginManifest,
            string pluginVersion,
            bool force)
        {
            try
            {
                Log.Debug($"[{pluginManifest.PluginName}] Checking for update", VerboseOutput);
                var release = Internal.Utils.DownloadLatest(implementation, pluginManifest);

                if (!force && release.Tag == pluginVersion)
                {
                    Log.Debug($"[{pluginManifest.PluginName}] Up to date", VerboseOutput);
                    return Action.NONE;
                }

                Log.Debug(
                    $"[{pluginManifest.PluginName}] Not up to date, Current: {pluginVersion}, Newest: {release.Tag}",
                    VerboseOutput);

                if (!force && release.Tag == pluginManifest.CurrentVersion)
                {
                    Log.Info($"[{pluginManifest.PluginName}] Update already downloaded, waiting for server restart");
                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                    return Action.RESTART;
                }

                foreach (var asset in release.Assets)
                    Internal.Utils.DownloadAsset(implementation, asset, pluginManifest);

                pluginManifest.UpdatePlugin(release);

                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"[{pluginManifest.PluginName}] AutoUpdate Failed: {ex.Message}");
                Log.Error(ex.StackTrace);
                return Action.NONE;
            }
        }

        private static void UpdateManifest()
        {
            var changed = false;
            foreach (var plugin in Loader.Plugins.Where(x => x is IAutoUpdateablePlugin))
            {
                var manifest = GetPluginManifest(plugin);

                if (manifest is not null)
                    continue;

                manifest = CreatePluginManifest(plugin);
                Log.Info($"[{manifest.PluginName}] Detected new plugin, adding to manifest");
                changed = true;
            }

            if (!changed)
                return;

            ServerManifest.LastUpdateCheck = DateTime.Now;
            SaveServerManifest();
        }

        [Obsolete("Only for Backwards Compatibility")]
        private static void HandleBackwardsCompatibility()
        {
            var changed = false;
            foreach (var plugin in Loader.Plugins.OfType<IPlugin<IAutoUpdatableConfig>>())
            {
                var manifest = GetPluginManifest(plugin);

                if (manifest is not null)
                    continue;

                manifest = CreatePluginManifestBackwardsCompatible(plugin);
                Log.Info($"[{manifest.PluginName}] Detected new plugin using Backwards Compatibility, adding to manifest");
                changed = true;
            }

            if (!changed)
                return;

            ServerManifest.LastUpdateCheck = DateTime.Now;
            SaveServerManifest();
        }

        private static bool UpdateBasedOnManifest()
        {
            var changed = false;
            foreach (var manifest in ServerManifest.Plugins.Values)
            {
                var plugin = Loader.Plugins.FirstOrDefault(x =>
                    x.GetPluginName() == manifest.PluginName);
                var pluginVersion = plugin?.Version;
                string stringPluginVersion;
                if (pluginVersion is null)
                    stringPluginVersion = manifest.CurrentVersion;
                else if (pluginVersion.Major == 1 && pluginVersion.Minor == 0 && pluginVersion.Build == 0)
                {
                    stringPluginVersion = manifest.CurrentVersion;
                    manifest.CurrentBuildId = $"{manifest.CurrentVersion}-manual-00000000";
                }
                else if (pluginVersion.Major >= 4)
                    stringPluginVersion = plugin.Assembly.GetName().Version.ToString(3);
                else
                    stringPluginVersion = pluginVersion.ToString(3);

                if (DoAutoUpdate(manifest, pluginVersion is null, stringPluginVersion) != Action.NONE)
                    changed = true;
            }

            if (!changed)
                return false;

            ServerManifest.LastUpdateCheck = DateTime.Now;
            SaveServerManifest();

            return true;
        }

        private static Action? UpdateDevelopment(
            IImplementation implementation,
            PluginManifest pluginManifest,
            string pluginVersion,
            bool force)
        {
            try
            {
                var res = implementation.DownloadArtifact(pluginManifest, force);

                return res == Action.UPDATE_AND_RESTART ? DoAutoUpdate(pluginManifest, force, pluginVersion, forceStable: true) : res;
            }
            catch (WebException ex)
            {
                Log.Error($"[{pluginManifest.PluginName}] AutoUpdate Failed: WebException");
                Log.Error(ex.Status + ": " + ex.Response);
                Log.Error(ex);

                return Action.NONE;
            }
            catch (Exception ex)
            {
                Log.Error($"[{pluginManifest.PluginName}] AutoUpdate Failed: Exception");
                Log.Error(ex);

                return Action.NONE;
            }
        }
    }
}
