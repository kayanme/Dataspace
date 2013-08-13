using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Common.Utility.Dictionary;
using Dataspace.Common.Utility.Dictionary;
using TwoLevelCachePerfomance.Classes;


namespace TwoLevelCachePerfomance
{
    internal class Program
    {


        private static IEnumerable<ExperimentConfiguration> ConfigGenerator(ExperimentConfiguration baseCfg)
        {
            for (int tries = 1000; tries <= 5000; tries += 2000)
            {
                for (int measure = 0; measure < 15; measure++)
                {
                    var bsCfg = new ExperimentConfiguration(baseCfg)
                                    {
                                        KeyCount = tries,
                                        TryCount = tries*1000,
                                        GarbageCollectingRate = tries*200,
                                        ResultShowRate = tries*40,
                                        CheckThreshold = 50                                    
                                    };
                    bsCfg.AddFrequency(bsCfg.FormFreqs());


                    for (var incInt = 0.3f; incInt <= 0.9f; incInt += 0.1f)
                        for (var decInt = 0.05f; decInt <= 0.55f; decInt += 0.1f)
                        {
                            yield return new ExperimentConfiguration(bsCfg)
                                             {
                                                 GoneIntensityForBranchDecrease = decInt,
                                                 GoneIntensityForBranchIncrease = incInt,
                                                 RebalancingMode = RebalancingMode.Light,
                                                 RebalanceActive = true
                                             };
                            yield return new ExperimentConfiguration(bsCfg)
                                             {
                                                 GoneIntensityForBranchDecrease = decInt,
                                                 GoneIntensityForBranchIncrease = incInt,
                                                 RebalancingMode = RebalancingMode.Heavy,
                                                 RebalanceActive = true
                                             };
                            yield return new ExperimentConfiguration(bsCfg)
                            {
                                GoneIntensityForBranchDecrease = decInt,
                                GoneIntensityForBranchIncrease = incInt,
                                RebalancingMode = RebalancingMode.Hybrid,
                                RebalanceActive = true
                            };
                            yield return new ExperimentConfiguration(bsCfg)
                                             {
                                                 GoneIntensityForBranchDecrease = decInt,
                                                 GoneIntensityForBranchIncrease = incInt,
                                                 RebalanceActive = false
                                             };
                        }
                }
            }
        }

        private static void Save(IEnumerable<Experiment> exps, int num)
        {
            using (var f = File.Open(string.Format(".\\exp{0}.xml", num), FileMode.Create))
            using (var writer = XmlWriter.Create(f, new XmlWriterSettings
                                                        {
                                                            Indent = true,
                                                            OmitXmlDeclaration = true,
                                                            NewLineChars = "\n",
                                                            NamespaceHandling = NamespaceHandling.OmitDuplicates
                                                        }))
            {

                writer.WriteStartElement("Results", "Empty");
                foreach (var experiment in exps)
                {
                    writer.WriteStartElement("expData");
                    experiment.WriteXml(writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        private static void Exp1()
        {
            int tries = 5000;
            var config = new ExperimentConfiguration
                             {
                                 PenaltyForExternalGet = 350,
                                 WriteTimeout = 15,
                                 ParallelWork = false,
                                 KeyCount = tries,
                                 TryCount = tries*3000,
                                 GarbageCollectingRate = tries*1000,
                                 ResultShowRate = tries*40,
                                 MaxFixedCache2BranchLength = 6,                              
                                 RebalanceActive = true,
                                 ExperimentTimeStep = 6000,
                                 MinBranchLengthToRebalance = 6,
                                 GoneIntensityForBranchDecrease = 0.20f,
                                 GoneIntensityForBranchIncrease = 0.205f,
                                 CheckThreshold = 50,                                                          
                             };
            config.AddFrequency(config.FormFreqs());
            config.AddFrequency(config.FormFreqs(), tries*1000);
            config.AddFrequency(config.FormFreqs(), tries*2000);
            var hbd = new ExperimentConfiguration(config) {RebalancingMode = RebalancingMode.Hybrid};
            var lgt = new ExperimentConfiguration(config) {RebalancingMode = RebalancingMode.Light};
            var hv = new ExperimentConfiguration(config) {RebalancingMode = RebalancingMode.Heavy};
            var none = new ExperimentConfiguration(config) {RebalanceActive = false};
            var exp1 = new Experiment(hbd, "Hybrid");
            var exp2 = new Experiment(lgt, "Light");
            var exp3 = new Experiment(hv, "Heavy");
            var exp4 = new Experiment(none, "None");
            Console.WindowWidth = 140;
            Console.BufferHeight = 4000;
            exp1.Run();
            //exp2.Run();
       //     exp3.Run();
       //     exp4.Run();
            exp1.Print();
            //exp2.Print();
       //     exp3.Print();
        //    exp4.Print();
            Console.ReadLine();
        }

        private static void Main(string[] args)
        {
            Exp1();
            var baseConfig = new ExperimentConfiguration
                                 {
                                     PenaltyForExternalGet = 350,
                                     WriteTimeout = 15,
                                     ParallelWork = false,
                                 };



            //   baseConfig.AddFrequency(baseConfig.FormFreqs(), keyCount*200);
            //   baseConfig.AddFrequency(baseConfig.FormFreqs(), keyCount * 600);
            {
                var configs = ConfigGenerator(baseConfig);
                var exps = configs.Select((k, i) => new Experiment(k, "Experiment " + i)).ToArray();

                var count = exps.Count();
                const int groupLength = 30;
                var expGroups = new List<Experiment>[exps.Length/groupLength];

                const int startGroup = 65;
                var done = startGroup*groupLength;

                for (int i = 0; i < exps.Length; i++)
                {
                    if (expGroups[i/groupLength] == null)
                        expGroups[i/groupLength] = new List<Experiment>();
                    expGroups[i/groupLength].Add(exps[i]);
                }

                for (int grNum = startGroup; grNum < expGroups.Length; grNum++)
                {
                    Parallel.ForEach(expGroups[grNum], k =>
                                                           {
                                                               k.Run();
                                                               Console.WriteLine("{0}/{1}", ++done, count);
                                                           });
                    Save(expGroups[grNum], grNum);
                }


                Console.WriteLine("Done");
                Console.ReadLine();

            }
        }
    }
}
