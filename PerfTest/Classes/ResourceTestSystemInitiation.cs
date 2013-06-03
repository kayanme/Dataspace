using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Data;
using Dataspace.Common.Interfaces;
using Dataspace.Loggers;
using Indusoft.Test.MockresourceProviders;

namespace PerfTest.Classes
{
    public class ResourceTestSystemInitiation
    {
        public Assembly CreateResources(int count)
        {
            var b = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TestResources"),
                                                                      AssemblyBuilderAccess.RunAndSave);
            var resModule = b.DefineDynamicModule("Resources");
            var rand = new Random();

            var register = resModule.DefineType("Registrator", TypeAttributes.Public);
            register.SetParent(typeof(ResourceRegistrator));
            var exportAttr = new CustomAttributeBuilder(typeof(ExportAttribute).GetConstructor(new[] { typeof(Type) }),
                                                         new object[] { typeof(ResourceRegistrator) });
            register.SetCustomAttribute(exportAttr);

            var meth = register.DefineMethod("get_ResourceTypes", MethodAttributes.Virtual | MethodAttributes.Public,
                                  typeof(Type[]), Type.EmptyTypes);
            var gen = meth.GetILGenerator();
            gen.Emit(OpCodes.Ldc_I4, count);
            gen.Emit(OpCodes.Newarr, typeof(Type));

            for(var i = 0;i<count;i++)
            {
                
                var type = resModule.DefineType("Res"+i,TypeAttributes.Public|TypeAttributes.Serializable|TypeAttributes.Class);
                type.DefineDefaultConstructor(MethodAttributes.Public);                
                var resAttr = new CustomAttributeBuilder(typeof (ResourceAttribute).GetConstructors()[0],
                                                         new object[] {"Res" + i});
                type.SetCustomAttribute(resAttr);
                var cacheAttr = new CustomAttributeBuilder(typeof (SimpleCachingAttribute).GetConstructors()[0],
                                                           new object[0]);
                type.SetCustomAttribute(cacheAttr);
               
                if (i % 4 == 0 && i!=0)
                {                                     
                    type.SetParent(resModule.GetType("Res"+i/4));
                }
                else
                {
                    type.SetParent(typeof(ResBase));
                }
                
                var t2 = type.CreateType();
                gen.Emit(OpCodes.Dup);
                gen.Emit(OpCodes.Ldc_I4, i);
                gen.Emit(OpCodes.Ldtoken,t2);
                gen.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), Type.EmptyTypes);
                gen.Emit(OpCodes.Stelem_Ref);
            }
            gen.Emit(OpCodes.Ret);
            register.CreateType();
            return b;
        }

        
    }
}
