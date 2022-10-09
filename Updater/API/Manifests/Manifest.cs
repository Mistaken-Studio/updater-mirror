// -----------------------------------------------------------------------
// <copyright file="Manifest.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;

namespace Mistaken.Updater.API.Manifests
{
    internal struct Manifest : IManifest
    {
        [JsonProperty("plugin_name")] public string PluginName { get; set; }

        [JsonProperty("version")] public string Version { get; set; }
    }
}
