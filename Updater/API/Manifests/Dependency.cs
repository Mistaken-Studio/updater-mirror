// -----------------------------------------------------------------------
// <copyright file="Dependency.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;

namespace Mistaken.Updater.API.Manifests
{
    internal struct Dependency
    {
        [JsonProperty("FileName")] public string FileName { get; set; }

        [JsonProperty("IsPlugin")] public bool IsPlugin { get; set; }

        [JsonProperty("DownloadUrl")] public string DownloadUrl { get; set; }
    }
}