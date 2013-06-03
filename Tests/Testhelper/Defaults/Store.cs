using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Dataspace.Common;

namespace Indusoft.Test.MockresourceProviders
{

    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Store
    {
        public  static bool WasGettings = false;

        public static TimeSpan GetDelay = TimeSpan.FromSeconds(0.0005);

        private const bool _useInheritance = true;

        internal Dictionary<Type, Dictionary<Guid, object>> _caches = new Dictionary<Type, Dictionary<Guid, object>>();

        public Dictionary<Guid, object> this[Type type]
        {
            get { return _caches[type]; }
        }

        public void AddResource(Type name, Guid id, object obj)
        {
            Dictionary<Guid, object> cache;
            if (_useInheritance)
            foreach (var bas in name.Construct(k => k != typeof(object), k => k.BaseType))
            {
                cache = _caches[bas];
                lock (cache)
                {                 
                    if (cache.ContainsKey(id))
                        cache[id] = obj;
                    else cache.Add(id, obj);
                }
            }
            else
            {
                cache = _caches[name];
                if (cache.ContainsKey(id))
                    cache[id] = obj;
                else cache.Add(id, obj);
            }
           
        }

        public object GetResource(Type name, Guid id)
        {
            WasGettings = true;
            Thread.Sleep(GetDelay);
            var cache = _caches[name];
            lock (cache)
            {
                if (cache.ContainsKey(id))
                    return cache[id];
                else return null;
            }
        }

        public void DeleteResource(Type name, Guid id)
        {
            Dictionary<Guid, object> cache;
            if (_useInheritance)
            foreach (var bas in _caches.Keys.Where(k=>k.IsAssignableFrom(name) || name.IsAssignableFrom(k) || (k==name)))
            {
                cache = _caches[bas];
                lock (cache)
                {
                    if (cache.ContainsKey(id))
                        cache.Remove(id);
                }
            }
            else
            {
                cache = _caches[name];
                if (cache.ContainsKey(id))
                    cache.Remove(id);
            }          
        }

        public void AddName(Type name)
        {
            Dictionary<Guid, object> cache;
            if (_useInheritance)
            foreach (var bas in name.Construct(k => k != typeof(object), k => k.BaseType))
            {
                if (!_caches.ContainsKey(bas))
                    _caches.Add(bas, new Dictionary<Guid, object>());
            }
            else
            {
                if (!_caches.ContainsKey(name))
                  _caches.Add(name, new Dictionary<Guid, object>());
            }
        }
    }
}
