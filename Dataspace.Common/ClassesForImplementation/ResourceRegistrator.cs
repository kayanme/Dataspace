using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dataspace.Common.Attributes;
using Dataspace.Common.Attributes.CachingPolicies;
using Dataspace.Common.ServiceResources;

namespace Dataspace.Common.ClassesForImplementation
{
    /// <summary>
    /// Провайдер типов для регистрации в качестве ресурсов.
    /// </summary>
    public abstract class ResourceRegistrator
    {

        protected Registration CreateRegistration(Type type,string name,bool isSecuritized,bool isCacheData,bool collectRareItems,IEnumerable<CachingPolicyAttribute> cachingPolicies)
        {
            return new Registration(name,type,isSecuritized,isCacheData,collectRareItems,cachingPolicies);
        }

        protected internal virtual IEnumerable<Registration> GetRegistrations()
        {
            foreach (var type in ResourceTypes)
            {
                string name;
                bool isCacheData;
                bool isSecuritized;
                IEnumerable<CachingPolicyAttribute> cachePolicies;
                bool collectRareItems = true;
                try
                {                    
                    //определение ресурса
                    var definition = Attribute.GetCustomAttribute(type, typeof (ResourceAttribute)) as ResourceAttribute;
                    //или кэш-данных
                    var cacheData =
                        Attribute.GetCustomAttribute(type, typeof (CachingDataAttribute)) as CachingDataAttribute;
                    //защищен ли ресурс настройками безопасности
                    var securitized =
                        Attribute.GetCustomAttribute(type, typeof (SecuritizedAttribute)) as SecuritizedAttribute;
                    cachePolicies =
                        Attribute.GetCustomAttributes(type, typeof(CachingPolicyAttribute),false).Cast<CachingPolicyAttribute>().ToArray();

                    isSecuritized = securitized != null;
                    if (definition != null) //если это ресурс
                    {
                        Debug.Assert(cacheData == null, "Либо ресурс, либо данные для кэширования!");
                        if (cacheData != null)
                            throw new InvalidOperationException("Либо ресурс, либо данные для кэширования!");
                        name = definition.Name;
                        isCacheData = false;
                        collectRareItems = cacheData.CollectRareItems;

                    }
                    else if (cacheData != null) //если это кэш-данные
                    {
                        name = cacheData.Name;
                        isCacheData = true;
                    }
                    else
                    {
                        Debug.Fail(
                            "Регистрируемый объект должен быть помечен либо атрибутом [Resource], либо атрибутом [CacheData]. ");
                        throw new InvalidOperationException(
                            "Регистрируемый объект должен быть помечен либо атрибутом [Resource], либо атрибутом [CacheData]. ");
                    }                    
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Ошибка подготовки типа:" + type.Name, ex);
                }
                yield return CreateRegistration(type, name, isSecuritized, isCacheData,collectRareItems,cachePolicies);
            }
        }

        protected virtual Type[] ResourceTypes { get; set; }
       
    }
}
