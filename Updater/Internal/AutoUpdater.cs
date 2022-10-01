// -----------------------------------------------------------------------
// <copyright file="AutoUpdater.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Mistaken.Updater.API.Config;

namespace Mistaken.Updater.Internal
{
    /// <inheritdoc cref="IPlugin{TConfig}"/>
    internal class AutoUpdater : Plugin<AutoUpdaterPluginConfig>, IAutoUpdateablePlugin
    {
        /// <inheritdoc/>
        public override string Name => "Updater";

        /// <inheritdoc/>
        public override string Author => "Mistaken Devs";

        /// <inheritdoc/>
        public override PluginPriority Priority => PluginPriority.Last;

        /// <inheritdoc/>
        public override Version RequiredExiledVersion => new Version(5, 2, 0);

        /// <inheritdoc/>
        public override string Prefix => "MUPDATER";

        /// <inheritdoc/>
        public AutoUpdateConfig AutoUpdateConfig => new AutoUpdateConfig
        {
            Type = SourceType.GITLAB,
            Url = "https://git.mistaken.pl/api/v4/projects/8",
        };

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

            Exiled.Events.Handlers.Server.WaitingForPlayers += Server_WaitingForPlayers;

            MEC.Timing.CallDelayed(60, () => Exiled.Events.Handlers.Server.RestartingRound += Server_RestartingRound);

            Updater.AutoUpdater.Initialize();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RestartingRound -= Server_RestartingRound;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= Server_WaitingForPlayers;
        }

        internal static AutoUpdater Instance { get; private set; }

        private static bool ignoreRestartingRound;

        private static void Server_RestartingRound()
        {
            if (ignoreRestartingRound)
                return;

            if (ServerStatic.StopNextRound != ServerStatic.NextRoundAction.DoNothing)
                return;

            MEC.Timing.CallDelayed(5f, () =>
            {
                if (Updater.AutoUpdater.DoAutoUpdates())
                    ignoreRestartingRound = true;
            });
        }

        private static void Server_WaitingForPlayers()
        {
            ignoreRestartingRound = false;
        }
    }
}
