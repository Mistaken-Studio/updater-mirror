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
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Newtonsoft.Json;

namespace Mistaken.API.Internal
{
    /// <inheritdoc/>
    public class AutoUpdater : Plugin<AutoUpdaterPluginConfig>
    {
        /// <inheritdoc/>
        public override string Name => "MistakenUpdater";

        /// <inheritdoc/>
        public override string Author => "Mistaken Devs";

        /// <inheritdoc/>
        public override PluginPriority Priority => PluginPriority.Last;

        /// <inheritdoc/>
        public override Version RequiredExiledVersion => new Version(2, 11, 0);

        /// <inheritdoc/>
        public override string Prefix => "MUPDATE";

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

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
                var path = Path.Combine(Paths.Plugins, "AutoUpdater");
                if (!Directory.Exists(path))
                {
                    Log.Debug($"[{plugin.Name}]{path} doesn't exist, creating...", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    Directory.CreateDirectory(path);
                }
                else
                    Log.Debug($"[{plugin.Name}]{path} exist", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                path = Path.Combine(path, $"{plugin.Author}.{plugin.Name}.txt");
                if (!File.Exists(path))
                {
                    Log.Debug($"[{plugin.Name}]{path} doesn't exist, forcing auto update", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    if (this.DoAutoUpdate(plugin, true))
                        changed = true;
                }
                else
                {
                    Log.Debug($"[{plugin.Name}] {path} exist, checking version", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    if (this.DoAutoUpdate(plugin, false))
                        changed = true;
                }
            }

            return changed;
        }

        internal bool DoAutoUpdate(IPlugin<IAutoUpdatableConfig> plugin, bool force)
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

            switch (plugin.Config.AutoUpdateConfig.Type)
            {
                case AutoUpdateType.GITHUB:
                    Log.Debug($"[{plugin.Name}] Checking for update using GITHUB, chekcing latest release", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    using (var client = new WebClient())
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(plugin.Config.AutoUpdateConfig.Token))
                                client.Headers.Add($"Authorization: token {plugin.Config.AutoUpdateConfig.Token}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "PluginUpdater");
                            string releaseUrl = plugin.Config.AutoUpdateConfig.Url + "/releases/latest";
                            var rawResult = client.DownloadString(releaseUrl);
                            if (rawResult == string.Empty)
                            {
                                Log.Error($"[{plugin.Name}] AutoUpdate Failed: AutoUpdate URL returned empty page");
                                return false;
                            }

                            var decoded = Newtonsoft.Json.JsonConvert.DeserializeObject<GitHub.Release>(rawResult);
                            var result = decoded;
                            if (!force && result.Tag == plugin.Version.ToString())
                            {
                                Log.Debug($"[{plugin.Name}] Up to date", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                                return false;
                            }

                            foreach (var link in result.Assets)
                            {
                                Log.Debug($"[{plugin.Name}] Downloading |" + link.Url, plugin.Config.AutoUpdateConfig.VerbouseOutput);
                                using (var client2 = new WebClient())
                                {
                                    client2.Headers.Add($"Authorization: token {plugin.Config.AutoUpdateConfig.Token}");
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

                            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.txt"), result.Tag);
                            Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                            return true;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"AutoUpdate Failed: {ex.Message}");
                            Log.Error(ex.StackTrace);
                            return false;
                        }
                    }

                case AutoUpdateType.GITHUB_DEVELOPMENT:
                    Log.Debug($"[{plugin.Name}] Checking for update using GITHUB, chekcing for artifacts", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    using (var client = new WebClient())
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(plugin.Config.AutoUpdateConfig.Token))
                                client.Headers.Add($"Authorization: token {plugin.Config.AutoUpdateConfig.Token}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                            string artifactsUrl = plugin.Config.AutoUpdateConfig.Url + "/actions/artifacts";
                            var rawResult = client.DownloadString(artifactsUrl);
                            if (rawResult == string.Empty)
                            {
                                Log.Error($"[{plugin.Name}] AutoUpdate Failed: {artifactsUrl} returned empty page");
                                return false;
                            }

                            var artifacts = Newtonsoft.Json.JsonConvert.DeserializeObject<GitHub.Artifacts>(rawResult);
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

                            Log.Debug($"[{plugin.Name}] Downloading |" + artifact.DownloadUrl, plugin.Config.AutoUpdateConfig.VerbouseOutput);
                            using (var client2 = new WebClient())
                            {
                                client2.Headers.Add($"Authorization: token {plugin.Config.AutoUpdateConfig.Token}");
                                client2.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                                client2.Headers.Add(HttpRequestHeader.Accept, "application/octet-stream");
                                string path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.artifacts.zip");
                                client2.DownloadFile(artifact.DownloadUrl, path);

                                string extractedPath = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.artifacts.extracted");
                                ZipFile.ExtractToDirectory(path, extractedPath);
                                while (true)
                                {
                                    Log.Debug($"[{plugin.Name}] Scanning {extractedPath} for files", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                                    var files = Directory.GetFiles(extractedPath, "*.dll");
                                    if (files.Length != 0)
                                    {
                                        Log.Debug($"[{plugin.Name}] Found files in {extractedPath}", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                                        foreach (var file in files)
                                        {
                                            string name = Path.GetFileName(file);
                                            string targetPath;
                                            if (name.StartsWith("Dependencie-"))
                                            {
                                                name = name.Substring(12);
                                                targetPath = Path.Combine(Paths.Dependencies, name);
                                            }
                                            else
                                                targetPath = Path.Combine(Paths.Plugins, name);

                                            Log.Debug($"[{plugin.Name}] Copping {file} to {targetPath}", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                                            File.Copy(file, targetPath, true);
                                        }

                                        break;
                                    }

                                    var directories = Directory.GetDirectories(extractedPath);
                                    if (directories.Length == 0)
                                    {
                                        Log.Error($"[{plugin.Name}] Artifact is empty");
                                        return false;
                                    }

                                    extractedPath = directories[0];
                                }
                            }

                            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.txt"), artifact.NodeId);
                            Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                            return true;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"[{plugin.Name}] AutoUpdate Failed: {ex.Message}");
                            Log.Error(ex.StackTrace);
                            return false;
                        }
                    }

                case AutoUpdateType.GITLAB:
                    Log.Debug($"[{plugin.Name}] Checking for update using GITLAB, chekcing for releases", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    using (var client = new WebClient())
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(plugin.Config.AutoUpdateConfig.Token))
                                client.Headers.Add($"PRIVATE-TOKEN: {plugin.Config.AutoUpdateConfig.Token}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                            string releasesLink = plugin.Config.AutoUpdateConfig.Url + "/releases";
                            Log.Debug($"[{plugin.Name}] Downloading release list from {releasesLink}", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                            var rawResult = client.DownloadString(releasesLink);
                            if (rawResult == string.Empty)
                            {
                                Log.Error($"[{plugin.Name}] AutoUpdate Failed: {releasesLink} returned empty page");
                                return false;
                            }

                            var releases = Newtonsoft.Json.JsonConvert.DeserializeObject<GitLab.Release[]>(rawResult);
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
                            {
                                Log.Debug($"[{plugin.Name}] Downloading |" + link.Url, plugin.Config.AutoUpdateConfig.VerbouseOutput);
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

                            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.txt"), release.Tag);
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                            return true;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"[{plugin.Name}] AutoUpdate Failed: {ex.Message}");
                            Log.Error(ex.StackTrace);
                            return false;
                        }
                    }

                case AutoUpdateType.GITLAB_DEVELOPMENT:
                    Log.Debug($"[{plugin.Name}] Checking for update using GITLAB, chekcing for artifacts", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    using (var client = new WebClient())
                    {
                        try
                        {
                            string jobsUrl = plugin.Config.AutoUpdateConfig.Url + "/jobs?scope=success";
                            if (!string.IsNullOrWhiteSpace(plugin.Config.AutoUpdateConfig.Token))
                                client.Headers.Add($"PRIVATE-TOKEN: {plugin.Config.AutoUpdateConfig.Token}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                            Log.Debug($"[{plugin.Name}] Downloading job list from {jobsUrl}", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                            var rawResult = client.DownloadString(jobsUrl);
                            if (rawResult == string.Empty)
                            {
                                Log.Error($"[{plugin.Name}] AutoUpdate Failed: {jobsUrl} returned empty page");
                                return false;
                            }

                            var jobs = Newtonsoft.Json.JsonConvert.DeserializeObject<GitLab.Job[]>(rawResult);
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

                            string artifactUrl = plugin.Config.AutoUpdateConfig.Url + $"/jobs/{job.Id}/artifacts";
                            Log.Debug($"[{plugin.Name}] Downloading |" + artifactUrl, plugin.Config.AutoUpdateConfig.VerbouseOutput);
                            string path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.artifacts.zip");
                            client.DownloadFile(artifactUrl, path);
                            string extractedPath = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.artifacts.extracted");
                            ZipFile.ExtractToDirectory(path, extractedPath);
                            while (true)
                            {
                                Log.Debug($"[{plugin.Name}] Scanning {extractedPath} for files", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                                var files = Directory.GetFiles(extractedPath, "*.dll");
                                if (files.Length != 0)
                                {
                                    Log.Debug($"[{plugin.Name}] Found files in {extractedPath}", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                                    foreach (var file in files)
                                    {
                                        string name = Path.GetFileName(file);
                                        string targetPath;
                                        if (name.StartsWith("Dependencie-"))
                                        {
                                            name = name.Substring(12);
                                            targetPath = Path.Combine(Paths.Dependencies, name);
                                        }
                                        else
                                            targetPath = Path.Combine(Paths.Plugins, name);

                                        Log.Debug($"[{plugin.Name}] Copping {file} to {targetPath}", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                                        File.Copy(file, targetPath, true);
                                    }

                                    break;
                                }

                                var directories = Directory.GetDirectories(extractedPath);
                                if (directories.Length == 0)
                                {
                                    Log.Error($"[{plugin.Name}] Artifact is empty");
                                    return false;
                                }

                                extractedPath = directories[0];
                            }

                            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.txt"), job.Commit.ShortId);
                            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                            return true;
                        }
                        catch (System.Exception ex)
                        {
                            Log.Error($"AutoUpdate Failed: {ex.Message}");
                            Log.Error(ex.StackTrace);
                            return false;
                        }
                    }

                default:
                    throw new ArgumentOutOfRangeException("AutoUpdateType", $"Unknown AutoUpdateType ({plugin.Config.AutoUpdateConfig.Type})");
            }
        }

        private void Server_RestartingRound()
        {
            this.DoAutoUpdates();
        }
    }
}
