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
            public static Release[] Download(IPlugin<IAutoUpdatableConfig> plugin, AutoUpdateConfig config)
            {
                using (var client = new WebClient())
                {
                    if (!string.IsNullOrWhiteSpace(config.Token))
                        client.Headers.Add($"PRIVATE-TOKEN: {config.Token}");
                    client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                    string releasesLink = config.Url + "/releases";
                    Log.Debug($"[{plugin.Name}] Downloading release list from {releasesLink}", config.VerbouseOutput);
                    var rawResult = client.DownloadString(releasesLink);
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

            public void Download(IPlugin<IAutoUpdatableConfig> plugin, AutoUpdateConfig config)
            {
                using (var client = new WebClient())
                {
                    Log.Debug($"[{plugin.Name}] Downloading |" + this.Url, config.VerbouseOutput);
                    if (!string.IsNullOrWhiteSpace(config.Token))
                        client.Headers.Add($"PRIVATE-TOKEN: {config.Token}");
                    client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                    string name = this.Name;
                    if (name.StartsWith("Dependencie-"))
                    {
                        name = name.Substring(12);
                        string path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                        client.DownloadFile(this.Url, path);
                        File.Copy(path, Path.Combine(Paths.Dependencies, name), true);
                    }
                    else
                    {
                        string path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                        client.DownloadFile(this.Url, path);
                        File.Copy(path, Path.Combine(Paths.Plugins, name), true);
                    }
                }
            }
        }

        internal class Job
        {
            public static Job[] Download(IPlugin<IAutoUpdatableConfig> plugin, AutoUpdateConfig config)
            {
                using (var client = new WebClient())
                {
                    string jobsUrl = config.Url + "/jobs?scope=success";
                    if (!string.IsNullOrWhiteSpace(config.Token))
                        client.Headers.Add($"PRIVATE-TOKEN: {config.Token}");
                    client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
                    Log.Debug($"[{plugin.Name}] Downloading job list from {jobsUrl}", config.VerbouseOutput);
                    var rawResult = client.DownloadString(jobsUrl);
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

            public void DownloadArtifacts(IPlugin<IAutoUpdatableConfig> plugin, AutoUpdateConfig config)
            {
                using (var client = new WebClient())
                {
                    string artifactUrl = config.Url + $"/jobs/{this.Id}/artifacts";
                    Log.Debug($"[{plugin.Name}] Downloading |" + artifactUrl, config.VerbouseOutput);
                    string path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.artifacts.zip");
                    client.DownloadFile(artifactUrl, path);
                    string extractedPath = Path.Combine(Paths.Plugins, "AutoUpdater", $"{plugin.Author}.{plugin.Name}.artifacts.extracted");
                    ZipFile.ExtractToDirectory(path, extractedPath);
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
