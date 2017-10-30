using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Okta.Sdk;
using Okta.Sdk.Configuration;

namespace SocialLoginExample
{
    // Add a GetCollection method to OktaClient
    // (after SDK 1.0.0-alpha4 this won't be necessary)
    public class ExtendedOktaClient : OktaClient
    {
        public ExtendedOktaClient(OktaClientConfiguration apiClientConfiguration = null, ILogger logger = null)
            : base(apiClientConfiguration, logger)
        {
        }

        public IAsyncEnumerable<T> GetCollection<T>(string uri, IEnumerable<KeyValuePair<string, object>> queryParameters = null)
            where T : Resource, new()
            => GetCollectionClient<T>(new Okta.Sdk.Internal.HttpRequest
            {
                Uri = uri,
                QueryParameters = queryParameters ?? Enumerable.Empty<KeyValuePair<string, object>>()
            });
    }
}
