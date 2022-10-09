// -----------------------------------------------------------------------
// <copyright file="PluginDependency.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mistaken.Updater.API
{
    /// <summary>
    /// Plugin Dependency.
    /// </summary>
    public struct PluginDependency
    {
        /// <summary>
        /// Gets or sets file Name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is Plugin.
        /// </summary>
        public bool IsPlugin { get; set; }

        /// <summary>
        /// Gets or sets download Url.
        /// </summary>
        public string DownloadUrl { get; set; }
    }
}