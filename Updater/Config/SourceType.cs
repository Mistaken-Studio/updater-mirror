// -----------------------------------------------------------------------
// <copyright file="SourceType.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mistaken.Updater.Config
{
    /// <summary>
    /// Source Type.
    /// </summary>
    public enum SourceType
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
        /// Download update from HTTP Server
        /// </summary>
        HTTP,
    }
}
