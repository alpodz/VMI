using DB.Admin;
using Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Core.Core
{
    public class ConfigHelper: IConfigHelper
    {
        public Dictionary<String,String> Values = new Dictionary<String,String>();

        public ConfigHelper(Dictionary<String, Base> cache)
        {
            foreach(Configuration config in cache.Values.Cast<Configuration>())
                Values.Add(config.Name, config.Value);
        }

        Dictionary<string, string> IConfigHelper.Values { get { return Values; } }

        public string HasRequiredValues( String[] RequiredKeys)
        {
            var SystemMessage = String.Empty;
            foreach (var Key in RequiredKeys)
            {
                if (!Values.ContainsKey(Key))
                    SystemMessage += $"{Key} + is missing from Configuration. <BR>";
            }
            return SystemMessage;
        }
    }
}
