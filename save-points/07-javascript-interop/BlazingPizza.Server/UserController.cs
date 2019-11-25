using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Extensions;
using System.Net.Http;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace BlazingPizza.Server
{
    [ApiController]
    public class UserController : Controller
    {
        private static UserInfo LoggedOutUser = new UserInfo { IsAuthenticated = false };

        [HttpGet("user")]
        public async Task<UserInfo> GetUser()
        {
            try {
                Console.WriteLine("********************************************************");
                var auth = await HttpContext.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);
                foreach(var token in auth.Properties.GetTokens())
                {
                    Console.WriteLine("****************************");
                    Console.WriteLine(token.Name);
                    Console.WriteLine(token.Value);
                }
                Console.WriteLine("********************************************************");
            }
            catch {
                Console.WriteLine("*****************************");
                Console.WriteLine("*****************************                HELP                *****************************");
                Console.WriteLine("*****************************");
            }
            return User.Identity.IsAuthenticated
                ? new UserInfo { Name = User.Identity.Name, IsAuthenticated = true}
                : LoggedOutUser;
        }

        [HttpGet("user/refresh")]
        public async Task<UserInfo> RefreshUserToken()
        {
            Console.WriteLine("TEST REFRESH");
            try {
                var auth = await HttpContext.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);
                var user = auth.Principal;
                var authProperties = auth.Properties;

                var currentAuthType = user.Identity.AuthenticationType;
                var refreshToken = authProperties.GetTokenValue("refresh_token");
                var options = await Task.FromResult<OAuthOptions>(HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<MicrosoftAccountOptions>>().Get(currentAuthType));
                var accessToken = authProperties.GetTokenValue("access_token");

                var pairs = new Dictionary<string, string>()
                {
                    { "client_id", options.ClientId },
                    { "client_secret", options.ClientSecret },
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken }
                };

                var content = new FormUrlEncodedContent(pairs);

                var refreshResponse = await options.Backchannel.PostAsync(options.TokenEndpoint, content, HttpContext.RequestAborted);

                Console.WriteLine(refreshResponse.StatusCode);

                refreshResponse.EnsureSuccessStatusCode();

                using (var payload = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync()))
                {

                    // Persist the new acess token
                    authProperties.UpdateTokenValue("access_token", payload.RootElement.GetString("access_token"));
                    refreshToken = payload.RootElement.GetString("refresh_token");
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        authProperties.UpdateTokenValue("refresh_token", refreshToken);
                    }
                    if (payload.RootElement.TryGetProperty("expires_in", out var property) && property.TryGetInt32(out var seconds))
                    {
                        var expiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(seconds);
                        authProperties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));
                    }
                    await HttpContext.SignInAsync(user, authProperties);
                }
            }
            catch(Exception e) {
                Console.WriteLine("Could not refresh");
                Console.WriteLine(e);
            }
            return User.Identity.IsAuthenticated
                ? new UserInfo { Name = User.Identity.Name, IsAuthenticated = true}
                : LoggedOutUser;
        }

        [HttpGet("user/signin")]
        public async Task SignIn(string redirectUri)
        {
            if (string.IsNullOrEmpty(redirectUri) || !Url.IsLocalUrl(redirectUri))
            {
                redirectUri = "/";
            }

            await HttpContext.ChallengeAsync(
                MicrosoftAccountDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = redirectUri  });
            
            
        }

        [HttpGet("user/signout")]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("~/");
        }
    }
}
