using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;

using Resources.Test.TestResources;

namespace Resources.Security.Test.SecurityResources
{

    [Export(typeof(ResourcePoster))]
    internal class SecurityGroupPoster:ResourcePoster<SecurityGroup>
    {
#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;

        [Import] 
        private ISecurityManager _updater;
#pragma warning restore 0649
        protected override void WriteResourceTyped(Guid key, SecurityGroup resource)
        {
            if (_pool.SecurityGroups.ContainsKey(key))
                _pool.SecurityGroups[key] = resource;
            else
                _pool.SecurityGroups.Add(key, resource);
            _updater.UpdateSecurity();            
        }

        protected override void DeleteResourceTyped(Guid key)
        {
            if (_pool.SecurityGroups.ContainsKey(key))
                _pool.SecurityGroups.Remove(key);
            _updater.UpdateSecurity();            
        }
    }
}
