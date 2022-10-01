// -----------------------------------------------------------------------
// <copyright file="ServerManifest.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Newtonsoft.Json;

namespace Mistaken.Updater.Config
{
    /// <summary>
    /// Server Manifest.
    /// </summary>
    public class ServerManifest
    {
        /// <summary>
        /// Gets Plugins.
        /// </summary>
        [JsonProperty("Plugins")]
        public Dictionary<string, PluginManifest> Plugins { get; private set; } = new Dictionary<string, PluginManifest>();

        /// <summary>
        /// Gets last update check time.
        /// </summary>
        [JsonProperty("LastUpdateCheck")]
        public DateTime? LastUpdateCheck { get; internal set; }

        [JsonProperty("Tokens")]
        internal Dictionary<string, string> Tokens { get; set; } = new Dictionary<string, string>();

        internal void ApplyTokens()
        {
            if (this.Tokens is null)
            {
                Log.Warn("ServerManifest Tokens are null, resetting ...");
                this.Tokens = new Dictionary<string, string>();
            }

            foreach (var token in this.Tokens)
            {
                foreach (var manifest in this.Plugins.Values)
                {
                    var tokenKey = $"${token.Key}";
                    if (manifest.UpdateUrl?.Contains(tokenKey) ?? false)
                        manifest.UpdateUrl = manifest.UpdateUrl.Replace(tokenKey, token.Value);
                    if (manifest.Token?.Contains(tokenKey) ?? false)
                        manifest.Token = manifest.Token.Replace(tokenKey, token.Value);
                }
            }
        }
    }
}