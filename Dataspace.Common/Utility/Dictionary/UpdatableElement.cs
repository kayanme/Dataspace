using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace Common.Utility.Dictionary
{

    internal class UpdatableElement
    {
        private bool _shouldUpdate;

       
        internal void DropUpdate(DateTime time)
        {
            _shouldUpdate = false;
        }

        public void SetUpdate(DateTime time)
        {
            _shouldUpdate = true;
        }

        public virtual bool NeedUpdate()
        {
            return _shouldUpdate;
        }

        public UpdatableElement()
        {
            _shouldUpdate = false;
        }
    }

    internal class UpdatableElement<TValue> : UpdatableElement
    {

        private ulong _period;
        private ulong _totalTime;
        private DateTime _lastGetTime = DateTime.MinValue;

        private SpinLock _lock = new SpinLock();


        private ulong GetShiftFromLastTime(DateTime time)
        {           
            if (_lastGetTime == DateTime.MinValue)
                return 1;//чтобы не было деления на 0
            var span = (time - _lastGetTime);
            if (span.TotalMilliseconds<=0)//если время на машине скачет
                span = TimeSpan.FromMilliseconds(1);
            checked//специально контролирую переполнение - очень важно и возможно
            {
                var mSecSpan = span.TotalMilliseconds;           
                //если _totalTime станет чересчур большим, прибавка вызовет переполнение и некорректный результат, поэтому предварительно проверяем и сокращаем времена, если слишком велики
                while (ulong.MaxValue - _totalTime < mSecSpan)
                {
                    _period >>= 1;
                    _totalTime >>= 1;                  
                }
                return (ulong)mSecSpan;
            }
        }

        public float GetFrequency()
        {
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                var time = DateTime.Now;
                var mSec = GetShiftFromLastTime(time);
                return (float)_period/(_totalTime + mSec);
            }
            finally
            {
                if (lockTaken)
                    _lock.Exit();
            }
        }

        public virtual TValue Element { get; set; }

        public void FixTake(DateTime time)
        {
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
           
                checked//специально контролирую переполнение - очень важно и возможно
                {                   
                    var mSecSpan = GetShiftFromLastTime(time);
                    _totalTime += mSecSpan;
                    _period++;
                    _lastGetTime = time;
                }

            }
            finally
            {
                if (lockTaken)
                    _lock.Exit();
            }
        }

        public UpdatableElement()
        {
            
        }
        
        public UpdatableElement(TValue obj)
        {
            Element = obj;
            DropUpdate(DateTime.MinValue);
        }
    }

    internal class NoReferenceUpdatableElement<TValue> : UpdatableElement<TValue>,IDisposable
    {
        private readonly MemoryStream stream = new MemoryStream();

        private readonly BinaryFormatter _formatter = new BinaryFormatter();

        private SpinLock _lock = new SpinLock();

        private bool _isNull;

        public override TValue Element
        {
            get
            {
                try
                {
                    Contract.Requires(stream.CanSeek || stream.Length >0);
                    bool taken = false;
                    _lock.Enter(ref taken);
                    if (_isNull)
                        return default(TValue);                    
                    stream.Position = 0;
                    return (TValue)_formatter.Deserialize(stream);
                }
                finally
                {

                    _lock.Exit();
                }

            }
            set
            {
                try
                {
                    Contract.Requires(ReferenceEquals(value, default(TValue)) || stream !=null);
                    bool taken = false;
                    _lock.Enter(ref taken);
                    if (ReferenceEquals(value,default(TValue)))
                    {
                        _isNull = true;
                    }
                    else
                    {
                        _isNull = false;
                        stream.Position = 0;
                        _formatter.Serialize(stream, value);
                    }
                }
                finally
                {

                    _lock.Exit();
                }
            }
        }

        public void Dispose()
        {
            stream.Dispose();
        }
    }
}
