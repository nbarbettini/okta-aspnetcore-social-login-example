using System.Security.Claims;
using System.Threading.Tasks;
using SocialLoginExample.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Okta.Sdk;
using Okta.Sdk.Configuration;

namespace SocialLoginExample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                // Configuration pulled from appsettings.json by default:
                options.ClientId = Configuration["okta:ClientId"];
                options.ClientSecret = Configuration["okta:ClientSecret"];
                options.Authority = Configuration["okta:Authority"];
                options.CallbackPath = "/authorization-code/callback";
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.UseTokenLifetime = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name"
                };
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = ApplyCustomParameters,
                    OnUserInformationReceived = CopyCustomClaims
                };
            });

            // Create an instance of the Okta SDK client and make it available to inject
            services.AddSingleton<IOktaClient>(new ExtendedOktaClient(new OktaClientConfiguration
            {
                OrgUrl = Configuration["okta:Org"],
                Token = Configuration["okta:ApiToken"]
            }));

            services.AddMvc(options =>
            {
                options.Filters.Add<RequireProfileCompletionFilter>();
            });
        }

        // When we want to log in with a specific provider, we have
        // to pass some additional parameters to the /authorize endpoint.
        private Task ApplyCustomParameters(RedirectContext context)
        {
            if (context.Properties.Items.TryGetValue("idp", out var idpId))
            {
                context.ProtocolMessage.SetParameter("idp", idpId);
            }

            if (context.Properties.Items.TryGetValue("login_hint", out var loginHint))
            {
                context.ProtocolMessage.LoginHint = loginHint;
            }

            return Task.CompletedTask;
        }

        // Custom claims in the ID token or userinfo endpoint aren't automatically
        // copied into the ClaimsPrincipal, so it must be done manually here.
        // (This method is used by both OpenIdConnectMiddleware definitions in Events.OnUserInformationReceived)
        private Task CopyCustomClaims(UserInformationReceivedContext context)
        {
            if (context.User.TryGetValue("rewardsNumber", out var rewardsNumber))
            {
                var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;
                claimsIdentity.AddClaim(new Claim("rewardsNumber", rewardsNumber?.ToString()));
            }
            return Task.CompletedTask;
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
