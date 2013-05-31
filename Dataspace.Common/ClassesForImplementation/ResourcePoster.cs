using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Transactions;
using Dataspace.Common.Statistics;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Services;
using Dataspace.Common.Transactions;
using Dataspace.Common.Statistics;
using Dataspace.Common.Announcements;
using Dataspace.Common.Exceptions;
using Dataspace.Common.Interfaces.Internal;

namespace Dataspace.Common.ClassesForImplementation
{
    public abstract class ResourcePoster
    {
#pragma warning disable 0649
        [Import]
        protected ITypedPool TypedPool { get; private set; }

        [Import]
        protected IGenericPool GenericPool { get; private set; }
        
        private IStatChannel _statChannel;

#pragma warning restore 0649    

        internal void SetStatChannel(IStatChannel statChannel)
        {
            _statChannel = statChannel;
        }

        internal void WriteResourceRegardlessofTransaction(Guid key,object resource)
        {
            var t = Stopwatch.StartNew();
            WriteResourceInt(key, resource);
            t.Stop();         
            if (_statChannel !=null)
            _statChannel.SendMessageAboutOneResource(key, Actions.Posted, t.Elapsed);
           
        }

       
        protected abstract void WriteResourceInt(Guid key, object resource);


        internal void DeleteResourceRegardlessofTransaction(Guid key)
        {
            var t = Stopwatch.StartNew();
            DeleteResourceInt(key);
            t.Stop();
            _statChannel.SendMessageAboutOneResource(key, Actions.Posted, t.Elapsed);
        }        
        protected abstract void DeleteResourceInt(Guid key);                  
    }

    public abstract class ResourcePoster<T>:ResourcePoster where T:class
    {


        protected sealed override void WriteResourceInt(Guid key, object resource)
        {            
                Debug.Assert(resource is T);
                WriteResourceTyped(key, resource as T);                    
        }

        protected abstract void WriteResourceTyped(Guid key, T resource);

        protected sealed override void DeleteResourceInt(Guid key)
        {
        
               DeleteResourceTyped(key);
          
        }

        protected abstract void DeleteResourceTyped(Guid key);


      
      
       
    }
}
