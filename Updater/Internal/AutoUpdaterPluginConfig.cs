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
        public Dictionary<string, string> AutoUpdateConfig { get; set; } = new Dictionary<string, string>
        {
            { "Url", "https://git.mistaken.pl/api/v4/projects/8" },
            { "Token", string.Empty },
            { "Type", "GITLAB" },
            { "VerbouseOutput", "false" },
        };

        /// <inheritdoc/>
        public bool IsEnabled { get; set; } = true;
    }
}
