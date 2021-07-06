using Exiled.API.Features;
using Exiled.API.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Mistaken.API
{
    public abstract class AutoUpdatablePlugin<TConfig> : Plugin<TConfig> where TConfig : IAutoUpdatableConfig, new()
    {
        public string NewestVersion;
        public override void OnEnabled()
        {
            var path = Path.Combine(Paths.Configs, "AutoUpdater");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, $"{this.Author}.{this.Name}.txt");
            if (!File.Exists(path))
                AutoUpdate(true);
            else
            {
                NewestVersion = File.ReadAllText(path);
                Exiled.Events.Handlers.Server.RestartingRound += Server_RestartingRound;
                AutoUpdate(false);
            }
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RestartingRound -= Server_RestartingRound;
            base.OnDisabled();
        }

        private void AutoUpdate(bool force)
        {
            switch (this.Config.AutoUpdateType)
            {
                case AutoUpdateType.GITHUB:
                    break;
                case AutoUpdateType.GITLAB:
                    using (var client = new WebClient())
                    {
                        try
                        {
                            client.Headers.Add("PRIVATE-TOKEN", this.Config.AutoUpdateToken);
                            var rawResult = client.DownloadString(this.Config.AutoUpdateURL);
                            if(rawResult == "")
                            {
                                return;
                            }
                            var decoded = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic[]>(rawResult);
                            if(decoded.Length == 0)
                            {
                                return;
                            }
                            var result = decoded[0];
                            var releaseUrl = result.assets.sources[0].url;
                        }
                        catch(System.Exception ex)
                        {

                        }
                    }
                    break;
                case AutoUpdateType.LOGIN:
                    break;
            }
        }

        private void Server_RestartingRound()
        {
            AutoUpdate(false);
        }
    }

    public interface IAutoUpdatableConfig : IConfig
    {
        [Description("")]
        string AutoUpdateURL { get; set; }
        [Description("")]
        AutoUpdateType AutoUpdateType { get; set; }
        [Description("")]
        string AutoUpdateLogin { get; set; }
        [Description("")]
        string AutoUpdateToken { get; set; }
    }

    public enum AutoUpdateType
    {
        GITLAB,
        GITHUB,
        LOGIN
    }
}
