// -----------------------------------------------------------------------
// <copyright file="GitHub.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using Exiled.API.Features;
using Newtonsoft.Json;

namespace Mistaken.API
{
    internal class GitHub
    {
        internal class Release
        {
            [JsonProperty("tag_name")]
            public string Tag { get; set; }

            [JsonProperty("assets")]
            public Asset[] Assets { get; set; }
        }

        internal class Asset
        {
            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        internal class Artifacts
        {
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
        }
    }
}
