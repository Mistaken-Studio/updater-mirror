using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mistaken.Updater.API
{
    public class Manifest
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("plugin_name")]
        public string PluginName {  get; set; }
    }
}
