// -----------------------------------------------------------------------
// <copyright file="GitHub.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Mistaken.Updater.Config;
using Newtonsoft.Json;

namespace Mistaken.Updater.API
{
    internal class GitHub
    {
        internal class Release
        {
            public static Release DownloadLatest(IPlugin<IAutoUpdatableConfig> plugin, AutoUpdateConfig config)
            {
                using (var client = new WebClient())
                {
                    if (!string.IsNullOrWhiteSpace(config.Token))
                        client.Headers.Add($"Authorization: token {config.Token}");
                    client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                    string releaseUrl = config.Url + "/releases/latest";
                    var rawResult = client.DownloadString(releaseUrl);
                    if (rawResult == string.Empty)
                    {
                        Log.Error($"[{plugin.Name}] AutoUpdate Failed: AutoUpdate URL returned empty page");
                        return null;
                    }

                    return Newtonsoft.Json.JsonConvert.DeserializeObject<GitHub.Release>(rawResult);
                }
            }

            [JsonProperty("tag_name")]
            public string Tag { get; set; }

            [JsonProperty("assets")]
            public Asset[] Assets { get; set; }
        }

        internal class Asset
        {
            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            public void DownloadAsset(IPlugin<IAutoUpdatableConfig> plugin, AutoUpdateConfig config)
            {
                Log.Debug($"[{plugin.Name}] Downloading |" + this.Url, config.VerbouseOutput);
                using (var client2 = new WebClient())
                {
                    if (!string.IsNullOrWhiteSpace(config.Token))
                        client2.Headers.Add($"Authorization: token {config.Token}");
                    client2.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                    client2.Headers.Add(HttpRequestHeader.Accept, "application/octet-stream");
                    string name = this.Name;
                    if (name.StartsWith("Dependencie-"))
                    {
                        name = name.Substring(12);
                        string path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                        client2.DownloadFile(this.Url, path);
                        File.Copy(path, Path.Combine(Paths.Dependencies, name), true);
                        File.Delete(path);
                    }
                    else
                    {
                        string path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                        client2.DownloadFile(this.Url, path);
                        File.Copy(path, Path.Combine(Paths.Plugins, name), true);
                        File.Delete(path);
                    }
                }
            }
        }

        internal class Artifacts
        {
            public static Artifacts Download(IPlugin<IAutoUpdatableConfig> plugin, AutoUpdateConfig config)
            {
                using (var client = new WebClient())
                {
                    if (!string.IsNullOrWhiteSpace(config.Token))
                        client.Headers.Add($"Authorization: token {config.Token}");
                    client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                    string artifactsUrl = config.Url + "/actions/artifacts";
                    var rawResult = client.DownloadString(artifactsUrl);
                    if (rawResult == string.Empty)
                    {
                        Log.Error($"[{plugin.Name}] AutoUpdate Failed: {artifactsUrl} returned empty page");
                        return null;
                    }

                    return Newtonsoft.Json.JsonConvert.DeserializeObject<GitHub.Artifacts>(rawResult);
                }
            }

            [JsonProperty("artifacts")]
            public Artifact[] ArtifactsArray { get; set; }
        }

        internal class Artifact
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("archive_download_url")]
            public string DownloadUrl { get; set; }

            [JsonProperty("node_id")]
            public string NodeId { get; set; }

            public void Download(IPlugin<IAutoUpdatableConfig> plugin, AutoUpdateConfig config)
            {
                Log.Debug($"[{plugin.Name}] Downloading |" + this.DownloadUrl, config.VerbouseOutput);
                using (var client2 = new WebClient())
                {
                    if (!string.IsNullOrWhiteSpace(config.Token))
                        client2.Headers.Add($"Authorization: token {config.Token}");
                    client2.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                    client2.Headers.Add(HttpRequestHeader.Accept, "application/octet-stream");
                    string path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.artifacts.zip");
                    client2.DownloadFile(this.DownloadUrl, path);

                    string extractedPath = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.artifacts.extracted");
                    ZipFile.ExtractToDirectory(path, extractedPath);
                    File.Delete(path);
                    while (true)
                    {
                        Log.Debug($"[{plugin.Name}] Scanning {extractedPath} for files", config.VerbouseOutput);
                        var files = Directory.GetFiles(extractedPath, "*.dll");
                        if (files.Length != 0)
                        {
                            Log.Debug($"[{plugin.Name}] Found files in {extractedPath}", config.VerbouseOutput);
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

                                Log.Debug($"[{plugin.Name}] Copping {file} to {targetPath}", config.VerbouseOutput);
                                File.Copy(file, targetPath, true);
                            }

                            break;
                        }

                        var directories = Directory.GetDirectories(extractedPath);
                        if (directories.Length == 0)
                        {
                            Log.Error($"[{plugin.Name}] Artifact is empty");
                            return;
                        }

                        extractedPath = directories[0];
                    }

                    Directory.Delete(extractedPath, true);
                }
            }
        }
    }
}
