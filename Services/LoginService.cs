using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Tracker.Services
{
    public interface ILoginService
    {
        public bool IsAuthenticated { get; }

        public Task Login(string username, string role);
    }

    public class LoginService : ILoginService
    {
        private IHttpContextAccessor _contextAccessor;

        public LoginService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public bool IsAuthenticated => _contextAccessor.HttpContext!.User.Identity!.IsAuthenticated;

        public async Task Login(string username, string role)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "LoginData");
            await _contextAccessor.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
        }
    }
}
