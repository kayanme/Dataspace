﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Dataspace.Common.ClassesForImplementation;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Interfaces.Internal;
using Dataspace.Common.Interfaces.Internal;

namespace Dataspace.Common.Announcements
{

    [Export(typeof(IAnnouncerSubscriptor))]
    [Export(typeof(IAnnouncerSubscriptorInt))]
    [Export(typeof(IInitialize))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AnnouncerSubscriptor : IAnnouncerSubscriptorInt,IInitialize
    {
#pragma warning disable 0649
        [ImportMany]
        private IEnumerable<AnnouncerUplink> _uplinks;

        [ImportMany]
        private IEnumerable<AnnouncerDownlink> _downlinks;

        [Import]
        private IInterchangeCachier _cachier;

        [Import]
        private IGenericPool _genericPool;
#pragma warning restore 0649


        private readonly Dictionary<string, SubscriptionToken> _issuedTokens =
            new Dictionary<string, SubscriptionToken>();

        private readonly Dictionary<string, List<Guid>> _subscriptions = new Dictionary<string, List<Guid>>();

        private readonly List<string> _totalSubscriptions = new List<string>();

        private List<IDisposable> _subscriptionTokens;

        private SubscriptionToken SubscribeForResourceChangeCommon(Type t, SubscriptionToken tokenToAppend,
                                                                   bool forPropagation)
        {
            _cachier.MarkSubscriptionForResource(t);
            SubscriptionToken token;
            string name = _genericPool.GetNameByType(t);
            lock (_issuedTokens)
            {
                if (_issuedTokens.ContainsKey(name))
                {
                    token = _issuedTokens[name];
                    if (!forPropagation)
                        token.SubscriptionCounter++;
                    else
                    {
                        token.PropagationSubscriptionCounter++;
                    }
                }
                else
                {
                    token = new SubscriptionToken(this) { ResourceType = t, ResourceName = name };
                    if (!forPropagation)
                        token.SubscriptionCounter = 1;
                    else
                    {
                        token.PropagationSubscriptionCounter = 1;
                    }
                    _issuedTokens.Add(name, token);
                }

                if (tokenToAppend != null)
                {
                    if (tokenToAppend is CombinedSubscriptionToken)
                    {
                        (tokenToAppend as CombinedSubscriptionToken).AddToken(token);
                        token = tokenToAppend;
                    }
                    else
                    {
                        var combinedToken = new CombinedSubscriptionToken(this);
                        combinedToken.AddToken(tokenToAppend);
                        combinedToken.AddToken(token);
                        token = combinedToken;
                    }
                }
                if (forPropagation)
                {
                    _totalSubscriptions.Add(name);
                }
            }
            return token;
        }

        private void UnmarkSimpleToken(SubscriptionToken token)
        {
            token.SubscriptionCounter--;
            if (token.SubscriptionCounter == 0 && token.PropagationSubscriptionCounter == 0)
            {
                _cachier.UnmarkSubscriptionForResource(token.ResourceType);
                _issuedTokens.Remove(token.ResourceName);
            }
        }

        private void UnmarkSimpleTokenForPropagation(SubscriptionToken token)
        {
            _cachier.UnmarkSubscriptionForResource(token.ResourceType);
            token.PropagationSubscriptionCounter--;
            if (token.PropagationSubscriptionCounter == 0)
            {
                _totalSubscriptions.Remove(token.ResourceName);
                if (token.SubscriptionCounter == 0 && token.PropagationSubscriptionCounter == 0)
                {
                    _cachier.UnmarkSubscriptionForResource(token.ResourceType);
                    _issuedTokens.Remove(token.ResourceName);
                }
            }
        }

        public void AddResourceName(string name)
        {
            _subscriptions.Add(name, new List<Guid>());
        }

        public void Clear()
        {
            lock (_issuedTokens)
            {
                foreach (SubscriptionToken token in _issuedTokens.Values.ToArray())
                    UnsubscribeForResourceChange(token);
            }
            _subscriptions.Clear();
        }

        /// <summary>
        ///     Подписка на обновление ресурса заданного типа.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <returns>
        ///     Маркер подписки
        /// </returns>
        public SubscriptionToken SubscribeForResourceChange<T>()
        {
            return SubscribeForResourceChangeCommon(typeof(T), null, false);
        }

        /// <summary>
        ///     Подписка на обновление ресурса заданного типа.
        /// </summary>
        /// <typeparam name="T">Тип ресурса</typeparam>
        /// <returns>
        ///     Маркер подписки
        /// </returns>
        public SubscriptionToken SubscribeForResourceChangePropagation<T>()
        {
            return SubscribeForResourceChangeCommon(typeof(T), null, true);
        }


        /// <summary>
        ///     Подписка на обновление ресурса заданного типа.
        /// </summary>
        /// <param name="token">Маркер обновления.</param>
        public void UnsubscribeForResourceChangePropagation(SubscriptionToken token)
        {
            lock (_issuedTokens)
            {
                if (token is CombinedSubscriptionToken)
                {
                    var stoken = (token as CombinedSubscriptionToken);
                    foreach (
                        SubscriptionToken subscriptionToken in
                            stoken.Tokens.Where(k => k.PropagationSubscriptionCounter > 0))
                    {
                        UnmarkSimpleTokenForPropagation(subscriptionToken);
                    }
                    stoken.ClearTokens();
                }
                else
                {
                    UnmarkSimpleTokenForPropagation(token);
                }
            }
        }

        /// <summary>
        ///     Merges the with subscription for resource change.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        internal SubscriptionToken MergeWithSubscriptionForResourceChange<T>(SubscriptionToken token)
        {
            return SubscribeForResourceChangeCommon(typeof(T), token, false);
        }


        public void SubscribeForResourceChangePropagation(string resourceName, Guid id)
        {
            _subscriptions[resourceName].Add(id);
        }


        /// <summary>
        ///     Отписка от обновления ресурса.
        /// </summary>
        /// <param name="resourceName">Название ресурса.</param>
        /// <param name="id">Ключ ресурса.</param>
        /// <remarks>
        ///     Применяется для запрета публикации события об изменении конкретного ресурса в связи "вверх-вниз".
        ///     Автоматически применяется при апдейте ресурса.
        ///     <see cref="AnnouncerUplink" />Очередь событий
        /// </remarks>
        public void UnsubscribeForResourceChangePropagation(string resourceName, Guid id)
        {
            _subscriptions[resourceName].Remove(id);
        }

        /// <summary>
        ///     Отписывается от обновления ресурса.
        /// </summary>
        /// <param name="token">Маркер обновления.</param>
        public void UnsubscribeForResourceChange(SubscriptionToken token)
        {
            lock (_issuedTokens)
            {
                if (token is CombinedSubscriptionToken)
                {
                    var stoken = (token as CombinedSubscriptionToken);
                    foreach (SubscriptionToken subscriptionToken in stoken.Tokens.Where(k => k.SubscriptionCounter > 0))
                    {
                        UnmarkSimpleToken(subscriptionToken);
                    }
                    stoken.ClearTokens();
                }
                else
                {
                    UnmarkSimpleToken(token);
                }
            }
        }

        /// <summary>
        ///     Публикует изменение актуальности ресурса.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="id">The id.</param>
        public void AnnonunceUnactuality(string resourceName, Guid id)
        {
            var res = new UnactualResourceContent { ResourceKey = id, ResourceName = resourceName };
            if (_totalSubscriptions.Contains(resourceName) || _subscriptions[resourceName].Contains(id))
                foreach (AnnouncerUplink uplink in _uplinks)
                {
                    uplink.OnNext(res);
                }

            if (_issuedTokens.ContainsKey(resourceName))
                _issuedTokens[resourceName].InvokeUnactuality(res);
        }

        /// <summary>
        ///     Публикует изменение актуальности токена безопасности.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="id">The id.</param>
        public void AnnonunceSecurityUnactuality(string resourceName, Guid? id)
        {
            SecurityUpdate res;
            if (id.HasValue)
            {
                res = new SecurityUpdate
                {
                    ResourceKey = id.Value,
                    ResourceName = resourceName
                };
            }
            else
            {
                res = new SecurityUpdate
                {
                    UpdateAll = true,
                    ResourceName = resourceName
                };
            }

            foreach (AnnouncerUplink uplink in _uplinks)
            {
                uplink.OnNext(res);
            }

            if (_issuedTokens.ContainsKey(resourceName))
                _issuedTokens[resourceName].InvokeUnactuality(res);
        }


        public int Order
        {
            get { return 9; }
        }

        public void Initialize()
        {
            _subscriptionTokens = _downlinks.Select(link => link.Subscribe(_cachier)).ToList();
        }

        private bool _wasDisposed;

        public void Dispose()
        {
            if (!_wasDisposed)
            {
                _subscriptionTokens.ForEach(k => k.Dispose());
                _downlinks.ToList().ForEach(k => k.Dispose());
                _subscriptionTokens = null;
                _wasDisposed = true;
            }
        }
    }
}
