using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;

using Resources.Test.TestResources;
using Resources.Test.TestResources.SecurityResource;

namespace Resources.Security.Test.SecurityResources
{
    
    [Export(typeof(ResourcePoster))]
    internal class SecurityPermissionsPoster:ResourcePoster<SecurityPermissions>
    {
#pragma warning disable 0649
        [Import]
        private ResourcePool _pool;

        [Import] 
        private ISecurityManager _updater;
#pragma warning restore 0649
        protected override void WriteResourceTyped(Guid key, SecurityPermissions resource)
        {
            if (_pool.Elements.ContainsKey(key))
                _pool.SecurityPermissions[key] = resource;
            else
                _pool.SecurityPermissions.Add(key, resource);
            _updater.UpdateSecurity(typeof(Model),key);
        }

        protected override void DeleteResourceTyped(Guid key)
        {
            if (_pool.SecurityPermissions.ContainsKey(key))
                _pool.SecurityPermissions.Remove(key);
            _updater.UpdateSecurity(typeof(Model),key);
        }
    }
}
