// -----------------------------------------------------------------------
// <copyright file="Utils.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Net;
using Exiled.API.Features;
using Mistaken.Updater.API;
using Mistaken.Updater.API.Abstract;
using Mistaken.Updater.API.Implementations;
using Mistaken.Updater.API.Manifests;
using Newtonsoft.Json;

namespace Mistaken.Updater.Internal
{
    internal static class Utils
    {
        public static IRelease<IAsset, ICommit> DownloadLatest(
            IImplementation implementation,
            PluginManifest pluginManifest)
        {
            using var client = new WebClient();
            implementation.AddHeaders(client, pluginManifest);

            var releaseUrl = pluginManifest.UpdateUrl + implementation.UrlSuffix;

            Log.Debug($"[{pluginManifest.PluginName}] Downloading release list from {releaseUrl}", AutoUpdater.VerboseOutput);
            var rawResult = client.DownloadString(releaseUrl);

            if (rawResult != string.Empty)
            {
                if (implementation is GitLab)
                {
                    try
                    {
                        return (IRelease<IAsset, ICommit>)((Array)JsonConvert.DeserializeObject(
                            rawResult,
                            implementation.ReleaseType))?.GetValue(0);
                    }
                    catch (InvalidCastException ex)
                    {
                        Log.Error($"Target: IRelease<IAsset, ICommit>[], Result: {JsonConvert.DeserializeObject(rawResult, implementation.ReleaseType)?.GetType().Name ?? "NULL"}");
                        Log.Error(ex);
                    }
                }

                return (IRelease<IAsset, ICommit>)JsonConvert.DeserializeObject(
                    rawResult,
                    implementation.ReleaseType);
            }

            Log.Error($"[{pluginManifest.PluginName}] AutoUpdate Failed: AutoUpdate URL returned empty page");
            return null;
        }

        public static void DownloadAsset(
            IImplementation implementation,
            IAsset asset,
            PluginManifest pluginManifest)
        {
            Log.Debug($"[{pluginManifest.PluginName}] Downloading asset from " + asset.Url, AutoUpdater.VerboseOutput);
            using var client = new WebClient();
            implementation.AddHeaders(client, pluginManifest);

            client.Headers.Add(HttpRequestHeader.Accept, "application/octet-stream");

            var name = asset.Name;
            string outputPath;
            string path;

            if (name.StartsWith("Dependency-"))
            {
                name = name.Substring(12);
                path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
                outputPath = Path.Combine(Paths.Dependencies, name);
            }
            else
            {
                outputPath = Path.Combine(Paths.Plugins, name);
                path = Path.Combine(Paths.Plugins, "AutoUpdater", name);
            }

            client.DownloadFile(asset.Url, path);
            File.Copy(path, outputPath, true);
            File.Delete(path);

            Log.Debug($"[{pluginManifest.PluginName}] Downloaded asset from " + asset.Url + " to " + outputPath, AutoUpdater.VerboseOutput);
        }

        internal static void MoveFiles(PluginManifest pluginManifest, string extractedPath)
        {
            while (true)
            {
                Log.Debug($"[{pluginManifest.PluginName}] Scanning {extractedPath} for files", AutoUpdater.VerboseOutput);
                var files = Directory.GetFiles(extractedPath, "*.dll");
                if (files.Length != 0)
                {
                    Log.Debug($"[{pluginManifest.PluginName}] Found files in {extractedPath}", AutoUpdater.VerboseOutput);
                    foreach (var file in files)
                    {
                        string name = Path.GetFileName(file);
                        string targetPath;
                        if (name.StartsWith("Dependency-"))
                        {
                            name = name.Substring(12);
                            targetPath = Path.Combine(Paths.Dependencies, name);
                        }
                        else
                            targetPath = Path.Combine(Paths.Plugins, name);

                        Log.Debug($"[{pluginManifest.PluginName}] Copping {file} to {targetPath}", AutoUpdater.VerboseOutput);
                        File.Copy(file, targetPath, true);
                    }

                    return;
                }

                var directories = Directory.GetDirectories(extractedPath);
                if (directories.Length == 0)
                {
                    Log.Error($"[{pluginManifest.PluginName}] Artifact is empty");
                    return;
                }

                extractedPath = directories[0];
            }
        }

        internal static MistakenManifest? GetMistakenManifest(PluginManifest pluginManifest, string extractedPath)
        {
            Log.Debug($"[{pluginManifest.PluginName}] Scanning {extractedPath} for files", AutoUpdater.VerboseOutput);
            var files = Directory.GetFiles(extractedPath, "manifest.json", SearchOption.AllDirectories);
            if (files.Length == 0)
                return null;

            Log.Debug(
                $"[{pluginManifest.PluginName}] Found manifest file: {files.First()}",
                AutoUpdater.VerboseOutput);
            var manifest =
                JsonConvert.DeserializeObject<MistakenManifest>(
                    File.ReadAllText(files.First()));

            return manifest;
        }
    }
}