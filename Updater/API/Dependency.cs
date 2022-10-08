// -----------------------------------------------------------------------
// <copyright file="Dependency.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace Mistaken.Updater.API
{
    internal struct Dependency
    {
        public string FileName { get; set; }

        public string DownloadUrl { get; set; }

        public List<string> RequiredBy { get; set; }
    }
}