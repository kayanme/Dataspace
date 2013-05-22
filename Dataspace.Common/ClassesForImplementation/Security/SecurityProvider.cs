using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Dataspace.Common.Statistics;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Security;
using Dataspace.Common.Services;
using Dataspace.Common.Utility;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Security;
using Dataspace.Common.Services;
using Dataspace.Common.Statistics;

namespace Dataspace.Common.ClassesForImplementation.Security
{

    /// <summary>
    /// Провайдер безопасности.
    /// </summary>
    public abstract class SecurityProvider:IDisposable 
    {
#pragma warning disable 0649

        [Import]
        private IStatChannel _channel;

        internal IStatChannel IntChannel { get { return _channel; } }

        [Import]
        protected ITypedPool Cachier { get; private set; }

        [Import]
        protected internal ISecurityManager Manager { get; private set; }

        [Import]
        private IAnnouncerSubscriptorInt _subscriptor;

        [Import]
        private StatisticsCollector  _statisticsCollector; 

        private ICachierStorage<string> _tokenProvider;

        [Import]
        private SecurityContextFactory _codeProvider;

#pragma warning restore 0649

        private string _resName;

        private readonly ConcurrentDictionary<Guid,Accumulator<Guid,SecurityToken>> _accumulators
            = new ConcurrentDictionary<Guid, Accumulator<Guid, SecurityToken>>();

        internal void SetDataCache(string typeName)
        {
            Debug.Assert(_channel != null,"Не установлены импорты - зарегистрируйте CompositionContainer для экспорта MEF или используйте собственный контейнер");
#if DEBUG
            if (_channel == null)
                Debugger.Break();
#endif
            _tokenProvider = new CurrentCachierStorage(_channel,_statisticsCollector);                  
            _channel.Register(typeName);
            _resName = typeName;
        }

        protected static string ConvertToString(Guid id, Guid code)
        {
            
            unsafe
            {
              var ar = new byte[32];
              fixed (byte* ptr = ar)             
              {
                  var ptr2 = (ulong*) ptr;
                  var uid = (ulong*) &id;
                  var ucode = (ulong*) &code;
                  ptr2[0] = uid[0];
                  ptr2[1] = uid[1];
                  ptr2[2] = ucode[0];
                  ptr2[3] = ucode[1];
              }
              return BitConverter.ToString(ar);
            }                   
        }

        internal SecurityToken GetToken(Guid id)
        {
            Contract.Ensures(Contract.Result<SecurityToken>() != null);
            var context = _codeProvider.GetContext();
            Debug.Assert(context != null);
            var key = ConvertToString(id, context.SessionCode);
            var token = _tokenProvider.RetrieveByFunc(key, t => GetTokenFor(id, context)) as SecurityToken;        
            return token;
        }

        protected SecurityToken StraightTake(Guid id)
        {
            Contract.Ensures(Contract.Result<SecurityToken>() != null);
            var context = _codeProvider.GetContext();
            Debug.Assert(context != null);          
            var token =  GetTokenFor(id, context);
            return token;
        }

        internal Lazy<SecurityToken> GetTokenDeferred(Guid id)
        {
            Contract.Ensures(Contract.Result<Lazy<SecurityToken>>() != null);
            var context = _codeProvider.GetContext();         
            var acc = _accumulators.GetOrAdd(context.SessionCode,
                                            k => ReturnAccumulator(_tokenProvider, context));
            var lazyToken = acc.GetValue(id);
            var value = new Lazy<SecurityToken>(lazyToken);
                                   
            return value;
        }

        internal SecurityToken GetTokenFor(Guid id, ISecurityContext context)
        {
            Contract.Requires(context != null);
            Contract.Ensures(Contract.Result<SecurityToken>() != null);
            var t = Stopwatch.StartNew();
            var token = GetTokenForInt(id,context);
            t.Stop();
            _channel.SendMessageAboutOneResource(id,Actions.ExternalGet,t.Elapsed);
            return token;
        }

        internal void SetSecurityUnactual(Guid id)
        {
            var key = ConvertToString(id, Guid.Empty);            
            _tokenProvider.SetUpdateNecessity(key);
            _subscriptor.AnnonunceSecurityUnactuality(_resName, id);
        }

        internal void SetSecurityUnactual()
        {                        
            _tokenProvider.SetUpdateNecessity(null);
            _subscriptor.AnnonunceSecurityUnactuality(_resName, null);
        }

        internal abstract SecurityToken GetTokenForInt(Guid id, ISecurityContext context);

        internal abstract Accumulator<Guid, SecurityToken> ReturnAccumulator(ICachierStorage<string> storage, ISecurityContext context);

        protected SecurityToken CreateToken(bool canRead,bool canWrite)
        {
            return new SecurityToken(canRead,canWrite);
        }

        public void Dispose()
        {
            
        }
    }

    [ContractClass(typeof(SecurityProviderContracts))]
    public abstract class SecurityProvider<TType,TSecContext>:SecurityProvider where TSecContext:class,ISecurityContext
    {

       

        internal sealed override SecurityToken GetTokenForInt(Guid id, ISecurityContext context)
        {
            Contract.Requires(context != null);
            Contract.Ensures(Contract.Result<SecurityToken>() != null);
            return GetTokenFor(id, (TSecContext)context);
        }


        internal sealed override Accumulator<Guid, SecurityToken> ReturnAccumulator(ICachierStorage<string> storage, ISecurityContext context)
        {
            Contract.Requires(context != null);
            Contract.Requires(storage != null);
            Contract.Ensures(Contract.Result<Accumulator<Guid, SecurityToken>>() != null);
            return new Accumulator<Guid, SecurityToken>(
                (id,value) =>  storage.Push(ConvertToString(id, context.SessionCode), value),
                (id)=>storage.HasActualValue(ConvertToString(id, context.SessionCode)),
                (id)=>storage.RetrieveByFunc(ConvertToString(id, context.SessionCode),ss => GetToken(id)) as SecurityToken,                
                ids =>
                              {                              
                                  var t = Stopwatch.StartNew();                                 
                                  var tokens = GetTokensFor(ids, (TSecContext) context);
                                  t.Stop();
                                  IntChannel.SendMessageAboutMultipleResources(ids.ToArray(), Actions.ExternalGet, t.Elapsed);
                                  return tokens;
                              });
        } 


        protected abstract IEnumerable<KeyValuePair<Guid, SecurityToken>> GetTokensFor(IEnumerable<Guid> ids,
                                                                                         TSecContext context);
            
        protected abstract SecurityToken GetTokenFor(Guid id, TSecContext context);
            
    }

   
  

    [ContractClassFor(typeof(SecurityProvider<,>))]
    public class SecurityProviderContracts : SecurityProvider<object, ISecurityContext>
    {


         protected override IEnumerable<KeyValuePair<Guid, SecurityToken>> GetTokensFor(IEnumerable<Guid> ids, ISecurityContext context)
         {
             Contract.Requires(context != null);
             Contract.Requires(ids != null);
             Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<Guid, SecurityToken>>>() != null);
             Contract.Ensures(ids.Count() == Contract.Result<IEnumerable<KeyValuePair<Guid, SecurityToken>>>().Count());
             Contract.Ensures(ids.All(k => Contract.Result<IEnumerable<KeyValuePair<Guid, SecurityToken>>>().Any(k2 => k2.Key == k)));
             return null;
         }

         protected override SecurityToken GetTokenFor(Guid id, ISecurityContext context)
         {
             Contract.Requires(context != null);
             Contract.Ensures(Contract.Result<SecurityToken>() != null);
             return null;
         }
    }
}
