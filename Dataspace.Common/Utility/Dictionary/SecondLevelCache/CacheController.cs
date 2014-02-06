using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dataspace.Common.Utility.Dictionary.SecondLevelCache
{
    internal class CacheController<TKey,TValue>
    {
        private readonly SecondLevelCache<TKey, TValue> _controlledCache;

        internal readonly CacheAdaptationSettings Settings;

        private bool _lastActionWasBranchIncreasing = false;

        private int _nodeBeenGoneSinceLastBranchIncreasing = 0;

        private bool _wasGC = false;

        private bool _changeBranchLength;

        public CacheController(SecondLevelCache<TKey,TValue> controlledCache,bool fixAllNodes)
        {
            _controlledCache = controlledCache;
            _changeBranchLength = !fixAllNodes;
            Settings = new CacheAdaptationSettings
                           {
                               CheckThreshold= 50,
                               GoneIntensityForBranchIncrease = 0.205f,
                               GoneIntensityForBranchDecrease = 0.2f,
                               MinBranchLengthToRebalance = 6,
                               RebalancingMode = RebalancingMode.Hybrid
                           };
            _controlledCache.NodeGoneEvent += (o, e) =>Interlocked.Increment(ref _nodeBeenGoneSinceLastBranchIncreasing);
        }

        private class GCNotificator
        {
            public static bool IsCreated;
            private readonly CacheController<TKey, TValue> _c;
            private object _tempMe;
            public GCNotificator(CacheController<TKey,TValue> c)
            {
                _c = c;
                _c._wasGC = false;
                IsCreated = true;
            }
            ~GCNotificator()
            {

                if (GC.GetGeneration(this) < 2 && !Environment.HasShutdownStarted)
                {
                    _tempMe = this;
                    GC.ReRegisterForFinalize(this);
                    _tempMe = null;
                }
                else
                {
                    _c._wasGC = true;
                    IsCreated = false;    
                }
                
            }        
        }
        
        private void CreateNotificationIfItDoesntExists()
        {
            if (!GCNotificator.IsCreated)
                new GCNotificator(this);
        }

        private void DecreaseBranchDepthIfNeeded()
        {
            if (_changeBranchLength)
            {
                _lastActionWasBranchIncreasing = false;
                if (_controlledCache.State.MaxFixedBranchDepth > 1)
                {
                    _controlledCache.State.MaxFixedBranchDepth--;
                    GC.Collect(2);
                }
                _nodeBeenGoneSinceLastBranchIncreasing = 0;
            }
        }

        private void IncreaseBranchDepthIfNeeded()
        {
            if (_changeBranchLength)
            {
                _controlledCache.State.MaxFixedBranchDepth++;              
                _nodeBeenGoneSinceLastBranchIncreasing = 0;
                _lastActionWasBranchIncreasing = true;
            }
        }

        /// <summary >
        /// Принимает решение об адаптакии кэша к текущим условиям. На данный момент решения следующие:
        /// 1. Если интенсивность ухода данных из кэша слишком низка
        ///  и с момента последней проверки была полная сборка мусора, то укоротить длину фиксированной ветви.
        /// Проверка на сборку мусора нужна, чтобы дать время отработать сборщику после укорочения ветки.
        /// 2. Если интенсивность ухода слишком высока,  надо сначала перебалансировать дерево, а потом, если 
        /// интенсивность не поменяется, то удлинить фиксированную ветвь. Опять же, надо дождаться сбора мусора, 
        /// чтобы ветки сильно не перерастали.
        /// Балансировка запустится, только если достаточно узлов в дереве и достаточно длинная ветвь (а то все результаты балансировки уйдут в мусор).
        /// </summary>
        internal void MakeDecision()
        {            
            var wasNodeGone =  _nodeBeenGoneSinceLastBranchIncreasing>0;
          
            if (_controlledCache.State.GoneIntensity < Settings.GoneIntensityForBranchDecrease
                && (_wasGC || wasNodeGone)
                &&  (double)_nodeBeenGoneSinceLastBranchIncreasing/_controlledCache.Count < 0.05)
            {
                DecreaseBranchDepthIfNeeded();

            }
            else if (_controlledCache.State.GoneIntensity > Settings.GoneIntensityForBranchIncrease
                && !_controlledCache.State.RebalancingQueued
                && (double)_nodeBeenGoneSinceLastBranchIncreasing/_controlledCache.Count > 0.05)
            {            
                               
                if (!_lastActionWasBranchIncreasing 
                 && Settings.MinBranchLengthToRebalance<_controlledCache.State.MaxFixedBranchDepth)
                {
                    _controlledCache.QueueRebalance(Settings.RebalancingMode);
               
                }
                IncreaseBranchDepthIfNeeded();
            }
           
            CreateNotificationIfItDoesntExists();
        }
    }
}
