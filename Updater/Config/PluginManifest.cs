// -----------------------------------------------------------------------
// <copyright file="PluginManifest.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Exiled.API.Interfaces;
using Mistaken.Updater.API;
using Mistaken.Updater.API.Abstract;
using Mistaken.Updater.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Mistaken.Updater.Config
{
    /// <summary>
    /// Plugin Manifest.
    /// </summary>
    public class PluginManifest
    {
        /// <summary>
        /// Gets Plugin Name.
        /// </summary>
        public string PluginName { get; internal set; }

        /// <summary>
        /// Gets Current Version.
        /// </summary>
        public string CurrentVersion { get; private set; }

        /// <summary>
        /// Gets Current Build Id.
        /// </summary>
        public string CurrentBuildId { get; internal set; }

        /// <summary>
        /// Gets Update Time.
        /// </summary>
        public DateTime? UpdateTime { get; private set; }

        /// <summary>
        /// Gets Source Type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SourceType SourceType { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether development version should be downloaded.
        /// </summary>
        public bool Development { get; private set; }

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

        internal string UpdateUrl { get; set; }

        internal string Token { get; set; }

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