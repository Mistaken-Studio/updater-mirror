﻿// -----------------------------------------------------------------------
// <copyright file="PluginManifest.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Exiled.API.Interfaces;
using Mistaken.Updater.API.Abstract;
using Mistaken.Updater.API.Config;
using Mistaken.Updater.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Mistaken.Updater.API
{
    /// <summary>
    /// Plugin Manifest.
    /// </summary>
    public class PluginManifest
    {
        /// <summary>
        /// Gets or sets plugin Name.
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// Gets or sets current Version.
        /// </summary>
        public string CurrentVersion { get; set; }

        /// <summary>
        /// Gets or sets current Build Id.
        /// </summary>
        public string CurrentBuildId { get; set; }

        /// <summary>
        /// Gets or sets update Time.
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// Gets or sets source Type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SourceType SourceType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether development version should be downloaded.
        /// </summary>
        public bool Development { get; set; }

        /// <summary>
        /// Gets or sets update Url.
        /// </summary>
        public string UpdateUrl { get; set; }

        /// <summary>
        /// Gets or sets token.
        /// </summary>
        public string Token { get; set; }

        internal PluginManifest(IAutoUpdateablePlugin p)
        {
            var plugin = p as IPlugin<IConfig>;
            var config = p.AutoUpdateConfig;

            this.PluginName = plugin.GetPluginName();
            this.SourceType = config.Type;
            this.UpdateUrl = config.Url;
        }

        internal PluginManifest()
        {
        }

        internal void UpdatePlugin(Manifest manifest)
        {
            this.CurrentVersion = manifest.Version;
            this.CurrentBuildId = manifest.Version;
            this.UpdateTime = DateTime.Now;
        }

        internal void UpdatePlugin(IRelease<IAsset, ICommit> release)
        {
            this.CurrentVersion = release.Tag;
            this.CurrentBuildId = $"{release.Tag}-release-{release.Commit.ShortId}";
            this.UpdateTime = DateTime.Now;
        }

        internal void UpdatePlugin(string tag, string shortId, string branch)
        {
            this.CurrentVersion = shortId;
            this.CurrentBuildId = $"{tag}-{branch}-{shortId}";
            this.UpdateTime = DateTime.Now;
        }
    }
}