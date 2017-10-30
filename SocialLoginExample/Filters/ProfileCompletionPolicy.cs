using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Okta.Sdk;

namespace SocialLoginExample.Filters
{
    public class ProfileCompletionPolicy
    {
        private readonly IOktaClient _oktaClient;

        public ProfileCompletionPolicy(IOktaClient oktaClient)
        {
            _oktaClient = oktaClient;
        }

        public async Task<bool> IsCompleteAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await RewardsNumberExists(user, cancellationToken);
            // ... and anything else we'd need to check to determine if
            // the user's profile is complete
        }

        private async Task<bool> RewardsNumberExists(ClaimsPrincipal user, CancellationToken cancellationToken = default(CancellationToken))
        {
            // This check is relying on the fact that:
            //  1) The `default` authorization server in Okta is configured to return a
            //  `rewardsNumber` claim in the /userinfo response
            //  2) The OIDC middleware (Startup.cs) is configured to copy that claim into the
            //  ClaimsPrincipal when the user logs in
            var rewardsNumber = user.Claims.FirstOrDefault(c => c.Type == "rewardsNumber")?.Value;
            if (!string.IsNullOrEmpty(rewardsNumber))
            {
                return true;
            }

            // If the user completes their profile, this attribute will be populated on their Okta profile
            // but not copied into the ClaimsPrincipal until the next time they log in.
            // So we need to check the Okta user profile too, if it's not in the claims:
            var oktaUserId = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(oktaUserId))
            {
                // Need to display a user-friendly error view
                throw new NotImplementedException("Todo: error view. Could not look up Okta user (this shouldn't happen)");
            }

            var oktaUser = await _oktaClient.Users.GetUserAsync(oktaUserId, cancellationToken);
            var rewardsNumberOnProfile = oktaUser.Profile.GetProperty<string>("rewardsNumber");

            return !string.IsNullOrEmpty(rewardsNumberOnProfile);
        }
    }
}
