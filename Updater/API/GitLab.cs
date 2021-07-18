// -----------------------------------------------------------------------
// <copyright file="GitLab.cs" company="Mistaken">
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
    internal class GitLab
    {
        internal class Release
        {
            public static async Task<Release[]> Download(IPlugin<IAutoUpdatableConfig> plugin)
            {
                using (var client = new WebClient())
                {
                    if (!string.IsNullOrWhiteSpace(plugin.Config.AutoUpdateConfig.Token))
                        client.Headers.Add($"PRIVATE-TOKEN: {plugin.Config.AutoUpdateConfig.Token}");
                    client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                    string releasesLink = plugin.Config.AutoUpdateConfig.Url + "/releases";
                    Log.Debug($"[{plugin.Name}] Downloading release list from {releasesLink}", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    var rawResult = await client.DownloadStringTaskAsync(releasesLink);
                    if (rawResult == string.Empty)
                    {
                        Log.Error($"[{plugin.Name}] AutoUpdate Failed: {releasesLink} returned empty page");
                        return null;
                    }

                    return Newtonsoft.Json.JsonConvert.DeserializeObject<GitLab.Release[]>(rawResult);
                }
            }

            [JsonProperty("tag_name")]
            public string Tag { get; set; }

            [JsonProperty("assets")]
            public Assets Assets { get; set; }
        }

        internal class Assets
        {
            [JsonProperty("links")]
            public Link[] Links { get; set; }
        }

        internal class Link
        {
            [JsonProperty("direct_asset_url")]
            public string Url { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            public async Task Download(IPlugin<IAutoUpdatableConfig> plugin)
            {
                using (var client = new WebClient())
                {
                    Log.Debug($"[{plugin.Name}] Downloading |" + this.Url, plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    if (!string.IsNullOrWhiteSpace(plugin.Config.AutoUpdateConfig.Token))
                        client.Headers.Add($"PRIVATE-TOKEN: {plugin.Config.AutoUpdateConfig.Token}");
                    client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                    string name = this.Name;
                    if (name.StartsWith("Dependencie-"))
                    {
                        name = name.Substring(12);
                        string path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                        await client.DownloadFileTaskAsync(this.Url, path);
                        File.Copy(path, Path.Combine(Paths.Dependencies, name), true);
                    }
                    else
                    {
                        string path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                        await client.DownloadFileTaskAsync(this.Url, path);
                        File.Copy(path, Path.Combine(Paths.Plugins, name), true);
                    }
                }
            }
        }

        internal class Job
        {
            public static async Task<Job[]> Download(IPlugin<IAutoUpdatableConfig> plugin)
            {
                using (var client = new WebClient())
                {
                    string jobsUrl = plugin.Config.AutoUpdateConfig.Url + "/jobs?scope=success";
                    if (!string.IsNullOrWhiteSpace(plugin.Config.AutoUpdateConfig.Token))
                        client.Headers.Add($"PRIVATE-TOKEN: {plugin.Config.AutoUpdateConfig.Token}");
                    client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                    Log.Debug($"[{plugin.Name}] Downloading job list from {jobsUrl}", plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    var rawResult = await client.DownloadStringTaskAsync(jobsUrl);
                    if (rawResult == string.Empty)
                    {
                        Log.Error($"[{plugin.Name}] AutoUpdate Failed: {jobsUrl} returned empty page");
                        return null;
                    }

                    return Newtonsoft.Json.JsonConvert.DeserializeObject<GitLab.Job[]>(rawResult);
                }
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("commit")]
            public Commit Commit { get; set; }

            public async Task DownloadArtifacts(IPlugin<IAutoUpdatableConfig> plugin)
            {
                using (var client = new WebClient())
                {
                    string artifactUrl = plugin.Config.AutoUpdateConfig.Url + $"/jobs/{this.Id}/artifacts";
                    Log.Debug($"[{plugin.Name}] Downloading |" + artifactUrl, plugin.Config.AutoUpdateConfig.VerbouseOutput);
                    string path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.artifacts.zip");
                    await client.DownloadFileTaskAsync(artifactUrl, path);
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
                            return;
                        }

                        extractedPath = directories[0];
                    }
                }
            }
        }

        internal class Commit
        {
            [JsonProperty("short_id")]
            public string ShortId { get; set; }
        }
    }
}
