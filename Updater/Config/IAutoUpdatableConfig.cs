// -----------------------------------------------------------------------
// <copyright file="IAutoUpdatableConfig.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Mistaken.Updater.Config
{
    /// <summary>
    /// <see cref="IConfig"/> but with fields requied for AutoUpdates.
    /// </summary>
    public interface IAutoUpdatableConfig : IConfig
    {
        /// <summary>
        /// Gets or sets config values used by AutoUpdater.
        /// </summary>
        [Description("Auto Update Settings")]
        AutoUpdateConfig AutoUpdateConfig { get; set; }
    }
}
