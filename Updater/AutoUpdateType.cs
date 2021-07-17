﻿// -----------------------------------------------------------------------
// <copyright file="AutoUpdateType.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mistaken.API
{
    /// <summary>
    /// Auto Update Type.
    /// </summary>
    public enum AutoUpdateType
    {
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
    }
}
