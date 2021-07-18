﻿// -----------------------------------------------------------------------
// <copyright file="AutoUpdateConfig.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;

namespace Mistaken.Updater.Config
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
        /// </summary>
        [Description("Url used for auto updating")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets Auto Update type.
        /// </summary>
        [Description("Auto Update type, can be any of [GITLAB, GITHUB, GITLAB_DEVELOPMENT, GITHUB_DEVELOPMENT]")]
        public AutoUpdateType Type { get; set; }

        /// <summary>
        /// Gets or sets token used for authorization.
        /// </summary>
        [Description("Token used for authorization")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether auto updater's debugs should be visible.
        /// </summary>
        [Description("Should auto updater's debugs be displayed")]
        public bool VerbouseOutput { get; set; }
    }
}
