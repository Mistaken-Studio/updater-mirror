// -----------------------------------------------------------------------
// <copyright file="MistakenManifest.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;

namespace Mistaken.Updater.API.Manifests
{
    internal struct MistakenManifest : IManifest
    {
        public string PluginName => $"{this.Author.Replace(' ', '_')}/{this.Name.Replace(' ', '_')}";

        [JsonProperty("Name")] public string Name { get; set; }

        [JsonProperty("Author")] public string Author { get; set; }

        [JsonProperty("LatestVersion")] public string Version { get; set; }

        [JsonProperty("BuildDate")] public string BuildDate { get; set; }

        [JsonProperty("BuildId")] public string BuildId { get; set; }

        [JsonProperty("FileName")] public string FileName { get; set; }

        [JsonProperty("UpdateUrl")] public string UpdateUrl { get; set; }

        [JsonProperty("Dependencies")] public Dependency[] Dependencies { get; set; }
    }
}