using System.Collections.Generic;

namespace Dataspace.Common.Announcements
{
    internal sealed class CombinedSubscriptionToken:SubscriptionToken
    {

        private List<SubscriptionToken>  _tokens = new List<SubscriptionToken>();

        internal CombinedSubscriptionToken(AnnouncerSubscriptor subscriptor) : base(subscriptor)
        {
        }

        public IEnumerable<SubscriptionToken>  Tokens {get { return _tokens; }}

        internal void AddToken(SubscriptionToken token)
        {
            _tokens.Add(token);
            token.ResourceMarkedAsUnactual += TokenOnResourceMarkedAsUnactual;
        }

        private void TokenOnResourceMarkedAsUnactual(object sender, ResourceUnactualEventArgs eventArgs)
        {
            InvokeUnactuality(eventArgs.Resource);
        }

      

        internal void ClearTokens()
        {
            foreach (var subscriptionToken in _tokens)
            {
                subscriptionToken.ResourceMarkedAsUnactual -= TokenOnResourceMarkedAsUnactual;
            }
            _tokens.Clear();
        }
    }
}
