// -----------------------------------------------------------------------
// <copyright file="AutoUpdaterPluginConfig.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Mistaken.Updater.Config;
using Newtonsoft.Json;

namespace Mistaken.Updater.Internal
{
    /// <inheritdoc/>
    public class AutoUpdaterPluginConfig : IAutoUpdatableConfig
    {
        /// <inheritdoc/>
        public AutoUpdateConfig AutoUpdateConfig { get; set; }

        /// <inheritdoc/>
        public bool IsEnabled { get; set; }
    }
}
