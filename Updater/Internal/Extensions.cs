// -----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Interfaces;

namespace Mistaken.Updater.Internal
{
    internal static class Extensions
    {
        public static string GetPluginName(this IPlugin<IConfig> plugin)
        {
            return $"{plugin.Author.Replace(' ', '_')}/{plugin.Name.Replace(' ', '_')}";
        }
    }
}
