// -----------------------------------------------------------------------
// <copyright file="GitHub.cs" company="Mistaken">
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
using Newtonsoft.Json;

namespace Mistaken.Updater.API.Implementations
{
    internal class GitHub : IImplementation
    {
        public Type ReleaseType => typeof(Release);

        public string UrlSuffix => "/releases/latest";

        public void AddHeaders(WebClient client, PluginManifest pluginManifest)
        {
            if (!string.IsNullOrWhiteSpace(pluginManifest.Token))
                client.Headers.Add($"Authorization: token {pluginManifest.Token}");

            client.Headers.Add(HttpRequestHeader.UserAgent, "MistakenPluginUpdater");
        }

        public AutoUpdater.Action? DownloadArtifact(PluginManifest pluginManifest, bool force)
        {
            try
            {
                var artifacts = Artifacts.Download(this, pluginManifest);
                if (artifacts.ArtifactsArray.Length == 0)
                {
                    Log.Debug($"[{pluginManifest.PluginName}] No artifacts found, searching for Releases", AutoUpdater.VerboseOutput);
                    return AutoUpdater.Action.UPDATE_AND_RESTART;
                }

                var artifact = artifacts.ArtifactsArray.OrderByDescending(x => x.Id).First();

                if (!force && artifact.NodeId == pluginManifest.CurrentVersion)
                {
                    Log.Debug($"[{pluginManifest.PluginName}] Up to date", AutoUpdater.VerboseOutput);
                    return AutoUpdater.Action.NONE;
                }

                artifact.Download(this, pluginManifest);

                pluginManifest.UpdatePlugin("0.0.0", artifact.NodeId, artifact.WorkflowRunField?.HeadBranch ?? "unknown");

                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"[{pluginManifest.PluginName}] AutoUpdate Failed: {ex.Message}");
                Log.Error(ex.StackTrace);
                return AutoUpdater.Action.NONE;
            }
        }

        internal class Release : IRelease<Asset, Commit>
        {
            [JsonProperty("tag_name")]
            public string Tag { get; set; }

            [JsonProperty("assets")]
            public Asset[] Assets { get; set; }

            public Commit Commit => new Commit { ShortId = this.NodeId };

            [JsonProperty("node_id")]
            public string NodeId { get; set; }
        }

        internal class Asset : IAsset
        {
            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        internal class Commit : ICommit
        {
            [JsonProperty("short_id")]
            public string ShortId { get; set; }
        }

        internal class Artifacts
        {
            public static Artifacts Download(IImplementation implementation, PluginManifest pluginManifest)
            {
                using (var client = new WebClient())
                {
                    implementation.AddHeaders(client, pluginManifest);
                    string artifactsUrl = pluginManifest.UpdateUrl + "/actions/artifacts";
                    var rawResult = client.DownloadString(artifactsUrl);
                    if (rawResult == string.Empty)
                    {
                        Log.Error($"[{pluginManifest.PluginName}] AutoUpdate Failed: {artifactsUrl} returned empty page");
                        return null;
                    }

                    return JsonConvert.DeserializeObject<Artifacts>(rawResult);
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

            [JsonProperty("workflow_run")]
            public WorkflowRun WorkflowRunField { get; set; }

            public void Download(IImplementation implementation, PluginManifest pluginManifest)
            {
                Log.Debug($"[{pluginManifest.PluginName}] Downloading artifact from " + this.DownloadUrl, AutoUpdater.VerboseOutput);
                using (var client = new WebClient())
                {
                    var extractedPath = Path.Combine(Paths.Plugins, "AutoUpdater", $"{pluginManifest.PluginName.Replace('/', '_')}.artifacts.extracted");
                    var path = Path.Combine(Paths.Plugins, "AutoUpdater", $"{pluginManifest.PluginName.Replace('/', '_')}.artifacts.zip");

                    implementation.AddHeaders(client, pluginManifest);
                    client.Headers.Add(HttpRequestHeader.Accept, "application/octet-stream");

                    client.DownloadFile(this.DownloadUrl, path);

                    ZipFile.ExtractToDirectory(path, extractedPath);
                    File.Delete(path);

                    Internal.Utils.MoveFiles(pluginManifest, extractedPath);

                    Directory.Delete(extractedPath, true);
                }
            }

            public class WorkflowRun
            {
                [JsonProperty("head_branch")]
                public string HeadBranch { get; set; }
            }
        }
    }
}
