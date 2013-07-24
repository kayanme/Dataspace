using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Common.Utility.Dictionary;
using Dataspace.Common.Utility.Dictionary;

namespace TwoLevelCachePerfomance.Classes
{
    using CacheType = UpgradedCache<int, object, NoReferenceUpdatableElement<object>>;

    internal sealed class Experiment:IXmlSerializable
    {
        private static readonly XmlSerializer _configSerializer = new XmlSerializer(typeof(ExperimentConfiguration));
        private static readonly XmlSerializer _dataRowSerializer = new XmlSerializer(typeof(DataRow));

        private readonly ExperimentConfiguration _configuration;
     
        private readonly Stopwatch _watch = new Stopwatch();

        private int _externalGetPenalties = 0;

        private Random _rnd;

        private int _nodeGoneCounter;

        private int _branchChangedCounter;

        private CacheType _cache;

        private readonly string _name;

        private readonly DateTime _startTime = new DateTime(2011,1,1);

        private readonly Queue<Row> _rows = new Queue<Row>();

        private int[] _keySequence;
     
        private static int SearchByFreq(double[] freqs, double seed)
        {
            int lowBound = 0;
            int upBound = freqs.Length;
            int result = (upBound + lowBound) / 2;
            while (!(freqs[result] < seed && freqs[result + 1] > seed || result == 0))
            {
                if (freqs[result] > seed)
                {
                    upBound = result;
                }
                else
                {
                    lowBound = result;
                }
                result = (upBound + lowBound) / 2;
            }
            return result;
        }

        private double[] FreqMatch(int getCount)
        {
            var targetFreqs = _configuration.Frequences.Last(k => k.IterationToSwitch <= getCount).TargetFreq;
            var realFreqs = new int[_configuration.KeyCount];
            for(int i=0;i<getCount;i++)
            {
                realFreqs[_keySequence[i]]++;
            }
            var tf =
                Enumerable.Range(1, targetFreqs.Length - 1)
                          .Select(k => targetFreqs[k] - targetFreqs[k - 1])
                          .ToArray();
            var targCount = tf.Select(k => k * getCount).ToArray();
            var diff = realFreqs.Zip(targCount, (r, t) => (r - t) * (r - t) / getCount / getCount).ToArray();
            return diff;
        }      

        private TimeSpan GetAvgTime(int iters)
        {
            return TimeSpan.FromMilliseconds((double)(_watch.ElapsedMilliseconds
                     + _externalGetPenalties * _configuration.PenaltyForExternalGet) / iters);
        }


        private void AddEventRow(int i, string evnt)
        {
            _rows.Enqueue(new EventRow{Event = evnt,Iteration = i});
        }

        private void AddRowWithCurrentData(int i,int elapsedFromLastRow)
        {
            var row = new DataRow
                          {
                              Iteration = i,
                              ProbabilitiesStability = FreqMatch(i).Sum()*10000,
                              ExpectedRate = _cache.SecondLevelCacheState.ExpectedRate,
                              Rate = _cache.SecondLevelCacheState.Rate,
                              AverageTime = GetAvgTime(elapsedFromLastRow).Milliseconds,
                              C1ItemsCount = _cache.FirstLevelCacheState.CurrentItems,
                              C2ItemsCount = _cache.SecondLevelCacheState.Count,
                              C2ItemsGone = _nodeGoneCounter,
                              GoneIntensity = _cache.SecondLevelCacheState.GoneIntensity,
                              BranchDepth = _cache.SecondLevelCacheState.MaxFixedBranchDepth,
                              BranchChanges = _branchChangedCounter
                          };
            _rows.Enqueue(row);
        }

        public Experiment(ExperimentConfiguration config,string name = "Experiment")
        {
            _configuration = config;
            _name = name;
        }

        private int _wasRebalance = 0;

        private void GenerateKeys()
        {
            double[] curFreqs = _configuration.Frequences[0].TargetFreq;            
            int nextFreqSeq = -1;
            if (_configuration.Frequences.Count > 1)
                nextFreqSeq = 1;
            for (int i = 0; i < _configuration.TryCount; i++)
            {
                var key = SearchByFreq(curFreqs, _rnd.NextDouble());
                _keySequence[i] = key;
                if (nextFreqSeq!=-1 && _configuration.Frequences[nextFreqSeq].IterationToSwitch == i)
                {
                    curFreqs = _configuration.Frequences[nextFreqSeq].TargetFreq;
                    if (_configuration.Frequences.Count-1 > nextFreqSeq)
                        nextFreqSeq++;
                    else
                    {
                        nextFreqSeq = -1;
                    }
                }
            }
        }

        private readonly object _resultSendLock = new object();

        private int gotBeforeResultShow;

        private void ProcessTurn(int i)
        {
            var key = _keySequence[i];
            CacheNode<int, object>.TestCurrentTime = _startTime.AddMilliseconds(i * _configuration.ExperimentTimeStep);
            bool inCache = true;
            _watch.Start();
            _cache.RetrieveByFunc(key, k =>
            {
                inCache = false;
                return null;
            }, CacheNode<int, object>.TestCurrentTime);
            _watch.Stop();
            if (!inCache)
                Interlocked.Increment(ref _externalGetPenalties);          
            Interlocked.Increment(ref gotBeforeResultShow);
            if (Interlocked.CompareExchange(ref _wasRebalance,0,1) == 1)
            {          
                AddEventRow(i,"Tree was rebalanced");               
            }
            if (i != 0 && i % _configuration.GarbageCollectingRate == 0)
            {
                GC.Collect();
                AddEventRow(i, "Garbage force collected");
            }
            if (i != 0 && _configuration.Frequences.Any(k=>k.IterationToSwitch == i))
            {
                AddEventRow(i, "Frequences changed");               
            }

            if (i != 0 && i % (_configuration.ResultShowRate) == 0)
            {
                lock (_resultSendLock)
                {
                    if (i != 0 && i%(_configuration.ResultShowRate) == 0)
                    {
                        AddRowWithCurrentData(i,gotBeforeResultShow);
                        _externalGetPenalties = 0;
                        gotBeforeResultShow = 0;
                        _watch.Reset();
                        _nodeGoneCounter = 0;
                        _branchChangedCounter = 0;
                    }
                }
            }
            
        }


        public void Run()
        {                             

            _rnd = _configuration.Seed == int.MinValue ? new Random() : new Random(_configuration.Seed);
            Action<Action> activeRebalance = (r) => Task.Run(r).ContinueWith(tk => _wasRebalance = 1);
            Action<Action> noRebalance = r => { };
            _cache = _configuration.RebalanceActive ?
                new CacheType(queueRebalance: activeRebalance)
              : new CacheType(queueRebalance: noRebalance);

            _cache.SecondLevelCacheState.NodeGone += (o, e) => Interlocked.Increment(ref _nodeGoneCounter);
            _cache.SecondLevelCacheState.BranchLengthChangedEvent += (o, e) => Interlocked.Increment(ref _branchChangedCounter);

            _cache.SecondLevelCacheState.MaxFixedBranchDepth = _configuration.MaxFixedCache2BranchLength;
            _cache.SecondLevelCacheState.WriteTimeout = _configuration.WriteTimeout;
            _cache.SecondLevelCacheState.AdaptationSettings.RebalancingMode = _configuration.RebalancingMode;
            _cache.SecondLevelCacheState.AdaptationSettings.GoneIntensityForBranchDecrease =
                _configuration.GoneIntensityForBranchDecrease;
            _cache.SecondLevelCacheState.AdaptationSettings.GoneIntensityForBranchIncrease =
                _configuration.GoneIntensityForBranchIncrease;
            _cache.SecondLevelCacheState.AdaptationSettings.CheckThreshold = _configuration.CheckThreshold;
            _cache.SecondLevelCacheState.AdaptationSettings.MinBranchLengthToRebalance =
                _configuration.MinBranchLengthToRebalance;

            _keySequence = new int[_configuration.TryCount];
            GenerateKeys();
            if (_configuration.ParallelWork)
            {
                Parallel.For(0, _configuration.TryCount,
                             new ParallelOptions {MaxDegreeOfParallelism = 10},
                             i => ProcessTurn(i)
                    );
            }
            else
            {
                for (int i = 0; i < _configuration.TryCount; i++)
                {
                    ProcessTurn(i);
                }
            }
            _keySequence = null;
        }

        public void PrintSummary()
        {
            Console.WriteLine(_name);
            Console.WriteLine(header);
            WriteSummaryData();
            Console.WriteLine();
        }

        const string header = "   i    | stblty | exp.get | act.get | avg.time | c1  |  c2  | gone c2  | gone intns  |  br.depth  | br.changes";
        const string data = "{0:D8}| {1:f4} | {2:f3}   |   {3:f2}  |     {4:D2}   | {5:D3} | {6:D4} |   {7:D4}   |    {8:f3}    | {9:D2}    | {10:D2}";

        private void WriteSummaryData()
        {
            Console.WriteLine(data,
                             _configuration.TryCount,
                             _rows.OfType<DataRow>().Average(k => k.ProbabilitiesStability),
                             _rows.OfType<DataRow>().Average(k => k.ExpectedRate),
                             _rows.OfType<DataRow>().Average(k => k.Rate),
                             (int)_rows.OfType<DataRow>().Average(k => k.AverageTime),
                             (int)_rows.OfType<DataRow>().Average(k => k.C1ItemsCount),
                             (int)_rows.OfType<DataRow>().Average(k => k.C2ItemsCount),
                             (int)_rows.OfType<DataRow>().Average(k => k.C2ItemsGone),
                             (float)_rows.OfType<DataRow>().Average(k => k.GoneIntensity),
                             (int)_rows.OfType<DataRow>().Average(k => k.BranchDepth),
                             (int)_rows.OfType<DataRow>().Average(k => k.BranchChanges)
               );
        }

        public void Print()
        {
           
            Console.WriteLine(_name);
            Console.WriteLine(header);
            foreach (var row in _rows.OrderBy(k=>k.Iteration))
            {
                if (row is DataRow)
                {
                    var dr = row as DataRow;
                    Console.WriteLine(data,
                        dr.Iteration,dr.ProbabilitiesStability,
                        dr.ExpectedRate,dr.Rate,dr.AverageTime,
                        dr.C1ItemsCount,dr.C2ItemsCount,dr.C2ItemsGone,
                        dr.GoneIntensity,dr.BranchDepth,dr.BranchChanges );
                }
                else
                {
                    Console.WriteLine("                       {0}                     ",(row as EventRow).Event);
                }
            }
            Console.WriteLine("Summary");
            WriteSummaryData();       

            Console.WriteLine();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            var nmspc = new XmlSerializerNamespaces(new[] { new XmlQualifiedName(string.Empty, "Empty") });
            writer.WriteStartElement("Experiment");
            writer.WriteAttributeString("Name",_name);
          
            _configSerializer.Serialize(writer, _configuration,nmspc);
            writer.WriteStartElement("DataCollected");
            foreach (var row in _rows.OrderBy(k => k.Iteration))
            {
                if (row is DataRow)
                {

                    var dr = row as DataRow;
                    _dataRowSerializer.Serialize(writer, dr, nmspc);                    
                }
                else
                {
                    writer.WriteStartElement("Event");
                    writer.WriteValue((row as EventRow).Event);
                    writer.WriteEndElement();               
                }
            }
            writer.WriteEndElement();
            writer.WriteStartElement("Summary");
            writer.WriteAttributeString("Tries", _configuration.TryCount.ToString());
            writer.WriteAttributeString("ProbStability", _rows.OfType<DataRow>().Average(k => k.ProbabilitiesStability).ToString());
            writer.WriteAttributeString("ExpRate", _rows.OfType<DataRow>().Average(k => k.ExpectedRate).ToString());
            writer.WriteAttributeString("Rate", _rows.OfType<DataRow>().Average(k => k.Rate).ToString());
            writer.WriteAttributeString("AvgTime", _rows.OfType<DataRow>().Average(k => k.AverageTime).ToString());
            writer.WriteAttributeString("Cache1Items", _rows.OfType<DataRow>().Average(k => k.C1ItemsCount).ToString());
            writer.WriteAttributeString("Cache2Items", _rows.OfType<DataRow>().Average(k => k.C2ItemsCount).ToString());
            writer.WriteAttributeString("Cache2ItemsGone",_rows.OfType<DataRow>().Average(k => k.C2ItemsGone).ToString());
            writer.WriteAttributeString("GoneIntensity", _rows.OfType<DataRow>().Average(k => k.GoneIntensity).ToString());
            writer.WriteAttributeString("BranchDepth", _rows.OfType<DataRow>().Average(k => k.BranchDepth).ToString());
            writer.WriteAttributeString("BranchChanges", _rows.OfType<DataRow>().Average(k => k.BranchChanges).ToString());
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
    }
}
