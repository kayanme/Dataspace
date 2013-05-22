using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Dataspace.Common.Utility
{
    public sealed class Accumulator<TKey, TValue>:IDisposable 
    {

        private readonly Predicate<TKey> _presentsInSource;

        private readonly Func<TKey, TValue> _simpleGetter;

        private readonly Action<TKey, TValue> _pushItem;

        private readonly SerialGetter _getter;

        private readonly ReaderWriterLockSlim _queueGetLock = new ReaderWriterLockSlim();

        private readonly ConcurrentQueue<TKey> _valuesToGet = new ConcurrentQueue<TKey>();

        public delegate IEnumerable<KeyValuePair<TKey, TValue>> SerialGetter(IEnumerable<TKey> keys);


        private readonly IEqualityComparer<TKey> _comparer;
        /// <summary>
        /// Initializes a new instance of the <see cref="Accumulator{TKey,TValue}" /> class.
        /// </summary>
        /// <param name="presentsInSource">Функция для определения наличия элемента с ключом в хранилище </param>
        /// <param name="simpleGetter">Получатель данных из хранилища, выполняющий перед получением (если значение отсутствует) некоторое действие по получению из конечного источника.</param>
        /// <param name="getter">Получатель серии.</param>
        /// <param name="comparer">Сравниватель ключей.</param>
        /// <param name="pushItem"> Записыватель элемента с ключом в хранилище</param>
        public Accumulator(Action<TKey, TValue> pushItem,
                           Predicate<TKey> presentsInSource,
                           Func<TKey, TValue> simpleGetter,
                           SerialGetter getter,
                           IEqualityComparer<TKey> comparer = null)
        {

            _presentsInSource = presentsInSource;
            _getter = getter;
            _simpleGetter = simpleGetter;
            _pushItem = pushItem;
            _comparer = comparer??EqualityComparer<TKey>.Default;
        }


        private void LoadPack()
        {
            var keys = new List<TKey>();
            try
            {
                _queueGetLock.EnterWriteLock();               
                TKey key;               
                while (_valuesToGet.TryDequeue(out key))
                {
                    keys.Add(key);
                }
            }
            finally
            {
                _queueGetLock.ExitWriteLock();
            }
            var idsToGet = keys.Distinct(_comparer).Where(k => !_presentsInSource(k)).ToArray();
            if (!idsToGet.Any())
                return;
            var rawValues = _getter(idsToGet);
#if DEBUG
            Debug.Assert(idsToGet.All(k => rawValues.Count(k2 => k2.Key.Equals(k)) == 1),
                         "Ключи полученных результатов не совпадают с ключами запроса. Возможно, некорректный запрос.");
            if (!idsToGet.All(k => rawValues.Count(k2 => k2.Key.Equals(k)) == 1))
                throw new InvalidOperationException(
                    "Ключи полученных результатов не совпадают с ключами запроса. Возможно, некорректный запрос.");
#endif
            var vals = rawValues.ToDictionary(k => k.Key, k => k.Value);
            Debug.Assert(idsToGet.Count() == vals.Count());

            foreach (var t in idsToGet)
                _pushItem(t, vals[t]);
        }

        public Func<TValue> GetValue(TKey key)
        {
#if DEBUG2
            if (key.Equals(default(TKey)))
            {
                Debugger.Break();
                Debug.Print("Пустой GUID считается недопустимым");
            }
#endif
            _queueGetLock.EnterReadLock();
            _valuesToGet.Enqueue(key);
            _queueGetLock.ExitReadLock();
            return () =>
                       {


                           LoadPack();
                           return _simpleGetter(key);

                       };

        }

        public void Dispose()
        {
            _queueGetLock.Dispose();
        }

       
    }
}
