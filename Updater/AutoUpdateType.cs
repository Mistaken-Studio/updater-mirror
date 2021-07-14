// -----------------------------------------------------------------------
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
        /// Download update using GIT, Login and Password
        /// </summary>
        LOGIN,
    }
}
