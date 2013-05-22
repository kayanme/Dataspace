using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Services
{
    internal abstract class AppConfigProvider
    {
        public abstract bool ContainsKey(string key);

        public abstract string GetValue(string key);
    }

    [Export(typeof(AppConfigProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class RealAppConfigProvider : AppConfigProvider
    {
        public override bool ContainsKey(string key)
        {
            return ConfigurationManager.AppSettings.AllKeys.Contains(key);
        }

        public override string GetValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
