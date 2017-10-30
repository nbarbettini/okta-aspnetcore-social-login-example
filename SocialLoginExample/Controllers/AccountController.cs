using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using SocialLoginExample.Models;
using SocialLoginExample.SdkResource;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Okta.Sdk;

namespace SocialLoginExample.Controllers
{
    public class AccountController : Controller
    {
        private readonly IOktaClient _oktaClient;

        public AccountController(IOktaClient oktaClient)
        {
            _oktaClient = oktaClient;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Challenge(
                    new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") },
                    OpenIdConnectDefaults.AuthenticationScheme);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult LoginWithIdp(string idp)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            };

            properties.Items.Add("idp", idp);

            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Link(string idp)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("LinkChallenge", "Account", new
                {
                    idp,
                    login_hint = email
                })
            };

            // Sign out and redirect to /Account/LinkChallenge
            return SignOut(properties, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult LinkChallenge(string idp, string login_hint)
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                // Shouldn't happen! Panic and go home.
                return RedirectToAction("Index", "Home");
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            };

            properties.Items.Add("idp", idp);
            properties.Items.Add("login_hint", login_hint);

            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                return SignOut(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["ReturnUrl"] = Url.Action();

            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewData["ReturnUrl"] = Url.Action();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var profile = new UserProfile
            {
                FirstName = model.GivenName,
                LastName = model.Surname,
                Email = model.Email,
                Login = model.Email,
            };
            profile["rewardsNumber"] = model.RewardsNumber;

            try
            {
                var user = await _oktaClient.Users.CreateUserAsync(new CreateUserWithPasswordOptions
                {
                    Activate = true,
                    Password = model.Password,
                    Profile = profile,
                    RecoveryQuestion = "ResetKey",
                    RecoveryAnswer = Guid.NewGuid().ToString()
                });
            }
            catch (OktaApiException oaex)
            {
                // Redisplay form with error message
                model.Error = oaex.ErrorSummary;
                return View(model);
            }

            // Success
            return RedirectToAction("RegisterThanks");
        }

        [HttpGet]
        public IActionResult RegisterThanks() => View();

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CompleteProfile()
        {
            // Sanity check: is their profile already complete?
            var profilePolicy = new Filters.ProfileCompletionPolicy(_oktaClient);
            if (await profilePolicy.IsCompleteAsync(User))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = Url.Action();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model)
        {
            ViewData["ReturnUrl"] = Url.Action();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var oktaUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(oktaUserId))
            {
                // Need to display a user-friendly error view
                throw new NotImplementedException("Todo: error view. Could not look up Okta user (this shouldn't happen)");
            }

            try
            {
                var user = await _oktaClient.Users.GetUserAsync(oktaUserId);
                user.Profile["rewardsNumber"] = model.RewardsNumber;
                await user.UpdateAsync();
            }
            catch (OktaApiException oaex)
            {
                // Redisplay form with error message
                model.Error = oaex.ErrorSummary;
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Manage()
        {
            var oktaUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(oktaUserId))
            {
                // Need to display a user-friendly error view
                throw new NotImplementedException("Todo: error view. Could not look up Okta user (this shouldn't happen)");
            }

            var viewModel = new ManageAccountViewModel();

            try
            {
                var oktaUser = await _oktaClient.Users.GetUserAsync(oktaUserId);

                viewModel.RewardsNumber = oktaUser.Profile.GetProperty<string>("rewardsNumber");

                // todo remove
                var extendedClient = (_oktaClient as ExtendedOktaClient);

                var idps = await extendedClient
                    .GetCollection<IdentityProvider>($"/api/v1/users/{oktaUser.Id}/idps")
                    .ToArray();

                viewModel.FacebookLinked = idps.Any(x => x.Type.Equals("facebook", StringComparison.OrdinalIgnoreCase));

                var isGoogleLinked = idps.Any(x => x.Type.Equals("google", StringComparison.OrdinalIgnoreCase));

                return View(viewModel);
            }
            catch (OktaApiException)
            {
                // TODO: Need to display a user-friendly error view
                throw new NotImplementedException("Unable to look up the Okta user");
            }
        }
    }
}
