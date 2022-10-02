// -----------------------------------------------------------------------
// <copyright file="IAutoUpdateablePlugin.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Interfaces;

namespace Mistaken.Updater.API.Config
{
    /// <summary>
    /// <see cref="IConfig"/> but with fields required for AutoUpdates.
    /// </summary>
    public interface IAutoUpdateablePlugin
    {
        /// <summary>
        /// Gets config values used by AutoUpdater.
        /// </summary>
        AutoUpdateConfig AutoUpdateConfig { get; }
    }
}
