// -----------------------------------------------------------------------
// <copyright file="IRelease.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mistaken.Updater.API.Abstract
{
    internal interface IRelease<out TAsset, out TCommit>
        where TAsset : IAsset
        where TCommit : ICommit
    {
        string Tag { get; }

        TAsset[] Assets { get; }

        TCommit Commit { get; }
    }
}