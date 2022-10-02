// -----------------------------------------------------------------------
// <copyright file="IAsset.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mistaken.Updater.API.Abstract
{
    internal interface IAsset
    {
        string Url { get; }

        string Name { get; }
    }
}