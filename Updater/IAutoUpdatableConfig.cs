// -----------------------------------------------------------------------
// <copyright file="IAutoUpdatableConfig.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Mistaken.API
{
    /// <summary>
    /// <see cref="IConfig"/> but with fields requied for AutoUpdates.
    /// </summary>
    public interface IAutoUpdatableConfig : IConfig
    {
        /// <summary>
        /// Gets or sets url used for auto updating.
        /// For GitLab it should be something like "https://gitlab.example.com/api/v4/projects/1/".
        /// For GitHub it should be something like "https://api.github.com/repos/example/repo/".
        /// </summary>
        [Description("Url used for auto updating")]
        string AutoUpdateUrl { get; set; }

        /// <summary>
        /// Gets or sets Auto Update type.
        /// </summary>
        [Description("Auto Update type, can be any of [GITLAB, GITHUB, GITLAB_DEVELOPMENT, GITHUB_DEVELOPMENT]")]
        AutoUpdateType AutoUpdateType { get; set; }

        /// <summary>
        /// Gets or sets token used for authorization.
        /// </summary>
        [Description("Token used for authorization")]
        string AutoUpdateToken { get; set; }
    }
}
