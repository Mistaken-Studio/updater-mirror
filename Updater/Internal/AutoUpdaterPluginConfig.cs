// -----------------------------------------------------------------------
// <copyright file="AutoUpdaterPluginConfig.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Mistaken.Updater.Internal
{
    /// <inheritdoc/>
    public class AutoUpdaterPluginConfig : IConfig
    {
        /// <inheritdoc/>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether verbose output be displayed.
        /// </summary>
        [Description("Value indicating whether verbose output be displayed")]
        public bool VerboseOutput { get; set; } = false;
    }
}
