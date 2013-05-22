using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dataspace.Common.Services
{
    [Export]
    internal class DefaultFabrica
    {
#pragma warning disable 0649

        [Import(AllowDefault = false)]
        private CompositionService _container;

#pragma warning restore 0649

        internal T DefaultCreator<T>(string errorName, string methodName, Type targetType, object defProv) where T : class
        {
            if (defProv == null)
                throw new InvalidOperationException(string.Format("Невозможно создать {0} по умолчанию для ресурса {1}: отсутствует фабрика", errorName, targetType));
            try
            {
                var def =
                    defProv.GetType()
                        .GetMethod(methodName)
                        .MakeGenericMethod(targetType)
                        .Invoke(defProv, null) as T;
                Debug.Assert(def != null);
             
                _container.Container.SatisfyImportsOnce(def);
                return def;
            }
            catch (NullReferenceException ex)
            {
                throw new InvalidOperationException(
                  string.Format("Невозможно создать {0} по умолчанию для ресурса {1}: {2}", errorName, targetType,
                                ex.InnerException));
            }
            catch (TargetInvocationException ex)
            //при вызове метода провадера исключение завернется в TargetInvocationException
            {
                throw new InvalidOperationException(
                    string.Format("Невозможно создать {0} по умолчанию для ресурса {1}: {2}", errorName, targetType,
                                  ex.InnerException));
            }
        }
    }
}
