// -----------------------------------------------------------------------
// <copyright file="GitLab.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Exiled.API.Features;
using Mistaken.Updater.API.Abstract;
using Mistaken.Updater.Config;
using Mistaken.Updater.Internal;
using Newtonsoft.Json;

namespace Mistaken.Updater.API
{
    internal class GitLab : IImplementation
    {
        public Type ReleaseType => typeof(Release[]);

        public string UrlSuffix => "/releases";

        public void AddHeaders(WebClient client, PluginManifest pluginManifest)
        {
            if (!string.IsNullOrWhiteSpace(pluginManifest.Token))
                client.Headers.Add($"PRIVATE-TOKEN: {pluginManifest.Token}");

            client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
        }

        public AutoUpdater.Action? DownloadArtifact(PluginManifest pluginManifest, bool force)
        {
            try
            {
                var jobs = Job.Download(this, pluginManifest);
                if (!jobs.Any(x => x.ArtifactsFileInfo.HasValue))
                {
                    Log.Debug($"[{pluginManifest.PluginName}] No jobs found, searching for releases", AutoUpdater.VerboseOutput);
                    return AutoUpdater.Action.UPDATE_AND_RESTART;
                }

                var job = jobs.First(x => x.ArtifactsFileInfo.HasValue);
                if (!force && job.Commit.ShortId == pluginManifest.CurrentVersion)
                {
                    Log.Debug($"[{pluginManifest.PluginName}] Up to date", AutoUpdater.VerboseOutput);
                    return AutoUpdater.Action.NONE;
                }

                job.DownloadArtifacts(this, pluginManifest);

                pluginManifest.UpdatePlugin("0.0.0", job.Commit.ShortId, job.Commit.LastPipelineField?.Ref ?? "unknown");
                return null;
            }
            catch (WebException ex)
            {
                Log.Error($"[{pluginManifest.PluginName}] AutoUpdate Failed: WebException");
                Log.Error(ex.Status + ": " + ex.Response);
                Log.Error(ex);

                return AutoUpdater.Action.NONE;
            }
            catch (Exception ex)
            {
                Log.Error($"[{pluginManifest.PluginName}] AutoUpdate Failed: ");
                Log.Error(ex);

                return AutoUpdater.Action.NONE;
            }
        }

        internal struct ArtifactsFile
        {
        }

        internal class Release : IRelease<Link, Commit>
        {
            [JsonProperty("tag_name")]
            public string Tag { get; set; }

            [JsonIgnore]
            public Link[] Assets => this.AssetsClassLinks.Links;

            [JsonProperty("commit")]
            public Commit Commit { get; set; }

            [JsonProperty("assets")]
            public AssetsClass AssetsClassLinks { get; set; }
        }

        internal class AssetsClass
        {
            [JsonProperty("links")]
            public Link[] Links { get; set; }
        }

        internal class Link : IAsset
        {
            [JsonProperty("direct_asset_url")]
            public string Url { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        internal class Commit : ICommit
        {
            [JsonProperty("short_id")]
            public string ShortId { get; set; }

            [JsonProperty("last_pipeline")]
            public LastPipeline LastPipelineField { get; set; }

            public class LastPipeline
            {
                [JsonProperty("ref")]
                public string Ref { get; set; }
            }
        }

        internal class Job
        {
            public static Job[] Download(IImplementation implementation, PluginManifest pluginManifest)
            {
                using (var client = new WebClient())
                {
                    var jobsUrl = pluginManifest.UpdateUrl + "/jobs?scope=success";
                    implementation.AddHeaders(client, pluginManifest);
                    Log.Debug($"[{pluginManifest.PluginName}] Downloading job list from {jobsUrl}", AutoUpdater.VerboseOutput);
                    var rawResult = client.DownloadString(jobsUrl);
                    if (rawResult != string.Empty)
                        return JsonConvert.DeserializeObject<Job[]>(rawResult);

                    Log.Error($"[{pluginManifest.PluginName}] AutoUpdate Failed: {jobsUrl} returned empty page");
                    return null;

                }
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("artifacts_file")]
            public ArtifactsFile? ArtifactsFileInfo { get; set; }

            [JsonProperty("commit")]
            public Commit Commit { get; set; }

            public void DownloadArtifacts(IImplementation implementation, PluginManifest pluginManifest)
            {
                using (var client = new WebClient())
                {
                    var path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{pluginManifest.PluginName.Replace('/', '_')}.artifacts.zip");
                    var extractedPath = Path.Combine(Paths.Plugins, "AutoUpdater", $"{pluginManifest.PluginName.Replace('/', '_')}.artifacts.extracted");

                    var artifactUrl = pluginManifest.UpdateUrl + $"/jobs/{this.Id}/artifacts";

                    implementation.AddHeaders(client, pluginManifest);

                    Log.Debug($"[{pluginManifest.PluginName}] Downloading artifact from " + artifactUrl, AutoUpdater.VerboseOutput);

                    client.DownloadFile(artifactUrl, path);

                    ZipFile.ExtractToDirectory(path, extractedPath);
                    File.Delete(path);

                    ReleaseUtil.MoveFiles(pluginManifest, extractedPath);

                    Directory.Delete(extractedPath, true);
                }
            }
        }
    }
}
