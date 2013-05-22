using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.Data;

namespace Dataspace.Common.Services
{

    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SettingsHolder
    {
        public Settings Settings = new Settings();

        #region Imports

#pragma warning disable 0649
        [Import]
        internal AppConfigProvider Provider;
#pragma warning restore 0649

        #endregion
    }
}
