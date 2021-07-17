// -----------------------------------------------------------------------
// <copyright file="AutoUpdateCommand.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandSystem;

namespace Mistaken.API
{
    /// <inheritdoc/>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class AutoUpdateCommand : ICommand
    {
        /// <inheritdoc/>
        public string Command => "autoupdate";

        /// <inheritdoc/>
        public string[] Aliases => new string[0];

        /// <inheritdoc/>
        public string Description => "Forces Auto Update";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
                return false;

            if (arguments.Count == 0)
            {
                response = "Missing argument \"plugin\"\nAutoUpdate [plugin]";
                return false;
            }

            string pluginName = arguments.Array[0].ToLower();

            var plugin = AutoUpdate.Instances.Find(x => x.Plugin.Name.ToLower() == pluginName || x.Plugin.Prefix.ToLower() == pluginName);

            if (plugin == null)
            {
                response = $"Plugin with name \"{pluginName}\" not found";
                return false;
            }

            if (plugin.DoAutoUpdate(false))
                Exiled.API.Features.Server.Restart();

            response = $"Updated {plugin.Plugin.Author}.{plugin.Plugin.Name} to version {plugin.CurrentVersion}";
            return true;
        }
    }
}
