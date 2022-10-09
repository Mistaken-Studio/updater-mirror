// -----------------------------------------------------------------------
// <copyright file="MistakenUpdater.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;
using Mistaken.Updater.API;
using Mistaken.Updater.API.Manifests;
using Mistaken.Updater.Internal;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global
namespace Mistaken.Updater
{
    internal static class MistakenUpdater
    {
        public static async Task<(FailCodes result, string message)> InstallPlugin(string downloadUrl, string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(downloadUrl))
                    throw new ArgumentException("Was null or empty", nameof(downloadUrl));

                // LoadManifest
                var serverManifest = await LoadManifest();

                // LockManifest
                LockManifest();

                using var client = new HttpClient();

                PrepareHttpClient(client, token);

                ConcurrentBag<string> plugins = new();
                ConcurrentBag<string> dependencies = new();

                // Run private InstallPlugin
                var (result, message) = await InstallPlugin(
                    serverManifest,
                    client,
                    downloadUrl,
                    token,
                    plugins,
                    dependencies);

                if (result != FailCodes.SUCCESS)
                {
                    Revert();

                    return (result, message);
                }

                // Regenerate Using List
                (result, message) = RegenerateDependencies(serverManifest);

                if (result != FailCodes.SUCCESS)
                {
                    Revert();

                    return (result, message);
                }

                // Copy all files to respective directories
                foreach (var dependency in dependencies)
                {
                    File.Copy(
                        Path.Combine(updaterTmpAddPath, dependency),
                        Path.Combine(dependenciesPath, dependency),
                        true);
                }

                foreach (var plugin in plugins)
                {
                    File.Copy(
                        Path.Combine(updaterTmpAddPath, plugin),
                        Path.Combine(pluginsPath, plugin),
                        true);
                }

                // Cleanup
                foreach (var file in Directory.GetFiles(updaterTmpAddPath))
                    File.Delete(file);

                // UnlockManifest
                UnlockManifest();

                // SaveManifest
                await SaveManifest(serverManifest);

                return (FailCodes.SUCCESS, "Installed plugin");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Revert();
                return (FailCodes.UNEXPECTED_EXCEPTION, ex.ToString());
            }
        }

        public static async Task<(FailCodes result, string message)> UninstallPlugin(string pluginName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pluginName))
                    throw new ArgumentException("Was null or empty", nameof(pluginName));

                // LoadManifest
                var serverManifest = await LoadManifest();

                // LockManifest
                LockManifest();

                // Run private UninstallPlugin
                var (result, message) = UninstallPlugin(serverManifest, pluginName);

                if (result != FailCodes.SUCCESS)
                {
                    Revert();

                    return (result, message);
                }

                // Regenerate Using List
                (result, message) = RegenerateDependencies(serverManifest);

                if (result != FailCodes.SUCCESS)
                {
                    Revert();

                    return (result, message);
                }

                foreach (var file in Directory.GetFiles(updaterTmpRemovePath))
                    File.Delete(file);

                foreach (var file in Directory.GetFiles(updaterTmpRemoveDependenciesPath))
                    File.Delete(file);

                // UnlockManifest
                UnlockManifest();

                // SaveManifest
                await SaveManifest(serverManifest);

                return (FailCodes.SUCCESS, "Uninstalled plugin");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Revert();
                return (FailCodes.UNEXPECTED_EXCEPTION, ex.ToString());
            }
        }

        public enum FailCodes
        {
            SUCCESS,
            FAILED_TO_DOWNLOAD_MANIFEST,
            FAILED_TO_PARSE_MANIFEST,
            ALREADY_INSTALLED,
            FAILED_TO_DOWNLOAD_ASSEMBLY,
            FAILED_TO_STORE_ASSEMBLY,
            FAILED_TO_INSTALL_DEPENDENCY,
            FAILED_TO_FIND_DEPENDENCY,
            DEPENDENCY_REQUIRED_BY_ANOTHER_PLUGIN,
            FAILED_TO_DELETE_ASSEMBLY,
            FAILED_TO_FIND_PLUGIN,
            FAILED_TO_UNINSTALL_DEPENDENCY,
            UNEXPECTED_EXCEPTION,
        }

        internal static void SetupPaths()
        {
            pluginsPath = Paths.Plugins;
            dependenciesPath = Paths.Dependencies;
            updaterPath = Path.Combine(pluginsPath, "AutoUpdater");
            if (!Directory.Exists(updaterPath))
                Directory.CreateDirectory(updaterPath);

            updaterTmpPath = Path.Combine(updaterPath, "tmp");
            if (!Directory.Exists(updaterTmpPath))
                Directory.CreateDirectory(updaterTmpPath);

            updaterTmpAddPath = Path.Combine(updaterTmpPath, "add");
            if (!Directory.Exists(updaterTmpAddPath))
                Directory.CreateDirectory(updaterTmpAddPath);

            updaterTmpRemovePath = Path.Combine(updaterTmpPath, "remove");
            if (!Directory.Exists(updaterTmpRemovePath))
                Directory.CreateDirectory(updaterTmpRemovePath);

            updaterTmpRemoveDependenciesPath = Path.Combine(updaterTmpRemovePath, "dependencies");
            if (!Directory.Exists(updaterTmpRemoveDependenciesPath))
                Directory.CreateDirectory(updaterTmpRemoveDependenciesPath);
        }

        internal static async Task<ServerManifest> LoadManifest()
        {
            var cancellationTokenSource = new CancellationTokenSource(15 * 1000);

            try
            {
                while (lockEnabled)
                    await Task.Delay(10, cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Detected problem with lock");
            }

            var tor = JsonConvert.DeserializeObject<ServerManifest>(
                File.ReadAllText(Path.Combine(updaterPath, "manifest.json")));

            tor.ApplyTokens();

            return tor;
        }

        internal static async Task SaveManifest(ServerManifest manifest)
        {
            var cancellationTokenSource = new CancellationTokenSource(15 * 1000);

            try
            {
                while (lockEnabled)
                    await Task.Delay(10, cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Detected problem with lock");
            }

            manifest.UnApplyTokens();

            File.WriteAllText(
                Path.Combine(updaterPath, "manifest.json"),
                JsonConvert.SerializeObject(manifest, Formatting.Indented));
        }

        private static string pluginsPath;
        private static string dependenciesPath;
        private static string updaterPath;
        private static string updaterTmpPath;
        private static string updaterTmpAddPath;
        private static string updaterTmpRemovePath;
        private static string updaterTmpRemoveDependenciesPath;

        private static bool lockEnabled;

        private static void LockManifest()
        {
            lockEnabled = true;
        }

        private static void UnlockManifest()
        {
            lockEnabled = false;
        }

        private static (FailCodes result, string message) RegenerateDependencies(ServerManifest manifest)
        {
            // Update required by
            foreach (var value in manifest.Dependency.Values)
            {
                value.RequiredBy.Clear();
            }

            foreach (var value in manifest.Plugins.Values)
            {
                if (value.Dependencies is null)
                {
                    Log.Error($"Malformed config, plugin({value.PluginName}) dependency is null");
                    continue;
                }

                foreach (var pluginDependency in value.Dependencies
                             .Where(pluginDependency => !pluginDependency.IsPlugin))
                {
                    if (manifest.Dependency.TryGetValue(pluginDependency.FileName, out var dependency))
                    {
                        dependency.RequiredBy.Add(value.PluginName);
                    }
                    else
                    {
                        Log.Error($"Malformed config, dependency not found {pluginDependency.FileName}");
                    }
                }
            }

            // Uninstall not used
            foreach (var value in manifest.Dependency.Values
                         .Where(x => x.RequiredBy.Count == 0).ToArray())
            {
                var (result, message) = UninstallDependency(manifest, value.FileName);

                if (result != FailCodes.SUCCESS)
                {
                    return (FailCodes.FAILED_TO_UNINSTALL_DEPENDENCY, $"[{result}] {message}");
                }
            }

            return (FailCodes.SUCCESS, "Regenerated using list");
        }

        private static void PrepareHttpClient(HttpClient client, string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Add("Private-Token", token);

            client.DefaultRequestHeaders.Add(HttpRequestHeader.UserAgent.ToString(), "MistakenPluginUpdater");
        }

        private static void Revert()
        {
            // Cleanup
            foreach (var file in Directory.GetFiles(updaterTmpAddPath))
                File.Delete(file);

            foreach (var file in Directory.GetFiles(updaterTmpRemovePath))
                File.Move(file, Path.Combine(pluginsPath, Path.GetFileName(file)));

            foreach (var file in Directory.GetFiles(updaterTmpRemoveDependenciesPath))
                File.Move(file, Path.Combine(dependenciesPath, Path.GetFileName(file)));

            // UnlockManifest
            UnlockManifest();
        }

        // Download manifest
        // If failed then fail with failed to download manifest error
        // Parse manifest
        // If failed then fail with malformed manifest
        // Check if plugin is not already installed
        // - If true then check if download url matches
        //   - If true then fail with already installed error
        //   - If false then override information? and continue
        // - If false then continue
        // Download assembly to tmp
        // If failed then fail with failed to download assembly
        // Start installing dependencies
        // foreach dependency
        // - If Dependency
        //   - Run InstallDependency without copping to target folder and store file name
        //   - If failed fail with failed to install dependency error and return dependency installation error
        // - If Plugin
        //   - Run InstallPlugin without copping to target folder and store plugin and it's dependencies file names
        //   - If failed fail with failed to install dependency error and return dependency installation error
        private static async Task<(FailCodes result, string message)> InstallPlugin(
            ServerManifest serverManifest,
            HttpClient client,
            string downloadUrl,
            string token,
            ConcurrentBag<string> plugins,
            ConcurrentBag<string> dependencies)
        {
            // Download manifest
            var response = await client.GetAsync(downloadUrl);

            // If failed then fail with failed to download manifest error
            if (!response.IsSuccessStatusCode)
            {
                return (FailCodes.FAILED_TO_DOWNLOAD_MANIFEST,
                    $"[{response.StatusCode}] {await response.Content.ReadAsStringAsync()}");
            }

            var rawManifest = await response.Content.ReadAsStringAsync();

            MistakenManifest pluginManifest;
            try
            {
                // Parse manifest
                pluginManifest = JsonConvert.DeserializeObject<MistakenManifest>(rawManifest);
            }
            catch (Exception ex)
            {
                // If failed then fail with malformed manifest
                Log.Error(ex);
                Log.Error(rawManifest);
                return (FailCodes.FAILED_TO_PARSE_MANIFEST,
                    ex.Message);
            }

            return await InstallPlugin(
                serverManifest,
                client,
                pluginManifest,
                downloadUrl,
                token,
                plugins,
                dependencies);
        }

        private static async Task<(FailCodes result, string message)> InstallPlugin(
            ServerManifest serverManifest,
            HttpClient client,
            MistakenManifest pluginManifest,
            string manifestDownloadUrl,
            string token,
            ConcurrentBag<string> plugins,
            ConcurrentBag<string> dependencies)
        {
            try
            {
                if (plugins.Contains(pluginManifest.FileName))
                    return (FailCodes.SUCCESS, "Already installed");

                // Check if plugin is not already installed
                // - If true then check if download url matches
                if (serverManifest.Plugins.TryGetValue(pluginManifest.PluginName, out var installedManifest))
                {
                    // - If true then fail with already installed error
                    if (installedManifest.UpdateUrl == pluginManifest.UpdateUrl)
                    {
                        return (FailCodes.ALREADY_INSTALLED,
                            "Plugin is already installed");
                    }

                    // - If false then override information? and continue
                    else
                    {
                        // ToDo: Rethink this?
                        installedManifest.UpdateUrl = manifestDownloadUrl;
                        installedManifest.Token = token;
                    }
                }

                // - If false then continue
                else
                {
                    serverManifest.Plugins.Add(
                        pluginManifest.PluginName,
                        new PluginManifest(pluginManifest)
                        {
                            Token = token,
                            UpdateUrl = manifestDownloadUrl,
                        });
                }

                // Download assembly to tmp
                var response = await client.GetAsync(pluginManifest.UpdateUrl);

                // If failed then fail with failed to download assembly
                if (!response.IsSuccessStatusCode)
                {
                    return (FailCodes.FAILED_TO_DOWNLOAD_ASSEMBLY,
                        $"[{response.StatusCode}] {await response.Content.ReadAsStringAsync()}");
                }

                try
                {
                    File.WriteAllBytes(
                        Path.Combine(updaterTmpAddPath, pluginManifest.FileName),
                        await response.Content.ReadAsByteArrayAsync());
                    plugins.Add(pluginManifest.FileName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    return (FailCodes.FAILED_TO_STORE_ASSEMBLY, ex.Message);
                }

                // Start installing dependencies
                // foreach dependency
                foreach (var dependency in pluginManifest.Dependencies)
                {
                    // - If Plugin
                    //   - Run InstallPlugin without copping to target folder and store plugin and it's dependencies file names
                    //   - If failed fail with failed to install dependency error and return dependency installation error
                    if (dependency.IsPlugin)
                    {
                        var (result, message) = await InstallPlugin(
                            serverManifest,
                            client,
                            dependency.DownloadUrl,
                            token,
                            plugins,
                            dependencies);

                        if (result != FailCodes.SUCCESS && result != FailCodes.ALREADY_INSTALLED)
                        {
                            return (FailCodes.FAILED_TO_INSTALL_DEPENDENCY,
                                $"[{dependency.FileName}] [{result}] {message}");
                        }
                    }

                    // - If Dependency
                    //   - Run InstallDependency without copping to target folder and store file name
                    //   - If failed fail with failed to install dependency error and return dependency installation error
                    else
                    {
                        var (result, message) = await InstallDependency(
                            serverManifest,
                            client,
                            dependency.DownloadUrl,
                            dependency.FileName,
                            dependencies);

                        if (result != FailCodes.SUCCESS && result != FailCodes.ALREADY_INSTALLED)
                        {
                            return (FailCodes.FAILED_TO_INSTALL_DEPENDENCY,
                                $"[{dependency.FileName}] [{result}] {message}");
                        }
                    }
                }

                return (FailCodes.SUCCESS, "Installed plugin");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return (FailCodes.UNEXPECTED_EXCEPTION, ex.ToString());
            }
        }

        private static (FailCodes result, string message) UninstallPlugin(
            ServerManifest serverManifest,
            string pluginName)
        {
            // Check if plugin is installed in manifest
            if (!serverManifest.Plugins.TryGetValue(pluginName, out var installedPlugin))
            {
                // - If false then fail with plugin not found error
                return (FailCodes.FAILED_TO_FIND_PLUGIN, $"Failed to find plugin in manifest({pluginName})");
            }

            // - If true then continue
            else
            {
                serverManifest.Plugins.Remove(pluginName);
            }

            // Check if plugin is an dependency of any plugin
            if (serverManifest.Plugins.Values.Any(x => x.Dependencies.Any(y => y.FileName == installedPlugin.FileName)))
            {
                // - If true then fail with plugin required by other plugin
                return (FailCodes.DEPENDENCY_REQUIRED_BY_ANOTHER_PLUGIN, "Plugin is used by another plugin");
            }

            // - If false then continue
            else
            {
            }

            // foreach dependency
            foreach (var installedPluginDependency in installedPlugin.Dependencies.ToArray())
            {
                installedPlugin.Dependencies.Remove(installedPluginDependency);

                // If is dependency
                if (!installedPluginDependency.IsPlugin)
                {
                    // If true then ...
                    var dep = serverManifest.Dependency[installedPluginDependency.FileName];
                    dep.RequiredBy.Remove(installedPlugin.PluginName);

                    // If plugin is only dependency
                    if (dep.RequiredBy.Count == 0)
                    {
                        // - If true then start UninstallDependency without saving manifest
                        var (result, message) = UninstallDependency(serverManifest, installedPluginDependency.FileName);

                        if (result != FailCodes.SUCCESS)
                        {
                            return (FailCodes.FAILED_TO_UNINSTALL_DEPENDENCY, $"[{result}] {message}");
                        }
                    }

                    // - If false then continue
                    else
                    {
                    }
                }

                // - If false then continue
                else
                {
                }
            }

            try
            {
                // Remove assembly
                File.Move(
                    Path.Combine(pluginsPath, installedPlugin.FileName),
                    Path.Combine(updaterTmpRemovePath, installedPlugin.FileName));
            }
            catch (Exception ex)
            {
                // If failed fail with failed to delete assembly error
                Log.Error(ex);
                return (FailCodes.FAILED_TO_DELETE_ASSEMBLY, ex.Message);
            }

            return (FailCodes.SUCCESS, "Uninstalled plugin successfully");
        }

        private static async Task<(FailCodes result, string message)> InstallDependency(
            ServerManifest serverManifest,
            HttpClient client,
            string downloadUrl,
            string fileName,
            ConcurrentBag<string> dependencies)
        {
            if (dependencies.Contains(fileName))
                return (FailCodes.SUCCESS, "Already installed");

            // Check if dependency is not already installed
            if (serverManifest.Dependency.TryGetValue(fileName, out var installedDependency))
            {
                // - If true then check if download url matches
                if (installedDependency.DownloadUrl == downloadUrl)
                {
                    // - If true then fail with already installed error
                    return (FailCodes.ALREADY_INSTALLED, "Dependency already installed");
                }

                // - If false then override information? and continue
                else
                {
                    // ToDo: Rethink this?
                    installedDependency.DownloadUrl = downloadUrl;
                }
            }

            // - If false then continue
            else
            {
                serverManifest.Dependency.Add(
                    fileName,
                    new API.Dependency
                    {
                        DownloadUrl = downloadUrl,
                        FileName = fileName,
                        RequiredBy = new List<string>(),
                    });
            }

            // Download assembly to tmp
            var response = await client.GetAsync(downloadUrl);

            // If failed then fail with failed to download assembly
            if (!response.IsSuccessStatusCode)
            {
                return (FailCodes.FAILED_TO_DOWNLOAD_ASSEMBLY,
                    $"[{response.StatusCode}] {await response.Content.ReadAsStringAsync()}");
            }

            try
            {
                File.WriteAllBytes(
                    Path.Combine(updaterTmpAddPath, fileName),
                    await response.Content.ReadAsByteArrayAsync());
                dependencies.Add(fileName);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return (FailCodes.FAILED_TO_STORE_ASSEMBLY, ex.Message);
            }

            return (FailCodes.SUCCESS, "Installed dependency");
        }

        private static (FailCodes result, string message) UninstallDependency(
            ServerManifest serverManifest,
            string fileName)
        {
            // Check if dependency is installed
            if (!serverManifest.Dependency.TryGetValue(fileName, out var dependency))
            {
                // - If false then fail with dependency not found error
                return (FailCodes.FAILED_TO_FIND_DEPENDENCY, $"Failed to find dependency: {fileName}");
            }

            // - If true then continue
            else
            {
                serverManifest.Dependency.Remove(fileName);
            }

            // Check if dependency is an dependency of any plugin
            if (dependency.RequiredBy.Count != 0)
            {
                // - If true then fail with dependency required by a plugin
                return (FailCodes.DEPENDENCY_REQUIRED_BY_ANOTHER_PLUGIN, "Dependency is required by a plugin");
            }

            // - If false then continue
            else
            {
            }

            if (!File.Exists(Path.Combine(dependenciesPath, fileName)))
            {
                // If failed fail with failed to delete assembly error
                return (FailCodes.FAILED_TO_DELETE_ASSEMBLY, $"File({fileName}) not found");
            }

            try
            {
                // Remove assembly
                File.Move(
                    Path.Combine(dependenciesPath, fileName),
                    Path.Combine(updaterTmpRemoveDependenciesPath, fileName));
            }
            catch (Exception ex)
            {
                // If failed fail with failed to delete assembly error
                return (FailCodes.FAILED_TO_DELETE_ASSEMBLY, ex.Message);
            }

            return (FailCodes.SUCCESS, "Uninstalled dependency");
        }
    }
}