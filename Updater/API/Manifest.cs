// -----------------------------------------------------------------------
// <copyright file="Manifest.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;

namespace Mistaken.Updater.API
{
    internal struct Manifest
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("plugin_name")]
        public string PluginName { get; set; }
    }
}
