using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Indusoft.Common.ResourceLayer.Interfaces.Internal;

namespace Indusoft.Common.ResourceLayer.Services
{
    internal class ProxyBuilder
    {
        private AssemblyBuilder _builder;

        private ModuleBuilder _module;

        public Type CreateProxyForType(Type t)
        {
            var type = _module.DefineType(t.Name + "Resource_Proxy",TypeAttributes.Sealed| TypeAttributes.Class|TypeAttributes.NestedAssembly,t,new[]{typeof(IProxyWithLazyItem)});
            type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] {typeof (Lazy<object>)});
            return type;
        }

        public ProxyBuilder()
        {
            _builder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("ResourceProxy"), AssemblyBuilderAccess.Run);
            _module = _builder.DefineDynamicModule("Proxies");

        }
    }
}
