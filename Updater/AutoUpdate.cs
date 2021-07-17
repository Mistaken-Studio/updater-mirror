// -----------------------------------------------------------------------
// <copyright file="AutoUpdate.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Newtonsoft.Json;

namespace Mistaken.API
{
    /// <summary>
    /// Class used to update plugins.
    /// </summary>
    public class AutoUpdate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoUpdate"/> class.
        /// </summary>
        /// <param name="plugin">Plugin with auto update config.</param>
        /// <param name="verbouseOutput">If <see langword="true"/> then debug will be displayed in console.</param>
        public AutoUpdate(IPlugin<IAutoUpdatableConfig> plugin, bool verbouseOutput = false)
        {
            this.verbouseOutput = verbouseOutput;
            this.url = plugin.Config.AutoUpdateUrl;
            this.type = plugin.Config.AutoUpdateType;
            this.token = plugin.Config.AutoUpdateToken;
            this.Plugin = plugin;

            Instances.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoUpdate"/> class.
        /// </summary>
        /// <param name="plugin">Plugin.</param>
        /// <param name="type">Auto Update Type.</param>
        /// <param name="url">Url used for requesting releases.</param>
        /// <param name="token">Token used for authorization, if <see langword="null"/> then won't be included in request.</param>
        /// <param name="verbouseOutput">If <see langword="true"/> then debug will be displayed in console.</param>
        public AutoUpdate(IPlugin<IConfig> plugin, AutoUpdateType type, string url, string token = null, bool verbouseOutput = false)
        {
            this.verbouseOutput = verbouseOutput;
            this.url = url;
            this.type = type;
            this.token = token;
            this.Plugin = plugin;

            Instances.Add(this);
        }

        /// <summary>
        /// Gets current plugin version.
        /// </summary>
        public string CurrentVersion { get; private set; }

        /// <summary>
        /// Enables <see cref="AutoUpdate"/>.
        /// </summary>
        public void Enable()
        {
            var path = Path.Combine(Paths.Plugins, "AutoUpdater");
            if (!Directory.Exists(path))
            {
                Log.Debug($"{path} doesn't exist, creating...", this.verbouseOutput);
                Directory.CreateDirectory(path);
            }
            else
                Log.Debug($"{path} exist", this.verbouseOutput);
            path = Path.Combine(path, $"{this.Plugin.Author}.{this.Plugin.Name}.txt");
            if (!File.Exists(path))
            {
                Log.Debug($"{path} doesn't exist, forcing auto update", this.verbouseOutput);
                this.DoAutoUpdate(true);
            }
            else
            {
                Log.Debug($"{path} exist, checking version", this.verbouseOutput);
                this.CurrentVersion = File.ReadAllText(path);
                Exiled.Events.Handlers.Server.RestartingRound += this.Server_RestartingRound;
                this.DoAutoUpdate(false);
            }
        }

        /// <summary>
        /// Disables <see cref="AutoUpdate"/>.
        /// </summary>
        public void Disable()
        {
            Exiled.Events.Handlers.Server.RestartingRound -= this.Server_RestartingRound;
        }

        internal static readonly List<AutoUpdate> Instances = new List<AutoUpdate>();

        internal IPlugin<IConfig> Plugin { get; }

        internal bool DoAutoUpdate(bool force)
        {
            Log.Debug("Running AutoUpdate...", this.verbouseOutput);
            if (string.IsNullOrWhiteSpace(this.url))
            {
                Log.Debug("AutoUpdate is disabled", this.verbouseOutput);
                return false;
            }

            if (!force)
            {
                if (File.ReadAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Plugin.Author}.{this.Plugin.Name}.txt")) != this.CurrentVersion)
                {
                    Log.Info("Update is downloaded, server will restart next round");
                    ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
                    return false;
                }
            }

            switch (this.type)
            {
                case AutoUpdateType.GITHUB:
                    Log.Debug($"Checking for update using GITHUB, chekcing latest release", this.verbouseOutput);
                    using (var client = new WebClient())
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(this.token))
                                client.Headers.Add($"Authorization: token {this.token}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "PluginUpdater");
                            string releaseUrl = this.url + "/releases/latest";
                            var rawResult = client.DownloadString(releaseUrl);
                            if (rawResult == string.Empty)
                            {
                                Log.Error("AutoUpdate Failed: AutoUpdate URL returned empty page");
                                return false;
                            }

                            var decoded = Newtonsoft.Json.JsonConvert.DeserializeObject<GitHub.Release>(rawResult);
                            var result = decoded;
                            if (!force && result.Tag == this.CurrentVersion)
                            {
                                Log.Debug("Up to date", this.verbouseOutput);
                                return false;
                            }

                            foreach (var link in result.Assets)
                            {
                                Log.Debug("Downloading |" + link.Url, this.verbouseOutput);
                                using (var client2 = new WebClient())
                                {
                                    client2.Headers.Add($"Authorization: token {this.token}");
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

                            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Plugin.Author}.{this.Plugin.Name}.txt"), result.Tag);
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
                    Log.Debug($"Checking for update using GITHUB, chekcing for artifacts", this.verbouseOutput);
                    using (var client = new WebClient())
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(this.token))
                                client.Headers.Add($"Authorization: token {this.token}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                            string artifactsUrl = this.url + "/actions/artifacts";
                            var rawResult = client.DownloadString(artifactsUrl);
                            if (rawResult == string.Empty)
                            {
                                Log.Error($"AutoUpdate Failed: {artifactsUrl} returned empty page");
                                return false;
                            }

                            var artifacts = Newtonsoft.Json.JsonConvert.DeserializeObject<GitHub.Artifacts>(rawResult);
                            if (artifacts.ArtifactsArray.Length == 0)
                            {
                                Log.Error("No artifacts found");
                                return false;
                            }

                            var artifact = artifacts.ArtifactsArray.OrderByDescending(x => x.Id).First();
                            if (!force && artifact.NodeId == this.CurrentVersion)
                            {
                                Log.Debug("Up to date", this.verbouseOutput);
                                return false;
                            }

                            Log.Debug("Downloading |" + artifact.DownloadUrl, this.verbouseOutput);
                            using (var client2 = new WebClient())
                            {
                                client2.Headers.Add($"Authorization: token {this.token}");
                                client2.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                                client2.Headers.Add(HttpRequestHeader.Accept, "application/octet-stream");
                                string path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Plugin.Author}.{this.Plugin.Name}.artifacts.zip");
                                client2.DownloadFile(artifact.DownloadUrl, path);

                                string extractedPath = Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Plugin.Author}.{this.Plugin.Name}.artifacts.extracted");
                                ZipFile.ExtractToDirectory(path, extractedPath);
                                while (true)
                                {
                                    Log.Debug($"Scanning {extractedPath} for files", this.verbouseOutput);
                                    var files = Directory.GetFiles(extractedPath, "*.dll");
                                    if (files.Length != 0)
                                    {
                                        Log.Debug($"Found files in {extractedPath}", this.verbouseOutput);
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

                                            Log.Debug($"Copping {file} to {targetPath}", this.verbouseOutput);
                                            File.Copy(file, targetPath, true);
                                        }

                                        break;
                                    }

                                    var directories = Directory.GetDirectories(extractedPath);
                                    if (directories.Length == 0)
                                    {
                                        Log.Error($"Artifact is empty");
                                        return false;
                                    }

                                    extractedPath = directories[0];
                                }
                            }

                            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Plugin.Author}.{this.Plugin.Name}.txt"), artifact.NodeId);
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

                case AutoUpdateType.GITLAB:
                    Log.Debug($"Checking for update using GITLAB, chekcing for releases", this.verbouseOutput);
                    using (var client = new WebClient())
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(this.token))
                                client.Headers.Add($"PRIVATE-TOKEN: {this.token}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                            string releasesLink = this.url + "/releases";
                            Log.Debug($"Downloading release list from {releasesLink}", this.verbouseOutput);
                            var rawResult = client.DownloadString(releasesLink);
                            if (rawResult == string.Empty)
                            {
                                Log.Error($"AutoUpdate Failed: {releasesLink} returned empty page");
                                return false;
                            }

                            var releases = Newtonsoft.Json.JsonConvert.DeserializeObject<GitLab.Release[]>(rawResult);
                            if (releases.Length == 0)
                            {
                                Log.Error("AutoUpdate Failed: No releases found");
                                return false;
                            }

                            var release = releases[0];
                            if (!force && release.Tag == this.CurrentVersion)
                            {
                                Log.Debug("Up to date", this.verbouseOutput);
                                return false;
                            }

                            foreach (var link in release.Assets.Links)
                            {
                                Log.Debug("Downloading |" + link.Url, this.verbouseOutput);
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

                            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Plugin.Author}.{this.Plugin.Name}.txt"), release.Tag);
                            this.CurrentVersion = release.Tag;
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

                case AutoUpdateType.GITLAB_DEVELOPMENT:
                    Log.Debug($"Checking for update using GITLAB, chekcing for artifacts", this.verbouseOutput);
                    using (var client = new WebClient())
                    {
                        try
                        {
                            string jobsUrl = this.url + "/jobs?scope=success";
                            if (!string.IsNullOrWhiteSpace(this.token))
                                client.Headers.Add($"PRIVATE-TOKEN: {this.token}");
                            client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                            Log.Debug($"Downloading job list from {jobsUrl}", this.verbouseOutput);
                            var rawResult = client.DownloadString(jobsUrl);
                            if (rawResult == string.Empty)
                            {
                                Log.Error($"AutoUpdate Failed: {jobsUrl} returned empty page");
                                return false;
                            }

                            var jobs = Newtonsoft.Json.JsonConvert.DeserializeObject<GitLab.Job[]>(rawResult);
                            if (jobs.Length == 0)
                            {
                                Log.Error("AutoUpdate Failed: No jobs found");
                                return false;
                            }

                            var job = jobs[0];
                            if (!force && job.Commit.ShortId == this.CurrentVersion)
                            {
                                Log.Debug("Up to date", this.verbouseOutput);
                                return false;
                            }

                            string artifactUrl = this.url + $"/jobs/{job.Id}/artifacts";
                            Log.Debug("Downloading |" + artifactUrl, this.verbouseOutput);
                            string path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Plugin.Author}.{this.Plugin.Name}.artifacts.zip");
                            client.DownloadFile(artifactUrl, path);
                            string extractedPath = Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Plugin.Author}.{this.Plugin.Name}.artifacts.extracted");
                            ZipFile.ExtractToDirectory(path, extractedPath);
                            while (true)
                            {
                                Log.Debug($"Scanning {extractedPath} for files", this.verbouseOutput);
                                var files = Directory.GetFiles(extractedPath, "*.dll");
                                if (files.Length != 0)
                                {
                                    Log.Debug($"Found files in {extractedPath}", this.verbouseOutput);
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

                                        Log.Debug($"Copping {file} to {targetPath}", this.verbouseOutput);
                                        File.Copy(file, targetPath, true);
                                    }

                                    break;
                                }

                                var directories = Directory.GetDirectories(extractedPath);
                                if (directories.Length == 0)
                                {
                                    Log.Error($"Artifact is empty");
                                    return false;
                                }

                                extractedPath = directories[0];
                            }

                            File.WriteAllText(Path.Combine(Paths.Plugins, "AutoUpdater", $"{this.Plugin.Author}.{this.Plugin.Name}.txt"), job.Commit.ShortId);
                            this.CurrentVersion = job.Commit.ShortId;
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
                    throw new ArgumentOutOfRangeException("AutoUpdateType", $"Unknown AutoUpdateType ({this.type})");
            }
        }

        private readonly bool verbouseOutput;
        private readonly string url;
        private readonly string token;
        private readonly AutoUpdateType type;

        private void Server_RestartingRound()
        {
            this.DoAutoUpdate(false);
        }
    }
}
