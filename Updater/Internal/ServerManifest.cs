// -----------------------------------------------------------------------
// <copyright file="ServerManifest.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Mistaken.Updater.API;
using Newtonsoft.Json;

namespace Mistaken.Updater.Internal
{
    internal class ServerManifest
    {
        [JsonProperty("Plugins")]
        internal Dictionary<string, PluginManifest> Plugins { get; private set; } = new();

        [JsonProperty("LastUpdateCheck")]
        internal DateTime? LastUpdateCheck { get; set; }

        [JsonProperty("Tokens")]
        internal Dictionary<string, string> Tokens { get; set; } = new();

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

        internal void UnApplyTokens()
        {
            if (this.Tokens is null)
            {
                Log.Warn("ServerManifest Tokens are null, resetting ...");
                this.Tokens = new Dictionary<string, string>();
                return;
            }

            foreach (var token in this.Tokens)
            {
                foreach (var manifest in this.Plugins.Values)
                {
                    var tokenKey = $"${token.Key}";
                    if (manifest.UpdateUrl?.Contains(token.Value) ?? false)
                        manifest.UpdateUrl = manifest.UpdateUrl.Replace(token.Value, tokenKey);
                    if (manifest.Token?.Contains(token.Value) ?? false)
                        manifest.Token = manifest.Token.Replace(token.Value, tokenKey);
                }
            }
        }
    }
}