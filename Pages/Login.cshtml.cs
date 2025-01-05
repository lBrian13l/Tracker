using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Tracker.Models;
using Tracker.Models.Users;
using Tracker.Services;

namespace Tracker.Pages
{
    public class LoginModel : PageModel
    {
        private ApplicationDbContext _db;
        private ILoginService _loginService;
        private Regex _emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        public LoginModel(ApplicationDbContext dbContext, ILoginService loginService)
        {
            _db = dbContext;
            _loginService = loginService;
        }

        public string? WrongMessage { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Enter login")]
        public string? LoginData { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Enter password")]
        public string? Password { get; set; }

        public IActionResult OnGet()
        {
            if (_loginService.IsAuthenticated)
                return RedirectToPagePermanent("Index");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                User? user = null;

                if (_emailRegex.IsMatch(LoginData!))
                    user = await _db.Users.FirstOrDefaultAsync(user => user.Email == LoginData);
                else
                    user = await _db.Users.FirstOrDefaultAsync(u => u.Name == LoginData);

                if (user == null)
                {
                    WrongMessage = "Wrong login or password";
                    return Page();
                }

                await _loginService.Login(user.Name, user.Role);
                string? returnUrl = Request.Query["ReturnUrl"];

                if (returnUrl != null)
                    return RedirectPermanent(returnUrl);
                else
                    return RedirectToPagePermanent("Index");
            }

            return Page();
        }
    }
}
