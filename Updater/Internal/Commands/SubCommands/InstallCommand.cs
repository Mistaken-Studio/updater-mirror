// -----------------------------------------------------------------------
// <copyright file="InstallCommand.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using CommandSystem;
using Exiled.API.Features;
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

            Task.Run(
                async () =>
                {
                    var (result, message) = await AutoUpdater.InstallPluginFromManifest(downloadUrl, token);

                    Log.Info($"[{result}] {message}");
                });

            response = "Downloading ...";
            return true;
        }
    }
}
