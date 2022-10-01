// -----------------------------------------------------------------------
// <copyright file="ICommit.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mistaken.Updater.API.Abstract
{
    internal interface ICommit
    {
        string ShortId { get; }
    }
}