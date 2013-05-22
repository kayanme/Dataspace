using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Indusoft.Common.Utility.Dictionary;

namespace Indusoft.Common.TransientCachier.Scopes
{
    public class NoCachingScope:ResourceCollecterScope,IDisposable
    {
        private static ResourceHolder CreateHolder()
        {
            return new ResourceHolder();
        }

        [ThreadStatic]
        private static bool _isActive;

    

        private ResourceHolder _myHolder;

        public static bool IsActive
        {
            get { return _isActive; }
        }

        public NoCachingScope()
            : base(CreateHolder())
        {
            _myHolder = Holder;
            _isActive = true;
        }

        void IDisposable.Dispose()
        {            
            base.Dispose();
            _myHolder.Dispose();
            _isActive = false;
        }
    }
}
