// -----------------------------------------------------------------------
// <copyright file="UninstallCommand.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using JetBrains.Annotations;

namespace Mistaken.Updater.Internal.Commands.SubCommands
{
    /// <inheritdoc/>
    [UsedImplicitly]
    [CommandHandler(typeof(UpdaterParentCommand))]
    internal class UninstallCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "uninstall";

        /// <inheritdoc/>
        public string[] Aliases => new[] { "remove" };

        /// <inheritdoc/>
        public string Description => "Uninstalls a plugin";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var pluginName = arguments.Array![arguments.Offset];

            return AutoUpdater.UninstallPlugin(pluginName, out response);
        }
    }
}
