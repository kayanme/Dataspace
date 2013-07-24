using System;
using System.Reflection;
using System.Linq;
using Dataspace.Common.Data;
using Resources.Notification.Test.Resource.Level1Providers;
using Resources.Notification.Test.Resource.Level2Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Notification.Test
{
    [TestClass]
    public partial class NotificationTest
    {

        Settings settings = new Settings { AutoSubscription = false };

        [TestMethod]
        [TestCategory("Notification")]
        [TestCategory("Caching")]
        public void NoTransaction()
        {
            const string notChanged = "Not Changed"; 
            const string changed = "Changed";
           
            var ne1 = new NotifiedElement { Key = Guid.NewGuid(), Name = "" };
            var une1 = new UnnotifiedElement { Key = Guid.NewGuid(), Name = "" };
            _level2Interaction.Post(ne1.Key,ne1);
            _level2Interaction.Post(une1.Key,une1);

            #region Transfer correctness check
            var resne = _level2Interaction.Get<NotifiedElement>(ne1.Key);
            var resune = _level2Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(ne1.Name,resne.Name);
            Assert.AreEqual(une1.Name, resune.Name);

            resne = _level1Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level1Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(ne1.Name, resne.Name);
            Assert.AreEqual(une1.Name, resune.Name);
            #endregion

            #region Than check, that no changes are coming back to level 2 yet
            _level1Interaction.Post(ne1.Key, new NotifiedElement{Key = ne1.Key,Name = notChanged});
            _level1Interaction.Post(une1.Key, new UnnotifiedElement { Key = une1.Key, Name = notChanged });

            resne = _level2Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level2Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual("", resne.Name);
            Assert.AreEqual("", resune.Name);

            resne = _level1Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level1Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(notChanged, resne.Name);
            Assert.AreEqual(notChanged, resune.Name);
            var notifyings = _level2Interaction.CheckSubscriptionCameFromLastCheck();
            Assert.IsFalse(notifyings.Any());

            #endregion

            _level2Interaction.Post(ne1.Key, new NotifiedElement { Key = ne1.Key, Name = notChanged });
            _level2Interaction.Post(une1.Key, new UnnotifiedElement { Key = une1.Key, Name = notChanged });
            _level2Interaction.Subscribe();



            #region Checking results after subscription and preparing for change tracking
            resne = _level2Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level2Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(notChanged, resne.Name);
            Assert.AreEqual(notChanged, resune.Name);

            resne = _level1Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level1Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(notChanged, resne.Name);
            Assert.AreEqual(notChanged, resune.Name);
            #endregion


            _level1Interaction.Post(ne1.Key, new NotifiedElement { Key = ne1.Key, Name = changed });
            _level1Interaction.Post(une1.Key, new UnnotifiedElement { Key = une1.Key, Name = changed });

            #region Checking results after changing. Unnotifing element on level 2 should remain unchanged. Token should return event
            resne = _level2Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level2Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(changed, resne.Name);
            Assert.AreEqual(notChanged, resune.Name);

            resne = _level1Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level1Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(changed, resne.Name);
            Assert.AreEqual(changed, resune.Name);

            notifyings = _level2Interaction.CheckSubscriptionCameFromLastCheck();
            Assert.AreEqual(1,notifyings.Count());
            Assert.AreEqual("NotifiedElement",notifyings.First().ResourceName);
            Assert.AreEqual(resne.Key, notifyings.First().ResourceKey);
            #endregion

            _level2Interaction.Unsubscribe();

            #region Checking results after unsubscribing. Everything should be the same.
            resne = _level2Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level2Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(changed, resne.Name);
            Assert.AreEqual(notChanged, resune.Name);

            resne = _level1Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level1Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(changed, resne.Name);
            Assert.AreEqual(changed, resune.Name);

            notifyings = _level2Interaction.CheckSubscriptionCameFromLastCheck();
            Assert.IsFalse(notifyings.Any());
            #endregion

            _level1Interaction.Post(ne1.Key, new NotifiedElement { Key = ne1.Key, Name = notChanged });
            _level1Interaction.Post(une1.Key, new UnnotifiedElement { Key = une1.Key, Name = notChanged });


            #region Checking results first time. Update for notifying element will arrive, because the were alive subscription. But due to token unsubscription, we should not have event of unactuality.
            resne = _level2Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level2Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(notChanged, resne.Name);
            Assert.AreEqual(notChanged, resune.Name);

            resne = _level1Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level1Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(notChanged, resne.Name);
            Assert.AreEqual(notChanged, resune.Name);

            notifyings = _level2Interaction.CheckSubscriptionCameFromLastCheck();
            Assert.IsFalse(notifyings.Any());
            #endregion

            _level1Interaction.Post(ne1.Key, new NotifiedElement { Key = ne1.Key, Name = changed });
            _level1Interaction.Post(une1.Key, new UnnotifiedElement { Key = une1.Key, Name = changed });

            #region But this time subscription should go away. 
            resne = _level2Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level2Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(notChanged, resne.Name);
            Assert.AreEqual(notChanged, resune.Name);

            resne = _level1Interaction.Get<NotifiedElement>(ne1.Key);
            resune = _level1Interaction.Get<UnnotifiedElement>(une1.Key);
            Assert.AreEqual(changed, resne.Name);
            Assert.AreEqual(changed, resune.Name);

            notifyings = _level2Interaction.CheckSubscriptionCameFromLastCheck();
            Assert.IsFalse(notifyings.Any());
            #endregion
        }

        private Level1Activator _level1Interaction;
        private Level2Activator _level2Interaction;

        [TestInitialize]
        public void Preparation()
        {
       
            var rootDomain = AppDomain.CreateDomain("RootDomain", null, new AppDomainSetup { ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase });
          
            _level1Interaction =
                rootDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName,
                                                   typeof (Level1Activator).FullName) as Level1Activator;

            var level2Domain = AppDomain.CreateDomain("Level2Domain", null, new AppDomainSetup { ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase });

            _level2Interaction =
               level2Domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName,
                                                  typeof(Level2Activator).FullName) as Level2Activator;

            _level2Interaction.SetTransporter(_level1Interaction.Transporter,settings);

            Settings.NoCacheGarbageChecking = true;
            
        }


        [TestCleanup]
        public void Shutdown()
        {
            _level1Interaction.Shutdown();
            _level2Interaction.Shutdown();
        }
    }
}
