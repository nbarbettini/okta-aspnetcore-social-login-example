using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Okta.Sdk;

namespace SocialLoginExample.SdkResource
{
    // The Okta .NET SDK (as of version 1.0.0-alpha3) doesn't support
    // the IdP API, but you can easily define a class that represents the
    // properties you need and then use it with the SDK.

    // Identity Provider model:
    // https://developer.okta.com/docs/api/resources/idps.html#identity-provider-model
    public class IdentityProvider : Resource
    {
        public string Id => GetStringProperty("id");

        public string Type => GetStringProperty("type");

        public string Name => GetStringProperty("name");

        public string Status => GetStringProperty("status");

        public DateTimeOffset? Created => GetDateTimeProperty("created");
    }
}
