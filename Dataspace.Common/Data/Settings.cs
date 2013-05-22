using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Dataspace.Common.Attributes;
using Dataspace.Common.Services;

namespace Dataspace.Common.Data
{
    [Serializable]
    public sealed class Settings
    {
        /// <summary>
        /// Использовать транзакции конечного источника данных (true), или формировать и изолировать данные самостоятельно(false).
        /// </summary>
        public bool NativeTransactionMode = true;
        /// <summary>
        /// Автоматическая подписка уровня на все изменения нижележащего уровня.
        /// </summary>
        public bool AutoSubscription = true;
        /// <summary>
        /// Проверять корректность провайдеров и схем (замедляет работу).
        /// </summary>
        public bool CheckMode = false;

        /// <summary>
        /// Учитывать наследование данных
        /// </summary>
        public bool AwareOfInheritance = true;

        /// <summary>
        /// Набор флагов, определяющих активные модули
        /// </summary>
        public Enum[] ActivationFlags;

        /// <summary>
        /// Не освобождать кэш при сборке мусора.
        /// </summary>
        /// <remarks>Требуется в тех тестах, которые так или иначе связаны с проверкой результатов кэширования для исключения неожиданностей. Использование в иных случах категорически не рекомендуется.</remarks>
        [NonSerialized]
        internal static bool NoCacheGarbageChecking = false;

        /// <summary>
        /// Проверка на зацикливание в дереве. 
        /// Достаточно накладна, при этом нужна только на этапе отладки дерева (внешний код на наличие циклов не повлияет),
        /// так что эта константа ставится только на момент отладки, чтоб компилятор убирал соответствующие проверки.
        /// </summary>
        [NonSerialized]
        internal const bool CheckCycles = false;

        internal bool FlagsMatch(Enum[] presentFlags)
        {
            if (ActivationFlags == null)
                return true;
            return ActivationFlags.All(given => presentFlags.Where(k => k.GetType() == given.GetType())
                                                       .All(k => k.Equals(given)));
        }

        internal bool AppConfigMatchMatch(IEnumerable<KeyValuePair<string,string>> presentFlags,AppConfigProvider provider)
        {
            var result =
            presentFlags.Where(k => provider.ContainsKey(k.Key))
                        .All(k => k.Value.Equals(provider.GetValue(k.Key)));

            return result;
        }

        internal bool ActivationSwitchMatch(ActivationSwitchAttribute attribute, AppConfigProvider provider)
        {
            return FlagsMatch(attribute.CombinedEnums) && AppConfigMatchMatch(attribute.Configs,provider);
        }

        internal bool ActivationSwitchMatch(IEnumerable<ActivationSwitchAttribute> attributes, AppConfigProvider provider)
        {
            var enums = attributes.Where(k => k.Switch != null).Select(k => k.Switch).OfType<Enum>().ToArray();
            var keys = attributes.Where(k => k.Name != null)
                       .Zip(attributes.Where(k=>k.Value!=null),
                          (a,b)=> new KeyValuePair<string,string>(a.Name,b.Value)).ToArray();

            return FlagsMatch(enums) & AppConfigMatchMatch(keys,provider);
        }
    }
}
