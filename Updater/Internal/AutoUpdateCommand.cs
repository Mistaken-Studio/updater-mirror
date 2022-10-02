// -----------------------------------------------------------------------
// <copyright file="AutoUpdateCommand.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using Exiled.API.Features;
using RoundRestarting;

namespace Mistaken.Updater.Internal
{
    /// <inheritdoc/>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    internal class AutoUpdateCommand : ICommand
    {
        /// <inheritdoc/>
        // ReSharper disable once StringLiteralTypo
        public string Command => "autoupdate";

        /// <inheritdoc/>
        public string[] Aliases => new[] { "update" };

        /// <inheritdoc/>
        public string Description => "Forces Auto Update";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
                return false;

            if (AutoUpdater.DoAutoUpdates())
            {
                Mirror.NetworkServer.SendToAll(new RoundRestartMessage(
                    RoundRestartType.FullRestart,
                    GameCore.ConfigFile.ServerConfig.GetInt("full_restart_rejoin_time", 25),
                    0,
                    true,
                    true));
                IdleMode.PauseIdleMode = true;
                MEC.Timing.CallDelayed(1, Server.Restart);
                response = "Restarting";
                return true;
            }
            else
            {
                response = "No need to restart";
                return false;
            }
        }
    }
}
