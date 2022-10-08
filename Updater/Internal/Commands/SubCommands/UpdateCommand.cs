// -----------------------------------------------------------------------
// <copyright file="UpdateCommand.cs" company="Mistaken">
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
    internal class UpdateCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "update";

        /// <inheritdoc/>
        public string[] Aliases => Array.Empty<string>();

        /// <inheritdoc/>
        public string Description => "Forces Auto Update";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (AutoUpdater.DoAutoUpdates())
            {
                AutoUpdater.RestartServer();
                response = "Restarting";
                return true;
            }

            response = "No need to restart";
            return false;
        }
    }
}
