// -----------------------------------------------------------------------
// <copyright file="AutoUpdateConfig.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;

namespace Mistaken.Updater.API.Config
{
    /// <summary>
    /// Struct used for storing auto updater config values.
    /// </summary>
    public struct AutoUpdateConfig
    {
        /// <summary>
        /// Gets or sets url used for auto updating.
        /// For GitLab it should be something like "https://gitlab.example.com/api/v4/projects/1/".
        /// For GitHub it should be something like "https://api.github.com/repos/example/repo/".
        /// For HTTP is should be something like "https://example.com/plugins/pluginname" and it should contain two files:
        ///     manifest.json with latest version of plugin and plugin dll name with .dll.
        ///         Example manifest.json content:
        ///         {
        ///             "version": "1.0.0",
        ///             "plugin_name": "MyPlugin.dll"
        ///         }
        ///     Plugin file with the same name as stated in "plugin_name" in manifest.json.
        /// </summary>
        [Description("Url used for auto updating")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets Auto Update type.
        /// </summary>
        [Description("Auto Update type, can be any of [GITLAB, GITHUB, GITLAB_DEVELOPMENT, GITHUB_DEVELOPMENT, HTTP]")]
        public SourceType Type { get; set; }
    }
}
