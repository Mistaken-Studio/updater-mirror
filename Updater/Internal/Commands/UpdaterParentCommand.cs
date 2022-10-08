// -----------------------------------------------------------------------
// <copyright file="UpdaterParentCommand.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using JetBrains.Annotations;
using Mistaken.Updater.Internal.Commands.SubCommands;

namespace Mistaken.Updater.Internal.Commands
{
    /// <inheritdoc/>
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [UsedImplicitly]
    internal class UpdaterParentCommand : ParentCommand
    {
        public UpdaterParentCommand()
        {
            this.LoadGeneratedCommands();
        }

        /// <inheritdoc/>
        public override string Command => "updater";

        public override string[] Aliases => Array.Empty<string>();

        /// <inheritdoc/>
        public override string Description => "Updater manager";

        public sealed override void LoadGeneratedCommands()
        {
            this.RegisterCommand(new UpdateCommand());
            this.RegisterCommand(new InstallCommand());
            this.RegisterCommand(new UninstallCommand());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "updater update/...";
            return false;
        }
    }
}
