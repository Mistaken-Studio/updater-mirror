// -----------------------------------------------------------------------
// <copyright file="IImplementation.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Net;

namespace Mistaken.Updater.API.Abstract
{
    internal interface IImplementation
    {
        Type ReleaseType { get; }

        string UrlSuffix { get; }

        void AddHeaders(WebClient client, PluginManifest pluginManifest);

        AutoUpdater.Action? DownloadArtifact(PluginManifest pluginManifest, bool force);
    }
}