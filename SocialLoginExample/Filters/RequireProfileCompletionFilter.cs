using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Okta.Sdk;

namespace SocialLoginExample.Filters
{
    public class RequireProfileCompletionFilter : IAsyncAuthorizationFilter
    {
        private readonly IOktaClient _oktaClient;
        private readonly string[] _safeUrls;

        public RequireProfileCompletionFilter(IOktaClient oktaClient)
        {
            _oktaClient = oktaClient;

            _safeUrls = new[]
            {
                "/Account/Logout",
                "/Account/CompleteProfile"
            };
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                return;
            }

            if (_safeUrls.Contains(context.HttpContext.Request.Path.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            // Make sure the user's profile is complete
            // (users who sign up through a social provider will need to fill in their rewards number)

            // The actual check is delegated to ProfileCompletionPolicy so it can also be
            // called elsewhere (see comments in ProfileCompletionPolicy)

            var profilePolicy = new ProfileCompletionPolicy(_oktaClient);
            var isComplete = await profilePolicy.IsCompleteAsync(context.HttpContext.User);

            if (!isComplete)
            {
                context.Result = new RedirectToActionResult("CompleteProfile", "Account", null);
            }
        }
    }
}
