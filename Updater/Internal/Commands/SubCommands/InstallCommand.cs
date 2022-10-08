// -----------------------------------------------------------------------
// <copyright file="InstallCommand.cs" company="Mistaken">
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
    internal class InstallCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "install";

        /// <inheritdoc/>
        public string[] Aliases => new[] { "i" };

        /// <inheritdoc/>
        public string Description => "Installs a plugin";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var downloadUrl = arguments.Array![arguments.Offset];
            string token = null;
            if (arguments.Count > 1)
                token = arguments.Array[arguments.Offset + 1];

            response = AutoUpdater.InstallPluginFromManifest(downloadUrl, token);

            if (response is not null)
                return false;

            response = "Downloaded successfully";
            return true;
        }
    }
}
