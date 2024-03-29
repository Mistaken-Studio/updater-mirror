﻿// -----------------------------------------------------------------------
// <copyright file="AutoUpdateType.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// ReSharper disable once CheckNamespace
namespace Mistaken.Updater.Config
{
    /// <summary>
    /// Auto Update Type.
    /// </summary>
    [System.Obsolete("Removed in V2")]
    public enum AutoUpdateType
    {
        /// <summary>
        /// Auto update is disabled
        /// </summary>
        DISABLED,

        /// <summary>
        /// Download update from GitLab
        /// </summary>
        GITLAB,

        /// <summary>
        /// Download update from GitHub
        /// </summary>
        GITHUB,

        /// <summary>
        /// Download update from GitLab using artifacts
        /// </summary>
        GITLAB_DEVELOPMENT,

        /// <summary>
        /// Download update from GitHub using artifacts
        /// </summary>
        GITHUB_DEVELOPMENT,

        /// <summary>
        /// Download update from HTTP Server
        /// </summary>
        HTTP,
    }
}
