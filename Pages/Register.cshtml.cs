using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Tracker.Models;
using Tracker.Models.Hideout;
using Tracker.Services;

namespace Tracker.Pages
{
    [BindProperties]
    public class RegisterModel : PageModel
    {
        private ApplicationDbContext _db;
        private ILoginService _loginService;

        public RegisterModel(ApplicationDbContext dbContext, ILoginService loginService)
        {
            _db = dbContext;
            _loginService = loginService;
        }

        [Required(ErrorMessage = "Field is required")]
        [RegularExpression("[A-Za-z0-9._-]+$", ErrorMessage = "Must contain letters, digits, dots or underscores")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Field is required")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Wrong email")]
        public string? UserEmail { get; set; }

        [Required(ErrorMessage = "Field is required")]
        [RegularExpression("[A-Za-z0-9]+$", ErrorMessage = "Must contain letters or digits")]
        [StringLength(16, MinimumLength = 8, ErrorMessage = "Must be a string with a minimum length of 8 and a maximum length of 16")]
        public string? UserPassword { get; set; }

        [Required(ErrorMessage = "Field is required")]
        [Compare(nameof(UserPassword), ErrorMessage = "Passwords don't match")]
        public string? PasswordConformation { get; set; }

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
                if (await CheckEmail() & await CheckName())
                {
                    User newUser = await AddNewUser();
                    await _loginService.Login(newUser.Name, newUser.Role);
                    string? returnUrl = Request.Query["ReturnUrl"];

                    if (returnUrl != null)
                        return RedirectPermanent(returnUrl);
                    else
                        return RedirectToPagePermanent("Index");
                }
            }

            return Page();
        }

        private async Task<bool> CheckEmail()
        {
            User? user = await _db.Users.FirstOrDefaultAsync(user => user.Email == UserEmail);

            if (user == null)
                return true;

            ModelState.AddModelError(nameof(UserEmail), "Email is already used");
            return false;
        }

        private async Task<bool> CheckName()
        {
            User? user = await _db.Users.FirstOrDefaultAsync(user => user.Name == UserName);

            if (user == null)
                return true;

            ModelState.AddModelError(nameof(UserName), "Name is already used");
            return false;
        }

        private async Task<User> AddNewUser()
        {
            User newUser = new User(UserName!, UserEmail!.ToLower(), UserPassword!);
            List<Station> stations = await _db.Stations.ToListAsync();
            newUser.UserInfo!.Stations = stations;
            await _db.Users.AddAsync(newUser);
            await _db.SaveChangesAsync();
            return newUser;
        }
    }
}
