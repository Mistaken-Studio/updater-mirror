// -----------------------------------------------------------------------
// <copyright file="AutoUpdateConfig.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Exiled.API.Features;

namespace Mistaken.Updater.Config
{
    /// <summary>
    /// Struct used for storing auto updater config values.
    /// </summary>
    public struct AutoUpdateConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoUpdateConfig"/> struct.
        /// </summary>
        /// <param name="input">Input data.</param>
        public AutoUpdateConfig(Dictionary<string, string> input)
        {
            if (input == null)
            {
                var configs = Exiled.Loader.ConfigManager.LoadSorted(Exiled.Loader.ConfigManager.Read());
                foreach (var item in configs.ToArray())
                {
                    if (!(item.Value is IAutoUpdatableConfig config))
                        continue;
                    if (config.AutoUpdateConfig == null)
                    {
                        config.AutoUpdateConfig = new Dictionary<string, string>()
                        {
                            { "Url", null },
                            { "Token", null },
                            { "Type", "GITHUB" },
                            { "VerbouseOutput", "false" },
                        };
                        configs[item.Key] = config;
                        Log.Info($"Updated {item.Key}'s AutoUpdate config");
                    }
                }

                Exiled.Loader.ConfigManager.Save(configs);
                Exiled.Loader.ConfigManager.Reload();

                input = new Dictionary<string, string>()
                {
                    { "Url", null },
                    { "Token", null },
                    { "Type", "GITHUB" },
                    { "VerbouseOutput", "false" },
                };
            }

            if (input.TryGetValue("Url", out string url))
                this.Url = url;
            else
                this.Url = null;
            if (input.TryGetValue("Token", out string token))
                this.Token = token;
            else
                this.Token = null;
            if (input.TryGetValue("VerbouseOutput", out string verbouseOutput) && bool.TryParse(verbouseOutput, out bool vo))
                this.VerbouseOutput = vo;
            else
                this.VerbouseOutput = false;

            this.Type = AutoUpdateType.GITHUB;
            if (input.TryGetValue("Type", out string type))
            {
                var names = Enum.GetNames(typeof(AutoUpdateType));
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i] == type)
                    {
                        this.Type = (AutoUpdateType)i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets url used for auto updating.
        /// For GitLab it should be something like "https://gitlab.example.com/api/v4/projects/1/".
        /// For GitHub it should be something like "https://api.github.com/repos/example/repo/".
        /// For HTTP is should be something like "https://example.com/plugins/pluginname" and it should contain two files: 
        ///     manifest.json with latest version of plugin and plugin dll name with .dll.
        ///         Example manifest.json content:
        ///         {
        ///             "version": "1.0.0",
        ///             "plugin_name": "MyPlugin.dll"
        ///         }
        ///     Plugin file with the same name as stated in "plugin_name" in manifest.json.
        /// </summary>
        [Description("Url used for auto updating")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets Auto Update type.
        /// </summary>
        [Description("Auto Update type, can be any of [GITLAB, GITHUB, GITLAB_DEVELOPMENT, GITHUB_DEVELOPMENT, HTTP]")]
        public AutoUpdateType Type { get; set; }

        /// <summary>
        /// Gets or sets token used for authorization.
        /// </summary>
        [Description("Token used for authorization")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether auto updater's debugs should be visible.
        /// </summary>
        [Description("Should auto updater's debugs be displayed")]
        public bool VerbouseOutput { get; set; }
    }
}
