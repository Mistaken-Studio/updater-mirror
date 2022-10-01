// -----------------------------------------------------------------------
// <copyright file="IAutoUpdatableConfig.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Mistaken.Updater.Config
{
    /// <summary>
    /// <see cref="IConfig"/> but with fields requied for AutoUpdates.
    /// </summary>
    [System.Obsolete("Removed in V2")]
    public interface IAutoUpdatableConfig : IConfig
    {
        /// <summary>
        /// Gets or sets config values used by AutoUpdater.
        /// </summary>
        [Description("Auto Update Settings")]
        Dictionary<string, string> AutoUpdateConfig { get; set; }
    }
}
