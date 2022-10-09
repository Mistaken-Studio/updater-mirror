// -----------------------------------------------------------------------
// <copyright file="IManifest.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mistaken.Updater.API.Manifests
{
    internal interface IManifest
    {
        string PluginName { get; }

        string Version { get; }
    }
}