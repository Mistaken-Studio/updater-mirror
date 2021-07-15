// -----------------------------------------------------------------------
// <copyright file="AutoUpdatablePlugin.cs" company="Mistaken">
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

    internal class GitLab
    {
        internal class Release
        {
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
        }
    }
}
