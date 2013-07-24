using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Dataspace.Common.Utility.Dictionary;

namespace TwoLevelCachePerfomance.Classes
{
    [Serializable]
    [XmlRoot("Configuration",Namespace = "Empty")]
    public sealed class ExperimentConfiguration
    {
        private Random _rnd;

        [XmlRoot("Frequence",Namespace="Empty")]
        public struct FreqPart
        {
           [XmlArray(ElementName = "Freq")]
           public double[] TargetFreq;
           
           [XmlAttribute]
           public int IterationToSwitch;
        }

        [XmlAttribute]
        public  int KeyCount;

        [XmlAttribute]
        public  int TryCount;
     
        [XmlAttribute]
        public  int GarbageCollectingRate;
      
        [XmlIgnore]
        public  int PenaltyForExternalGet;

        [XmlIgnore]
        public  int ResultShowRate;

        [XmlAttribute]
        public float GoneIntensityForBranchDecrease;

        [XmlAttribute]
        public float GoneIntensityForBranchIncrease;
      
        [XmlAttribute]
        public int CheckThreshold;

        [XmlAttribute]
        public int ExperimentTimeStep;

        [XmlAttribute]
        public int MinBranchLengthToRebalance;

        [XmlArray(ElementName="Frequences")]
        public readonly List<FreqPart> Frequences = new List<FreqPart>();

        [XmlIgnore]
        public int Seed = int.MinValue;

        [XmlAttribute]
        public bool RebalanceActive;

        [XmlAttribute]
        public int MaxFixedCache2BranchLength = 10;

        [XmlIgnore]
        public int WriteTimeout = 50;

        [XmlAttribute]
        public bool ParallelWork;

        [XmlAttribute]
        public RebalancingMode RebalancingMode = RebalancingMode.Heavy;

        

        public void AddFrequency(double[] freq,int iter=0)
        {
            Frequences.Add(new FreqPart{IterationToSwitch = iter,TargetFreq = freq});
        }

        public double[] FormFreqs()
        {
            _rnd = _rnd ??( Seed == int.MinValue ? new Random() : new Random(Seed));
            var freqs = Enumerable.Range(0, KeyCount).Select(k => Math.Pow(_rnd.Next(KeyCount),3)).ToArray();
            var sum = freqs.Select(k => k).Sum();
            var calcFreqs = freqs.Select(k => k / sum).ToArray();
            double lastFreq = 0;
            var targetFreqsList = new List<double>();

            foreach (var calcFreq in calcFreqs)
            {
                targetFreqsList.Add(lastFreq);
                lastFreq = calcFreq + lastFreq;

            }
            targetFreqsList.Add(1);
            return targetFreqsList.ToArray();
        }

        public ExperimentConfiguration()
        {
            
        }

        public ExperimentConfiguration(ExperimentConfiguration baseConfiguration)
        {        
            this.GarbageCollectingRate  = baseConfiguration.GarbageCollectingRate;
            this.KeyCount  = baseConfiguration.KeyCount;
            this.MaxFixedCache2BranchLength = baseConfiguration.MaxFixedCache2BranchLength;
            this.PenaltyForExternalGet = baseConfiguration.PenaltyForExternalGet;
            this.RebalanceActive = baseConfiguration.RebalanceActive;
            this.ResultShowRate = baseConfiguration.ResultShowRate;
            this.Seed = baseConfiguration.Seed;
            this.Frequences.AddRange(baseConfiguration.Frequences);
            this.TryCount = baseConfiguration.TryCount;
            this.WriteTimeout = baseConfiguration.WriteTimeout;
            this.ParallelWork = baseConfiguration.ParallelWork;
            this.RebalancingMode = baseConfiguration.RebalancingMode;

            this.GoneIntensityForBranchDecrease = baseConfiguration.GoneIntensityForBranchDecrease;
            this.GoneIntensityForBranchIncrease = baseConfiguration.GoneIntensityForBranchIncrease;
            this.CheckThreshold = baseConfiguration.CheckThreshold;
            this.MinBranchLengthToRebalance = baseConfiguration.MinBranchLengthToRebalance;
        }
       
    }
}
