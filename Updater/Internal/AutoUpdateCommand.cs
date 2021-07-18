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
using Exiled.API.Interfaces;
using Mistaken.Updater.Config;

namespace Mistaken.Updater.Internal
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

            var plugin = Exiled.Loader.Loader.Plugins.Where(x => x.Config is IAutoUpdatableConfig).Select(x => x as IPlugin<IAutoUpdatableConfig>).FirstOrDefault(x => x.Name.ToLower() == pluginName || x.Prefix.ToLower() == pluginName);

            if (plugin == null)
            {
                response = $"Plugin with name \"{pluginName}\" not found";
                return false;
            }

            if (AutoUpdater.Instance.DoAutoUpdate(plugin, false))
                Exiled.API.Features.Server.Restart();

            response = $"Updated {plugin.Author}.{plugin.Name}";
            return true;
        }
    }
}
