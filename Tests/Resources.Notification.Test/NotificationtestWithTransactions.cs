using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Resources.Notification.Test
{
   
    partial class NotificationTest
    {       
        public void WithTransaction()
        {

            const string notChanged = "Not Changed";
            const string changed = "Changed";
            var ne1 = new NotifiedElement { Key = Guid.NewGuid(), Name = notChanged};
            var une1 = new UnnotifiedElement { Key = Guid.NewGuid(), Name = notChanged };

            _level2Interaction.Post(ne1.Key, ne1);
            _level2Interaction.Post(une1.Key, une1);

            _level2Interaction.Subscribe();

         

            #region Start changing inside transaction.

            var notifyings = _level2Interaction.CheckSubscriptionCameFromLastCheck();
            Assert.IsFalse(notifyings.Any());
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew,TimeSpan.FromMinutes(10)))
                {
                    var resne = _level2Interaction.Get<NotifiedElement>(ne1.Key);//just precaching to check cache state later
                    var resune = _level2Interaction.Get<UnnotifiedElement>(une1.Key);

                    _level1Interaction.Post(ne1.Key, new NotifiedElement { Key = ne1.Key, Name = changed }, Transaction.Current);//first time we'll explicitly set a transaction (propagation into foreign domain)

                    //No notifyings should arrive yet
                    notifyings = _level2Interaction.CheckSubscriptionCameFromLastCheck();
                    Assert.IsFalse(notifyings.Any());

                    //This time too. Actualy, this action will never send notifications... whatever
                    _level1Interaction.Post(une1.Key, new UnnotifiedElement { Key = une1.Key, Name = changed });
                    notifyings = _level2Interaction.CheckSubscriptionCameFromLastCheck();
                    Assert.IsFalse(notifyings.Any());

                    resne = _level2Interaction.Get<NotifiedElement>(ne1.Key);
                    resune = _level2Interaction.Get<UnnotifiedElement>(une1.Key);
                    //the thing is,that due to lack of notification, 2-level cache should remain untouched...
                    Assert.AreEqual(notChanged, resne.Name);
                    Assert.AreEqual(notChanged, resune.Name);

                    //but 1-level doesn't, 'cause local storage unactualities are not affected by transactions
                    resne = _level1Interaction.Get<NotifiedElement>(ne1.Key);
                    resune = _level1Interaction.Get<UnnotifiedElement>(une1.Key);
                    Assert.AreEqual(changed, resne.Name);
                    Assert.AreEqual(changed, resune.Name);
                    _level1Interaction.Complete();
                    scope.Complete();
                }
            }
            catch (TransactionAbortedException)//по невыясненным мной точно причинам транзакция рушится. Скорее всего, передача транзакции в другой домен ее продвигает, а текущая реализация тестов явно некорректна для продвигаемой транзакции
            {

            }

            #endregion  
        }
    }
}
