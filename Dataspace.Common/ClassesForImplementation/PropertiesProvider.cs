using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Dataspace.Common.Attributes;
using Dataspace.Common.Interfaces;

namespace Dataspace.Common.ClassesForImplementation
{
    public abstract class PropertiesProvider
    {
        internal delegate object PropertyFiller(Guid? id,string name);

        [Import]
        protected ITypedPool Cachier;
        
        private bool IsValidFiller(MethodInfo info)
        {
            return    info.GetParameters().Count() == 2
                   && info.GetParameters()[0].ParameterType == typeof (Guid?)
                   && info.GetParameters()[1].ParameterType == typeof(string)
                   && info.ReturnType == typeof (object);
        }

        internal IDictionary<string,PropertyFiller> CollectPropertiesFillers()
        {
            var targetMethods = GetType().GetMethods()
                .Where(k => k.GetCustomAttributes(typeof (ResourceAttribute), false).Any());

            Debug.Assert(targetMethods.All(IsValidFiller),"Описатель ресурса имеет неверную сигнатуру (требуется Guid?->string->object)");
            if (!targetMethods.All(IsValidFiller))
                throw new InvalidOperationException("Описатель ресурса имеет неверную сигнатуру (требуется Guid?->string->object)");

            var fillers = targetMethods.Select(k =>
                                               new
                                                   {
                                                       names = k.GetCustomAttributes(typeof (ResourceAttribute), false).OfType<ResourceAttribute>().Select(k2=>k2.Name),
                                                       filler = new PropertyFiller((id,s) => k.Invoke(this, new object[] {id,s}))
                                                   })
                                       .SelectMany(k => k.names.Select(k2 => new {name = k2, k.filler}));

            try
            {
                return fillers.ToDictionary(k=>k.name,k=>k.filler);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("Нельзя описывать один ресурс два раза в одном неймспейсе");                
            }
        }
    }
}
