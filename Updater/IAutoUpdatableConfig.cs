// -----------------------------------------------------------------------
// <copyright file="IAutoUpdatableConfig.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Mistaken.API
{
    /// <summary>
    /// <see cref="IConfig"/> but with fields requied for AutoUpdates.
    /// </summary>
    public interface IAutoUpdatableConfig : IConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether debug should be displayed in console or not.
        /// </summary>
        [Description("If debug should be displayed")]
        bool AutoUpdateVerbouseOutput { get; set; }

        /// <summary>
        /// Gets or sets url used for auto updating.
        /// </summary>
        [Description("Url used for auto updating")]
        string AutoUpdateURL { get; set; }

        /// <summary>
        /// Gets or sets Auto Update type.
        /// </summary>
        [Description("Auto Update type")]
        AutoUpdateType AutoUpdateType { get; set; }

        /// <summary>
        /// Gets or sets login, used only when <see cref="AutoUpdateType"/> is <see cref="AutoUpdateType.LOGIN"/>.
        /// </summary>
        [Description("Login, used only when AutoUpdateType is LOGIN")]
        string AutoUpdateLogin { get; set; }

        /// <summary>
        /// Gets or sets token used for authorization.
        /// </summary>
        [Description("Token used for authorization")]
        string AutoUpdateToken { get; set; }
    }
}
